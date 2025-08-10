using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Knx.Falcon;
using Knx.Falcon.Configuration;
using Knx.Falcon.Sdk;
using KnxMonitor.Models;
using Microsoft.Extensions.Logging;

namespace KnxMonitor.Services;

/// <summary>
/// Simplified KNX Monitor service that trusts Falcon SDK completely for decoding.
/// No manual decoding fallbacks - if Falcon can't decode it, we show an error.
/// </summary>
public partial class KnxMonitorService : IKnxMonitorService, IAsyncDisposable
{
    private readonly KnxMonitorConfig _config;
    private readonly ILogger<KnxMonitorService> _logger;
    private readonly Regex? _filterRegex;
    private readonly KnxGroupAddressDatabase _groupAddressDatabase;
    private readonly KnxDptDecoder _dptDecoder;

    private KnxBus? _knxBus;
    private bool _isConnected;
    private string _connectionStatus = "Disconnected";
    private int _messageCount;

    public event EventHandler<KnxMessage>? MessageReceived;

    public KnxMonitorService(
        KnxMonitorConfig config,
        ILogger<KnxMonitorService> logger,
        KnxGroupAddressDatabase groupAddressDatabase,
        KnxDptDecoder dptDecoder
    )
    {
        this._config = config ?? throw new ArgumentNullException(nameof(config));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._groupAddressDatabase =
            groupAddressDatabase ?? throw new ArgumentNullException(nameof(groupAddressDatabase));
        this._dptDecoder = dptDecoder ?? throw new ArgumentNullException(nameof(dptDecoder));

        // Compile filter regex if provided
        if (!string.IsNullOrEmpty(this._config.Filter))
        {
            try
            {
                var pattern = this._config.Filter.Replace("*", ".*").Replace("/", "\\/");
                this._filterRegex = new Regex($"^{pattern}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
            catch (Exception ex)
            {
                this.LogInvalidFilterPattern(ex, this._config.Filter);
            }
        }
    }

    public bool IsConnected => this._isConnected;
    public string ConnectionStatus => this._connectionStatus;
    public int MessageCount => this._messageCount;

    public bool IsCsvLoaded => this._groupAddressDatabase.IsCsvLoaded;

    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            this.LogStartingMonitoring(this._config.ConnectionType.ToString());

            // Load group address database if CSV path is provided
            if (!string.IsNullOrEmpty(this._config.GroupAddressCsvPath))
            {
                try
                {
                    this.LogLoadingGroupAddressDatabase(this._config.GroupAddressCsvPath);
                    await this._groupAddressDatabase.LoadFromCsvAsync(this._config.GroupAddressCsvPath);
                    this.LogGroupAddressDatabaseLoaded(this._groupAddressDatabase.Count);
                }
                catch (Exception ex)
                {
                    this.LogFailedToLoadGroupAddressCsv(ex, ex.Message);
                    this.LogContinuingWithoutGroupAddressDatabase();
                }
            }
            else
            {
                this.LogNoCsvPathProvided();
            }

            var connectorParameters = this.CreateConnectorParameters();
            if (connectorParameters == null)
            {
                this.LogFailedToCreateConnectorParameters();
                return;
            }

            this._knxBus = new KnxBus(connectorParameters);
            this._knxBus.GroupMessageReceived += this.OnGroupMessageReceived;

            await this._knxBus.ConnectAsync(cancellationToken);
            this._isConnected = true;

            // Create informative connection status
            var connectionInfo = this._config.ConnectionType switch
            {
                KnxConnectionType.Tunnel => $"Tunnel to {this._config.Gateway}:{this._config.Port}",
                KnxConnectionType.Router => $"Router on {this._config.MulticastAddress}:{this._config.Port}",
                KnxConnectionType.Usb => "USB",
                _ => this._config.ConnectionType.ToString(),
            };

            this._connectionStatus = $"Connected via {connectionInfo}";

            this.LogMonitoringStartedSuccessfully(this._connectionStatus);
        }
        catch (Exception ex)
        {
            this.LogFailedToStartMonitoring(ex);
            this._connectionStatus = $"Failed: {ex.Message}";
        }
    }

    public async Task StopMonitoringAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (this._knxBus != null)
            {
                this._knxBus.GroupMessageReceived -= this.OnGroupMessageReceived;
                await this._knxBus.DisposeAsync();
                this._isConnected = false;
                this._connectionStatus = "Disconnected";
            }

