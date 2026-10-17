using Akinci.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Akinci.UI.ViewModels;

public partial class SettingsDialogViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;

    [ObservableProperty]
    private string _defaultDirectory;

    [ObservableProperty]
    private int _defaultConnections;

    [ObservableProperty]
    private string _virusTotalApiKey;

    public SettingsService SettingsService => _settingsService;

    public SettingsDialogViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        var current = settingsService.Current;
        _defaultDirectory = current.DefaultDownloadDirectory;
        _defaultConnections = current.DefaultConnections;
        _virusTotalApiKey = current.VirusTotalApiKey ?? "";
    }

    public void Save()
    {
        var current = _settingsService.Current;
        current.DefaultDownloadDirectory = DefaultDirectory;
        current.DefaultConnections = DefaultConnections;
        current.VirusTotalApiKey = string.IsNullOrWhiteSpace(VirusTotalApiKey) ? null : VirusTotalApiKey.Trim();
        _settingsService.SaveSettings(current);
    }
}
