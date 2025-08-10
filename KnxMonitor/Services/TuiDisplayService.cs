using System.Text;
using KnxMonitor.Models;
using Microsoft.Extensions.Logging;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace KnxMonitor.Services;

/// <summary>
/// Award-worthy Terminal.Gui V2 implementation for KNX Monitor.
/// Provides enterprise-grade windowed interface with proper V2 API compatibility and coordinated shutdown.
/// </summary>
public partial class TuiDisplayService : IDisplayService
{
    private readonly ILogger<TuiDisplayService> _logger;
    private readonly KnxMessageTableModel _tableModel;
    private readonly KnxMonitorConfig _config;
    private Window? _mainWindow;
    private TableView? _tableView;
    private StatusBar? _statusBar;
    private Label? _connectionStatusLabel;
    private Label? _messageCountLabel;
    private Label? _lastMessageLabel;
    private Label? _filterStatusLabel;
    private readonly object _lockObject = new();
    private bool _isRunning;
    private DateTime _startTime;
    private IKnxMonitorService? _monitorService;
    private CancellationTokenSource? _cancellationTokenSource;
    private TaskCompletionSource<bool>? _shutdownCompletionSource;
    private bool _shutdownRequested = false;
    private Timer? _refreshTimer;

    public TuiDisplayService(ILogger<TuiDisplayService> logger, KnxMonitorConfig config)
    {
        this._logger = logger;
        this._config = config;
        this._tableModel = new KnxMessageTableModel();
        this._startTime = DateTime.Now;
    }

    // IDisplayService properties
    public bool IsRunning => this._isRunning;
    public string? CurrentFilter => this._tableModel.Filter;
    public int MessageCount => this._tableModel.Messages.Count;
    public DateTime StartTime => this._startTime;

    public void Initialize()
    {
        try
        {
            Application.Init();

            // Note: Ctrl+C handling is now managed at the Program level
            // This avoids conflicts between main program and TUI service

            this.LogTerminalGuiInitializedSuccessfully();
        }
        catch (Exception ex)
        {
            this.LogFailedToInitializeTerminalGui(ex);
            throw;
        }
    }

    /// <summary>
    /// Requests shutdown of the TUI application.
    /// This method is called by the main program's Ctrl+C handler or F10 key.
    /// </summary>
    public void RequestShutdown()
    {
        if (this._shutdownRequested)
        {
            return;
        }

        try
        {
            this._shutdownRequested = true;
            this.LogTuiShutdownRequested();

            // Request Terminal.Gui to stop
            Application.RequestStop();

            // Signal that shutdown is complete
            this._shutdownCompletionSource?.TrySetResult(true);
        }
        catch (Exception ex)
        {
            this.LogErrorDuringTuiShutdownRequest(ex);
            this._shutdownCompletionSource?.TrySetException(ex);
        }
    }

    public void Start()
    {
        if (this._isRunning)
        {
            this.LogTuiDisplayServiceAlreadyRunning();
            return;
        }

        try
        {
            this._isRunning = true;
            this.CreateMainWindow();

            if (this._mainWindow != null)
            {
                Application.Run(this._mainWindow);
            }
        }
        catch (Exception ex)
        {
            this.LogErrorRunningTuiDisplayService(ex);
            throw;
        }
        finally
        {
            this._isRunning = false;
        }
    }

    public void Stop()
    {
        if (!this._isRunning)
        {
            return;
        }

        try
        {
            this._isRunning = false;
            this.RequestShutdown();
            this.LogTuiDisplayServiceStopped();
        }
        catch (Exception ex)
        {
            this.LogErrorStoppingTuiDisplayService(ex);
        }
    }

    public void Shutdown()
    {
        try
        {
            this.Stop();
            Application.Shutdown();
            this.LogTerminalGuiShutdownCompleted();
        }
        catch (Exception ex)
        {
            this.LogErrorDuringTerminalGuiShutdown(ex);
        }
    }

