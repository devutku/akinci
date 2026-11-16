using Akinci.Domain.ValueObjects;
using Akinci.UI.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Akinci.UI.ViewModels;

public partial class AddDownloadDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _url = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DetectedUrlText))]
    private int _detectedUrlCount;

    public string DetectedUrlText => Strings.LinksDetected(DetectedUrlCount);

    [ObservableProperty]
    private string _submitButtonText = Strings.StartDownload;

    [ObservableProperty]
    private string _destinationDirectory;

    [ObservableProperty]
    private int _connections = 8;

    [ObservableProperty]
    private string? _customFileName;

    [ObservableProperty]
    private string _selectedAuthType = Strings.AuthNone;

    [ObservableProperty]
    private string? _userName;

    [ObservableProperty]
    private string? _password;

    [ObservableProperty]
    private string? _domain;

    [ObservableProperty]
    private bool _useDefaultCredentials;

    public string[] AvailableAuthTypes { get; } = [Strings.AuthNone, "Basic", "NTLM", "Kerberos", "Negotiate"];

    public bool IsAuthRequired => SelectedAuthType != Strings.AuthNone;
    public bool ShowDomainField => SelectedAuthType is "NTLM" or "Kerberos" or "Negotiate";

    public AddDownloadDialogViewModel(string defaultDirectory, int defaultConnections = 8, string? initialUrl = null)
    {
        _destinationDirectory = defaultDirectory;
        _connections = defaultConnections;
        if (!string.IsNullOrWhiteSpace(initialUrl))
        {
            Url = initialUrl.Trim();
        }
    }

    partial void OnUrlChanged(string value)
    {
        var count = GetValidUrls().Count;
        DetectedUrlCount = count;
        SubmitButtonText = count > 1 ? Strings.StartDownloads(count) : Strings.StartDownload;
    }

    public List<string> GetValidUrls()
    {
        if (string.IsNullOrWhiteSpace(Url)) return [];

        var lines = Url.Split(new[] { '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var result = new List<string>();

        foreach (var line in lines)
        {
            if (Uri.TryCreate(line, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == "ftp" || uri.Scheme == "ftps"))
            {
                result.Add(line);
            }
        }

        return result;
    }

    partial void OnSelectedAuthTypeChanged(string value)
    {
        OnPropertyChanged(nameof(IsAuthRequired));
        OnPropertyChanged(nameof(ShowDomainField));
    }

    public DownloadCredentials? BuildCredentials()
    {
        if (SelectedAuthType == Strings.AuthNone) return null;

        if (UseDefaultCredentials)
        {
            return SelectedAuthType switch
            {
                "NTLM" => DownloadCredentials.WindowsDefault(Akinci.Domain.Enums.AuthenticationType.Ntlm),
                "Kerberos" => DownloadCredentials.WindowsDefault(Akinci.Domain.Enums.AuthenticationType.Kerberos),
                _ => DownloadCredentials.WindowsDefault(Akinci.Domain.Enums.AuthenticationType.Negotiate)
            };
        }

        if (string.IsNullOrWhiteSpace(UserName)) return null;

        return SelectedAuthType switch
        {
            "NTLM" => DownloadCredentials.Ntlm(UserName, Password ?? "", Domain),
            "Kerberos" => DownloadCredentials.Kerberos(UserName, Password ?? "", Domain),
            "Negotiate" => DownloadCredentials.Negotiate(UserName, Password ?? "", Domain),
            _ => DownloadCredentials.Basic(UserName, Password ?? "")
        };
    }
}
