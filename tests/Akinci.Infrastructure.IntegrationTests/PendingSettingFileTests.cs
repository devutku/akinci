using Akinci.Infrastructure.Settings;
using Xunit;

namespace Akinci.Infrastructure.IntegrationTests;

public class PendingSettingFileTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _filePath;

    public PendingSettingFileTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "Akinci_Pending_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _filePath = Path.Combine(_tempDirectory, "pending-virustotal-key");
    }

    [Fact]
    public void TryConsume_ReturnsValueAndDeletesFile()
    {
        File.WriteAllText(_filePath, "abc");

        var value = PendingSettingFile.TryConsume(_filePath);

        Assert.Equal("abc", value);
        Assert.False(File.Exists(_filePath));
    }

    [Fact]
    public void TryConsume_TrimsWhitespaceAndNewlines()
    {
        File.WriteAllText(_filePath, "  key123\r\n");

        Assert.Equal("key123", PendingSettingFile.TryConsume(_filePath));
    }

    [Fact]
    public void TryConsume_ReturnsNullWhenFileMissing()
    {
        Assert.Null(PendingSettingFile.TryConsume(_filePath));
    }

    [Fact]
    public void TryConsume_ReturnsNullAndDeletesWhenBlank()
    {
        File.WriteAllText(_filePath, "   \n");

        Assert.Null(PendingSettingFile.TryConsume(_filePath));
        Assert.False(File.Exists(_filePath));
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDirectory, true); } catch { }
    }
}
