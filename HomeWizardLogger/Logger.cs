using HomeWizardLogger.Services;
using Microsoft.Extensions.Logging;

namespace HomeWizardLogger;

public class Logger(ILogger<Logger> logger, IWebSocketService webSocketService)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting listener");
        
        // Start the WebSocket listener
        await webSocketService.StartListening(cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(-1, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error occurred executing {RunAsync}", nameof(RunAsync));
            }
        }
    }
}