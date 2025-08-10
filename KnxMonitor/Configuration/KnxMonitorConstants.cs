namespace KnxMonitor.Configuration;

/// <summary>
/// Constants for KNX Monitor configuration and validation.
/// Provides enterprise-grade configuration boundaries and limits.
/// </summary>
public static class KnxMonitorConstants
{
    /// <summary>
    /// Network configuration constants.
    /// </summary>
    public static class Network
    {
        /// <summary>
        /// Minimum valid port number.
        /// </summary>
        public const int MinPort = 1;

        /// <summary>
        /// Maximum valid port number.
        /// </summary>
        public const int MaxPort = 65535;

        /// <summary>
        /// Default health check port.
        /// </summary>
        public const int DefaultHealthCheckPort = 8080;

        /// <summary>
        /// Default KNX IP port.
        /// </summary>
        public const int DefaultKnxPort = 3671;

        /// <summary>
        /// Maximum connection retry attempts.
        /// </summary>
        public const int MaxRetryAttempts = 4;

        /// <summary>
        /// Base retry delay in seconds.
        /// </summary>
        public const int BaseRetryDelaySeconds = 2;
    }

    /// <summary>
    /// File system configuration constants.
    /// </summary>
    public static class FileSystem
    {
        /// <summary>
        /// Maximum CSV file size in bytes (10 MB).
        /// </summary>
        public const long MaxCsvFileSizeBytes = 10 * 1024 * 1024;

        /// <summary>
        /// Maximum number of group addresses in CSV.
        /// </summary>
        public const int MaxGroupAddresses = 100000;

        /// <summary>
        /// Supported CSV encodings.
        /// </summary>
        public static readonly string[] SupportedEncodings = { "UTF-8", "Latin1", "ASCII" };
    }

    /// <summary>
    /// KNX protocol configuration constants.
    /// </summary>
    public static class Knx
    {
        /// <summary>
        /// Maximum KNX data length in bytes.
        /// </summary>
        public const int MaxDataLength = 14;

        /// <summary>
        /// Valid KNX connection types.
        /// </summary>
        public static readonly string[] ValidConnectionTypes = { "tunneling", "routing", "usb" };

        /// <summary>
        /// Maximum group address value (15/7/255).
        /// </summary>
        public const int MaxGroupAddressMain = 15;
        public const int MaxGroupAddressMiddle = 7;
        public const int MaxGroupAddressSub = 255;
    }

    /// <summary>
    /// Performance and resource limits.
    /// </summary>
    public static class Performance
    {
        /// <summary>
        /// Maximum message queue size.
        /// </summary>
        public const int MaxMessageQueueSize = 10000;

        /// <summary>
        /// Message processing timeout in milliseconds.
        /// </summary>
        public const int MessageProcessingTimeoutMs = 5000;

        /// <summary>
        /// Maximum concurrent connections.
        /// </summary>
        public const int MaxConcurrentConnections = 100;

        /// <summary>
        /// Default buffer size for network operations.
        /// </summary>
        public const int DefaultBufferSize = 8192;
    }

    /// <summary>
    /// Logging configuration constants.
    /// </summary>
    public static class Logging
    {
        /// <summary>
        /// Maximum log message length.
        /// </summary>
        public const int MaxLogMessageLength = 4096;

        /// <summary>
        /// Default log retention days.
        /// </summary>
        public const int DefaultLogRetentionDays = 30;

        /// <summary>
        /// Maximum log file size in bytes (100 MB).
        /// </summary>
        public const long MaxLogFileSizeBytes = 100 * 1024 * 1024;
    }
}
