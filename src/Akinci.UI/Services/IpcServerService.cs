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

    public IpcServerService(
        MainWindowViewModel mainViewModel,
        SettingsService settingsService,
        DesktopNotificationService notificationService)
    {
        _mainViewModel = mainViewModel;
        _settingsService = settingsService;
        _notificationService = notificationService;

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://127.0.0.1:{IpcPort}/add-download/");
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

        // CORS başlıkları (tarayıcı eklentisinden doğrudan fetch yapılabilmesi için)
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "POST, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

        if (request.HttpMethod == "OPTIONS")
        {
            response.StatusCode = (int)HttpStatusCode.NoContent;
            response.Close();
            return;
        }

        if (request.HttpMethod == "POST")
        {
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
