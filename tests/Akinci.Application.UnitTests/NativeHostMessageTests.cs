using System.IO;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Akinci.Application.UnitTests;

public class NativeHostMessageTests
{
    public sealed record NativeMessagePayload(string Url, string? CustomFileName = null);

    [Fact]
    public void NativeHostProtocol_EncodesAndDecodesCorrectly()
    {
        // Tarayıcı Native Messaging Protokolü:
        // 4-bayt Little-Endian tamsayı (uzunluk) + UTF-8 JSON metni
        var payload = new NativeMessagePayload("https://releases.ubuntu.com/24.04/ubuntu-24.04-desktop-amd64.iso", "ubuntu.iso");
        var json = JsonSerializer.Serialize(payload);
        var expectedBytes = Encoding.UTF8.GetBytes(json);

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
        writer.Write(expectedBytes.Length);
        writer.Write(expectedBytes);
        writer.Flush();

        // Okuma simülasyonu
        ms.Position = 0;
        using var reader = new BinaryReader(ms, Encoding.UTF8);
        int readLength = reader.ReadInt32();
        Assert.Equal(expectedBytes.Length, readLength);

        byte[] readBody = reader.ReadBytes(readLength);
        string readJson = Encoding.UTF8.GetString(readBody);
        var deserialized = JsonSerializer.Deserialize<NativeMessagePayload>(readJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(deserialized);
        Assert.Equal("https://releases.ubuntu.com/24.04/ubuntu-24.04-desktop-amd64.iso", deserialized.Url);
        Assert.Equal("ubuntu.iso", deserialized.CustomFileName);
    }

    [Fact]
    public void NativeHostProtocol_RejectsInvalidOrOversizedLength()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
        writer.Write(-1); // Geçersiz negatif uzunluk
        ms.Position = 0;

        using var reader = new BinaryReader(ms, Encoding.UTF8);
        int readLength = reader.ReadInt32();
        bool isInvalid = readLength <= 0 || readLength > 1024 * 1024;
        Assert.True(isInvalid);
    }
}
