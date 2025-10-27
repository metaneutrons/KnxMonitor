using System.Net;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Linq;
using System.Reflection;
using KnxMonitor.Models;
using Microsoft.Extensions.Logging;

namespace KnxMonitor.Services;

/// <summary>
/// Lightweight web UI service hosting a minimal REST API and static frontend.
/// Built for readability and best-practice minimalism without ASP.NET dependencies.
/// </summary>
public partial class WebUiService
{
    private readonly ILogger<WebUiService> _logger;
    private readonly IKnxMonitorService _knx;

    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    private readonly object _sync = new();
    private readonly List<KnxMessage> _messages = new(capacity: 2048);
    private string? _filter;

    public WebUiService(ILogger<WebUiService> logger, IKnxMonitorService knx)
    {
        _logger = logger;
        _knx = knx;
        _knx.MessageReceived += OnMessage;
    }

    public Task StartAsync(IEnumerable<string> prefixes, string pathBase, bool healthEnabled, string healthPath, string readyPath, CancellationToken token = default)
    {
        if (_listener != null) return Task.CompletedTask;

        // Normalize paths
        string NormalizeBase(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "/";
            if (!s.StartsWith('/')) s = "/" + s;
            return s.EndsWith('/') ? s : s + "/";
        }
        string NormalizePath(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "/";
            if (!s.StartsWith('/')) s = "/" + s;
            return s;
        }
        var basePath = NormalizeBase(pathBase);
        var health = NormalizePath(healthPath);
        var ready = NormalizePath(readyPath);

        _listener = new HttpListener();
        foreach (var p in prefixes)
        {
            var pp = p.EndsWith("/") ? p : p + "/";
            _listener.Prefixes.Add(pp);
        }
        
        try
        {
            _listener.Start();
        }
        catch (HttpListenerException ex)
        {
            _logger.LogWarning("HttpListener not supported in this environment: {Message}. Web UI disabled.", ex.Message);
            _listener?.Close();
            _listener = null;
            return Task.CompletedTask;
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        _loop = Task.Run(() => LoopAsync(_cts.Token), _cts.Token);
        _logger.LogInformation("Web UI listening on: {Prefixes}", string.Join(", ", _listener.Prefixes));

        // Store config for routing
        _pathBase = basePath;
        _healthEnabled = healthEnabled;
        _healthPath = health;
        _readyPath = ready;

        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_listener == null) return;
        try
        {
            _cts?.Cancel();
            _listener.Stop();
            _listener.Close();
            if (_loop != null) await _loop;
        }
        finally
        {
            _listener = null;
            _loop = null;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private async Task LoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null)
        {
            try
            {
                var ctx = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleAsync(ctx), ct);
            }
            catch (ObjectDisposedException) { break; }
            catch (HttpListenerException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Web UI accept loop error");
            }
        }
    }

    private string _pathBase = "/";
    private bool _healthEnabled = true;
    private string _healthPath = "/health";
    private string _readyPath = "/ready";

    private async Task HandleAsync(HttpListenerContext ctx)
    {
        try
        {
            var req = ctx.Request;
            var res = ctx.Response;
            var absPath = req.Url?.AbsolutePath ?? "/";
            // Strip path base if present
            string path = absPath;
            if (!string.IsNullOrEmpty(_pathBase) && _pathBase != "/" && absPath.StartsWith(_pathBase, StringComparison.OrdinalIgnoreCase))
            {
                var rest = absPath.Substring(_pathBase.Length);
                path = "/" + rest.TrimStart('/');
            }

            switch (path)
            {
                case "/":
                case "/index.html":
#if DEBUG
                    await ServeStaticAsync(res, "index.html", "text/html; charset=utf-8");
#else
                    await ServeEmbeddedAsync(res, "index.html", "text/html; charset=utf-8");
#endif
                    break;
                case "/styles.css":
#if DEBUG
                    await ServeStaticAsync(res, "styles.css", "text/css; charset=utf-8");
#else
                    await ServeEmbeddedAsync(res, "styles.css", "text/css; charset=utf-8");
#endif
                    break;
                case "/app.js":
#if DEBUG
                    await ServeStaticAsync(res, "app.js", "application/javascript; charset=utf-8");
#else
                    await ServeEmbeddedAsync(res, "app.js", "application/javascript; charset=utf-8");
#endif
                    break;
                case "/api/status":
                    await JsonAsync(res, new
                    {
                        connected = _knx.IsConnected,
                        status = _knx.ConnectionStatus,
                        count = _knx.MessageCount,
                        filter = _filter,
                    });
                    break;
                case "/api/messages":
                    {
                        int take = ParseInt(req.QueryString["take"], 500);
                        string? filter = req.QueryString["filter"] ?? _filter;
                        var items = GetMessagesSnapshot(filter, take);
                        await JsonAsync(res, items);
                        break;
                    }
                case "/api/filter":
                    if (req.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
                    {
                        using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                        var body = await reader.ReadToEndAsync();
                        var payload = JsonSerializer.Deserialize<FilterPayload>(body);
                        _filter = payload?.filter;
                        await JsonAsync(res, new { ok = true, filter = _filter });
                    }
                    else
                    {
                        res.StatusCode = 405;
                        await WriteAsync(res, "Method Not Allowed");
                    }
                    break;
                case "/api/export":
                    {
                        var snapshot = GetMessagesSnapshot(_filter, 10_000);
                        var csv = BuildCsv(snapshot);
                        res.StatusCode = 200;
                        res.ContentType = "text/csv; charset=utf-8";
                        res.AddHeader("Content-Disposition", "attachment; filename=knx_messages.csv");
                        await WriteAsync(res, csv);
                        break;
                    }
                default:
                    if (_healthEnabled && string.Equals(path, _healthPath, StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleHealthAsync(res);
                        break;
                    }
                    if (_healthEnabled && string.Equals(path, _readyPath, StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleReadyAsync(res);
                        break;
                    }
                    res.StatusCode = 404;
                    await WriteAsync(res, "Not Found");
                    break;
            }
        }
        catch (Exception ex)
        {
            try
            {
                ctx.Response.StatusCode = 500;
                await WriteAsync(ctx.Response, "Internal Server Error");
            }
            catch { }
            _logger.LogError(ex, "Web UI request handling error");
        }
        finally
        {
            try { ctx.Response.OutputStream.Close(); } catch { }
        }
    }

    private List<object> GetMessagesSnapshot(string? filter, int take)
    {
        List<KnxMessage> copy;
        lock (_sync)
        {
            // take most recent first
            var start = Math.Max(0, _messages.Count - take);
            copy = _messages.Skip(start).ToList();
        }

        IEnumerable<KnxMessage> q = copy;
        if (!string.IsNullOrWhiteSpace(filter))
        {
            q = q.Where(m => MatchesFilter(m, filter));
        }

        return q
            .OrderByDescending(m => m.Timestamp)
            .Take(take)
            .Select(m => new
            {
                timestamp = m.Timestamp,
                type = m.MessageType.ToString(),
                source = m.SourceAddress,
                groupAddress = m.GroupAddress,
                value = m.DisplayValue,
                priority = m.Priority.ToString(),
                dpt = m.DataPointType,
                description = m.Description,
                raw = Convert.ToHexString(m.Data),
            } as object)
            .ToList();
    }

    private static bool MatchesFilter(KnxMessage m, string filter)
    {
        if (string.IsNullOrEmpty(filter)) return true;
        if (filter.EndsWith("/*", StringComparison.Ordinal))
        {
            var prefix = filter[..^2];
            return m.GroupAddress.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }
        return string.Equals(m.GroupAddress, filter, StringComparison.OrdinalIgnoreCase);
    }

    private void OnMessage(object? sender, KnxMessage m)
    {
        lock (_sync)
        {
            _messages.Add(m);
            if (_messages.Count > 50_000)
            {
                // keep memory bounded
                _messages.RemoveRange(0, _messages.Count - 50_000);
            }
        }
    }

    private static Task ServeFileAsync(HttpListenerResponse res, string contentType, string content)
    {
        res.StatusCode = 200;
        res.ContentType = contentType;
        return WriteAsync(res, content);
    }

    private static int ParseInt(string? s, int fallback)
    {
        return int.TryParse(s, out var v) && v > 0 ? v : fallback;
    }

    private async Task HandleHealthAsync(HttpListenerResponse res)
    {
        var payload = new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            knx = new { connected = _knx.IsConnected, connectionStatus = _knx.ConnectionStatus, messagesReceived = _knx.MessageCount }
        };
        await JsonAsync(res, payload);
    }

    private async Task HandleReadyAsync(HttpListenerResponse res)
    {
        var ready = _knx.IsConnected;
        var payload = new
        {
            status = ready ? "ready" : "not ready",
            timestamp = DateTime.UtcNow,
            knx = new { connected = _knx.IsConnected, connectionStatus = _knx.ConnectionStatus }
        };
        res.StatusCode = ready ? 200 : 503;
        await JsonAsync(res, payload);
    }

    private static async Task JsonAsync(HttpListenerResponse res, object data)
    {
        res.StatusCode = 200;
        res.ContentType = "application/json; charset=utf-8";
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = false });
        await WriteAsync(res, json);
    }

    private static async Task WriteAsync(HttpListenerResponse res, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        res.ContentLength64 = bytes.Length;
        await res.OutputStream.WriteAsync(bytes);
    }

    private static string BuildCsv(IEnumerable<object> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,Type,Source,GroupAddress,Value,Priority,DPT,Description,Raw");
        foreach (dynamic r in rows)
        {
            string esc(string? s) => string.IsNullOrEmpty(s) ? "" : '"' + s.Replace("\"", "\"\"") + '"';
            sb.AppendLine(string.Join(',',
                ((DateTime)r.timestamp).ToString("yyyy-MM-dd HH:mm:ss.fff"),
                r.type,
                r.source,
                r.groupAddress,
                esc((string?)r.value),
                r.priority,
                esc((string?)r.dpt),
                esc((string?)r.description),
                r.raw));
        }
        return sb.ToString();
    }

    private record FilterPayload(string? filter);

    // Static assets served from disk (wwwroot within application base directory)
    private static readonly string WebRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
    private static readonly Assembly ThisAssembly = typeof(WebUiService).Assembly;
    private const string ResourcePrefix = "KnxMonitor.wwwroot.";

    private static async Task ServeStaticAsync(HttpListenerResponse res, string fileName, string contentType)
    {
        var path = Path.Combine(WebRoot, fileName);
        if (File.Exists(path))
        {
            res.StatusCode = 200;
            res.ContentType = contentType;
            var bytes = await File.ReadAllBytesAsync(path);
            res.ContentLength64 = bytes.LongLength;
            await res.OutputStream.WriteAsync(bytes);
            return;
        }

        // Fallback to embedded if file not found on disk
        await ServeEmbeddedAsync(res, fileName, contentType);
    }

    private static async Task ServeEmbeddedAsync(HttpListenerResponse res, string fileName, string contentType)
    {
        var resourceName = ResourcePrefix + fileName.Replace('/', '.');
        await using var stream = ThisAssembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            res.StatusCode = 404;
            await WriteAsync(res, "Not Found");
            return;
        }
        res.StatusCode = 200;
        res.ContentType = contentType;
        // ContentLength64 may not be available for all streams; copy without setting if needed
        try { res.ContentLength64 = stream.Length; } catch { }
        await stream.CopyToAsync(res.OutputStream);
    }
}

