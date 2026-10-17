using Akinci.Application.DTOs;
using Akinci.Domain.Enums;
using Akinci.Domain.ValueObjects;
using FluentFTP;

namespace Akinci.Infrastructure.Ftp;

/// <summary>
/// FluentFTP kullanarak FTP/FTPS sunucularından dosya üstverisini ve aralık (REST) desteğini sorgulayan servis.
/// </summary>
public sealed class FtpMetadataService
{
    public async Task<FileMetadataDto> GetMetadataAsync(
        DownloadUrl url,
        DownloadCredentials? credentials = null,
        CancellationToken cancellationToken = default)
    {
        var (host, port, remotePath) = ParseFtpUrl(url);
        var (user, pass) = ResolveCredentials(credentials);

        var config = new FtpConfig
        {
            ConnectTimeout = 10000,
            ReadTimeout = 15000,
            DataConnectionType = FtpDataConnectionType.PASV
        };

        await using var client = new AsyncFtpClient(host, user, pass, port, config);

        if (url.Uri.Scheme.Equals("ftps", StringComparison.OrdinalIgnoreCase))
        {
            client.Config.EncryptionMode = FtpEncryptionMode.Explicit;
            client.Config.ValidateAnyCertificate = true;
        }

        await client.AutoConnect(cancellationToken).ConfigureAwait(false);

        long? fileSize = null;
        try
        {
            var size = await client.GetFileSize(remotePath, -1, cancellationToken).ConfigureAwait(false);
            if (size >= 0)
            {
                fileSize = size;
            }
        }
        catch
        {
            // SIZE komutu desteklenmiyorsa null kalır
        }

        bool supportsRange = client.HasFeature(FtpCapability.REST);
        var fileName = url.GetFileNameFromUrl();

        await client.Disconnect(cancellationToken).ConfigureAwait(false);

        return new FileMetadataDto(fileSize, supportsRange, fileName, null, "application/octet-stream");
    }

    internal static (string Host, int Port, string RemotePath) ParseFtpUrl(DownloadUrl url)
    {
        var host = url.Uri.Host;
        var port = url.Uri.Port > 0 ? url.Uri.Port : 21;
        var path = Uri.UnescapeDataString(url.Uri.AbsolutePath);
        return (host, port, path);
    }

    internal static (string User, string Pass) ResolveCredentials(DownloadCredentials? credentials)
    {
        if (credentials != null &&
            credentials.Type == AuthenticationType.Basic &&
            !string.IsNullOrEmpty(credentials.UserName))
        {
            return (credentials.UserName, credentials.Password ?? "");
        }

        return ("anonymous", "anonymous@akinci.download");
    }
}
