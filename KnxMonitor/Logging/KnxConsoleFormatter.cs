using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace KnxMonitor.Logging;

/// <summary>
/// Custom console formatter for clean KNX message output.
/// Provides clean, readable output for KNX messages while maintaining structured logging benefits.
/// </summary>
public sealed class KnxConsoleFormatter : ConsoleFormatter
{
    private readonly KnxConsoleFormatterOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="KnxConsoleFormatter"/> class.
    /// </summary>
    /// <param name="options">The formatter options.</param>
    public KnxConsoleFormatter(IOptionsMonitor<KnxConsoleFormatterOptions> options)
        : base("knx")
    {
        this._options = options.CurrentValue;
    }

    /// <inheritdoc/>
    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter
    )
    {
        var message = logEntry.Formatter(logEntry.State, logEntry.Exception);

        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        // For KNX message logs (event ID 2302), use clean format
        if (logEntry.EventId.Id == 2302)
        {
            // The message is already formatted as: [timestamp] MessageType Source -> GroupAddress = Value (Raw: XX) DPT Description
            textWriter.WriteLine(message);
            return;
        }

        // For other logs, use standard format with timestamp
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");

        // Format based on log level
        var levelString = logEntry.LogLevel switch
        {
            LogLevel.Error => "ERROR",
            LogLevel.Warning => "WARN",
            LogLevel.Information => "INFO",
            LogLevel.Debug => "DEBUG",
            _ => logEntry.LogLevel.ToString().ToUpperInvariant(),
        };

        textWriter.WriteLine($"[{timestamp}] {levelString}: {message}");

        // Include exception details if present
        if (logEntry.Exception != null)
        {
            textWriter.WriteLine($"[{timestamp}] EXCEPTION: {logEntry.Exception}");
        }
    }
}

/// <summary>
/// Options for the KNX console formatter.
/// </summary>
public class KnxConsoleFormatterOptions : ConsoleFormatterOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to include timestamps for non-KNX messages.
    /// </summary>
    public bool IncludeTimestamps { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to use clean format for KNX messages.
    /// </summary>
    public bool UseCleanKnxFormat { get; set; } = true;
}
