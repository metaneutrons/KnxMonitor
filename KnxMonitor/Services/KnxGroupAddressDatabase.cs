using System.Globalization;
using Microsoft.Extensions.Logging;

namespace KnxMonitor.Services;

/// <summary>
/// Database for KNX group addresses loaded from ETS CSV export.
/// Supports ETS export format "3/1" with semicolon separation.
/// </summary>
public partial class KnxGroupAddressDatabase
{
    private readonly Dictionary<string, GroupAddressInfo> _groupAddresses = new();
    private readonly ILogger<KnxGroupAddressDatabase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KnxGroupAddressDatabase"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public KnxGroupAddressDatabase(ILogger<KnxGroupAddressDatabase> logger)
    {
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Loads group addresses from ETS CSV export file.
    /// </summary>
    /// <param name="csvFilePath">Path to the CSV file.</param>
    /// <returns>Task representing the async operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when CSV format is invalid.</exception>
    public async Task LoadFromCsvAsync(string csvFilePath)
    {
        if (!File.Exists(csvFilePath))
        {
            throw new FileNotFoundException($"CSV file not found: {csvFilePath}");
        }

        // Read with Latin1 encoding (ISO-8859-1) as used by ETS exports
        var encoding = System.Text.Encoding.GetEncoding("ISO-8859-1");
        var fileContent = await File.ReadAllTextAsync(csvFilePath, encoding);
        var lines = SplitCsvIntoLogicalLines(fileContent);

        if (lines.Length == 0)
        {
            throw new InvalidOperationException("CSV file is empty");
        }

        // Parse header to validate format
        var header = lines[0];
        if (!IsValidEtsFormat(header))
        {
            throw new InvalidOperationException(
                "Invalid CSV format. Expected ETS export format '3/1' with semicolon separation. "
                    + "Header should contain: Main;Middle;Sub;Address;Central;Unfiltered;Description;DatapointType;Security"
            );
        }

        // Parse data rows
        var loadedCount = 0;
        for (int i = 1; i < lines.Length; i++)
        {
            try
            {
                var groupAddressInfo = ParseCsvLine(lines[i], i + 1);
                if (groupAddressInfo != null)
                {
                    _groupAddresses[groupAddressInfo.Address] = groupAddressInfo;
                    loadedCount++;
                }
            }
            catch (Exception ex)
            {
                // Log the error but continue processing other lines
                this.LogCsvParsingError(ex, i + 1, ex.Message);
            }
        }

        this.LogGroupAddressesLoaded(loadedCount, csvFilePath);
        IsCsvLoaded = true;
    }

    /// <summary>
    /// Gets group address information for the specified address.
    /// </summary>
    /// <param name="groupAddress">Group address (e.g., "1/2/3").</param>
    /// <returns>Group address info if found, null otherwise.</returns>
    public GroupAddressInfo? GetGroupAddressInfo(string groupAddress)
    {
        _groupAddresses.TryGetValue(groupAddress, out var info);
        return info;
    }

    /// <summary>
    /// Gets the count of loaded group addresses.
    /// </summary>
    public int Count => _groupAddresses.Count;

    /// <summary>
    /// Gets a value indicating whether a CSV file has been successfully loaded.
    /// </summary>
    public bool IsCsvLoaded { get; private set; }

    /// <summary>
    /// Splits CSV content into logical lines, handling multi-line quoted fields.
    /// </summary>
    /// <param name="csvContent">The entire CSV file content.</param>
    /// <returns>Array of logical CSV lines.</returns>
    private static string[] SplitCsvIntoLogicalLines(string csvContent)
    {
        var lines = new List<string>();
        var currentLine = new System.Text.StringBuilder();
        var inQuotes = false;
        var i = 0;

        while (i < csvContent.Length)
        {
            var c = csvContent[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
                currentLine.Append(c);
            }
            else if (c == '\n' && !inQuotes)
            {
                // End of logical line
                var line = currentLine.ToString().TrimEnd('\r');
                if (!string.IsNullOrWhiteSpace(line))
                {
                    lines.Add(line);
                }
                currentLine.Clear();
            }
            else if (c == '\r' && !inQuotes)
            {
                // Skip \r when not in quotes (will be handled with \n)
            }
            else
            {
                currentLine.Append(c);
            }

            i++;
        }

        // Add the last line if it exists
        var lastLine = currentLine.ToString().TrimEnd('\r');
        if (!string.IsNullOrWhiteSpace(lastLine))
        {
            lines.Add(lastLine);
        }

        return lines.ToArray();
    }

    private static bool IsValidEtsFormat(string header)
    {
        // Check for semicolon separation and expected columns
        var columns = header.Split(';');
        if (columns.Length < 9)
            return false;

        // Check for expected column names (case-insensitive, quotes optional)
        var expectedColumns = new[]
        {
            "main",
            "middle",
            "sub",
            "address",
            "central",
            "unfiltered",
            "description",
            "datapointtype",
            "security",
        };
        for (int i = 0; i < expectedColumns.Length; i++)
        {
            var column = columns[i].Trim('"').ToLowerInvariant();
            if (!column.Contains(expectedColumns[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static GroupAddressInfo? ParseCsvLine(string line, int lineNumber)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        var columns = ParseCsvColumns(line);
        if (columns.Length < 4) // Need at least Main, Middle, Sub, Address
        {
            return null; // Skip lines with too few columns
        }

        // Extract values with safe indexing
        var main = columns.Length > 0 ? columns[0].Trim() : "";
        var middle = columns.Length > 1 ? columns[1].Trim() : "";
        var sub = columns.Length > 2 ? columns[2].Trim() : "";
        var address = columns.Length > 3 ? columns[3].Trim() : "";
        var description = columns.Length > 6 ? columns[6].Trim() : "";
        var datapointType = columns.Length > 7 ? columns[7].Trim() : "";

        // Skip rows without valid group address
        if (string.IsNullOrEmpty(address) || !address.Contains('/'))
        {
            return null;
        }

        // Validate group address format (x/y/z)
        if (!IsValidGroupAddress(address))
        {
            return null; // Skip invalid addresses silently
        }

        return new GroupAddressInfo
        {
            Address = address,
            Main = main,
            Middle = middle,
            Sub = sub,
            Description = description,
            DatapointType = datapointType,
        };
    }

    private static string[] ParseCsvColumns(string line)
    {
        var columns = new List<string>();
        var currentColumn = new System.Text.StringBuilder();
        var inQuotes = false;
        var i = 0;

        while (i < line.Length)
        {
            var c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ';' && !inQuotes)
            {
                columns.Add(currentColumn.ToString());
                currentColumn.Clear();
            }
            else
            {
                currentColumn.Append(c);
            }

            i++;
        }

        // Add the last column
        columns.Add(currentColumn.ToString());

        return columns.ToArray();
    }

    private static bool IsValidGroupAddress(string address)
    {
        var parts = address.Split('/');
        if (parts.Length != 3)
            return false;

        // Check if all parts are numeric
        return parts.All(part => int.TryParse(part, out var value) && value >= 0);
    }
}

/// <summary>
/// Information about a KNX group address from ETS export.
/// </summary>
public class GroupAddressInfo
{
    /// <summary>
    /// Gets or sets the group address (e.g., "1/2/3").
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the main group name.
    /// </summary>
    public string Main { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the middle group name.
    /// </summary>
    public string Middle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sub group name.
    /// </summary>
    public string Sub { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the datapoint type (e.g., "DPST-1-1").
    /// </summary>
    public string DatapointType { get; set; } = string.Empty;

    /// <summary>
    /// Gets a display name for the group address.
    /// </summary>
    public string DisplayName
    {
        get
        {
            var parts = new[] { Main, Middle, Sub }.Where(p => !string.IsNullOrWhiteSpace(p));
            var name = string.Join(" / ", parts);
            return string.IsNullOrEmpty(name) ? Address : name;
        }
    }
}
