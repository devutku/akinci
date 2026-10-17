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
        _settingsService.SaveSettings(new AppSettings
        {
            DefaultDownloadDirectory = DefaultDirectory,
            DefaultConnections = DefaultConnections,
            VirusTotalApiKey = string.IsNullOrWhiteSpace(VirusTotalApiKey) ? null : VirusTotalApiKey.Trim()
        });
    }
}
