using System.Net;
using System.Text.Json;
using Akinci.Domain.ValueObjects;
using Akinci.UI.ViewModels;

namespace Akinci.UI.Services;

public sealed record BrowserDownloadRequest(
    string Url,
    string? CustomFileName = null,
    string? Referrer = null,
    string? Cookies = null,
    string? UserAgent = null);

/// <summary>
/// Tarayıcı eklentisi (Chrome, Firefox, Edge) ve Native Messaging host'tan gelen
/// indirme isteklerini dinleyen yerel IPC / HTTP soket servisi.
/// </summary>
public sealed class IpcServerService : IAsyncDisposable
{
    private const int IpcPort = 49500;
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private Task? _listenTask;
    private readonly MainWindowViewModel _mainViewModel;
    private readonly SettingsService _settingsService;
    private readonly DesktopNotificationService _notificationService;
    private readonly string _ipcToken;

    public IpcServerService(
        MainWindowViewModel mainViewModel,
        SettingsService settingsService,
        DesktopNotificationService notificationService)
    {
        _mainViewModel = mainViewModel;
        _settingsService = settingsService;
        _notificationService = notificationService;

        _ipcToken = EnsureIpcToken();

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://127.0.0.1:{IpcPort}/add-download/");
    }

    private static string EnsureIpcToken()
    {
        try
        {
            var appDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".akinci");
            if (!Directory.Exists(appDir))
            {
                Directory.CreateDirectory(appDir);
            }

            var tokenPath = Path.Combine(appDir, "ipc.token");
            if (File.Exists(tokenPath))
            {
                var existing = File.ReadAllText(tokenPath).Trim();
                if (!string.IsNullOrEmpty(existing))
                {
                    return existing;
                }
            }

            var newToken = Guid.NewGuid().ToString("N");
            File.WriteAllText(tokenPath, newToken);
            return newToken;
        }
        catch
        {
            return Guid.NewGuid().ToString("N");
        }
    }

    public void Start()
    {
        try
        {
            _listener.Start();
            _listenTask = Task.Run(() => ListenLoopAsync(_cts.Token));
        }
        catch (Exception ex)
        {
            // Port zaten kullanımdaysa veya yetki yoksa sessizce logla
            System.Diagnostics.Debug.WriteLine($"[IPC Server] Başlatılamadı: {ex.Message}");
        }
    }

    private async Task ListenLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = ProcessRequestAsync(context);
            }
            catch (HttpListenerException) { break; }
            catch (ObjectDisposedException) { break; }
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        // 1. Origin Güvenlik Doğrulaması (CSRF & Drive-by Download Koruması)
        var origin = request.Headers["Origin"];
        bool hasOrigin = !string.IsNullOrEmpty(origin);
        bool isExtensionOrigin = hasOrigin &&
            (origin!.StartsWith("chrome-extension://", StringComparison.OrdinalIgnoreCase) ||
             origin.StartsWith("moz-extension://", StringComparison.OrdinalIgnoreCase));

        // Eğer bir web sayfası (http://, https://) yerel porta istek atıyorsa doğrudan reddet
        if (hasOrigin && !isExtensionOrigin)
        {
            response.StatusCode = (int)HttpStatusCode.Forbidden;
            response.Close();
            return;
        }

        if (isExtensionOrigin)
        {
            response.Headers.Add("Access-Control-Allow-Origin", origin);
            response.Headers.Add("Access-Control-Allow-Methods", "POST, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, X-Akinci-Token");
        }

        if (request.HttpMethod == "OPTIONS")
        {
            response.StatusCode = isExtensionOrigin ? (int)HttpStatusCode.NoContent : (int)HttpStatusCode.Forbidden;
            response.Close();
            return;
        }

        if (request.HttpMethod == "POST")
        {
            // 2. Token veya Extension Origin Doğrulaması
            var providedToken = request.Headers["X-Akinci-Token"];
            bool isTokenValid = !string.IsNullOrEmpty(_ipcToken) && string.Equals(providedToken, _ipcToken, StringComparison.Ordinal);

            if (!isExtensionOrigin && !isTokenValid)
            {
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await using var errWriter = new StreamWriter(response.OutputStream);
                await errWriter.WriteAsync("{\"status\":\"error\",\"message\":\"Yetkisiz istek. Güvenlik doğrulaması başarısız.\"}");
                response.Close();
                return;
            }

            try
            {
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                var json = await reader.ReadToEndAsync();
                var downloadReq = JsonSerializer.Deserialize<BrowserDownloadRequest>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (downloadReq != null && !string.IsNullOrWhiteSpace(downloadReq.Url))
                {
                    var settings = _settingsService.Current;
                    DownloadCredentials? credentials = null;
                    if (!string.IsNullOrWhiteSpace(downloadReq.Cookies) ||
                        !string.IsNullOrWhiteSpace(downloadReq.Referrer) ||
                        !string.IsNullOrWhiteSpace(downloadReq.UserAgent))
                    {
                        credentials = DownloadCredentials.WithSession(
                            downloadReq.Cookies,
                            downloadReq.Referrer,
                            downloadReq.UserAgent);
                    }

                    await _mainViewModel.AddDownloadAsync(
                        downloadReq.Url.Trim(),
                        settings.DefaultDownloadDirectory,
                        settings.DefaultConnections,
                        downloadReq.CustomFileName,
                        credentials);

                    _notificationService.ShowNotification(
                        "Tarayıcıdan İndirme Eklendi",
                        $"Yeni görev kuyruğa alındı: {downloadReq.CustomFileName ?? downloadReq.Url}",
                        Application.Ports.NotificationType.Success);

                    response.StatusCode = (int)HttpStatusCode.OK;
                    await using var writer = new StreamWriter(response.OutputStream);
                    await writer.WriteAsync("{\"status\":\"success\"}");
                    response.Close();
                    return;
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                await using var writer = new StreamWriter(response.OutputStream);
                await writer.WriteAsync($"{{\"status\":\"error\",\"message\":\"{ex.Message}\"}}");
                response.Close();
                return;
            }
        }

        response.StatusCode = (int)HttpStatusCode.NotFound;
        response.Close();
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        try
        {
            _listener.Stop();
            _listener.Close();
        }
        catch { }

        if (_listenTask != null)
        {
            try { await _listenTask; } catch { }
        }
    }
}
