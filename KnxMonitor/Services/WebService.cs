using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;
using KnxMonitor.Models;

namespace KnxMonitor.Services;

/// <summary>
/// Modern web service using Kestrel with full KNX monitoring functionality
/// </summary>
public class WebService
{
    private readonly ILogger<WebService> _logger;
    private readonly IKnxMonitorService _knx;
    private WebApplication? _app;

    // Message collection and filtering
    private readonly object _sync = new();
    private readonly List<KnxMessage> _messages = new(capacity: 2048);
    private string? _filter;

    public WebService(ILogger<WebService> logger, IKnxMonitorService knx)
    {
        _logger = logger;
        _knx = knx;
        _knx.MessageReceived += OnMessage;
    }

    public async Task StartAsync(IEnumerable<string> prefixes, string pathBase = "/", 
        bool healthEnabled = true, string healthPath = "/health", string readyPath = "/ready", 
        CancellationToken token = default)
    {
        // Extract port from first prefix
        var firstPrefix = prefixes.FirstOrDefault() ?? "http://+:8080/";
        var uri = new Uri(firstPrefix.Replace("+", "localhost"));
        var port = uri.Port;

        var builder = WebApplication.CreateBuilder();
        
        // Configure Kestrel to listen on all interfaces
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(port);
        });

        // Configure logging based on global verbose flag
        builder.Logging.ClearProviders().AddConsole();
        
        if (!Program.IsVerbose)
        {
            // In normal mode, suppress ASP.NET Core noise
            builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.None)
                           .AddFilter("Microsoft.AspNetCore.Hosting", LogLevel.None)
                           .AddFilter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.None);
        }
        // In verbose mode, show all Kestrel startup messages

        _app = builder.Build();

        // Configure path base if specified
        if (!string.IsNullOrEmpty(pathBase) && pathBase != "/")
        {
            _app.UsePathBase(pathBase);
        }

        // Health endpoints
        if (healthEnabled)
        {
            _app.MapGet(healthPath, () => Results.Json(new { 
                status = "healthy", 
                timestamp = DateTime.UtcNow,
                knx = new { 
                    connected = _knx.IsConnected, 
                    connectionStatus = _knx.ConnectionStatus, 
                    messagesReceived = _knx.MessageCount 
                }
            }));

            _app.MapGet(readyPath, () => {
                var ready = _knx.IsConnected;
                return Results.Json(new { 
                    ready, 
                    timestamp = DateTime.UtcNow 
                }, statusCode: ready ? 200 : 503);
            });
        }

        // KNX Monitoring API endpoints
        _app.MapGet("/api/status", () => Results.Json(new {
            connected = _knx.IsConnected,
            status = _knx.ConnectionStatus,
            count = _knx.MessageCount,
            filter = _filter,
        }));

        _app.MapGet("/api/messages", (HttpContext context) => {
            int take = ParseInt(context.Request.Query["take"], 500);
            string? filter = context.Request.Query["filter"].FirstOrDefault() ?? _filter;
            var items = GetMessagesSnapshot(filter, take);
            return Results.Json(items);
        });

        _app.MapPost("/api/filter", async (HttpContext context) => {
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();
            var payload = JsonSerializer.Deserialize<FilterPayload>(body);
            _filter = payload?.Filter;
            return Results.Json(new { success = true, filter = _filter });
        });

        _app.MapGet("/api/export", () => {
            var snapshot = GetMessagesSnapshot(_filter, 10_000);
            var csv = BuildCsv(snapshot);
            return Results.File(Encoding.UTF8.GetBytes(csv), "text/csv", "knx_messages.csv");
        });

        // Static files and web UI
        _app.MapGet("/", () => Results.Content(GetIndexHtml(), "text/html"));
        _app.MapGet("/styles.css", () => Results.Content(GetStylesCss(), "text/css"));
        _app.MapGet("/app.js", () => Results.Content(GetAppJs(), "application/javascript"));
        
        // Fallback to index for SPA routing
        _app.MapFallback(() => Results.Content(GetIndexHtml(), "text/html"));

        try
        {
            await _app.StartAsync(token);
            _logger.LogInformation("Web server started successfully on port {Port}", port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start web server");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken token = default)
    {
        if (_app != null)
        {
            _logger.LogInformation("Stopping web server...");
            await _app.StopAsync(token);
            await _app.DisposeAsync();
            _app = null;
        }
    }

    private void OnMessage(object? sender, KnxMessage m)
    {
        lock (_sync)
        {
            _messages.Add(m);
            if (_messages.Count > 50_000)
            {
                // Keep memory bounded
                _messages.RemoveRange(0, _messages.Count - 50_000);
            }
        }
    }

    private List<object> GetMessagesSnapshot(string? filter, int take)
    {
        List<KnxMessage> copy;
        lock (_sync)
        {
            // Take most recent first
            var start = Math.Max(0, _messages.Count - take);
            copy = _messages.Skip(start).ToList();
        }

        IEnumerable<KnxMessage> q = copy;
        if (!string.IsNullOrWhiteSpace(filter))
        {
            q = q.Where(m => MatchesFilter(m, filter));
        }

        return q.Select(m => new
        {
            timestamp = m.Timestamp.ToString("HH:mm:ss.fff"),
            type = m.MessageType.ToString(),
            source = m.SourceAddress,
            destination = m.GroupAddress,
            value = m.Value,
            raw = Convert.ToHexString(m.Data),
            priority = m.Priority.ToString(),
            description = m.Description
        }).ToList<object>();
    }

    private static bool MatchesFilter(KnxMessage m, string filter)
    {
        if (filter.EndsWith("*"))
        {
            var prefix = filter[..^1];
            return m.GroupAddress.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }
        return string.Equals(m.GroupAddress, filter, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildCsv(List<object> messages)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,Type,Source,Destination,Value,Raw,Priority,Description");
        
        foreach (dynamic m in messages)
        {
            sb.AppendLine($"{m.timestamp},{m.type},{m.source},{m.destination},\"{m.value}\",{m.raw},{m.priority},\"{m.description}\"");
        }
        
        return sb.ToString();
    }

    private static int ParseInt(string? value, int defaultValue)
    {
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    private string GetIndexHtml() => """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>KNX Monitor</title>
    <link rel="stylesheet" href="/styles.css">
</head>
<body>
    <div class="container">
        <header>
            <h1>🏠 KNX Monitor</h1>
            <div class="status" id="status">
                <span class="status-indicator" id="statusIndicator">⏳</span>
                <span id="statusText">Connecting...</span>
            </div>
        </header>

        <div class="controls">
            <div class="filter-group">
                <label for="filterInput">Filter:</label>
                <input type="text" id="filterInput" placeholder="e.g., 1/2/3 or 1/2/*" />
                <button id="applyFilter">Apply</button>
                <button id="clearFilter">Clear</button>
            </div>
            <div class="action-group">
                <button id="exportCsv">Export CSV</button>
                <button id="clearMessages">Clear</button>
                <label>
                    <input type="checkbox" id="autoScroll" checked> Auto-scroll
                </label>
            </div>
        </div>

        <div class="message-container">
            <table id="messageTable">
                <thead>
                    <tr>
                        <th>Time</th>
                        <th>Type</th>
                        <th>Source</th>
                        <th>Destination</th>
                        <th>Value</th>
                        <th>Raw</th>
                        <th>Priority</th>
                        <th>Description</th>
                    </tr>
                </thead>
                <tbody id="messageBody">
                </tbody>
            </table>
        </div>

        <div class="footer">
            <span id="messageCount">0 messages</span>
            <span>•</span>
            <span>Powered by ASP.NET Core Kestrel</span>
        </div>
    </div>

    <script src="/app.js"></script>
</body>
</html>
""";

    private string GetStylesCss() => """
* {
    margin: 0;
    padding: 0;
    box-sizing: border-box;
}

body {
    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
    background: #f5f7fa;
    color: #333;
    line-height: 1.6;
}

.container {
    max-width: 1400px;
    margin: 0 auto;
    padding: 20px;
}

header {
    background: white;
    padding: 20px;
    border-radius: 8px;
    box-shadow: 0 2px 10px rgba(0,0,0,0.1);
    margin-bottom: 20px;
    display: flex;
    justify-content: space-between;
    align-items: center;
}

h1 {
    color: #2c3e50;
    font-size: 1.8em;
}

.status {
    display: flex;
    align-items: center;
    gap: 8px;
    font-weight: 500;
}

.status-indicator {
    font-size: 1.2em;
}

.status.connected .status-indicator { color: #27ae60; }
.status.disconnected .status-indicator { color: #e74c3c; }
.status.loading .status-indicator { color: #f39c12; }

.controls {
    background: white;
    padding: 15px 20px;
    border-radius: 8px;
    box-shadow: 0 2px 10px rgba(0,0,0,0.1);
    margin-bottom: 20px;
    display: flex;
    justify-content: space-between;
    align-items: center;
    flex-wrap: wrap;
    gap: 15px;
}

.filter-group, .action-group {
    display: flex;
    align-items: center;
    gap: 10px;
}

input[type="text"] {
    padding: 8px 12px;
    border: 1px solid #ddd;
    border-radius: 4px;
    font-size: 14px;
    min-width: 200px;
}

button {
    padding: 8px 16px;
    background: #3498db;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-size: 14px;
    transition: background 0.2s;
}

button:hover {
    background: #2980b9;
}

button:active {
    transform: translateY(1px);
}

.message-container {
    background: white;
    border-radius: 8px;
    box-shadow: 0 2px 10px rgba(0,0,0,0.1);
    overflow: hidden;
    max-height: 600px;
    overflow-y: auto;
}

table {
    width: 100%;
    border-collapse: collapse;
}

th {
    background: #34495e;
    color: white;
    padding: 12px 8px;
    text-align: left;
    font-weight: 500;
    position: sticky;
    top: 0;
    z-index: 10;
}

td {
    padding: 8px;
    border-bottom: 1px solid #eee;
    font-size: 13px;
}

tr:hover {
    background: #f8f9fa;
}

.message-type-read { color: #27ae60; }
.message-type-write { color: #e74c3c; }
.message-type-response { color: #3498db; }

.footer {
    text-align: center;
    margin-top: 20px;
    color: #7f8c8d;
    font-size: 14px;
}

@media (max-width: 768px) {
    .controls {
        flex-direction: column;
        align-items: stretch;
    }
    
    .filter-group, .action-group {
        justify-content: center;
    }
    
    input[type="text"] {
        min-width: auto;
        flex: 1;
    }
}
""";

    private string GetAppJs() => """
class KnxMonitor {
    constructor() {
        this.messages = [];
        this.filter = null;
        this.autoScroll = true;
        this.init();
    }

    init() {
        this.bindEvents();
        this.updateStatus();
        this.loadMessages();
        
        // Update every 2 seconds
        setInterval(() => {
            this.updateStatus();
            this.loadMessages();
        }, 2000);
    }

    bindEvents() {
        document.getElementById('applyFilter').addEventListener('click', () => this.applyFilter());
        document.getElementById('clearFilter').addEventListener('click', () => this.clearFilter());
        document.getElementById('exportCsv').addEventListener('click', () => this.exportCsv());
        document.getElementById('clearMessages').addEventListener('click', () => this.clearMessages());
        document.getElementById('autoScroll').addEventListener('change', (e) => {
            this.autoScroll = e.target.checked;
        });
        
        document.getElementById('filterInput').addEventListener('keypress', (e) => {
            if (e.key === 'Enter') this.applyFilter();
        });
    }

    async updateStatus() {
        try {
            const response = await fetch('/api/status');
            const data = await response.json();
            
            const statusEl = document.getElementById('status');
            const indicatorEl = document.getElementById('statusIndicator');
            const textEl = document.getElementById('statusText');
            
            if (data.connected) {
                statusEl.className = 'status connected';
                indicatorEl.textContent = '✅';
                textEl.textContent = `Connected (${data.count} messages)`;
            } else {
                statusEl.className = 'status disconnected';
                indicatorEl.textContent = '❌';
                textEl.textContent = 'Disconnected';
            }
        } catch (error) {
            const statusEl = document.getElementById('status');
            statusEl.className = 'status disconnected';
            document.getElementById('statusIndicator').textContent = '❌';
            document.getElementById('statusText').textContent = 'Connection Error';
        }
    }

    async loadMessages() {
        try {
            const url = this.filter ? `/api/messages?filter=${encodeURIComponent(this.filter)}` : '/api/messages';
            const response = await fetch(url);
            const messages = await response.json();
            
            this.messages = messages;
            this.renderMessages();
        } catch (error) {
            console.error('Failed to load messages:', error);
        }
    }

    renderMessages() {
        const tbody = document.getElementById('messageBody');
        const container = document.querySelector('.message-container');
        const wasAtBottom = container.scrollTop + container.clientHeight >= container.scrollHeight - 5;
        
        tbody.innerHTML = '';
        
        this.messages.forEach(msg => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${msg.timestamp}</td>
                <td><span class="message-type-${msg.type.toLowerCase()}">${msg.type}</span></td>
                <td>${msg.source}</td>
                <td>${msg.destination}</td>
                <td>${msg.value}</td>
                <td>${msg.raw}</td>
                <td>${msg.priority}</td>
                <td>${msg.description || ''}</td>
            `;
            tbody.appendChild(row);
        });
        
        document.getElementById('messageCount').textContent = `${this.messages.length} messages`;
        
        if (this.autoScroll && wasAtBottom) {
            container.scrollTop = container.scrollHeight;
        }
    }

    async applyFilter() {
        const filterInput = document.getElementById('filterInput');
        const filter = filterInput.value.trim();
        
        try {
            await fetch('/api/filter', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ filter: filter || null })
            });
            
            this.filter = filter || null;
            this.loadMessages();
        } catch (error) {
            console.error('Failed to apply filter:', error);
        }
    }

    async clearFilter() {
        document.getElementById('filterInput').value = '';
        await this.applyFilter();
    }

    async exportCsv() {
        try {
            const response = await fetch('/api/export');
            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = 'knx_messages.csv';
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            window.URL.revokeObjectURL(url);
        } catch (error) {
            console.error('Failed to export CSV:', error);
        }
    }

    clearMessages() {
        this.messages = [];
        this.renderMessages();
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new KnxMonitor();
});
""";
}

// Filter payload model
public class FilterPayload
{
    public string? Filter { get; set; }
}
