using Akinci.Domain.Exceptions;
using Akinci.Domain.ValueObjects;
using Xunit;

namespace Akinci.Domain.UnitTests;

public class DownloadUrlTests
{
    [Theory]
    [InlineData("http://example.com/file.zip", "file.zip")]
    [InlineData("https://releases.ubuntu.com/24.04/ubuntu-24.04-desktop-amd64.iso", "ubuntu-24.04-desktop-amd64.iso")]
    [InlineData("https://example.com/path/", "download.bin")]
    public void Create_WithValidHttpOrHttpsUrl_Success(string url, string expectedFileName)
    {
        var downloadUrl = DownloadUrl.Create(url);

        Assert.NotNull(downloadUrl);
        Assert.Equal(expectedFileName, downloadUrl.GetFileNameFromUrl());
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
    [InlineData("ftp://example.com/file.iso")]
    [InlineData("file:///C:/Users/Admin/file.txt")]
    [InlineData("not-a-valid-url")]
    public void Create_WithInvalidSchemeOrFormat_ThrowsDomainException(string url)
    {
        Assert.Throws<DomainException>(() => DownloadUrl.Create(url));
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