    private void CreateMainWindow()
    {
        this._mainWindow = new Window
        {
            Title = "KNX Monitor - Terminal.Gui V2",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };

        // Handle window closing
        this._mainWindow.Closing += (s, e) =>
        {
            this.LogMainWindowClosingInitiatingShutdown();
            this.RequestShutdown();
        };

        this.CreateWindowContent();

        this.LogMainWindowCreatedWithTerminalGuiComponents();
    }

    private void CreateWindowContent()
    {
        // Clear existing content
        this._mainWindow?.RemoveAll();

        // Create status frame at the top - uses computed layout for automatic resizing
        var statusFrame = new FrameView
        {
            Title = "Connection Status",
            X = 0,
            Y = 0,
            Width = Dim.Fill(), // Automatically adjusts to terminal width
            Height = 6,
        };

        this.CreateStatusLabels(statusFrame);
        this._mainWindow?.Add(statusFrame);

        // Create messages frame below status - uses computed layout for automatic resizing
        var messagesFrame = new FrameView
        {
            Title = "KNX Messages",
            X = 0,
            Y = Pos.Bottom(statusFrame),
            Width = Dim.Fill(), // Automatically adjusts to terminal width
            Height = Dim.Fill(1), // Automatically adjusts to terminal height, leaving space for status bar
        };

        this.CreateMessagesTable(messagesFrame);
        this._mainWindow?.Add(messagesFrame);

        // Create status bar at the bottom - uses computed layout for automatic resizing
        this.CreateStatusBar();
        this._mainWindow?.Add(this._statusBar!);

        // Set up key bindings
        this.SetupKeyBindings();

        this.LogWindowContentCreatedWithComputedLayout();
    }

    private void CreateStatusLabels(FrameView statusFrame)
    {
        // Connection status (consolidated status and details)
        this._connectionStatusLabel = new Label
        {
            Text = "Status: Disconnected",
            X = 1,
            Y = 0,
            Width = Dim.Fill()! - 2,
            Height = 1,
        };
        statusFrame.Add(this._connectionStatusLabel);

        this._messageCountLabel = new Label
        {
            Text = "Messages: 0",
            X = 1,
            Y = 1,
            Width = Dim.Fill()! - 2,
            Height = 1,
        };
        statusFrame.Add(this._messageCountLabel);

        this._lastMessageLabel = new Label
        {
            Text = "Last Message: None",
            X = 1,
            Y = 2,
            Width = Dim.Fill()! - 2,
            Height = 1,
        };
        statusFrame.Add(this._lastMessageLabel);

        this._filterStatusLabel = new Label
        {
            Text = "Filter: None",
            X = 1,
            Y = 3,
            Width = Dim.Fill()! - 2,
            Height = 1,
        };
        statusFrame.Add(this._filterStatusLabel);
    }

    private void CreateMessagesTable(FrameView messagesFrame)
    {
        // Create a simple table source implementation with CSV status
        var csvLoaded = this._monitorService?.IsCsvLoaded ?? false;
        var tableSource = new KnxMessageTableSource(this._tableModel, csvLoaded);

        this._tableView = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Table = tableSource,
        };

