using Akinci.Domain.Entities;
using Akinci.Domain.Enums;
using Akinci.Domain.ValueObjects;
using Akinci.Infrastructure.Persistence;
using Xunit;

namespace Akinci.Infrastructure.IntegrationTests;

public class SqliteRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly string _tempDbPath;
    private readonly SqliteDownloadRepository _repository;

    public SqliteRepositoryIntegrationTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"Akinci_Sqlite_{Guid.NewGuid():N}.db");
        _repository = new SqliteDownloadRepository(_tempDbPath);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AddAndGetByIdAsync_PersistsAllTaskFieldsAndChunksCorrectly()
    {
        var creds = DownloadCredentials.Basic("testuser", "secretpass");
        var task = DownloadTask.Create(
            DownloadUrl.Create("https://example.com/test-db.zip"),
            @"C:\Downloads\test-db.zip",
            totalBytes: 4000,
            supportsRangeRequests: true,
            credentials: creds);

        task.InitializeChunks(4);
        task.SetSha256("abc123hash");
        task.SetSecurityVerdict(SecurityVerdict.Clean("abc123hash", harmlessVotes: 68, totalEngines: 70));

        await _repository.AddAsync(task);

        // Veritabanından geri yükle
        var loaded = await _repository.GetByIdAsync(task.Id);

        Assert.NotNull(loaded);
        Assert.Equal(task.Id, loaded.Id);
        Assert.Equal("https://example.com/test-db.zip", loaded.Url.Value);
        Assert.Equal(@"C:\Downloads\test-db.zip", loaded.DestinationFilePath);
        Assert.Equal(4000, loaded.TotalBytes);
        Assert.True(loaded.SupportsRangeRequests);
        Assert.Equal(DownloadStatus.Pending, loaded.Status);
        Assert.Equal("abc123hash", loaded.Sha256Hash);

        // Kimlik bilgisi doğrulaması
        Assert.NotNull(loaded.Credentials);
        Assert.Equal(AuthenticationType.Basic, loaded.Credentials.Type);
        Assert.Equal("testuser", loaded.Credentials.UserName);

        // Güvenlik doğrulaması
        Assert.NotNull(loaded.SecurityVerdict);
        Assert.True(loaded.SecurityVerdict.IsSafe);
        Assert.Equal(68, loaded.SecurityVerdict.HarmlessVotes);
        Assert.Equal(70, loaded.SecurityVerdict.TotalEngines);

        // Chunk doğrulaması
        Assert.Equal(4, loaded.Chunks.Count);
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(task.Chunks[i].Index, loaded.Chunks[i].Index);
            Assert.Equal(task.Chunks[i].Range.Start, loaded.Chunks[i].Range.Start);
            Assert.Equal(task.Chunks[i].Range.End, loaded.Chunks[i].Range.End);
            Assert.Equal(task.Chunks[i].CurrentOffset, loaded.Chunks[i].CurrentOffset);
        }
    }

    [Fact]
    public async Task UpdateChunkOffsetAsync_UpdatesSingleChunkOffsetFastAndAccurately()
    {
        var task = DownloadTask.Create(
            DownloadUrl.Create("https://example.com/file.bin"),
            @"C:\Downloads\file.bin",
            1000,
            true);
        task.InitializeChunks(2);
        await _repository.AddAsync(task);

        // Chunk #0 ofsetini 250 olarak güncelle
        await _repository.UpdateChunkOffsetAsync(task.Id, chunkIndex: 0, newOffset: 250);

        var loaded = await _repository.GetByIdAsync(task.Id);
        Assert.NotNull(loaded);
        Assert.Equal(250, loaded.Chunks[0].CurrentOffset);
        Assert.Equal(250, loaded.Chunks[0].DownloadedBytes);
        Assert.Equal(500, loaded.Chunks[1].CurrentOffset); // Değişmemeli
    }

    [Fact]
    public async Task DeleteAsync_CascadesAndRemovesTaskAndChunks()
    {
        var task = DownloadTask.Create(
            DownloadUrl.Create("https://example.com/todelete.iso"),
            @"C:\Downloads\todelete.iso",
            1000,
            true);
        task.InitializeChunks(2);
        await _repository.AddAsync(task);

        await _repository.DeleteAsync(task.Id);

        var loaded = await _repository.GetByIdAsync(task.Id);
        Assert.Null(loaded);
    }

    public async Task DisposeAsync()
    {
        await _repository.DisposeAsync();

        // SQLite bağlantı havuzunu temizle
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        try
        {
            if (File.Exists(_tempDbPath)) File.Delete(_tempDbPath);
            var shm = _tempDbPath + "-shm";
            if (File.Exists(shm)) File.Delete(shm);
            var wal = _tempDbPath + "-wal";
            if (File.Exists(wal)) File.Delete(wal);
        }
        catch { }
    }
}
