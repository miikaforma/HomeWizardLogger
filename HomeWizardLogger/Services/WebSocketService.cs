using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using HomeWizardLogger.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using HomeWizardLogger.Database;
using HomeWizardLogger.Models;
using System.Collections.Concurrent;

namespace HomeWizardLogger.Services;

public class WebSocketService : IWebSocketService
{
    private readonly ILogger<WebSocketService> _logger;
    private readonly HomeWizardSettings _homeWizardSettings;
    private ClientWebSocket? _clientWebSocket;
    private CancellationTokenSource? _cts;
    private readonly X509Certificate2? _caCert;
    private readonly IDatabaseService _databaseService;
    private readonly TimeZoneInfo _measurementTimezone;
    private readonly ConcurrentQueue<Measurement> _failedInserts = new();
    private Task? _retryTask;
    private CancellationTokenSource? _retryCts;
    private bool _hasReportedUniqueId;
    private string? _lastReportedUniqueId;

    public WebSocketService(ILogger<WebSocketService> logger, IOptions<HomeWizardSettings> homeWizardOptions, IDatabaseService databaseService)
    {
        _logger = logger;
        _databaseService = databaseService;
        _homeWizardSettings = homeWizardOptions.Value;
        _measurementTimezone = TimeZoneInfo.FindSystemTimeZoneById(Const.TimezoneId);
        // Load HomeWizard CA certificate (PEM format)
        var caCertPath = Path.Combine(AppContext.BaseDirectory, "Certificates", "homewizard-ca-cert.pem");
        if (File.Exists(caCertPath))
        {
            _caCert = new X509Certificate2(File.ReadAllBytes(caCertPath));
        }
        else
        {
            _logger.LogWarning($"CA certificate not found at {caCertPath}. Certificate validation will fail.");
        }
    }

    public async Task StartListening(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting HomeWizard WebSocket listener (with reconnect)");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _retryCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _retryTask = Task.Run(() => RetryFailedInsertsAsync(_retryCts.Token), cancellationToken);
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                await ConnectAndListen(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("WebSocket listening cancelled.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WebSocket connection error. Will retry in 5 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(5), _cts.Token);
            }
        }
        await _retryCts.CancelAsync();
        if (_retryTask != null) await _retryTask;
    }

    private async Task ConnectAndListen(CancellationToken cancellationToken)
    {
        _clientWebSocket = new ClientWebSocket();
        // Secure certificate validation using HomeWizard CA
        _clientWebSocket.Options.RemoteCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
        {
            if (_caCert == null || cert == null || chain == null)
                return false;
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;
            
            chain.ChainPolicy.ExtraStore.Add(_caCert);
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            var isValid = chain.Build(new X509Certificate2(cert));
            var isRootedInCa = chain.ChainElements
                .Any(x => x.Certificate.Thumbprint == _caCert.Thumbprint);
            return isValid && isRootedInCa;
        };
        var wsUri = new Uri($"wss://{_homeWizardSettings.Address}/api/ws");
        _logger.LogInformation("Connecting to HomeWizard WebSocket...");
        await _clientWebSocket.ConnectAsync(wsUri, cancellationToken);
        _logger.LogInformation("Connected to HomeWizard WebSocket");
        await ReceiveLoop(cancellationToken);
    }

    private async Task ReceiveLoop(CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        while (_clientWebSocket is { State: WebSocketState.Open } && !cancellationToken.IsCancellationRequested)
        {
            var result = await _clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            var receivedAt = DateTimeOffset.UtcNow;
            if (result.MessageType == WebSocketMessageType.Close)
            {
                _logger.LogWarning("WebSocket closed by server");
                break;
            }
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            _logger.LogInformation("Received: {Message}", message);
            await HandleWebSocketMessage(message, receivedAt);
        }
    }

