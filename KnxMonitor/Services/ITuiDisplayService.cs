namespace KnxMonitor.Services;

/// <summary>
/// Enterprise-grade Terminal User Interface service for KNX Monitor.
/// Provides rich, interactive display capabilities using Terminal.Gui V2.
/// </summary>
public interface ITuiDisplayService : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether the TUI is currently running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Gets the current filter pattern applied to messages.
    /// </summary>
    string? CurrentFilter { get; }

    /// <summary>
    /// Gets the total number of messages processed.
    /// </summary>
    int MessageCount { get; }

    /// <summary>
    /// Gets the application start time.
    /// </summary>
    DateTime StartTime { get; }

    /// <summary>
    /// Starts the Terminal User Interface asynchronously.
    /// </summary>
    /// <param name="monitorService">The KNX monitor service to display data from.</param>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartAsync(IKnxMonitorService monitorService, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the Terminal User Interface asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the filter pattern for message display.
    /// </summary>
    /// <param name="filter">The new filter pattern (null to clear filter).</param>
    void UpdateFilter(string? filter);

    /// <summary>
    /// Clears all displayed messages.
    /// </summary>
    void ClearMessages();

    /// <summary>
    /// Exports current messages to a file.
    /// </summary>
    /// <param name="filePath">Path to export file.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExportMessagesAsync(string filePath);
}
