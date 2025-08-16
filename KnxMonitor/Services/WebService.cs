using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KnxMonitor.Services;

/// <summary>
/// Modern web service using Kestrel - works reliably in all environments including containers
/// </summary>
public class WebService
{
    private readonly ILogger<WebService> _logger;
    private readonly IKnxMonitorService _knx;
    private WebApplication? _app;

    public WebService(ILogger<WebService> logger, IKnxMonitorService knx)
    {
        _logger = logger;
        _knx = knx;
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

        // Minimal logging setup
        builder.Logging.ClearProviders().AddConsole();

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
                knx_connected = _knx.IsConnected,
                timestamp = DateTime.UtcNow 
            }));

            _app.MapGet(readyPath, () => {
                var ready = _knx.IsConnected;
                return Results.Json(new { 
                    ready, 
                    timestamp = DateTime.UtcNow 
                }, statusCode: ready ? 200 : 503);
            });
        }

        // API endpoints
        _app.MapGet("/api/status", () => Results.Json(new {
            connected = _knx.IsConnected,
            timestamp = DateTime.UtcNow
        }));

        // Simple web UI
        _app.MapGet("/", () => Results.Content(GetSimpleHtml(), "text/html"));
        _app.MapFallback(() => Results.Content(GetSimpleHtml(), "text/html"));

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

    private string GetSimpleHtml() => """
<!DOCTYPE html>
<html>
<head>
    <title>KNX Monitor</title>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; background: #f5f5f5; }
        .container { max-width: 800px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        .status { padding: 15px; border-radius: 5px; margin: 20px 0; font-weight: bold; }
        .connected { background-color: #d4edda; color: #155724; border: 1px solid #c3e6cb; }
        .disconnected { background-color: #f8d7da; color: #721c24; border: 1px solid #f5c6cb; }
        .loading { background-color: #fff3cd; color: #856404; border: 1px solid #ffeaa7; }
        h1 { color: #333; margin-bottom: 10px; }
        .subtitle { color: #666; margin-bottom: 30px; }
        .endpoints { background: #f8f9fa; padding: 20px; border-radius: 5px; margin: 20px 0; }
        .endpoints h3 { margin-top: 0; color: #495057; }
        .endpoints ul { margin: 0; }
        .endpoints li { margin: 8px 0; }
        .endpoints a { color: #007bff; text-decoration: none; }
        .endpoints a:hover { text-decoration: underline; }
    </style>
</head>
<body>
    <div class="container">
        <h1>🏠 KNX Monitor</h1>
        <p class="subtitle">Real-time KNX/EIB bus monitoring and debugging</p>
        
        <div id="status" class="status loading">⏳ Loading connection status...</div>
        
        <div class="endpoints">
            <h3>📡 API Endpoints</h3>
            <ul>
                <li><a href="/api/status">/api/status</a> - Connection status and info</li>
                <li><a href="/health">/health</a> - Health check endpoint</li>
                <li><a href="/ready">/ready</a> - Readiness probe</li>
            </ul>
        </div>
        
        <div class="endpoints">
            <h3>ℹ️ System Information</h3>
            <ul>
                <li><strong>Web Server:</strong> ASP.NET Core Kestrel</li>
                <li><strong>Runtime:</strong> .NET 9.0</li>
                <li><strong>Container Ready:</strong> ✅ Yes</li>
            </ul>
        </div>
    </div>
    
    <script>
        async function updateStatus() {
            try {
                const response = await fetch('/api/status');
                const data = await response.json();
                const statusDiv = document.getElementById('status');
                
                if (data.connected) {
                    statusDiv.className = 'status connected';
                    statusDiv.innerHTML = '✅ KNX Connected - Monitoring active';
                } else {
                    statusDiv.className = 'status disconnected';
                    statusDiv.innerHTML = '❌ KNX Disconnected - Check connection settings';
                }
            } catch (error) {
                const statusDiv = document.getElementById('status');
                statusDiv.className = 'status disconnected';
                statusDiv.innerHTML = '❌ Error loading status - ' + error.message;
            }
        }
        
        // Update status immediately and then every 5 seconds
        updateStatus();
        setInterval(updateStatus, 5000);
    </script>
</body>
</html>
""";
}