    private async Task RetryFailedInsertsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            while (_failedInserts.TryDequeue(out var measurement))
            {
                var success = await _databaseService.InsertMeasurementAsync(measurement, cancellationToken);
                if (!success)
                {
                    _failedInserts.Enqueue(measurement);
                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                    break;
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
    }

    private async Task HandleWebSocketMessage(string message, DateTimeOffset receivedAt)
    {
        try
        {
            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;
            if (root.TryGetProperty("type", out var typeElem))
            {
                var type = typeElem.GetString();
                switch (type)
                {
                    case "authorization_requested":
                    {
                        _logger.LogInformation("Authorization requested, sending token");
                        var authMsg = new
                        {
                            type = "authorization",
                            data = _homeWizardSettings.Token
                        };
                        var authJson = JsonSerializer.Serialize(authMsg);
                        var authBuffer = Encoding.UTF8.GetBytes(authJson);
                        if (_clientWebSocket != null)
                            await _clientWebSocket.SendAsync(new ArraySegment<byte>(authBuffer), WebSocketMessageType.Text, true, _cts!.Token);
                        _logger.LogInformation("Sent authorization message");
                        break;
                    }
                    case "authorized":
                    {
                        _logger.LogInformation("Authorized, subscribing to measurements");
                        var subMsg = new
                        {
                            type = "subscribe",
                            data = "measurement"
                        };
                        var subJson = JsonSerializer.Serialize(subMsg);
                        var subBuffer = Encoding.UTF8.GetBytes(subJson);
                        if (_clientWebSocket != null)
                            await _clientWebSocket.SendAsync(new ArraySegment<byte>(subBuffer), WebSocketMessageType.Text, true, _cts!.Token);
                        _logger.LogInformation("Sent subscribe message");
                        break;
                    }
                    case "measurement":
                        _logger.LogInformation("Received measurements: {Message}", message);
                        try
                        {
                            // HomeWizard sends the measurement in the 'data' property, not at the root
                            if (root.TryGetProperty("data", out var dataElem))
                            {
                                // Use custom deserialization to handle case-insensitive property names
                                var measurement = JsonSerializer.Deserialize<Measurement>(dataElem.GetRawText(), new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                });
                                if (measurement != null)
                                {
                                    var reportedUniqueId = measurement.UniqueId;
                                    measurement.UniqueId = _homeWizardSettings.UniqueIdOverride ?? reportedUniqueId;
                                    if (!_hasReportedUniqueId)
                                    {
                                        _logger.LogInformation("Measurement ID: reported {ReportedUniqueId}, stored {StoredUniqueId}",
                                            reportedUniqueId, measurement.UniqueId);
                                    }
                                    else if (!string.Equals(_lastReportedUniqueId, reportedUniqueId, StringComparison.Ordinal))
                                    {
                                        _logger.LogWarning("Reported measurement ID changed from {PreviousUniqueId} to {ReportedUniqueId}; storing as {StoredUniqueId}",
                                            _lastReportedUniqueId, reportedUniqueId, measurement.UniqueId);
                                    }
                                    _lastReportedUniqueId = reportedUniqueId;
                                    _hasReportedUniqueId = true;
                                    measurement.ResolveTimestamp(receivedAt, _measurementTimezone);
                                    var success = await _databaseService.InsertMeasurementAsync(measurement, _cts!.Token);
                                    if (!success)
                                    {
                                        _logger.LogWarning("Insert failed, queuing measurement for retry.");
                                        _failedInserts.Enqueue(measurement);
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("Failed to deserialize measurement JSON from 'data' property.");
                                }
                            }
                            else
                            {
                                _logger.LogWarning("No 'data' property found in measurement message.");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error deserializing or inserting measurement");
                        }
                        break;
                    default:
                        _logger.LogInformation("Received message of type {Type}: {Message}", type, message);
                        break;
                }
            }
            else
            {
                _logger.LogInformation("Received message: {Message}", message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling WebSocket message: {Message}", message);
        }
    }
}
