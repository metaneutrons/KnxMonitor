namespace KnxMonitor.Models;

/// <summary>
/// Represents a KNX bus message for display purposes.
/// Simplified to work directly with Falcon SDK decoded values - no DPT guessing rubbish.
/// </summary>
public class KnxMessage
{
    /// <summary>
    /// Gets or sets the timestamp when the message was received.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the source address of the message.
    /// </summary>
    public string SourceAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination group address.
    /// </summary>
    public string GroupAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message type (Read, Write, Response).
    /// </summary>
    public KnxMessageType MessageType { get; set; }

    /// <summary>
    /// Gets or sets the raw data payload.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the interpreted value from Falcon SDK.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the data point type (if known from Falcon SDK).
    /// </summary>
    public string? DataPointType { get; set; }

    /// <summary>
    /// Gets or sets the description from the group address database.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the message priority.
    /// </summary>
    public KnxPriority Priority { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this message was repeated.
    /// </summary>
    public bool IsRepeated { get; set; }

    /// <summary>
    /// Gets the data as a hexadecimal string.
    /// </summary>
    public string DataHex => Convert.ToHexString(this.Data);

    /// <summary>
    /// Gets a formatted display value using Falcon SDK decoded value.
    /// </summary>
    public string DisplayValue
    {
        get
        {
            // Use Falcon SDK decoded value if available
            if (this.Value != null)
            {
                return FormatFalconValue(this.Value);
            }

            // Fallback: show raw data
            if (this.Data.Length == 0)
            {
                return "Empty";
            }

            return $"Raw: {this.DataHex}";
        }
    }

    private static string FormatFalconValue(object value)
    {
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
}

/// <summary>
/// KNX message types.
/// </summary>
public enum KnxMessageType
{
    /// <summary>
    /// Read request.
    /// </summary>
    Read,

    /// <summary>
    /// Write request.
    /// </summary>
    Write,

    /// <summary>
    /// Response to read request.
    /// </summary>
    Response,
}

/// <summary>
/// KNX message priorities.
/// </summary>
public enum KnxPriority
{
    /// <summary>
    /// System priority.
    /// </summary>
    System,

    /// <summary>
    /// Normal priority.
    /// </summary>
    Normal,

    /// <summary>
    /// Urgent priority.
    /// </summary>
    Urgent,

    /// <summary>
    /// Low priority.
    /// </summary>
    Low,
}
