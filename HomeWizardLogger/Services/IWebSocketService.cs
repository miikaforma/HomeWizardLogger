namespace HomeWizardLogger.Services;

public interface IWebSocketService
{
    Task StartListening(CancellationToken cancellationToken = default);
}