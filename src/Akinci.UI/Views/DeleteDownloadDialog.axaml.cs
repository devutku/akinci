using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Akinci.UI.Views;

public enum DeleteDownloadOption
{
    Cancel,
    RemoveFromListOnly,
    DeleteFileAndList
}

public partial class DeleteDownloadDialog : Window
{
    public DeleteDownloadDialog()
    {
        InitializeComponent();
    }

    public DeleteDownloadDialog(string fileName, string filePath) : this()
    {
        var txtName = this.FindControl<TextBlock>("TxtFileName");
        if (txtName != null)
        {
            txtName.Text = fileName;
        }

        var txtPath = this.FindControl<TextBlock>("TxtFilePath");
        if (txtPath != null)
        {
            txtPath.Text = filePath;
        }
    }

    private void OnRemoveFromListClick(object? sender, RoutedEventArgs e)
    {
        Close(DeleteDownloadOption.RemoveFromListOnly);
    }

    private void OnDeleteFileAndListClick(object? sender, RoutedEventArgs e)
    {
        Close(DeleteDownloadOption.DeleteFileAndList);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(DeleteDownloadOption.Cancel);
    }
}
