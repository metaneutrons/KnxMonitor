using KnxMonitor.Models;

namespace KnxMonitor.Services;

/// <summary>
/// Interface for display services that can show KNX monitor output.
/// Supports both Terminal.Gui interactive mode and console logging mode.
/// </summary>
public interface IDisplayService : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether the display service is currently running.
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
    /// Starts the display service asynchronously.
    /// </summary>
    /// <param name="monitorService">The KNX monitor service to display data from.</param>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartAsync(IKnxMonitorService monitorService, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the display service asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the connection status display.
    /// </summary>
    /// <param name="status">Connection status.</param>
    /// <param name="isConnected">Whether the connection is active.</param>
    void UpdateConnectionStatus(string status, bool isConnected);

    /// <summary>
    /// Displays a KNX message.
    /// </summary>
    /// <param name="message">Message to display.</param>
    void DisplayMessage(KnxMessage message);
}
