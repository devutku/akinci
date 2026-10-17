using Akinci.UI.Services;
using Akinci.UI.ViewModels;
using Akinci.UI.Views;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Akinci.UI;

public partial class MainWindow : Window
{
    private readonly SettingsService? _settingsService;
    private readonly DesktopNotificationService? _notificationService;

    public bool AllowClose { get; set; } = false;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(
        MainWindowViewModel viewModel,
        SettingsService settingsService,
        DesktopNotificationService notificationService) : this()
    {
        DataContext = viewModel;
        _settingsService = settingsService;
        _notificationService = notificationService;

        Loaded += async (s, e) =>
        {
            _notificationService.AttachWindow(this);
            await viewModel.InitializeAsync();
        };

        KeyDown += (s, e) =>
        {
            if (e.KeyModifiers == Avalonia.Input.KeyModifiers.Control && e.Key == Avalonia.Input.Key.V)
            {
                OnAddDownloadClick(this, new RoutedEventArgs());
            }
            else if (e.KeyModifiers == Avalonia.Input.KeyModifiers.Control && e.Key == Avalonia.Input.Key.F)
            {
                var searchBox = this.FindControl<TextBox>("SearchInputBox");
                searchBox?.Focus();
            }
        };
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!AllowClose)
        {
            e.Cancel = true;
            Hide();
            _notificationService?.ShowNotification(
                "Akıncı Arka Planda Çalışıyor",
                "İndirmeleriniz devam ediyor. Görev çubuğundan geri açabilirsiniz.",
                Application.Ports.NotificationType.Information);
            return;
        }

        base.OnClosing(e);
    }

    private async void OnAddDownloadClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && _settingsService != null)
        {
            var settings = _settingsService.Current;
            var dialogVm = new AddDownloadDialogViewModel(
                settings.DefaultDownloadDirectory,
                settings.DefaultConnections);

            var dialog = new AddDownloadDialog
            {
                DataContext = dialogVm
            };

            var result = await dialog.ShowDialog<bool>(this);
            if (result && !string.IsNullOrWhiteSpace(dialogVm.Url))
            {
                var creds = dialogVm.BuildCredentials();
                await vm.AddDownloadAsync(
                    dialogVm.Url.Trim(),
                    dialogVm.DestinationDirectory,
                    dialogVm.Connections,
                    dialogVm.CustomFileName,
                    creds);
            }
        }
    }

    private async void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        if (_settingsService != null)
        {
            var dialogVm = new SettingsDialogViewModel(_settingsService);
            var dialog = new SettingsDialog
            {
                DataContext = dialogVm
            };

            await dialog.ShowDialog<bool>(this);
        }
    }

    private void OnFilterClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: string filter } && DataContext is MainWindowViewModel vm)
        {
            vm.SelectedFilter = filter;
        }
    }

    private void OnTitleBarPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
            else
            {
                BeginMoveDrag(e);
            }
        }
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnMaximizeClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}