        messagesFrame.Add(this._tableView);
    }

    private void CreateStatusBar()
    {
        this._statusBar = new StatusBar(
            new[]
            {
                new Shortcut(Key.F1, "Help", this.ShowHelp),
                new Shortcut(Key.F2, "Filter", this.ShowFilterDialog),
                new Shortcut(Key.F3, "Export", this.ShowExportDialog),
                new Shortcut(Key.F10, "Quit", this.RequestShutdown), // Now calls RequestShutdown instead of inline logic
            }
        );
    }

    private void SetupKeyBindings()
    {
        if (this._mainWindow == null)
            return;

        // Note: Ctrl+C is now handled at the Program level to avoid conflicts
        // Terminal.Gui V2 handles Esc automatically for closing dialogs
        // F-key shortcuts are handled by the status bar
    }

    private void ShowHelp()
    {
        var helpText = """
            KNX Monitor Help

            F1 - Show this help
            F2 - Set message filter
            F3 - Export messages
            F10 - Quit application
            Ctrl+C - Quit application
            Esc - Close dialogs

            Arrow keys - Navigate table
            Page Up/Down - Scroll table
            """;

        MessageBox.Query("Help", helpText, "OK");
    }

    private void ShowFilterDialog()
    {
        var dialog = new Dialog
        {
            Title = "Set Filter",
            Width = 60,
            Height = 12,
        };

        var filterField = new TextField
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill()! - 2,
            Height = 1,
            Text = this._tableModel.Filter ?? "",
        };

        var label = new Label
        {
            Text = "Filter expression:",
            X = 1,
            Y = 0,
            Width = Dim.Fill()! - 2,
            Height = 1,
        };

        var okButton = new Button
        {
            Text = "OK",
            X = Pos.Center() - 10,
            Y = Pos.Bottom(filterField) + 2,
            IsDefault = true,
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            X = Pos.Center() + 2,
            Y = Pos.Bottom(filterField) + 2,
        };

        okButton.Accepting += (s, e) =>
        {
            this._tableModel.Filter = filterField.Text?.ToString();
            this.UpdateFilterStatus();
            Application.RequestStop();
            e.Handled = true;
        };

        cancelButton.Accepting += (s, e) =>
        {
            Application.RequestStop();
            e.Handled = true;
        };

        dialog.Add(label, filterField, okButton, cancelButton);
        Application.Run(dialog);
        dialog.Dispose();
    }

    private void ShowExportDialog()
    {
        var dialog = new Dialog
        {
            Title = "Export Messages",
            Width = 60,
            Height = 12,
        };

        var pathField = new TextField
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill()! - 2,
            Height = 1,
            Text = $"knx_messages_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
        };

        var label = new Label
        {
            Text = "Export file path:",
            X = 1,
            Y = 0,
            Width = Dim.Fill()! - 2,
            Height = 1,
        };

        var okButton = new Button
        {
            Text = "Export",
            X = Pos.Center() - 10,
            Y = Pos.Bottom(pathField) + 2,
            IsDefault = true,
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            X = Pos.Center() + 2,
            Y = Pos.Bottom(pathField) + 2,
        };

        okButton.Accepting += (s, e) =>
        {
            try
            {
                var path = pathField.Text?.ToString();
                if (!string.IsNullOrEmpty(path))
                {
                    this.ExportMessages(path);
                    MessageBox.Query("Export", $"Messages exported to: {path}", "OK");
                }
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery("Export Error", $"Failed to export: {ex.Message}", "OK");
            }
            Application.RequestStop();
            e.Handled = true;
        };

        cancelButton.Accepting += (s, e) =>
        {
            Application.RequestStop();
            e.Handled = true;
        };

        dialog.Add(label, pathField, okButton, cancelButton);
        Application.Run(dialog);
        dialog.Dispose();
    }

    public void AddMessage(KnxMessage message)
    {
        lock (this._lockObject)
        {
            this._tableModel.AddMessage(message);
            this.UpdateUI();
        }
    }

    public void UpdateConnectionStatus(ConnectionStatusModel status)
    {
        lock (this._lockObject)
        {
            // Update connection status display
            this.UpdateConnectionStatusDisplay();
        }
    }

    public void ClearMessages()
    {
        lock (this._lockObject)
        {
            this._tableModel.Clear();
            this.UpdateUI();
        }
    }

    public void SetFilter(string? filter)
    {
        lock (this._lockObject)
        {
            this._tableModel.Filter = filter;
            this.UpdateFilterStatus();
            this.UpdateUI();
        }
    }

    public void ExportMessages(string filePath)
    {
        lock (this._lockObject)
        {
            var csv = new StringBuilder();
            var csvLoaded = this._monitorService?.IsCsvLoaded ?? false;

            if (csvLoaded)
            {
                csv.AppendLine("Timestamp,Source,Destination,Type,Data,Value,DPT,Description");
            }
            else
            {
                csv.AppendLine("Timestamp,Source,Destination,Type,Data,Value");
            }

            foreach (var messageRow in this._tableModel.Messages)
            {
                var message = messageRow.Message;
                if (csvLoaded)
                {
                    var description = message.Description?.Replace("\"", "\"\"") ?? ""; // Escape quotes for CSV
                    csv.AppendLine(
                        $"{message.Timestamp:yyyy-MM-dd HH:mm:ss.fff},{message.SourceAddress},{message.GroupAddress},{message.MessageType},{message.DataHex},{message.DisplayValue},{message.DataPointType ?? ""},\"{description}\""
                    );
                }
                else
                {
                    csv.AppendLine(
                        $"{message.Timestamp:yyyy-MM-dd HH:mm:ss.fff},{message.SourceAddress},{message.GroupAddress},{message.MessageType},{message.DataHex},{message.DisplayValue}"
                    );
                }
            }

            File.WriteAllText(filePath, csv.ToString());
            this.LogExportedMessagesToFile(this._tableModel.Messages.Count, filePath);
        }
    }

    private void UpdateUI()
    {
        if (!this._isRunning)
            return;

        // Marshal UI updates to the main Terminal.Gui thread
        Application.Invoke(() =>
        {
            try
            {
                this.UpdateMessageCount();
                this.UpdateLastMessage();

                // Update table data - Terminal.Gui should handle the refresh automatically
                this._tableView?.Update();
            }
            catch (Exception ex)
            {
                this.LogErrorUpdatingUi(ex);
            }
        });
    }

    private void UpdateConnectionStatusDisplay()
    {
        if (!this._isRunning || this._connectionStatusLabel == null)
            return;

        // Marshal UI updates to the main Terminal.Gui thread
        Application.Invoke(() =>
        {
            try
            {
                // Get actual connection status from monitor service
                var isConnected = this._monitorService?.IsConnected ?? false;
                var config = this.GetConnectionConfig();

                if (isConnected && config != null)
                {
                    var statusText = config.ConnectionType switch
                    {
                        KnxConnectionType.Tunnel =>
                            $"Status: Connected via IP Tunneling to {config.Gateway}:{config.Port}",
                        KnxConnectionType.Router =>
                            $"Status: Connected via multicast {config.MulticastAddress}:{config.Port}",
                        KnxConnectionType.Usb => "Status: Connected via USB to /dev/knx",
                        _ => "Status: Connected (unknown mode)",
                    };
                    this._connectionStatusLabel.Text = statusText;
                }
                else if (config != null)
                {
                    var statusText = config.ConnectionType switch
                    {
                        KnxConnectionType.Tunnel =>
                            $"Status: Disconnected (IP Tunneling to {config.Gateway}:{config.Port})",
                        KnxConnectionType.Router =>
                            $"Status: Disconnected (multicast {config.MulticastAddress}:{config.Port})",
                        KnxConnectionType.Usb => "Status: Disconnected (USB mode)",
                        _ => "Status: Disconnected",
                    };
                    this._connectionStatusLabel.Text = statusText;
                }
                else
                {
                    this._connectionStatusLabel.Text = "Status: Disconnected";
                }
            }
            catch (Exception ex)
            {
                this.LogErrorUpdatingConnectionStatusDisplay(ex);
            }
        });
    }

    private KnxMonitorConfig? GetConnectionConfig()
    {
        return this._config;
    }

    private void UpdateMessageCount()
    {
        if (this._messageCountLabel == null)
            return;

        this._messageCountLabel.Text = $"Messages: {this._tableModel.Messages.Count}";
    }

    private void UpdateLastMessage()
    {
        if (this._lastMessageLabel == null)
            return;

        var lastMessageRow = this._tableModel.Messages.FirstOrDefault();
        this._lastMessageLabel.Text =
            lastMessageRow != null
                ? $"Last Message: {lastMessageRow.Message.Timestamp:HH:mm:ss} {lastMessageRow.Message.SourceAddress} -> {lastMessageRow.Message.GroupAddress}"
                : "Last Message: None";
    }

    private void UpdateFilterStatus()
    {
        if (this._filterStatusLabel == null)
            return;

        this._filterStatusLabel.Text = $"Filter: {this._tableModel.Filter ?? "None"}";
    }

    public async Task StartAsync(IKnxMonitorService monitorService, CancellationToken cancellationToken = default)
    {
        this._monitorService = monitorService;
        this._cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        this._shutdownCompletionSource = new TaskCompletionSource<bool>();
        this._startTime = DateTime.Now;

        try
        {
            this.Initialize();

            // Subscribe to monitor service events
            if (this._monitorService != null)
            {
                this._monitorService.MessageReceived += this.OnMessageReceived;
            }

            // Start periodic refresh timer (every 100ms) to ensure UI updates
            this._refreshTimer = new Timer(
                this.OnRefreshTimer,
                null,
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(100)
            );

            // Start the TUI in a background task
            var tuiTask = Task.Run(
                () =>
                {
                    try
                    {
                        this.Start();
                        this.LogTuiApplicationCompletedNormally();
                        this._shutdownCompletionSource.TrySetResult(true);
                    }
                    catch (Exception ex)
                    {
                        this.LogErrorInTuiApplication(ex);
                        this._shutdownCompletionSource.TrySetException(ex);
                    }
                },
                this._cancellationTokenSource.Token
            );

            // Wait for either the TUI to close, cancellation, or shutdown completion
            var completedTask = await Task.WhenAny(
                this._shutdownCompletionSource.Task,
                Task.Delay(System.Threading.Timeout.Infinite, this._cancellationTokenSource.Token)
            );

            if (completedTask == this._shutdownCompletionSource.Task)
            {
                this.LogTuiApplicationHasBeenClosed();
            }
            else
            {
                this.LogTuiApplicationWasCancelled();
                this.RequestShutdown();
            }
        }
        catch (OperationCanceledException)
        {
            this.LogTuiDisplayServiceWasCancelled();
            this.RequestShutdown();
        }
        catch (Exception ex)
        {
            this.LogErrorInTuiDisplayService(ex);
            throw;
        }
        finally
        {
            // Unsubscribe from events
            if (this._monitorService != null)
            {
                this._monitorService.MessageReceived -= this.OnMessageReceived;
            }

            // Stop refresh timer
            this._refreshTimer?.Dispose();
            this._refreshTimer = null;
        }
    }

    /// <summary>
    /// Handles KNX messages received from the monitor service.
    /// </summary>
    private void OnMessageReceived(object? sender, KnxMessage message)
    {
        try
        {
            this.AddMessage(message);
        }
        catch (Exception ex)
        {
            this.LogErrorHandlingReceivedMessage(ex);
        }
    }

    /// <summary>
    /// Periodic refresh timer callback to ensure UI updates.
    /// </summary>
    private void OnRefreshTimer(object? state)
    {
        if (!this._isRunning)
            return;

        try
        {
            // Trigger UI updates
            Application.Invoke(() =>
            {
                try
                {
                    // Update the table view to refresh display
                    this._tableView?.Update();

                    // Also refresh connection status periodically
                    this.UpdateConnectionStatusDisplay();
                }
                catch (Exception)
                {
                    // Silently ignore refresh errors to avoid log spam
                }
            });
        }
        catch (Exception)
        {
            // Silently ignore timer callback errors
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(
            () =>
            {
                try
                {
                    this.LogStoppingTuiDisplayService();

                    // Stop refresh timer first
                    this._refreshTimer?.Dispose();
                    this._refreshTimer = null;

                    // Unsubscribe from monitor service events
                    if (this._monitorService != null)
                    {
                        this._monitorService.MessageReceived -= this.OnMessageReceived;
                    }

                    // Stop the TUI
                    this.Stop();

                    // Cancel any ongoing operations
                    this._cancellationTokenSource?.Cancel();

                    this.LogTuiDisplayServiceStoppedAsync();
                }
                catch (Exception ex)
                {
                    this.LogErrorStoppingTuiDisplayServiceAsync(ex);
                }
            },
            cancellationToken
        );
    }

    public void UpdateConnectionStatus(string status, bool isConnected)
    {
        if (!this._isRunning || this._connectionStatusLabel == null)
            return;

        // Marshal UI updates to the main Terminal.Gui thread
        Application.Invoke(() =>
        {
            try
            {
                this._connectionStatusLabel.Text = $"Connection: {status}";
            }
            catch (Exception ex)
            {
                this.LogErrorUpdatingConnectionStatus(ex);
            }
        });
    }

    public void DisplayMessage(KnxMessage message)
    {
        this.AddMessage(message);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await this.StopAsync();
            this.Dispose();
        }
        catch (Exception ex)
        {
            this.LogErrorDuringAsyncDispose(ex);
        }
    }

    public void Dispose()
    {
        try
        {
            // Stop refresh timer first
            this._refreshTimer?.Dispose();
            this._refreshTimer = null;

            // Unsubscribe from monitor service events
            if (this._monitorService != null)
            {
                this._monitorService.MessageReceived -= this.OnMessageReceived;
            }

            // Stop and shutdown TUI
            this.Stop();
            this.Shutdown();

            // Dispose cancellation token source
            this._cancellationTokenSource?.Dispose();

            // Complete shutdown if not already done
            this._shutdownCompletionSource?.TrySetResult(false);
        }
        catch (Exception ex)
        {
            this.LogErrorDuringDispose(ex);
        }
    }
}

