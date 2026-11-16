using Akinci.Application.Exceptions;
using Akinci.Domain.Enums;
using Akinci.Domain.Exceptions;

namespace Akinci.UI.Localization;

/// <summary>
/// İndirme hatalarını arayüz dilinde metne çevirir. Çekirdek katmanlar istisna mesajlarını Türkçe yazar;
/// kullanıcının sık gördüğü hatalar ise tür ve değerleriyle (ör. gerekli bayt, HTTP kodu) taşınır,
/// metin burada seçilen dilde kurulur. Tanınmayan hatalarda istisnanın kendi mesajı gösterilir.
/// </summary>
public static class DownloadErrorText
{
    public static string Format(Exception ex)
    {
        while (ex is AggregateException { InnerExceptions.Count: 1 } aggregate)
        {
            ex = aggregate.InnerExceptions[0];
        }

        return ex switch
        {
            InvalidDownloadUrlException url => url.Reason switch
            {
                InvalidDownloadUrlReason.Empty => Strings.ErrorUrlEmpty,
                InvalidDownloadUrlReason.InvalidFormat => Strings.ErrorUrlInvalid(url.Url ?? string.Empty),
                _ => Strings.ErrorUrlUnsupportedScheme(url.Scheme ?? string.Empty)
            },
            MissingCredentialUserNameException creds => creds.AuthenticationType switch
            {
                AuthenticationType.Kerberos => Strings.ErrorKerberosUserNameRequired,
                AuthenticationType.Ntlm => Strings.ErrorUserNameRequired("NTLM"),
                AuthenticationType.Negotiate => Strings.ErrorUserNameRequired("Negotiate"),
                _ => Strings.ErrorUserNameRequired("Basic")
            },
            InsufficientDiskSpaceException disk => Strings.ErrorInsufficientDiskSpace(
                InsufficientDiskSpaceException.FormatBytes(disk.RequiredBytes),
                InsufficientDiskSpaceException.FormatBytes(disk.AvailableBytes)),
            HtmlInsteadOfFileException => Strings.ErrorHtmlInsteadOfFile,
            RangeNotHonoredException range => Strings.ErrorRangeNotHonored(range.ChunkIndex, range.Offset),
            ServerErrorStatusException status => Strings.ErrorServerStatus(
                status.ChunkIndex, (int)status.StatusCode!.Value, status.ReasonPhrase),
            _ => ex.Message
        };
    }
}
