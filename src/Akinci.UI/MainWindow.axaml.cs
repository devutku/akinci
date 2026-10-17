using Akinci.UI.Services;
using Akinci.UI.ViewModels;
using Akinci.UI.Views;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Akinci.UI;

public partial class MainWindow : Window
{
    private readonly SettingsService? _settingsService;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel, SettingsService settingsService) : this()
    {
        DataContext = viewModel;
        _settingsService = settingsService;

        Loaded += async (s, e) =>
        {
            await viewModel.InitializeAsync();
        };
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
        if (sender is RadioButton { Tag: string filter } && DataContext is MainWindowViewModel vm)
        {
            vm.SelectedFilter = filter;
        }
    }
}