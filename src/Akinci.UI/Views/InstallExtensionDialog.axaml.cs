using Akinci.UI.Services;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Akinci.UI.Views;

public partial class InstallExtensionDialog : Window
{
    private readonly SettingsService? _settingsService;

    public InstallExtensionDialog()
    {
        InitializeComponent();
        DialogPlacement.KeepOnScreen(this);
    }

    public InstallExtensionDialog(SettingsService settingsService) : this()
    {
        _settingsService = settingsService;
    }

    private void OnInstallClick(object? sender, RoutedEventArgs e)
    {
        var result = BrowserIntegrationService.SetupIntegration();
        if (result.Success)
        {
            // Tarayıcı köprüsü kaydedildi; eklentiyi tek tıkla kurmak için Chrome Web Mağazası sayfasını aç
            BrowserIntegrationService.OpenChromeWebStorePage();

            if (_settingsService != null)
            {
                var settings = _settingsService.Current;
                settings.ExtensionPromptDismissed = true;
                _settingsService.SaveSettings(settings);
            }

            Close(true);
        }
        else
        {
            var txt = this.FindControl<TextBlock>("TxtStatus");
            if (txt != null)
            {
                txt.Text = result.Error ?? "Eklenti kurulamadı.";
                txt.Foreground = Avalonia.Media.Brushes.Red;
            }
        }
    }

    private void OnLaterClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void OnDismissClick(object? sender, RoutedEventArgs e)
    {
        if (_settingsService != null)
        {
            var settings = _settingsService.Current;
            settings.ExtensionPromptDismissed = true;
            _settingsService.SaveSettings(settings);
        }
        Close(false);
    }
}
