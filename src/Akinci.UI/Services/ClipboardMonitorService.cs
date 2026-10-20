using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Threading;

namespace Akinci.UI.Services;

/// <summary>
/// Sistem panosunu (Clipboard) periyodik olarak kontrol eden ve yeni kopyalanan geçerli
/// HTTP/HTTPS/FTP indirme bağlantılarını otomatik olarak yakalayan servis.
/// </summary>
public sealed class ClipboardMonitorService : IDisposable
{
    private readonly SettingsService _settingsService;
    private DispatcherTimer? _timer;
    private Window? _window;
    private string? _lastSeenUrl;
    private bool _isChecking;

    public event Action<string>? UrlDetected;

    public ClipboardMonitorService(SettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
    }

    public void Start(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));

        // İlk başlatmada panodaki mevcut içeriği alıp tohumla (Akıncı açıldığında eski panoyu doğrudan tetiklemesin)
        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                if (_window?.Clipboard != null)
                {
                    _lastSeenUrl = await _window.Clipboard.TryGetTextAsync();
                }
            }
            catch
            {
                // Clipboard ilk okuma güvenliği
            }
        });

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1500)
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    public void Stop()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Tick -= OnTimerTick;
            _timer = null;
        }
        _window = null;
    }

    private async void OnTimerTick(object? sender, EventArgs e)
    {
        if (!_settingsService.Current.EnableClipboardMonitor || _isChecking || _window?.Clipboard == null)
        {
            return;
        }

        _isChecking = true;
        try
        {
            var text = await _window.Clipboard.TryGetTextAsync();
            if (TryParseDownloadUrl(text, out var cleanUrl))
            {
                if (cleanUrl != _lastSeenUrl)
                {
                    _lastSeenUrl = cleanUrl;
                    UrlDetected?.Invoke(cleanUrl);
                }
            }
        }
        catch
        {
            // Pano diğer uygulamalar tarafından kilitliyse sessizce geç
        }
        finally
        {
            _isChecking = false;
        }
    }

    public static bool TryParseDownloadUrl(string? text, out string cleanUrl)
    {
        cleanUrl = string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        text = text.Trim();
        if (text.Length > 2048 || text.Contains('\n') || text.Contains('\r'))
        {
            return false;
        }

        try
        {
            var dlUrl = Akinci.Domain.ValueObjects.DownloadUrl.Create(text);
            if (dlUrl.Uri.IsLoopback)
            {
                return false;
            }

            cleanUrl = dlUrl.Value;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
