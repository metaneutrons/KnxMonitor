using Microsoft.Extensions.Logging;

namespace KnxMonitor.Services;

/// <summary>
/// High-performance LoggerMessage definitions for KnxGroupAddressDatabase.
/// Event IDs: 3000-3099 (Data Services - Database)
/// </summary>
public partial class KnxGroupAddressDatabase
{
    // CSV Loading Operations (3000-3099)
    [LoggerMessage(3001, LogLevel.Information, "Loaded {LoadedCount} group addresses from {CsvFilePath}")]
    private partial void LogGroupAddressesLoaded(int loadedCount, string csvFilePath);

    [LoggerMessage(3002, LogLevel.Warning, "Error parsing CSV line {LineNumber}: {ErrorMessage}")]
    private partial void LogCsvParsingError(Exception ex, int lineNumber, string errorMessage);

    [LoggerMessage(3003, LogLevel.Debug, "Processing CSV line {LineNumber}: {GroupAddress}")]
    private partial void LogProcessingCsvLine(int lineNumber, string groupAddress);

    [LoggerMessage(
        3004,
        LogLevel.Warning,
        "Skipping invalid group address format: {GroupAddress} on line {LineNumber}"
    )]
    private partial void LogSkippingInvalidGroupAddress(string groupAddress, int lineNumber);

    [LoggerMessage(3005, LogLevel.Debug, "CSV file contains {TotalLines} lines")]
    private partial void LogCsvFileLineCount(int totalLines);
}
