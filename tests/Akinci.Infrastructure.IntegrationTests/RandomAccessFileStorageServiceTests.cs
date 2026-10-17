using System.Security.Cryptography;
using Akinci.Infrastructure.Storage;
using Xunit;

namespace Akinci.Infrastructure.IntegrationTests;

public class RandomAccessFileStorageServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly RandomAccessFileStorageService _storageService;

    public RandomAccessFileStorageServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "Akinci_Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _storageService = new RandomAccessFileStorageService();
    }

    [Fact]
    public async Task PreAllocateAsync_CreatesFileWithSpecifiedExactSize()
    {
        var filePath = Path.Combine(_tempDirectory, "prealloc.bin");
        const long expectedSize = 10 * 1024 * 1024; // 10 MB

        await _storageService.PreAllocateAsync(filePath, expectedSize);

        Assert.True(_storageService.FileExists(filePath));
        Assert.Equal(expectedSize, _storageService.GetFileSize(filePath));
    }

    [Fact]
    public async Task WriteAtAsync_ConcurrentWritesAtDifferentOffsets_ProducesIdenticalData()
    {
        var filePath = Path.Combine(_tempDirectory, "concurrent.bin");
        const int chunkCount = 8;
        const int chunkSize = 64 * 1024; // 64 KB per chunk (Total 512 KB)
        const int totalSize = chunkCount * chunkSize;

        // Rastgele veri oluştur
        byte[] expectedData = new byte[totalSize];
        RandomNumberGenerator.Fill(expectedData);

        await _storageService.PreAllocateAsync(filePath, totalSize);

        // 8 paralel işçi ile farklı ofsetlere eşzamanlı RandomAccess yazımı
        var writeTasks = Enumerable.Range(0, chunkCount).Select(async i =>
        {
            long offset = i * chunkSize;
            var memory = new ReadOnlyMemory<byte>(expectedData, (int)offset, chunkSize);
            await _storageService.WriteAtAsync(filePath, offset, memory);
        });

        await Task.WhenAll(writeTasks);

        _storageService.CloseHandle(filePath);

        // Diskteki veriyi doğrula
        byte[] actualData = await File.ReadAllBytesAsync(filePath);
        Assert.Equal(expectedData, actualData);
    }

    public void Dispose()
    {
        _storageService.Dispose();
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, true);
            }
            catch
            {
                // Geçici dizin temizliği
            }
        }
    }
}
