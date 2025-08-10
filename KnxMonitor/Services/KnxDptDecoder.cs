using Knx.Falcon;
using Knx.Falcon.ApplicationData.DatapointTypes;
using Knx.Falcon.ApplicationData.MasterData;
using Microsoft.Extensions.Logging;

namespace KnxMonitor.Services;

/// <summary>
/// KNX DPT decoder using Falcon SDK's built-in decoding functionality.
/// </summary>
public partial class KnxDptDecoder
{
    private readonly Dictionary<string, DatapointSubtype> _dptCache = new();
    private readonly ILogger<KnxDptDecoder> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KnxDptDecoder"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public KnxDptDecoder(ILogger<KnxDptDecoder> logger)
    {
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Decodes a GroupValue using the specified DPT.
    /// </summary>
    /// <param name="groupValue">The GroupValue to decode.</param>
    /// <param name="dptString">The DPT string (e.g., "DPST-1-1").</param>
    /// <returns>Decoded value or null if decoding failed.</returns>
    public object? DecodeValue(GroupValue groupValue, string dptString)
    {
        if (groupValue == null || string.IsNullOrEmpty(dptString))
        {
            return null;
        }

        try
        {
            // Get or create DPT converter
            var datapointSubtype = GetDatapointSubtype(dptString);
            if (datapointSubtype == null)
            {
                return null;
            }

            // Create DPT converter using Falcon SDK
            var dptConverter = DptFactory.Default.Create(datapointSubtype);
            if (dptConverter == null)
            {
                return null;
            }

            // Check if conversion is possible
            if (!dptConverter.CanConvertToValue(groupValue))
            {
                return null;
            }

            // Decode using Falcon SDK
            var decodedValue = dptConverter.ToValue(groupValue);
            return FormatDecodedValue(decodedValue, dptString);
        }
        catch (Exception ex)
        {
            this.LogDptDecodingError(ex, dptString, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Gets a DatapointSubtype from a DPT string.
    /// </summary>
    /// <param name="dptString">DPT string (e.g., "DPST-1-1").</param>
    /// <returns>DatapointSubtype or null if not found.</returns>
    private DatapointSubtype? GetDatapointSubtype(string dptString)
    {
        // Check cache first
        if (_dptCache.TryGetValue(dptString, out var cached))
        {
            return cached;
        }

        try
        {
            // Parse DPT string (e.g., "DPST-1-1" -> main=1, sub=1)
            var dptInfo = ParseDptString(dptString);
            if (dptInfo == null)
            {
                _dptCache[dptString] = null!;
                return null;
            }

            // Find the datapoint subtype
            var datapointSubtype = FindDatapointSubtype(dptInfo.Value.mainType, dptInfo.Value.subType);

            // Cache the result (even if null)
            _dptCache[dptString] = datapointSubtype!;

            return datapointSubtype;
        }
        catch (Exception ex)
        {
            this.LogErrorParsingDpt(ex, dptString, ex.Message);
            _dptCache[dptString] = null!;
            return null;
        }
    }

    /// <summary>
    /// Parses a DPT string into main and sub type numbers.
    /// </summary>
    /// <param name="dptString">DPT string (e.g., "DPST-1-1").</param>
    /// <returns>Tuple of main and sub type numbers, or null if parsing failed.</returns>
    private static (int mainType, int subType)? ParseDptString(string dptString)
    {
        if (string.IsNullOrEmpty(dptString))
        {
            return null;
        }

        // Handle formats: "DPST-1-1", "DPT-1-1", "1.001", "1-1"
        var normalized = dptString.ToUpperInvariant().Replace("DPST-", "").Replace("DPT-", "").Replace(".", "-");

        var parts = normalized.Split('-');
        if (parts.Length >= 2 && int.TryParse(parts[0], out var mainType) && int.TryParse(parts[1], out var subType))
        {
            return (mainType, subType);
        }

        return null;
    }

    /// <summary>
    /// Finds a DatapointSubtype by main and sub type numbers.
    /// </summary>
    /// <param name="mainType">Main type number (e.g., 1).</param>
    /// <param name="subType">Sub type number (e.g., 1).</param>
    /// <returns>DatapointSubtype or null if not found.</returns>
    private DatapointSubtype? FindDatapointSubtype(int mainType, int subType)
    {
        try
        {
            // Use Falcon SDK's DptFactory to get all datapoint types
            var allDatapointTypes = DptFactory.Default.AllDatapointTypes;

            var datapointType = allDatapointTypes.FirstOrDefault(dt => dt.MainTypeNumber == mainType);
            if (datapointType == null)
            {
                return null;
            }

            var datapointSubtype = datapointType.SubTypes.FirstOrDefault(dst => dst.SubTypeNumber == subType);
            return datapointSubtype;
        }
        catch (Exception ex)
        {
            this.LogErrorFindingDatapointSubtype(ex, mainType, subType, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Formats the decoded value for display.
    /// </summary>
    /// <param name="decodedValue">The decoded value from Falcon SDK.</param>
    /// <param name="dptString">The DPT string for context.</param>
    /// <returns>Formatted string representation.</returns>
    private static object FormatDecodedValue(object decodedValue, string dptString)
    {
        if (decodedValue == null)
        {
            return "null";
        }

        // Return the decoded value as-is - Falcon SDK should give us the proper type
        return decodedValue switch
        {
            bool b => b ? "true" : "false",
            float f => Math.Round(f, 2).ToString("F2"),
            double d => Math.Round(d, 2).ToString("F2"),
            decimal dec => Math.Round(dec, 2).ToString("F2"),
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
            TimeSpan ts => ts.ToString(@"hh\:mm\:ss"),
            string s => s,
            byte by => by.ToString(),
            int i => i.ToString(),
            _ => decodedValue.ToString() ?? "unknown",
        };
    }
}
