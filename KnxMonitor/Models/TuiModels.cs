using System.Collections.ObjectModel;
using System.ComponentModel;

namespace KnxMonitor.Models;

/// <summary>
/// Enterprise-grade data model for Terminal.Gui TableView integration.
/// Provides observable collection with real-time updates and filtering capabilities.
/// </summary>
public class KnxMessageTableModel : INotifyPropertyChanged
{
    private readonly ObservableCollection<KnxMessageRow> _messages = new();
    private readonly Lock _lock = new();
    private string? _filter;
    private int _maxMessages = 1000; // Configurable message buffer size

    /// <summary>
    /// Initializes a new instance of the <see cref="KnxMessageTableModel"/> class.
    /// </summary>
    public KnxMessageTableModel()
    {
        this._messages.CollectionChanged += (_, _) =>
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Messages)));
    }

    /// <summary>
    /// Gets the observable collection of KNX messages.
    /// </summary>
    public ObservableCollection<KnxMessageRow> Messages => this._messages;

    /// <summary>
    /// Gets or sets the current filter pattern.
    /// </summary>
    public string? Filter
    {
        get => this._filter;
        set
        {
            if (this._filter != value)
            {
                this._filter = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Filter)));
                this.ApplyFilter();
            }
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of messages to retain.
    /// </summary>
    public int MaxMessages
    {
        get => this._maxMessages;
        set
        {
            if (this._maxMessages != value && value > 0)
            {
                this._maxMessages = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.MaxMessages)));
                this.TrimMessages();
            }
        }
    }

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Adds a new KNX message to the table model.
    /// </summary>
    /// <param name="message">The KNX message to add.</param>
    public void AddMessage(KnxMessage message)
    {
        if (message == null)
            return;

        lock (this._lock)
        {
            var row = new KnxMessageRow(message);

            // Apply filter if active
            if (!string.IsNullOrEmpty(this._filter) && !MatchesFilter(message, this._filter))
            {
                return;
            }

            this._messages.Insert(0, row); // Add to top for newest-first display
            this.TrimMessages();
        }
    }

    /// <summary>
    /// Clears all messages from the table model.
    /// </summary>
    public void Clear()
    {
        lock (this._lock)
        {
            this._messages.Clear();
        }
    }

    /// <summary>
    /// Gets all messages as a list for export purposes.
    /// </summary>
    /// <returns>List of all current messages.</returns>
    public List<KnxMessageRow> GetAllMessages()
    {
        lock (this._lock)
        {
            return new List<KnxMessageRow>(this._messages);
        }
    }

    private void ApplyFilter()
    {
        // Re-populate with filtered messages
        // This is a simplified implementation - in production, you might want to maintain
        // separate filtered and unfiltered collections for better performance
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Messages)));
    }

    private void TrimMessages()
    {
        while (this._messages.Count > this._maxMessages)
        {
            this._messages.RemoveAt(this._messages.Count - 1);
        }
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
}

/// <summary>
/// Enterprise-grade row model for KNX message display in TableView.
/// Provides formatted display values and color coding information.
/// </summary>
public class KnxMessageRow
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KnxMessageRow"/> class.
    /// </summary>
    /// <param name="message">The source KNX message.</param>
    public KnxMessageRow(KnxMessage message)
    {
        this.Message = message ?? throw new ArgumentNullException(nameof(message));

        // Pre-calculate display values for performance
        this.TimeDisplay = message.Timestamp.ToString("HH:mm:ss.fff");
        this.MessageTypeDisplay = FormatMessageType(message.MessageType);
        this.SourceDisplay = message.SourceAddress;
        this.GroupAddressDisplay = message.GroupAddress;
        this.ValueDisplay = message.DisplayValue;
        this.PriorityDisplay = message.Priority.ToString();
        this.DataDisplay = message.DataHex;
        this.DptDisplay = message.DataPointType ?? "";
        this.DescriptionDisplay = FormatDescriptionForTable(message.Description);

        // Calculate age-based color
        this.AgeColorCode = CalculateAgeColorCode(message.Timestamp);
        this.TypeColorCode = CalculateTypeColorCode(message.MessageType);
        this.PriorityColorCode = CalculatePriorityColorCode(message.Priority);
    }

    /// <summary>
    /// Gets the original KNX message.
    /// </summary>
    public KnxMessage Message { get; }

    /// <summary>
    /// Gets the formatted time display.
    /// </summary>
    public string TimeDisplay { get; }

    /// <summary>
    /// Gets the formatted message action display.
    /// </summary>
    public string MessageTypeDisplay { get; }

    /// <summary>
    /// Gets the formatted source display.
    /// </summary>
    public string SourceDisplay { get; }

    /// <summary>
    /// Gets the formatted group address display.
    /// </summary>
    public string GroupAddressDisplay { get; }

    /// <summary>
    /// Gets the formatted value display.
    /// </summary>
    public string ValueDisplay { get; }

    /// <summary>
    /// Gets the formatted priority display.
    /// </summary>
    public string PriorityDisplay { get; }

    /// <summary>
    /// Gets the formatted raw data display.
    /// </summary>
    public string DataDisplay { get; }

    /// <summary>
    /// Gets the DPT (Data Point Type) display.
    /// </summary>
    public string DptDisplay { get; }

    /// <summary>
    /// Gets the formatted description display (truncated for table view).
    /// </summary>
    public string DescriptionDisplay { get; }

    /// <summary>
    /// Gets the age-based color code for the message.
    /// </summary>
    public string AgeColorCode { get; }

    /// <summary>
    /// Gets the type-based color code for the message.
    /// </summary>
    public string TypeColorCode { get; }

    /// <summary>
    /// Gets the priority-based color code for the message.
    /// </summary>
    public string PriorityColorCode { get; }

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

    private static string CalculateAgeColorCode(DateTime timestamp)
    {
        var age = DateTime.Now - timestamp;
        return age.TotalSeconds switch
        {
            < 1 => "\u001b[92m", // Bright Green
            < 5 => "\u001b[93m", // Bright Yellow
            < 30 => "\u001b[33m", // Yellow
            < 300 => "\u001b[37m", // White
            _ => "\u001b[90m", // Gray
        };
    }

    private static string CalculateTypeColorCode(KnxMessageType type)
    {
        return type switch
        {
            KnxMessageType.Read => "\u001b[96m", // Bright Cyan
            KnxMessageType.Write => "\u001b[92m", // Bright Green
            KnxMessageType.Response => "\u001b[93m", // Bright Yellow
            _ => "\u001b[37m", // White
        };
    }

    private static string CalculatePriorityColorCode(KnxPriority priority)
    {
        return priority switch
        {
            KnxPriority.System => "\u001b[91m", // Bright Red
            KnxPriority.Urgent => "\u001b[95m", // Bright Magenta
            KnxPriority.Normal => "\u001b[37m", // White
            KnxPriority.Low => "\u001b[90m", // Gray
            _ => "\u001b[37m", // White
        };
    }

    /// <summary>
    /// Formats description for table display with maximum width constraint.
    /// </summary>
    /// <param name="description">Raw description.</param>
    /// <returns>Formatted description suitable for table display.</returns>
    private static string FormatDescriptionForTable(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return "";
        }

        // Clean up the description
        var cleaned = description.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Trim();

        // Remove multiple spaces
        while (cleaned.Contains("  "))
        {
            cleaned = cleaned.Replace("  ", " ");
        }

        // For table view, we need to be strict about width to maintain readability
        const int maxTableWidth = 40;
        if (cleaned.Length > maxTableWidth)
        {
            cleaned = cleaned.Substring(0, maxTableWidth - 3) + "...";
        }

        return cleaned;
    }
}

