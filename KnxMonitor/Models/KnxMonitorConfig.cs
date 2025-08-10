namespace KnxMonitor.Models;

/// <summary>
/// Configuration for the KNX monitor.
/// </summary>
public class KnxMonitorConfig
{
    /// <summary>
    /// Gets or sets the KNX connection type.
    /// </summary>
    public KnxConnectionType ConnectionType { get; set; } = KnxConnectionType.Tunnel;

    /// <summary>
    /// Gets or sets the KNX gateway address (required for tunnel connections).
    /// </summary>
    public string? Gateway { get; set; }

    /// <summary>
    /// Gets or sets the multicast address for router connections (default: 224.0.23.12).
    /// </summary>
    public string MulticastAddress { get; set; } = "224.0.23.12";

    /// <summary>
    /// Gets or sets the port number (default: 3671).
    /// </summary>
    public int Port { get; set; } = 3671;

    /// <summary>
    /// Gets or sets a value indicating whether verbose logging is enabled.
    /// </summary>
    public bool Verbose { get; set; }

    /// <summary>
    /// Gets or sets the group address filter pattern.
    /// </summary>
    public string? Filter { get; set; }

    /// <summary>
    /// Gets or sets the path to the KNX group address CSV file exported from ETS.
    /// </summary>
    public string? GroupAddressCsvPath { get; set; }
}

/// <summary>
/// KNX connection types supported by the monitor.
/// </summary>
public enum KnxConnectionType
{
    /// <summary>
    /// IP Tunneling connection.
    /// </summary>
    Tunnel,

    /// <summary>
    /// IP Routing connection.
    /// </summary>
    Router,

    /// <summary>
    /// USB connection.
    /// </summary>
    Usb,
}
