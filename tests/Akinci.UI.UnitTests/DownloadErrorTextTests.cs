using System.Net;
using Akinci.Application.Exceptions;
using Akinci.Domain.Enums;
using Akinci.Domain.Exceptions;
using Akinci.Domain.ValueObjects;
using Akinci.UI.Localization;

namespace Akinci.UI.UnitTests;

[Collection("Localization")]
public sealed class DownloadErrorTextTests : IDisposable
{
    public void Dispose() => Loc.Initialize(Loc.TurkishCode);

    /// <summary>Kullanıcının sık gördüğü, dile çevrilen hatalar.</summary>
    public static TheoryData<Exception> KnownErrors() => new()
    {
        Capture(() => DownloadUrl.Create("")),
        Capture(() => DownloadUrl.Create("not a url")),
        Capture(() => DownloadUrl.Create("file:///C:/secret.txt")),
        Capture(() => DownloadCredentials.Basic("", "pw")),
        Capture(() => DownloadCredentials.Ntlm(" ", "pw")),
        Capture(() => DownloadCredentials.Kerberos("", "pw")),
        Capture(() => DownloadCredentials.Negotiate("", "pw")),
        new InsufficientDiskSpaceException(requiredBytes: 3L * 1024 * 1024 * 1024, availableBytes: 1536L * 1024 * 1024),
        new HtmlInsteadOfFileException(),
        new RangeNotHonoredException(chunkIndex: 2, offset: 4096),
        new ServerErrorStatusException(chunkIndex: 0, HttpStatusCode.NotFound, "Not Found"),
    };

    [Theory]
    [MemberData(nameof(KnownErrors))]
    public void Turkish_MatchesCoreMessageExactly(Exception ex)
    {
        Loc.Initialize(Loc.TurkishCode);
        Assert.Equal(ex.Message, DownloadErrorText.Format(ex));
    }

    [Theory]
    [MemberData(nameof(KnownErrors))]
    public void English_IsNotTheTurkishCoreMessage(Exception ex)
    {
        Loc.Initialize(Loc.EnglishCode);
        var text = DownloadErrorText.Format(ex);
        Assert.NotEqual(ex.Message, text);
        Assert.DoesNotContain("ı", text);
        Assert.DoesNotContain("ş", text);
    }

    [Fact]
    public void English_CommonFailures_ReadAsExpected()
    {
        Loc.Initialize(Loc.EnglishCode);

        Assert.Equal("The download URL is empty.", DownloadErrorText.Format(Capture(() => DownloadUrl.Create(" "))));
        Assert.Equal("Not a valid URL: 'not a url'.", DownloadErrorText.Format(Capture(() => DownloadUrl.Create("not a url"))));
        Assert.Equal(
            "Unsupported URL scheme: 'file'. Only HTTP, HTTPS, FTP and FTPS are supported.",
            DownloadErrorText.Format(Capture(() => DownloadUrl.Create("file:///C:/secret.txt"))));
        Assert.Equal(
            "NTLM authentication needs a user name.",
            DownloadErrorText.Format(Capture(() => DownloadCredentials.Ntlm("", "pw"))));
        Assert.Equal(
            "The server returned a web page (HTML) instead of the file. The link you pasted is a web page, not a direct download link.",
            DownloadErrorText.Format(new HtmlInsteadOfFileException()));
        Assert.Equal(
            "Part #3 could not be downloaded. The server returned HTTP 403 (Forbidden).",
            DownloadErrorText.Format(new ServerErrorStatusException(3, HttpStatusCode.Forbidden, "Forbidden")));

        var disk = DownloadErrorText.Format(new InsufficientDiskSpaceException(2048, 512));
        Assert.Equal("Not enough disk space on the target drive. Needed: 2 KB, available: 512 B.", disk);
    }

    [Fact]
    public void UnknownErrors_ShowRawMessage()
    {
        Loc.Initialize(Loc.EnglishCode);
        Assert.Equal("boom", DownloadErrorText.Format(new IOException("boom")));

        var plainHttp = new HttpRequestException("Response status code does not indicate success: 404 (Not Found).", null, HttpStatusCode.NotFound);
        Assert.Equal(plainHttp.Message, DownloadErrorText.Format(plainHttp));
    }

    [Fact]
    public void SingleInnerAggregateException_IsUnwrapped()
    {
        Loc.Initialize(Loc.EnglishCode);
        var wrapped = new AggregateException(new HtmlInsteadOfFileException());
        Assert.Equal(Strings.ErrorHtmlInsteadOfFile, DownloadErrorText.Format(wrapped));
    }

    [Fact]
    public void ServerErrorStatus_KeepsStatusCodeForRetryPolicy()
    {
        HttpRequestException ex = new ServerErrorStatusException(1, HttpStatusCode.ServiceUnavailable, "Service Unavailable");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ex.StatusCode);
    }

    private static Exception Capture(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            return ex;
        }

        throw new InvalidOperationException("Beklenen istisna fırlatılmadı.");
    }
}
