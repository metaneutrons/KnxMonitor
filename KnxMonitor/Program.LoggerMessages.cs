using Microsoft.Extensions.Logging;

namespace KnxMonitor;

/// <summary>
/// High-performance LoggerMessage definitions for Program class.
/// Event IDs: 1000-1999 (Application/Program level)
/// </summary>
public static partial class Program
{
    // Application Lifecycle (1000-1099)
    [LoggerMessage(1001, LogLevel.Information, "Initiating graceful shutdown")]
    private static partial void LogInitiatingGracefulShutdown(ILogger logger);

    [LoggerMessage(1002, LogLevel.Information, "Shutdown signal received, stopping services")]
    private static partial void LogShutdownSignalReceived(ILogger logger);

    [LoggerMessage(1003, LogLevel.Information, "Display service completed, shutting down")]
    private static partial void LogDisplayServiceCompleted(ILogger logger);

    [LoggerMessage(1004, LogLevel.Information, "Operation cancelled, shutting down gracefully")]
    private static partial void LogOperationCancelled(ILogger logger);

    [LoggerMessage(1005, LogLevel.Information, "KNX Monitor stopped gracefully")]
    private static partial void LogKnxMonitorStoppedGracefully(ILogger logger);

    [LoggerMessage(1006, LogLevel.Error, "Error during cleanup: {ErrorMessage}")]
    private static partial void LogErrorDuringCleanup(ILogger logger, Exception ex, string errorMessage);

    // Service Management (1100-1199)
    [LoggerMessage(1100, LogLevel.Information, "Starting graceful cleanup")]
    private static partial void LogStartingGracefulCleanup(ILogger logger);

    [LoggerMessage(1101, LogLevel.Information, "Health check service started on port {Port}")]
    private static partial void LogHealthCheckServiceStarted(ILogger logger, int port);

    [LoggerMessage(1102, LogLevel.Information, "Health check service stopped")]
    private static partial void LogHealthCheckServiceStopped(ILogger logger);

    [LoggerMessage(1103, LogLevel.Error, "Error stopping health check service: {ErrorMessage}")]
    private static partial void LogErrorStoppingHealthCheckService(ILogger logger, Exception ex, string errorMessage);

    [LoggerMessage(1104, LogLevel.Information, "Display service stopped and disposed")]
    private static partial void LogDisplayServiceStoppedAndDisposed(ILogger logger);

    [LoggerMessage(1105, LogLevel.Error, "Error stopping display service: {ErrorMessage}")]
    private static partial void LogErrorStoppingDisplayService(ILogger logger, Exception ex, string errorMessage);

    [LoggerMessage(1106, LogLevel.Information, "Monitor service stopped and disposed")]
    private static partial void LogMonitorServiceStoppedAndDisposed(ILogger logger);

    [LoggerMessage(1107, LogLevel.Error, "Error stopping monitor service: {ErrorMessage}")]
    private static partial void LogErrorStoppingMonitorService(ILogger logger, Exception ex, string errorMessage);

    [LoggerMessage(1108, LogLevel.Information, "Host stopped and disposed")]
    private static partial void LogHostStoppedAndDisposed(ILogger logger);

    [LoggerMessage(1109, LogLevel.Error, "Error stopping host: {ErrorMessage}")]
    private static partial void LogErrorStoppingHost(ILogger logger, Exception ex, string errorMessage);

    // Connection Management (1200-1299)
    [LoggerMessage(1200, LogLevel.Information, "Starting KNX connection with retry policy")]
    private static partial void LogStartingKnxConnectionWithRetry(ILogger logger);

    [LoggerMessage(1201, LogLevel.Information, "Attempting KNX connection")]
    private static partial void LogAttemptingKnxConnection(ILogger logger);

    [LoggerMessage(1202, LogLevel.Information, "KNX Monitor connection successful")]
    private static partial void LogKnxMonitorConnectionSuccessful(ILogger logger);

    [LoggerMessage(1203, LogLevel.Information, "Connection cancelled by user")]
    private static partial void LogConnectionCancelledByUser(ILogger logger);

    [LoggerMessage(1204, LogLevel.Warning, "Connection attempt {RetryCount} failed: {ErrorMessage}")]
    private static partial void LogConnectionAttemptFailed(ILogger logger, int retryCount, string errorMessage);

    [LoggerMessage(1205, LogLevel.Information, "Retrying in {DelaySeconds} seconds... (attempt {NextAttempt}/4)")]
    private static partial void LogRetryingConnection(ILogger logger, double delaySeconds, int nextAttempt);

    [LoggerMessage(
        1206,
        LogLevel.Information,
        "Hint: Another KNX application may be using the multicast address. Try stopping other KNX tools."
    )]
    private static partial void LogMulticastAddressInUseHint(ILogger logger);

    // Configuration and Validation (1900-1999)
    [LoggerMessage(1900, LogLevel.Error, "Configuration error: {ErrorMessage}. Exiting.")]
    private static partial void LogConfigurationError(ILogger logger, string errorMessage);
}