/// <summary>
/// Table source implementation for Terminal.Gui V2 TableView
/// </summary>
public class KnxMessageTableSource : ITableSource
{
    private readonly KnxMessageTableModel _model;
    private readonly bool _csvLoaded;

    public KnxMessageTableSource(KnxMessageTableModel model, bool csvLoaded)
    {
        this._model = model;
        this._csvLoaded = csvLoaded;
    }

    public int Rows => this._model.Messages.Count;
    public int Columns => _csvLoaded ? 8 : 6; // With CSV: Timestamp, Source, Dest, Type, Raw Data, Value, DPT, Description

    // Without CSV: Timestamp, Source, Dest, Type, Raw Data, Value

    public string[] ColumnNames =>
        _csvLoaded
            ? new[] { "Timestamp", "Source", "Dest", "MsgType", "Raw Data", "Value", "DPT", "Description" }
            : new[] { "Timestamp", "Source", "Dest", "MsgType", "Raw Data", "Value" };

    public object this[int row, int col]
    {
        get
        {
            if (row < 0 || row >= this._model.Messages.Count)
                return "";

            var messageRow = this._model.Messages[row];
            if (_csvLoaded)
            {
                return col switch
                {
                    0 => messageRow.TimeDisplay,
                    1 => messageRow.SourceDisplay,
                    2 => messageRow.GroupAddressDisplay,
                    3 => messageRow.MessageTypeDisplay,
                    4 => messageRow.DataDisplay,
                    5 => messageRow.ValueDisplay,
                    6 => messageRow.DptDisplay,
                    7 => messageRow.DescriptionDisplay,
                    _ => "",
                };
            }
            else
            {
                return col switch
                {
                    0 => messageRow.TimeDisplay,
                    1 => messageRow.SourceDisplay,
                    2 => messageRow.GroupAddressDisplay,
                    3 => messageRow.MessageTypeDisplay,
                    4 => messageRow.DataDisplay,
                    5 => messageRow.ValueDisplay,
                    _ => "",
                };
            }
        }
    }

    public string GetColumnName(int col)
    {
        return col >= 0 && col < this.ColumnNames.Length ? this.ColumnNames[col] : "";
    }

    public Type GetColumnType(int col) => typeof(string);

    public void SetValue(int row, int col, object value)
    {
        // Read-only table
    }
}
