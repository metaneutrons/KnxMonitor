using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace KnxMonitor.Services;

/// <summary>
/// Simple HTTP health check service for container health monitoring.
/// </summary>
public partial class HealthCheckService : IDisposable
{
    private readonly ILogger<HealthCheckService> _logger;
    private readonly IKnxMonitorService _knxMonitorService;
    private HttpListener? _httpListener;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _listenerTask;
    private bool _disposed;

    public HealthCheckService(ILogger<HealthCheckService> logger, IKnxMonitorService knxMonitorService)
    {
        this._logger = logger;
        this._knxMonitorService = knxMonitorService;
    }

    /// <summary>
    /// Starts the health check HTTP server.
    /// </summary>
    /// <param name="port">Port to listen on (default: 8080).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task StartAsync(int port = 8080, CancellationToken cancellationToken = default)
    {
        if (this._httpListener != null)
        {
            return Task.CompletedTask; // Already started
        }

        try
        {
            this._httpListener = new HttpListener();
            this._httpListener.Prefixes.Add($"http://+:{port}/");
            this._httpListener.Start();

            this._cancellationTokenSource = new CancellationTokenSource();
            this._listenerTask = Task.Run(
                () => this.HandleRequestsAsync(this._cancellationTokenSource.Token),
                cancellationToken
            );

            this.LogHealthCheckServiceStarted(port);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            this.LogFailedToStartHealthCheckService(ex, port);
            throw;
        }
    }

    /// <summary>
    /// Stops the health check HTTP server.
    /// </summary>
    public async Task StopAsync()
    {
        if (this._httpListener == null)
        {
            return;
        }

        try
        {
            this._cancellationTokenSource?.Cancel();
            this._httpListener.Stop();
            this._httpListener.Close();

            if (this._listenerTask != null)
            {
                await this._listenerTask;
            }

            this.LogHealthCheckServiceStopped();
        }
        catch (Exception ex)
        {
            this.LogErrorStoppingHealthCheckService(ex);
        }
        finally
        {
            this._httpListener = null;
            this._listenerTask = null;
            this._cancellationTokenSource?.Dispose();
            this._cancellationTokenSource = null;
        }
    }

    private async Task HandleRequestsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && this._httpListener != null)
        {
            try
            {
                var context = await this._httpListener.GetContextAsync();
                _ = Task.Run(() => this.ProcessRequestAsync(context), cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                // Expected when stopping
                break;
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 995) // ERROR_OPERATION_ABORTED
            {
                // Expected when stopping
                break;
            }
            catch (Exception ex)
            {
                this.LogErrorHandlingHealthCheckRequest(ex);
            }
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;

            if (request.Url?.AbsolutePath == "/health")
            {
                await this.HandleHealthCheckAsync(response);
            }
            else if (request.Url?.AbsolutePath == "/ready")
            {
                await this.HandleReadinessCheckAsync(response);
            }
            else
            {
                response.StatusCode = 404;
                await WriteResponseAsync(response, "Not Found");
            }
        }
        catch (Exception ex)
        {
            this.LogErrorProcessingHealthCheckRequest(ex);
        }
    }

    private async Task HandleHealthCheckAsync(HttpListenerResponse response)
    {
        var healthStatus = new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime(),
            knx = new
            {
                connected = this._knxMonitorService.IsConnected,
                connectionStatus = this._knxMonitorService.ConnectionStatus,
                messagesReceived = this._knxMonitorService.MessageCount,
            },
        };

        response.StatusCode = 200;
        response.ContentType = "application/json";
        await WriteJsonResponseAsync(response, healthStatus);
    }

    private async Task HandleReadinessCheckAsync(HttpListenerResponse response)
    {
        var isReady = this._knxMonitorService.IsConnected;
        var statusCode = isReady ? 200 : 503;

        var readinessStatus = new
        {
            status = isReady ? "ready" : "not ready",
            timestamp = DateTime.UtcNow,
            knx = new
            {
                connected = this._knxMonitorService.IsConnected,
                connectionStatus = this._knxMonitorService.ConnectionStatus,
            },
        };

        response.StatusCode = statusCode;
        response.ContentType = "application/json";
        await WriteJsonResponseAsync(response, readinessStatus);
    }

    private static async Task WriteJsonResponseAsync(HttpListenerResponse response, object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await WriteResponseAsync(response, json);
    }

    private static async Task WriteResponseAsync(HttpListenerResponse response, string content)
    {
        var buffer = Encoding.UTF8.GetBytes(content);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
        response.OutputStream.Close();
    }

    public void Dispose()
    {
        if (this._disposed)
        {
            return;
        }

        this.StopAsync().GetAwaiter().GetResult();
        this._disposed = true;
    }
}
