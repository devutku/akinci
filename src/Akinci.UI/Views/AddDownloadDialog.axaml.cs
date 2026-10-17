using Akinci.UI.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace Akinci.UI.Views;

public partial class AddDownloadDialog : Window
{
    public AddDownloadDialog()
    {
        InitializeComponent();
    }

    private async void OnBrowseFolderClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AddDownloadDialogViewModel vm)
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "İndirme Konumunu Seçin",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                vm.DestinationDirectory = folders[0].Path.LocalPath;
            }
        }
    }

    private void OnSubmitClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AddDownloadDialogViewModel vm)
        {
            if (string.IsNullOrWhiteSpace(vm.Url))
            {
                return;
            }
            Close(true);
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
