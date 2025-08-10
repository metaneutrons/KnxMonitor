using System.Collections.Concurrent;
using KnxMonitor.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace KnxMonitor.Services;

/// <summary>
/// Service for displaying KNX monitor output in console logging mode.
/// Used when output is redirected or in containerized environments.
/// </summary>
public partial class DisplayService : IDisplayService
{
    private readonly KnxMonitorConfig _config;
    private readonly ILogger<DisplayService> _logger;
    private readonly ConcurrentQueue<KnxMessage> _messageQueue = new();
    private readonly object _displayLock = new();

    private Table? _statusTable;
    private Table? _messagesTable;
    private Layout? _layout;
    private bool _isRunning;
    private Task? _displayTask;
    private CancellationTokenSource? _cancellationTokenSource;
    private int _messageCount;
    private DateTime _startTime;
    private string? _currentFilter;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisplayService"/> class.
    /// </summary>
    /// <param name="config">Monitor configuration.</param>
    /// <param name="logger">Logger instance.</param>
    public DisplayService(KnxMonitorConfig config, ILogger<DisplayService> logger)
    {
        this._config = config ?? throw new ArgumentNullException(nameof(config));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._currentFilter = config.Filter;
    }

    /// <inheritdoc/>
    public bool IsRunning => this._isRunning;

    /// <inheritdoc/>
    public string? CurrentFilter => this._currentFilter;

    /// <inheritdoc/>
    public int MessageCount => this._messageCount;

    /// <inheritdoc/>
    public DateTime StartTime => this._startTime;

    /// <inheritdoc/>
    public async Task StartAsync(IKnxMonitorService monitorService, CancellationToken cancellationToken = default)
    {
        if (this._isRunning)
        {
            return;
        }

        this._startTime = DateTime.Now;
        this._isRunning = true;
        this._cancellationTokenSource = new CancellationTokenSource();

        // Subscribe to monitor events
        monitorService.MessageReceived += this.OnMessageReceived;

        // Initialize display
        this.InitializeDisplay();

        // Start display update task
        this._displayTask = Task.Run(
            async () =>
            {
                // Log initial connection status for container mode
                if (ShouldUseLoggingMode())
                {
                    this.LogKnxMonitorStarted(monitorService.ConnectionStatus);
                }

                while (!this._cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        this.UpdateDisplay(monitorService);

                        // Use different update intervals based on output mode
                        var delay = ShouldUseLoggingMode() ? 1000 : 500; // 1s for logs, 500ms for interactive (much less flickering)
                        await Task.Delay(delay, this._cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        this.LogErrorUpdatingDisplay(ex);
                    }
                }
            },
            this._cancellationTokenSource.Token
        );

        // CRITICAL FIX: Wait for the background task to complete instead of returning immediately
        // This ensures the StartAsync method doesn't complete until the service is actually stopped
        try
        {
            await this._displayTask;
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!this._isRunning)
        {
            return;
        }

        this._isRunning = false;

        try
        {
            this._cancellationTokenSource?.Cancel();

            if (this._displayTask != null)
            {
                await this._displayTask;
            }
        }
        catch (Exception ex)
        {
            this.LogErrorStoppingDisplayService(ex);
        }
        finally
        {
            this._cancellationTokenSource?.Dispose();
            this._cancellationTokenSource = null;
        }
    }

    /// <inheritdoc/>
    public void UpdateConnectionStatus(string status, bool isConnected)
    {
        // Implementation for logging mode
        if (ShouldUseLoggingMode())
        {
            this.LogConnectionStatusUpdate(status, isConnected);
        }
    }

    /// <inheritdoc/>
    public void DisplayMessage(KnxMessage message)
    {
        this._messageQueue.Enqueue(message);
        Interlocked.Increment(ref this._messageCount);

        // Enhanced logging is now handled by KnxMonitorService.ProcessMessage()
        // with CSV data, DPT types, and descriptions. We disable logging here
        // to avoid duplicate output.

        // Note: The message is still queued for potential TUI display or other processing
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await this.StopAsync();
        GC.SuppressFinalize(this);
    }

    private void OnMessageReceived(object? sender, KnxMessage message)
    {
        this.DisplayMessage(message);
    }

    private void InitializeDisplay()
    {
        if (ShouldUseLoggingMode())
        {
            return; // No visual initialization needed for logging mode
        }

        // Initialize Spectre.Console display for interactive mode
        this._statusTable = new Table()
            .AddColumn("Property")
            .AddColumn("Value")
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Blue);

