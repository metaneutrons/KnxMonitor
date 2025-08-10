using Microsoft.Extensions.Logging;

namespace KnxMonitor.Services;

/// <summary>
/// High-performance LoggerMessage definitions for DisplayService.
/// Event IDs: 4000-4099 (UI/Display Services)
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class DisplayService
{
    // Display Error Operations (4000-4099)
    [LoggerMessage(4001, LogLevel.Error, "Error updating display")]
    private partial void LogErrorUpdatingDisplay(Exception ex);

    [LoggerMessage(4002, LogLevel.Error, "Error stopping display service")]
    private partial void LogErrorStoppingDisplayService(Exception ex);

    [LoggerMessage(4003, LogLevel.Error, "Error updating visual display")]
    private partial void LogErrorUpdatingVisualDisplay(Exception ex);

    // Connection Status (4010-4019)
    [LoggerMessage(4010, LogLevel.Information, "Connection status: {Status} (Connected: {IsConnected})")]
    private partial void LogConnectionStatusUpdate(string status, bool isConnected);

    [LoggerMessage(4011, LogLevel.Information, "KNX Monitor started - {ConnectionStatus}")]
    private partial void LogKnxMonitorStarted(string connectionStatus);

    // Message Processing (4020-4029)
    [LoggerMessage(4020, LogLevel.Debug, "Processing message: {GroupAddress}")]
    private partial void LogProcessingMessage(string groupAddress);

    [LoggerMessage(4021, LogLevel.Debug, "Message queue size: {QueueSize}")]
    private partial void LogMessageQueueSize(int queueSize);
}