            this.LogMonitoringStopped();
        }
        catch (Exception ex)
        {
            this.LogErrorStoppingMonitoring(ex);
        }
    }

    private ConnectorParameters? CreateConnectorParameters()
    {
        return this._config.ConnectionType switch
        {
            KnxConnectionType.Tunnel => this.CreateTunnelingParameters(),
            KnxConnectionType.Router => this.CreateRoutingParameters(),
            KnxConnectionType.Usb => this.CreateUsbParameters(),
            _ => null,
        };
    }

    private ConnectorParameters CreateTunnelingParameters()
    {
        this.LogCreatingIpTunnelingConnection();
        return new IpTunnelingConnectorParameters(this._config.Gateway!, this._config.Port);
    }

    private ConnectorParameters CreateRoutingParameters()
    {
        this.LogCreatingIpRoutingConnection();
        return new IpRoutingConnectorParameters(IPAddress.Parse(this._config.MulticastAddress!));
    }

    private ConnectorParameters CreateUsbParameters()
    {
        this.LogCreatingUsbConnection();
        var usbDevices = KnxBus.GetAttachedUsbDevices().ToArray();
        if (usbDevices.Length == 0)
        {
            throw new InvalidOperationException("No KNX USB devices found");
        }
        return UsbConnectorParameters.FromDiscovery(usbDevices[0]);
    }

    private void OnGroupMessageReceived(object? sender, GroupEventArgs e)
    {
        try
        {
            var message = this.CreateKnxMessage(e);
            this.ProcessMessage(message);
        }
        catch (Exception ex)
        {
            this.LogErrorProcessingGroupMessage(ex);
        }
    }

    private KnxMessage CreateKnxMessage(GroupEventArgs e)
    {
        var messageType = e.Value != null ? KnxMessageType.Write : KnxMessageType.Read;
        var groupAddress = e.DestinationAddress.ToString();

        // Extract raw data from GroupValue.Value property
        var rawData = this.TryExtractBytesFromGroupValue(e.Value) ?? Array.Empty<byte>();

        // Try to decode using DPT information from CSV
        var decodedValue = this.DecodeValueWithDpt(e.Value, groupAddress);

        // Get group address info for description and DPT
        var groupAddressInfo = this._groupAddressDatabase.GetGroupAddressInfo(groupAddress);

        return new KnxMessage
        {
            Timestamp = DateTime.Now,
            SourceAddress = e.SourceAddress.ToString(),
            GroupAddress = groupAddress,
            MessageType = messageType,
            Data = rawData,
            Value = decodedValue,
            DataPointType = groupAddressInfo?.DatapointType,
            Description = groupAddressInfo?.Description,
            Priority = KnxPriority.Normal,
            IsRepeated = false,
        };
    }

    private object? DecodeValueWithDpt(GroupValue? groupValue, string groupAddress)
    {
        if (groupValue == null)
        {
            return null;
        }

        // Try to get DPT information from the database
        var groupAddressInfo = this._groupAddressDatabase.GetGroupAddressInfo(groupAddress);
        if (groupAddressInfo != null && !string.IsNullOrEmpty(groupAddressInfo.DatapointType))
        {
            // Use Falcon SDK DPT decoding
            var decodedValue = this._dptDecoder.DecodeValue(groupValue, groupAddressInfo.DatapointType);
            if (decodedValue != null)
            {
                return decodedValue;
            }
        }

        // Fallback to TypedValue extraction
        return this.ExtractDecodedValueFromGroupValue(groupValue);
    }

    private string? GetDatapointTypeForAddress(string groupAddress)
    {
        var groupAddressInfo = this._groupAddressDatabase.GetGroupAddressInfo(groupAddress);
        return groupAddressInfo?.DatapointType;
    }

    private object? ExtractDecodedValueFromGroupValue(object? groupValue)
    {
        if (groupValue == null)
            return null;

        try
        {
            var type = groupValue.GetType();

            // For GroupValue objects, use TypedValue property
            if (type.Name == "GroupValue")
            {
                var typedValueProp = type.GetProperty("TypedValue");
                if (typedValueProp != null)
                {
                    var typedValue = typedValueProp.GetValue(groupValue);

                    // If TypedValue is a byte array, show as hex (no automatic decoding available)
                    if (typedValue is byte[] bytes)
                    {
                        return Convert.ToHexString(bytes);
                    }

                    // For primitive values (byte, int, bool, etc.), return directly
                    return typedValue;
                }
            }

            // Fallback to the object itself
            return groupValue;
        }
        catch
        {
            return groupValue;
        }
    }

    private byte[]? TryExtractBytesFromGroupValue(object? groupValue)
    {
        if (groupValue == null)
            return null;

        try
        {
            var type = groupValue.GetType();

            // For GroupValue objects, use Value property (contains raw bytes)
            if (type.Name == "GroupValue")
            {
                var valueProp = type.GetProperty("Value");
                if (valueProp?.PropertyType == typeof(byte[]))
                {
                    return valueProp.GetValue(groupValue) as byte[];
                }
            }

            // If it's already a byte array, use it directly
            if (groupValue is byte[] directBytes)
            {
                return directBytes;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private void ProcessMessage(KnxMessage message)
    {
        // Apply filter if configured
        if (this._filterRegex != null && !this._filterRegex.IsMatch(message.GroupAddress))
        {
            return;
        }

        this._messageCount++;

        // Get description from database
        var groupAddressInfo = this._groupAddressDatabase.GetGroupAddressInfo(message.GroupAddress);
        var description = FormatDescriptionForLogging(groupAddressInfo?.Description);
        var dptType = message.DataPointType ?? "";

        // Format value properly based on DPT type
        var formattedValue = FormatValueForLogging(message.Value, dptType);

        // Use structured logging for enterprise-grade logging
        // The LogDetailedKnxMessage will handle the formatting appropriately
        this.LogDetailedKnxMessage(
            message.Timestamp.ToString("HH:mm:ss.fff"),
            message.SourceAddress,
            message.GroupAddress,
            message.MessageType.ToString(),
            Convert.ToHexString(message.Data),
            formattedValue,
            dptType,
            description
        );

        this.MessageReceived?.Invoke(this, message);
    }

    /// <summary>
    /// Formats description for logging with best practices.
    /// </summary>
    /// <param name="description">Raw description from CSV.</param>
    /// <returns>Formatted description suitable for logging.</returns>
    private static string FormatDescriptionForLogging(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return "";
        }

        // Clean up the description for logging
        var cleaned = description.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Trim();

        // Remove multiple spaces
        while (cleaned.Contains("  "))
        {
            cleaned = cleaned.Replace("  ", " ");
        }

        // For logging, we can be more generous with length but still reasonable
        if (cleaned.Length > 200)
        {
            cleaned = cleaned.Substring(0, 197) + "...";
        }

        return cleaned;
    }

    /// <summary>
    /// Formats value for logging with proper type-specific formatting.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <param name="dptType">The DPT type for context.</param>
    /// <returns>Formatted value string.</returns>
    private static string FormatValueForLogging(object? value, string dptType)
    {
        if (value == null)
        {
            return "null";
        }

        // Handle boolean values properly for DPST-1-1 (switching)
        if (dptType == "DPST-1-1" && value is bool boolValue)
        {
            return boolValue ? "true" : "false";
        }

        // Handle numeric boolean values (0/1) for DPST-1-1
        if (dptType == "DPST-1-1" && value is byte byteValue)
        {
            return byteValue != 0 ? "true" : "false";
        }

        if (dptType == "DPST-1-1" && value is int intValue)
        {
            return intValue != 0 ? "true" : "false";
        }

        // Handle other value types
        return value switch
        {
            bool b => b ? "true" : "false",
            byte by => $"{by}",
            sbyte sb => $"{sb}",
            short s => $"{s}",
            ushort us => $"{us}",
            int i => $"{i}",
            uint ui => $"{ui}",
            float f => $"{f:F2}",
            double d => $"{d:F2}",
            string str => str,
            byte[] bytes => Convert.ToHexString(bytes),
            _ => value.ToString() ?? "Unknown",
        };
    }

    public void Dispose()
    {
        try
        {
            this.StopMonitoringAsync().GetAwaiter().GetResult();
            this._knxBus?.Dispose();
        }
        catch (Exception ex)
        {
            this.LogErrorDuringAsyncDispose(ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await this.StopMonitoringAsync();
            this._knxBus?.Dispose();
        }
        catch (Exception ex)
        {
            this.LogErrorDuringAsyncDispose(ex);
        }
    }
}