        this._messagesTable = new Table()
            .AddColumn("Time")
            .AddColumn("Type")
            .AddColumn("Source")
            .AddColumn("Group Address")
            .AddColumn("Value")
            .AddColumn("Priority")
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Green);

        this._layout = new Layout("Root").SplitRows(new Layout("Header").Size(8), new Layout("Messages"));

        this._layout["Header"].Update(this.CreateHeaderPanel());
        this._layout["Messages"].Update(this._messagesTable);
    }

    private void UpdateDisplay(IKnxMonitorService monitorService)
    {
        if (ShouldUseLoggingMode())
        {
            return; // No visual updates needed for logging mode
        }

        lock (this._displayLock)
        {
            try
            {
                // Update status table
                this.UpdateStatusTable(monitorService);

                // Update messages table
                this.UpdateMessagesTable();

                // Render the layout
                AnsiConsole.Clear();
                if (this._layout != null)
                {
                    AnsiConsole.Write(this._layout);
                }
            }
            catch (Exception ex)
            {
                this.LogErrorUpdatingVisualDisplay(ex);
            }
        }
    }

    private void UpdateStatusTable(IKnxMonitorService monitorService)
    {
        if (this._statusTable == null)
            return;

        this._statusTable.Rows.Clear();

        var connectionStatus = monitorService.IsConnected ? "✓ Connected" : "✗ Disconnected";
        var connectionColor = monitorService.IsConnected ? Color.Green : Color.Red;

        this._statusTable.AddRow("Connection", $"[{connectionColor}]{connectionStatus}[/]");
        this._statusTable.AddRow("Type", FormatConnectionType(this._config.ConnectionType));
        this._statusTable.AddRow("Gateway", this._config.Gateway ?? "Unknown");
        this._statusTable.AddRow("Port", this._config.Port.ToString());
        this._statusTable.AddRow("Filter", this._currentFilter ?? "None");
        this._statusTable.AddRow("Messages", this._messageCount.ToString());
        this._statusTable.AddRow("Uptime", this.FormatUptime());
    }

    private void UpdateMessagesTable()
    {
        if (this._messagesTable == null)
            return;

        this._messagesTable.Rows.Clear();

        var messages = new List<KnxMessage>();
        while (this._messageQueue.TryDequeue(out var message) && messages.Count < 20)
        {
            if (string.IsNullOrEmpty(this._currentFilter) || MatchesFilter(message, this._currentFilter))
            {
                messages.Add(message);
            }
        }

        foreach (var message in messages.Take(20).Reverse())
        {
            var ageColor = CalculateAgeColor(message.Timestamp);
            var typeColor = CalculateTypeColor(message.MessageType);

            this._messagesTable.AddRow(
                $"[{ageColor}]{message.Timestamp:HH:mm:ss.fff}[/]",
                $"[{typeColor}]{FormatMessageType(message.MessageType)}[/]",
                message.SourceAddress,
                message.GroupAddress,
                message.DisplayValue,
                message.Priority.ToString()
            );
        }
    }

    private Panel CreateHeaderPanel()
    {
        return new Panel(this._statusTable ?? new Table()).Header("KNX Monitor").BorderColor(Color.Blue).Padding(1, 0);
    }

    /// <summary>
    /// Determines whether to use logging mode based on output redirection.
    /// </summary>
    /// <returns>True if logging mode should be used, false for interactive mode.</returns>
    private static bool ShouldUseLoggingMode()
    {
        // Check if output is redirected or if we're in a container environment
        return Console.IsOutputRedirected
            || !Console.IsInputRedirected
            || Environment.GetEnvironmentVariable("KNX_MONITOR_LOGGING_MODE") == "true"
            || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
    }

    private static bool MatchesFilter(KnxMessage message, string filter)
    {
        if (string.IsNullOrEmpty(filter))
            return true;

        // Support wildcard patterns like "1/2/*" or exact matches
        if (filter.EndsWith("/*"))
        {
            var prefix = filter[..^2];
            return message.GroupAddress.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return message.GroupAddress.Equals(filter, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatConnectionType(KnxConnectionType connectionType)
    {
        return connectionType switch
        {
            KnxConnectionType.Tunnel => "IP Tunneling",
            KnxConnectionType.Router => "IP Routing",
            KnxConnectionType.Usb => "USB",
            _ => "Unknown",
        };
    }

    private static string FormatMessageType(KnxMessageType type)
    {
        return type switch
        {
            KnxMessageType.Read => "Read",
            KnxMessageType.Write => "Write",
            KnxMessageType.Response => "Response",
            _ => "Unknown",
        };
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "Empty",
            bool b => b ? "true" : "false",
            byte[] bytes => Convert.ToHexString(bytes),
            _ => value.ToString() ?? "Empty",
        };
    }

    private string FormatUptime()
    {
        var uptime = DateTime.Now - this._startTime;
        return $"{uptime.Hours:D2}:{uptime.Minutes:D2}:{uptime.Seconds:D2}";
    }

    private static string CalculateAgeColor(DateTime timestamp)
    {
        var age = DateTime.Now - timestamp;
        return age.TotalSeconds switch
        {
            < 1 => "green",
            < 5 => "yellow",
            < 30 => "orange1",
            _ => "white",
        };
    }

    private static string CalculateTypeColor(KnxMessageType type)
    {
        return type switch
        {
            KnxMessageType.Read => "cyan",
            KnxMessageType.Write => "green",
            KnxMessageType.Response => "yellow",
            _ => "white",
        };
    }
}
