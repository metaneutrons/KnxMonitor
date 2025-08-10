using Microsoft.Extensions.Logging;

namespace KnxMonitor.Services;

/// <summary>
/// High-performance LoggerMessage definitions for KnxDptDecoder.
/// Event IDs: 3100-3199 (Data Services - Decoder)
/// </summary>
public partial class KnxDptDecoder
{
    // DPT Decoding Operations (3100-3199)
    [LoggerMessage(3101, LogLevel.Debug, "DPT decoding error for {DptString}: {ErrorMessage}")]
    private partial void LogDptDecodingError(Exception ex, string dptString, string errorMessage);

    [LoggerMessage(3102, LogLevel.Debug, "Error parsing DPT {DptString}: {ErrorMessage}")]
    private partial void LogErrorParsingDpt(Exception ex, string dptString, string errorMessage);

    [LoggerMessage(3103, LogLevel.Debug, "Error finding datapoint subtype {MainType}.{SubType}: {ErrorMessage}")]
    private partial void LogErrorFindingDatapointSubtype(Exception ex, int mainType, int subType, string errorMessage);

    [LoggerMessage(3104, LogLevel.Debug, "Successfully decoded DPT {DptString} with value: {DecodedValue}")]
    private partial void LogSuccessfullyDecodedDpt(string dptString, object decodedValue);

    [LoggerMessage(3105, LogLevel.Debug, "No decoder found for DPT {DptString}")]
    private partial void LogNoDecoderFound(string dptString);
}
