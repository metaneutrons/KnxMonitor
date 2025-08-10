using Microsoft.Extensions.Logging;

namespace KnxMonitor.Services;

/// <summary>
/// High-performance LoggerMessage definitions for KnxMonitorService.
/// Event IDs: 2000-2999 (Core Services)
/// </summary>
public partial class KnxMonitorService
{
    // Service Lifecycle (2000-2099)
    [LoggerMessage(2001, LogLevel.Information, "Starting KNX monitoring with connection type: {ConnectionType}")]
    private partial void LogStartingMonitoring(string connectionType);

    [LoggerMessage(2002, LogLevel.Information, "KNX monitoring started successfully - {ConnectionStatus}")]
    private partial void LogMonitoringStartedSuccessfully(string connectionStatus);

    [LoggerMessage(2003, LogLevel.Information, "KNX monitoring stopped")]
    private partial void LogMonitoringStopped();

    [LoggerMessage(2004, LogLevel.Error, "Failed to start KNX monitoring")]
    private partial void LogFailedToStartMonitoring(Exception ex);

    [LoggerMessage(2005, LogLevel.Error, "Error stopping KNX monitoring")]
    private partial void LogErrorStoppingMonitoring(Exception ex);

    // Connection Type Operations (2100-2199)
    [LoggerMessage(2100, LogLevel.Debug, "Creating IP tunneling connection")]
    private partial void LogCreatingIpTunnelingConnection();

    [LoggerMessage(2101, LogLevel.Debug, "Creating IP routing connection")]
    private partial void LogCreatingIpRoutingConnection();

    [LoggerMessage(2102, LogLevel.Debug, "Creating USB connection")]
    private partial void LogCreatingUsbConnection();

    [LoggerMessage(2103, LogLevel.Error, "Failed to create connector parameters")]
    private partial void LogFailedToCreateConnectorParameters();

    // CSV Database Operations (2200-2299)
    [LoggerMessage(2200, LogLevel.Information, "Loading group address database from: {CsvFilePath}")]
    private partial void LogLoadingGroupAddressDatabase(string csvFilePath);

    [LoggerMessage(2201, LogLevel.Information, "Group address database loaded with {Count} entries")]
    private partial void LogGroupAddressDatabaseLoaded(int count);

    [LoggerMessage(2202, LogLevel.Information, "No CSV path provided - continuing without group address database")]
    private partial void LogNoCsvPathProvided();

    [LoggerMessage(2203, LogLevel.Warning, "Failed to load group address CSV: {ErrorMessage}")]
    private partial void LogFailedToLoadGroupAddressCsv(Exception ex, string errorMessage);

    [LoggerMessage(2204, LogLevel.Information, "Continuing without group address database - raw values will be shown")]
    private partial void LogContinuingWithoutGroupAddressDatabase();

    // Message Processing (2300-2399) - THE MAIN KNX MESSAGE LOG
    [LoggerMessage(2301, LogLevel.Debug, "Received KNX message: {GroupAddress} ({MessageType})")]
    private partial void LogReceivedKnxMessage(string groupAddress, string messageType);

    [LoggerMessage(
        2302,
        LogLevel.Information,
        "[{Timestamp}] {MessageType} {SourceAddress} -> {GroupAddress} = {Value} (Raw: {RawData}) {DataPointType} {Description}"
    )]
    private partial void LogDetailedKnxMessage(
        string timestamp,
        string sourceAddress,
        string groupAddress,
        string messageType,
        string rawData,
        string value,
        string dataPointType,
        string description
    );

    [LoggerMessage(2303, LogLevel.Error, "Error processing group message")]
    private partial void LogErrorProcessingGroupMessage(Exception ex);

    // Filter Operations (2400-2499)
    [LoggerMessage(2400, LogLevel.Warning, "Invalid filter pattern: {Pattern}")]
    private partial void LogInvalidFilterPattern(Exception ex, string pattern);

    // Disposal Operations (2900-2999)
    [LoggerMessage(2900, LogLevel.Error, "Error during async dispose")]
    private partial void LogErrorDuringAsyncDispose(Exception ex);
}
