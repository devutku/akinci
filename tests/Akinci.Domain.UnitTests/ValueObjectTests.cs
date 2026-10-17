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
    public void Create_WithEmptyOrNull_ThrowsDomainException(string? url)
    {
        Assert.Throws<DomainException>(() => DownloadUrl.Create(url!));
    }

    [Theory]
    [InlineData("file:///C:/Users/Admin/file.txt")]
    [InlineData("ssh://git@github.com/repo.git")]
    [InlineData("not-a-valid-url")]
    public void Create_WithInvalidSchemeOrFormat_ThrowsDomainException(string url)
    {
        Assert.Throws<DomainException>(() => DownloadUrl.Create(url));
    }
}

public class DownloadCredentialsTests
{
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