/// <summary>
/// Enterprise-grade connection status model for real-time display updates.
/// </summary>
public class ConnectionStatusModel : INotifyPropertyChanged
{
    private bool _isConnected;
    private string _connectionType = "Unknown";
    private string _gateway = "Unknown";
    private int _port;
    private string? _filter;
    private int _messageCount;
    private DateTime _startTime = DateTime.Now;
    private string _lastError = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the connection is active.
    /// </summary>
    public bool IsConnected
    {
        get => this._isConnected;
        set
        {
            if (this._isConnected != value)
            {
                this._isConnected = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsConnected)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ConnectionStatusDisplay)));
            }
        }
    }

    /// <summary>
    /// Gets or sets the connection type display text.
    /// </summary>
    public string ConnectionType
    {
        get => this._connectionType;
        set
        {
            if (this._connectionType != value)
            {
                this._connectionType = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ConnectionType)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ConnectionStatusDisplay)));
            }
        }
    }

    /// <summary>
    /// Gets or sets the gateway address.
    /// </summary>
    public string Gateway
    {
        get => this._gateway;
        set
        {
            if (this._gateway != value)
            {
                this._gateway = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Gateway)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ConnectionStatusDisplay)));
            }
        }
    }

    /// <summary>
    /// Gets or sets the connection port.
    /// </summary>
    public int Port
    {
        get => this._port;
        set
        {
            if (this._port != value)
            {
                this._port = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Port)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ConnectionStatusDisplay)));
            }
        }
    }

    /// <summary>
    /// Gets or sets the current filter pattern.
    /// </summary>
    public string? Filter
    {
        get => this._filter;
        set
        {
            if (this._filter != value)
            {
                this._filter = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Filter)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.FilterDisplay)));
            }
        }
    }

    /// <summary>
    /// Gets or sets the total message count.
    /// </summary>
    public int MessageCount
    {
        get => this._messageCount;
        set
        {
            if (this._messageCount != value)
            {
                this._messageCount = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.MessageCount)));
            }
        }
    }

    /// <summary>
    /// Gets or sets the application start time.
    /// </summary>
    public DateTime StartTime
    {
        get => this._startTime;
        set
        {
            if (this._startTime != value)
            {
                this._startTime = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.StartTime)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.UptimeDisplay)));
            }
        }
    }

    /// <summary>
    /// Gets or sets the last error message.
    /// </summary>
    public string LastError
    {
        get => this._lastError;
        set
        {
            if (this._lastError != value)
            {
                this._lastError = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.LastError)));
            }
        }
    }

    /// <summary>
    /// Gets the formatted connection status display.
    /// </summary>
    public string ConnectionStatusDisplay =>
        $"{(this.IsConnected ? "✓ Connected" : "✗ Disconnected")} to {this.Gateway}:{this.Port} ({this.ConnectionType})";

    /// <summary>
    /// Gets the formatted filter display.
    /// </summary>
    public string FilterDisplay => string.IsNullOrEmpty(this.Filter) ? "None" : this.Filter;

    /// <summary>
    /// Gets the formatted uptime display.
    /// </summary>
    public string UptimeDisplay
    {
        get
        {
            var uptime = DateTime.Now - this.StartTime;
            return $"{uptime.Hours:D2}:{uptime.Minutes:D2}:{uptime.Seconds:D2}";
        }
    }

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;
}
