using Microsoft.Extensions.Logging;

namespace KnxMonitor.Services;

/// <summary>
/// High-performance LoggerMessage definitions for TuiDisplayService.
/// Eliminates boxing, reduces allocations, and provides compile-time safety.
/// </summary>
public partial class TuiDisplayService
{
    // Initialization and Lifecycle (9101-9110)
    [LoggerMessage(9101, LogLevel.Information, "Terminal.Gui V2 application initialized successfully")]
    private partial void LogTerminalGuiInitializedSuccessfully();

    [LoggerMessage(9102, LogLevel.Error, "Failed to initialize Terminal.Gui V2 application")]
    private partial void LogFailedToInitializeTerminalGui(Exception ex);

    [LoggerMessage(9103, LogLevel.Information, "TUI shutdown requested, closing application...")]
    private partial void LogTuiShutdownRequested();

    [LoggerMessage(9104, LogLevel.Error, "Error during TUI shutdown request")]
    private partial void LogErrorDuringTuiShutdownRequest(Exception ex);

    [LoggerMessage(9105, LogLevel.Warning, "TUI display service is already running")]
    private partial void LogTuiDisplayServiceAlreadyRunning();

    [LoggerMessage(9106, LogLevel.Error, "Error running TUI display service")]
    private partial void LogErrorRunningTuiDisplayService(Exception ex);

    [LoggerMessage(9107, LogLevel.Information, "TUI display service stopped")]
    private partial void LogTuiDisplayServiceStopped();

    [LoggerMessage(9108, LogLevel.Error, "Error stopping TUI display service")]
    private partial void LogErrorStoppingTuiDisplayService(Exception ex);

    [LoggerMessage(9109, LogLevel.Information, "Terminal.Gui V2 application shutdown completed")]
    private partial void LogTerminalGuiShutdownCompleted();

    [LoggerMessage(9110, LogLevel.Error, "Error during Terminal.Gui V2 shutdown")]
    private partial void LogErrorDuringTerminalGuiShutdown(Exception ex);

    // UI Creation and Management (9111-9115)
    [LoggerMessage(9111, LogLevel.Information, "Main window closing, initiating shutdown...")]
    private partial void LogMainWindowClosingInitiatingShutdown();

    [LoggerMessage(9112, LogLevel.Information, "Main window created with Terminal.Gui V2 components")]
    private partial void LogMainWindowCreatedWithTerminalGuiComponents();

    [LoggerMessage(9113, LogLevel.Debug, "Window content created with computed layout for automatic resizing")]
    private partial void LogWindowContentCreatedWithComputedLayout();

    [LoggerMessage(9114, LogLevel.Error, "Error updating UI")]
    private partial void LogErrorUpdatingUi(Exception ex);

    [LoggerMessage(9115, LogLevel.Error, "Error updating connection status display")]
    private partial void LogErrorUpdatingConnectionStatusDisplay(Exception ex);

    // Export and Data Operations (9116)
    [LoggerMessage(9116, LogLevel.Information, "Exported {Count} messages to {FilePath}")]
    private partial void LogExportedMessagesToFile(int count, string filePath);

    // Application Lifecycle Events (9117-9122)
    [LoggerMessage(9117, LogLevel.Information, "TUI application completed normally")]
    private partial void LogTuiApplicationCompletedNormally();

    [LoggerMessage(9118, LogLevel.Error, "Error in TUI application")]
    private partial void LogErrorInTuiApplication(Exception ex);

    [LoggerMessage(9119, LogLevel.Information, "TUI application has been closed")]
    private partial void LogTuiApplicationHasBeenClosed();

    [LoggerMessage(9120, LogLevel.Information, "TUI application was cancelled")]
    private partial void LogTuiApplicationWasCancelled();

    [LoggerMessage(9121, LogLevel.Information, "TUI display service was cancelled")]
    private partial void LogTuiDisplayServiceWasCancelled();

    [LoggerMessage(9122, LogLevel.Error, "Error in TUI display service")]
    private partial void LogErrorInTuiDisplayService(Exception ex);

    // Message Handling (9123)
    [LoggerMessage(9123, LogLevel.Error, "Error handling received message")]
    private partial void LogErrorHandlingReceivedMessage(Exception ex);

    // Async Operations (9124-9126)
    [LoggerMessage(9124, LogLevel.Information, "Stopping TUI display service...")]
    private partial void LogStoppingTuiDisplayService();

    [LoggerMessage(9125, LogLevel.Information, "TUI display service stopped")]
    private partial void LogTuiDisplayServiceStoppedAsync();

    [LoggerMessage(9126, LogLevel.Error, "Error stopping TUI display service")]
    private partial void LogErrorStoppingTuiDisplayServiceAsync(Exception ex);

    // Connection Status Updates (9127)
    [LoggerMessage(9127, LogLevel.Error, "Error updating connection status")]
    private partial void LogErrorUpdatingConnectionStatus(Exception ex);

    // Disposal Operations (9128-9129)
    [LoggerMessage(9128, LogLevel.Error, "Error during async dispose")]
    private partial void LogErrorDuringAsyncDispose(Exception ex);

    [LoggerMessage(9129, LogLevel.Error, "Error during dispose")]
    private partial void LogErrorDuringDispose(Exception ex);
}
