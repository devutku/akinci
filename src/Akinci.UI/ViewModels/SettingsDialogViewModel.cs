using Akinci.Application.Ports;
using Akinci.UI.Localization;
using Akinci.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Akinci.UI.ViewModels;

/// <summary>Ayarlardaki dil seçeneği. <see cref="Code"/> <c>null</c> ise sistem dili izlenir.</summary>
public sealed record LanguageOption(string? Code, string DisplayName);

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

    public IReadOnlyList<LanguageOption> LanguageOptions { get; } =
    [
        new LanguageOption(null, Strings.LanguageSystem),
        new LanguageOption(Loc.TurkishCode, "Türkçe"),
        new LanguageOption(Loc.EnglishCode, "English")
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLanguageChanged))]
    private LanguageOption _selectedLanguage;

    /// <summary>Seçilen dil şu an kullanılan dilden farklıysa yeniden başlatma notu gösterilir.</summary>
    public bool IsLanguageChanged =>
        Loc.Resolve(SelectedLanguage.Code, System.Globalization.CultureInfo.CurrentUICulture) != Loc.Current;

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
        _selectedLanguage = LanguageOptions.FirstOrDefault(o => o.Code == current.Language) ?? LanguageOptions[0];
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
        current.Language = SelectedLanguage.Code;
        _settingsService.SaveSettings(current);

        if (_throttler != null)
        {
            _throttler.LimitBytesPerSecond = current.SpeedLimitBytesPerSecond;
        }
    }
}
