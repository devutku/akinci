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
    }

    public InstallExtensionDialog(SettingsService settingsService) : this()
    {
        _settingsService = settingsService;
    }

    private void OnInstallClick(object? sender, RoutedEventArgs e)
    {
        var success = BrowserIntegrationService.SetupIntegration();
        if (success)
        {
            // Eklenti klasörünü aç ve Chrome eklentiler sayfasına yönlendir
            BrowserIntegrationService.OpenExtensionFolder();
            BrowserIntegrationService.OpenChromeExtensionsPage();

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
                txt.Text = "Kayıt defteri yazılırken yetki hatası oluştu. Lütfen Akıncı'yı yönetici olarak çalıştırın.";
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
