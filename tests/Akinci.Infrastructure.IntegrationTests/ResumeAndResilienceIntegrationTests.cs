using System.Net;
using System.Security.Cryptography;
using Akinci.Application.UseCases.CreateDownload;
using Akinci.Application.UseCases.ResumeDownload;
using Akinci.Application.UseCases.StartDownload;
using Akinci.Domain.Enums;
using Akinci.Infrastructure.Http;
using Akinci.Infrastructure.Persistence;
using Akinci.Infrastructure.Resilience;
using Akinci.Infrastructure.Storage;
using Xunit;

namespace Akinci.Infrastructure.IntegrationTests;

public class ResumeAndResilienceIntegrationTests : IAsyncLifetime
{
    private readonly HttpListener _listener;
    private readonly string _serverPrefix;
    private readonly byte[] _mockPayload;
    private readonly string _mockSha256;
    private readonly CancellationTokenSource _serverCts;
    private readonly string _tempDirectory;
    private readonly string _dbPath;
    private Task? _serverTask;

    public ResumeAndResilienceIntegrationTests()
    {
        _mockPayload = new byte[1024 * 1024]; // 1 MB
        RandomNumberGenerator.Fill(_mockPayload);
        _mockSha256 = Convert.ToHexString(SHA256.HashData(_mockPayload));

        int port = Random.Shared.Next(50000, 60000);
        _serverPrefix = $"http://127.0.0.1:{port}/";
        _listener = new HttpListener();
        _listener.Prefixes.Add(_serverPrefix);
        _serverCts = new CancellationTokenSource();

        _tempDirectory = Path.Combine(Path.GetTempPath(), "Akinci_ResumeTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _dbPath = Path.Combine(_tempDirectory, "downloads.db");
    }

    public Task InitializeAsync()
    {
        _listener.Start();
        _serverTask = Task.Run(() => RunServerAsync(_serverCts.Token));
        return Task.CompletedTask;
    }

    private async Task RunServerAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = ProcessRequestAsync(context);
            }
            catch { break; }
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        response.Headers.Add("Accept-Ranges", "bytes");

        if (request.HttpMethod == "HEAD")
        {
            response.ContentLength64 = _mockPayload.Length;
            response.StatusCode = (int)HttpStatusCode.OK;
            response.Close();
            return;
        }

        if (request.HttpMethod == "GET")
        {
            var rangeHeader = request.Headers["Range"];
            if (!string.IsNullOrEmpty(rangeHeader) && rangeHeader.StartsWith("bytes="))
            {
                var parts = rangeHeader["bytes=".Length..].Split('-');
                long start = long.Parse(parts[0]);
                long end = parts.Length > 1 && !string.IsNullOrEmpty(parts[1])
                    ? long.Parse(parts[1])
                    : _mockPayload.Length - 1;

                long length = end - start + 1;
                response.StatusCode = (int)HttpStatusCode.PartialContent;
                response.Headers.Add("Content-Range", $"bytes {start}-{end}/{_mockPayload.Length}");
                response.ContentLength64 = length;

                await response.OutputStream.WriteAsync(_mockPayload.AsMemory((int)start, (int)length));
                response.Close();
                return;
            }

            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentLength64 = _mockPayload.Length;
            await response.OutputStream.WriteAsync(_mockPayload);
            response.Close();
        }
    }

    [Fact]
    public async Task ResumeDownload_AfterInterruption_ResumesFromSavedOffsetsAndMatchesHash()
    {
        var metadataService = new HttpMetadataService();
        var chunkDownloader = new HttpChunkDownloader();
        using var fileStorage = new RandomAccessFileStorageService();
        var repository = new SqliteDownloadRepository(_dbPath);

        var createHandler = new CreateDownloadCommandHandler(metadataService, repository, fileStorage);
        var startHandler = new StartDownloadCommandHandler(repository, chunkDownloader, fileStorage);
        var resumeHandler = new ResumeDownloadCommandHandler(repository, chunkDownloader, fileStorage);

        // 1. İndirme görevini 4 parçayla oluştur
        var createCommand = new CreateDownloadCommand(
            Url: _serverPrefix + "resume-test",
            DestinationDirectory: _tempDirectory,
            RequestedConnections: 4,
            CustomFileName: "resumable_file.bin");

        var task = await createHandler.HandleAsync(createCommand);

        // 2. Parça #0 ve Parça #1'in kısmen indirildiğini simüle edip SQLite'a yazalım
        // Chunk 0: 0-262143 (tamamı bitmiş gibi)
        await repository.UpdateChunkOffsetAsync(task.Id, 0, task.Chunks[0].Range.End + 1);
        // Chunk 1: yarısına kadar inmiş
        long halfOffset = task.Chunks[1].Range.Start + (task.Chunks[1].Range.TotalBytes / 2);
        await repository.UpdateChunkOffsetAsync(task.Id, 1, halfOffset);

        // Diske de bu indirilen kısımları yazalım
        await fileStorage.WriteAtAsync(task.DestinationFilePath, 0, _mockPayload.AsMemory(0, (int)halfOffset));

        // Görevi duraklatılmış olarak işaretle
        var pausedTask = await repository.GetByIdAsync(task.Id);
        pausedTask!.Pause();
        await repository.UpdateAsync(pausedTask);

        // 3. Simülasyon: Uygulama yeniden açıldı, yeni repository örneği ile Resume çağrıldı
        await repository.DisposeAsync();
        var freshRepository = new SqliteDownloadRepository(_dbPath);
        var freshResumeHandler = new ResumeDownloadCommandHandler(freshRepository, chunkDownloader, fileStorage);

        var resumeCommand = new ResumeDownloadCommand(task.Id);
        await freshResumeHandler.HandleAsync(resumeCommand);

        // 4. Doğrulama: İndirme tamamlandı mı?
        var completedTask = await freshRepository.GetByIdAsync(task.Id);
        Assert.NotNull(completedTask);
        Assert.Equal(DownloadStatus.Completed, completedTask.Status);
        Assert.True(completedTask.IsAllChunksCompleted);

        // Dosya hash'i orijinaliyle birebir eşleşmeli
        fileStorage.CloseHandle(task.DestinationFilePath);
        byte[] finalBytes = await File.ReadAllBytesAsync(task.DestinationFilePath);
        string finalHash = Convert.ToHexString(SHA256.HashData(finalBytes));

        Assert.Equal(_mockSha256, finalHash);

        await freshRepository.DisposeAsync();
    }

    [Fact]
    public async Task RetryPolicy_OnTransientException_RetriesAndSucceeds()
    {
        int attempts = 0;
        await RetryPolicy.ExecuteAsync(
            attempt =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw new HttpRequestException("Geçici bağlantı hatası", null, HttpStatusCode.ServiceUnavailable);
                }
                return Task.CompletedTask;
            },
            maxRetries: 3,
            initialDelay: TimeSpan.FromMilliseconds(50));

        Assert.Equal(3, attempts);
    }

    public async Task DisposeAsync()
    {
        _serverCts.Cancel();
        try { _listener.Stop(); _listener.Close(); } catch { }
        if (_serverTask != null) { try { await _serverTask; } catch { } }

        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        if (Directory.Exists(_tempDirectory))
        {
            try { Directory.Delete(_tempDirectory, true); } catch { }
        }
    }
}
