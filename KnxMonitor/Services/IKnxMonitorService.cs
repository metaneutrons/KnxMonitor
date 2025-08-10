using KnxMonitor.Models;

namespace KnxMonitor.Services;

/// <summary>
/// Interface for the KNX monitoring service.
/// </summary>
public interface IKnxMonitorService : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Event raised when a KNX message is received.
    /// </summary>
    event EventHandler<KnxMessage>? MessageReceived;

    /// <summary>
    /// Gets a value indicating whether the monitor is connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Gets the connection status message.
    /// </summary>
    string ConnectionStatus { get; }

    /// <summary>
    /// Gets the total number of messages received since monitoring started.
    /// </summary>
    int MessageCount { get; }

    /// <summary>
    /// Gets a value indicating whether a CSV group address database has been loaded.
    /// </summary>
    bool IsCsvLoaded { get; }

    /// <summary>
    /// Starts monitoring the KNX bus.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartMonitoringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops monitoring the KNX bus.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopMonitoringAsync(CancellationToken cancellationToken = default);
}
