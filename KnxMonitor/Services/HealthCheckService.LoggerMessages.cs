using Microsoft.Extensions.Logging;

namespace KnxMonitor.Services;

/// <summary>
/// High-performance LoggerMessage definitions for HealthCheckService.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class HealthCheckService
{
    // Service Lifecycle Operations (9801-9804)
    [LoggerMessage(9801, LogLevel.Information, "Health check service started on port {Port}")]
    private partial void LogHealthCheckServiceStarted(int port);

    [LoggerMessage(9802, LogLevel.Error, "Failed to start health check service on port {Port}")]
    private partial void LogFailedToStartHealthCheckService(Exception ex, int port);

    [LoggerMessage(9803, LogLevel.Information, "Health check service stopped")]
    private partial void LogHealthCheckServiceStopped();

    [LoggerMessage(9804, LogLevel.Error, "Error stopping health check service")]
    private partial void LogErrorStoppingHealthCheckService(Exception ex);

    // Request Handling Error Operations (9805-9806)
    [LoggerMessage(9805, LogLevel.Error, "Error handling health check request")]
    private partial void LogErrorHandlingHealthCheckRequest(Exception ex);

    [LoggerMessage(9806, LogLevel.Error, "Error processing health check request")]
    private partial void LogErrorProcessingHealthCheckRequest(Exception ex);
}
