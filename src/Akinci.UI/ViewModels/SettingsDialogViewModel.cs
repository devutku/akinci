using Akinci.Application.Ports;
using Akinci.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Akinci.UI.ViewModels;

public partial class SettingsDialogViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly IBandwidthThrottler? _throttler;

    [ObservableProperty]
    private string _defaultDirectory;

    [ObservableProperty]
    private int _defaultConnections;

    [ObservableProperty]
    private int _maxConcurrentDownloads;

    [ObservableProperty]
    private long _speedLimitKbps;

    [ObservableProperty]
    private string _virusTotalApiKey;

    [ObservableProperty]
    private bool _enableClipboardMonitor;

    public SettingsService SettingsService => _settingsService;

    public SettingsDialogViewModel(SettingsService settingsService, IBandwidthThrottler? throttler = null)
    {
        _settingsService = settingsService;
        _throttler = throttler;
        var current = settingsService.Current;
        _defaultDirectory = current.DefaultDownloadDirectory;
        _defaultConnections = current.DefaultConnections;
        _maxConcurrentDownloads = current.MaxConcurrentDownloads;
        _speedLimitKbps = current.SpeedLimitBytesPerSecond > 0 ? current.SpeedLimitBytesPerSecond / 1024 : 0;
        _virusTotalApiKey = current.VirusTotalApiKey ?? "";
        _enableClipboardMonitor = current.EnableClipboardMonitor;
    }

    public void Save()
    {
        var current = _settingsService.Current;
        current.DefaultDownloadDirectory = DefaultDirectory;
        current.DefaultConnections = DefaultConnections;
        current.MaxConcurrentDownloads = Math.Max(0, MaxConcurrentDownloads);
        current.SpeedLimitBytesPerSecond = SpeedLimitKbps > 0 ? SpeedLimitKbps * 1024 : 0;
        current.VirusTotalApiKey = string.IsNullOrWhiteSpace(VirusTotalApiKey) ? null : VirusTotalApiKey.Trim();
        current.EnableClipboardMonitor = EnableClipboardMonitor;
        _settingsService.SaveSettings(current);

        if (_throttler != null)
        {
            _throttler.LimitBytesPerSecond = current.SpeedLimitBytesPerSecond;
        }
    }
}
