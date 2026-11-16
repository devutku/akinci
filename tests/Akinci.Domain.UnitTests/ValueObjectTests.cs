using Akinci.Domain.Enums;
using Akinci.Domain.Exceptions;
using Akinci.Domain.ValueObjects;
using Xunit;

namespace Akinci.Domain.UnitTests;

public class DownloadUrlTests
{
    [Theory]
    [InlineData("http://example.com/file.zip", "file.zip", true, false)]
    [InlineData("https://releases.ubuntu.com/24.04/ubuntu-24.04-desktop-amd64.iso", "ubuntu-24.04-desktop-amd64.iso", true, false)]
    [InlineData("ftp://ftp.example.com/debian.iso", "debian.iso", false, true)]
    [InlineData("ftps://secure-ftp.example.com/data.tar.gz", "data.tar.gz", false, true)]
    [InlineData("https://example.com/path/", "download.bin", true, false)]
    public void Create_WithValidUrl_Success(string url, string expectedFileName, bool isHttp, bool isFtp)
    {
        var downloadUrl = DownloadUrl.Create(url);

        Assert.NotNull(downloadUrl);
        Assert.Equal(expectedFileName, downloadUrl.GetFileNameFromUrl());
        Assert.Equal(isHttp, downloadUrl.IsHttp);
        Assert.Equal(isFtp, downloadUrl.IsFtp);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyOrNull_ThrowsInvalidDownloadUrlException(string? url)
    {
        var ex = Assert.Throws<InvalidDownloadUrlException>(() => DownloadUrl.Create(url!));
        Assert.Equal(InvalidDownloadUrlReason.Empty, ex.Reason);
        Assert.Equal("İndirme URL'i boş olamaz.", ex.Message);
    }

    [Theory]
    [InlineData("file:///C:/Users/Admin/file.txt", InvalidDownloadUrlReason.UnsupportedScheme)]
    [InlineData("ssh://git@github.com/repo.git", InvalidDownloadUrlReason.UnsupportedScheme)]
    [InlineData("not-a-valid-url", InvalidDownloadUrlReason.InvalidFormat)]
    public void Create_WithInvalidSchemeOrFormat_ThrowsInvalidDownloadUrlException(string url, InvalidDownloadUrlReason reason)
    {
        var ex = Assert.Throws<InvalidDownloadUrlException>(() => DownloadUrl.Create(url));
        Assert.Equal(reason, ex.Reason);
        Assert.Equal(url, ex.Url);
    }

    [Fact]
    public void Create_WithUnsupportedScheme_KeepsTurkishMessageAndScheme()
    {
        var ex = Assert.Throws<InvalidDownloadUrlException>(() => DownloadUrl.Create("ssh://git@github.com/repo.git"));
        Assert.Equal("ssh", ex.Scheme);
        Assert.Equal("Desteklenmeyen URL şeması: 'ssh'. Yalnızca HTTP, HTTPS, FTP ve FTPS desteklenir.", ex.Message);
    }
}

public class DownloadCredentialsTests
{
    [Theory]
    [InlineData(AuthenticationType.Basic, "Basic kimlik doğrulama için kullanıcı adı boş olamaz.")]
    [InlineData(AuthenticationType.Ntlm, "NTLM kimlik doğrulama için kullanıcı adı boş olamaz.")]
    [InlineData(AuthenticationType.Kerberos, "Kerberos kimlik doğrulama için kullanıcı adı veya kullanıcı anahtarı boş olamaz.")]
    [InlineData(AuthenticationType.Negotiate, "Negotiate kimlik doğrulama için kullanıcı adı boş olamaz.")]
    public void WithoutUserName_ThrowsMissingCredentialUserNameException(AuthenticationType type, string expectedMessage)
    {
        Func<DownloadCredentials> create = type switch
        {
            AuthenticationType.Ntlm => () => DownloadCredentials.Ntlm(" ", "pw"),
            AuthenticationType.Kerberos => () => DownloadCredentials.Kerberos("", "pw"),
            AuthenticationType.Negotiate => () => DownloadCredentials.Negotiate("", "pw"),
            _ => () => DownloadCredentials.Basic("", "pw")
        };

        var ex = Assert.Throws<MissingCredentialUserNameException>(() => create());
        Assert.Equal(type, ex.AuthenticationType);
        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public void Basic_CreatesValidCredentials()
    {
        var creds = DownloadCredentials.Basic("user1", "pass123");
        Assert.Equal(Akinci.Domain.Enums.AuthenticationType.Basic, creds.Type);
        Assert.Equal("user1", creds.UserName);
        Assert.Equal("pass123", creds.Password);
        Assert.False(creds.UseDefaultCredentials);
    }

    [Fact]
    public void Ntlm_WithDomain_CreatesValidCredentials()
    {
        var creds = DownloadCredentials.Ntlm("admin", "secret", "CORP");
        Assert.Equal(Akinci.Domain.Enums.AuthenticationType.Ntlm, creds.Type);
        Assert.Equal("admin", creds.UserName);
        Assert.Equal("secret", creds.Password);
        Assert.Equal("CORP", creds.Domain);
    }

    [Fact]
    public void Kerberos_CreatesValidCredentials()
    {
        var creds = DownloadCredentials.Kerberos("krbuser", "pass", "REALM.LOCAL");
        Assert.Equal(Akinci.Domain.Enums.AuthenticationType.Kerberos, creds.Type);
        Assert.Equal("krbuser", creds.UserName);
        Assert.Equal("REALM.LOCAL", creds.Domain);
    }

    [Fact]
    public void Negotiate_CreatesValidCredentials()
    {
        var creds = DownloadCredentials.Negotiate("spnegoUser", "pass");
        Assert.Equal(Akinci.Domain.Enums.AuthenticationType.Negotiate, creds.Type);
        Assert.Equal("spnegoUser", creds.UserName);
    }

    [Fact]
    public void WindowsDefault_CreatesDefaultCredentials()
    {
        var creds = DownloadCredentials.WindowsDefault();
        Assert.Equal(Akinci.Domain.Enums.AuthenticationType.Negotiate, creds.Type);
        Assert.True(creds.UseDefaultCredentials);
    }
}

public class DownloadSpeedTests
{
    [Fact]
    public void FormatHumanReadable_FormatsCorrectly()
    {
        Assert.Equal("500 B/s", new DownloadSpeed(500).FormatHumanReadable());
        Assert.Equal("1.5 KB/s", new DownloadSpeed(1536).FormatHumanReadable());
        Assert.Equal("10.00 MB/s", new DownloadSpeed(10 * 1024 * 1024).FormatHumanReadable());
        Assert.Equal("1.50 GB/s", new DownloadSpeed(1.5 * 1024 * 1024 * 1024).FormatHumanReadable());
    }

    [Fact]
    public void Constructor_NegativeValue_ClampsToZero()
    {
        var speed = new DownloadSpeed(-100);
        Assert.Equal(0, speed.BytesPerSecond);
    }
}
