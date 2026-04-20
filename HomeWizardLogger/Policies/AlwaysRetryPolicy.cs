using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace HomeWizardLogger.Policies;

public sealed class AlwaysRetryPolicy(ILogger logger) : IRetryPolicy
{
    private static readonly int[] DelaysInSeconds = [0, 2, 10, 30, 60, 120, 180];

    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        var retry = retryContext.PreviousRetryCount;
        var delaySeconds = retry < DelaysInSeconds.Length
            ? DelaysInSeconds[retry]
            : 180;
        
        if (retry == 0)
        {
            logger.LogWarning(retryContext.RetryReason, "Connection lost, attempting to reconnect...");
        }

        logger.LogInformation("Reconnection attempt {RetryCount} in {Delay}s", retry, delaySeconds);
        return TimeSpan.FromSeconds(delaySeconds);
    }
}
