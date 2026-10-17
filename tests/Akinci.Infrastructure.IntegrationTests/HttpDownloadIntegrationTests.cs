using System.Net;
using System.Security.Cryptography;
using System.Text;
using Akinci.Application.UseCases.CreateDownload;
using Akinci.Application.UseCases.StartDownload;
using Akinci.Domain.Enums;
using Akinci.Infrastructure.Http;
using Akinci.Infrastructure.Persistence;
using Akinci.Infrastructure.Storage;
using Xunit;

namespace Akinci.Infrastructure.IntegrationTests;

public class HttpDownloadIntegrationTests : IAsyncLifetime
{
    private readonly HttpListener _listener;
    private readonly string _serverPrefix;
    private readonly byte[] _mockPayload;
    private readonly string _mockSha256;
    private readonly CancellationTokenSource _serverCts;
    private readonly string _tempDirectory;
    private Task? _serverTask;

    public HttpDownloadIntegrationTests()
    {
        // 1. 2 MB test verisi üret ve SHA-256 hesapla
        _mockPayload = new byte[2 * 1024 * 1024]; // 2 MB
        RandomNumberGenerator.Fill(_mockPayload);
        _mockSha256 = Convert.ToHexString(SHA256.HashData(_mockPayload));

        // 2. Rastgele portta HttpListener başlat
        int port = Random.Shared.Next(40000, 50000);
        _serverPrefix = $"http://127.0.0.1:{port}/";
        _listener = new HttpListener();
        _listener.Prefixes.Add(_serverPrefix);
        _serverCts = new CancellationTokenSource();

        _tempDirectory = Path.Combine(Path.GetTempPath(), "Akinci_HttpTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
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
            catch (HttpListenerException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        response.Headers.Add("Accept-Ranges", "bytes");
        response.Headers.Add("Content-Disposition", "attachment; filename=\"test-artifact.bin\"");

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
                // Format: bytes=0-1023
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

            // Tam GET
            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentLength64 = _mockPayload.Length;
            await response.OutputStream.WriteAsync(_mockPayload);
            response.Close();
        }
    }

    [Fact]
    public async Task EndToEnd_MultiConnectionDownload_DownloadsExactPayloadAndMatchesSha256()
    {
        var metadataService = new HttpMetadataService();
        var chunkDownloader = new HttpChunkDownloader();
        using var fileStorage = new RandomAccessFileStorageService();
        var repository = new InMemoryDownloadRepository();

        var createHandler = new CreateDownloadCommandHandler(metadataService, repository, fileStorage);
        var startHandler = new StartDownloadCommandHandler(repository, chunkDownloader, fileStorage);

        // 8 paralel bağlantı ile indirme emri ver
        var createCommand = new CreateDownloadCommand(
            Url: _serverPrefix + "download",
            DestinationDirectory: _tempDirectory,
            RequestedConnections: 8,
            CustomFileName: "downloaded_test.bin");

        var task = await createHandler.HandleAsync(createCommand);

        Assert.Equal(8, task.Chunks.Count);
        Assert.Equal(_mockPayload.Length, task.TotalBytes);
        Assert.True(task.SupportsRangeRequests);

        // İndirmeyi başlat ve tamamlanmasını bekle
        var startCommand = new StartDownloadCommand(task.Id);
        await startHandler.HandleAsync(startCommand);

        // Görev durumu kontrolü
        var completedTask = await repository.GetByIdAsync(task.Id);
        Assert.NotNull(completedTask);
        Assert.Equal(DownloadStatus.Completed, completedTask.Status);
        Assert.True(completedTask.IsAllChunksCompleted);
        Assert.Equal(_mockPayload.Length, completedTask.DownloadedBytes);

        // Dosya diski ve SHA-256 kontrolü
        fileStorage.CloseHandle(task.DestinationFilePath);
        Assert.True(File.Exists(task.DestinationFilePath));

        byte[] downloadedBytes = await File.ReadAllBytesAsync(task.DestinationFilePath);
        string downloadedSha256 = Convert.ToHexString(SHA256.HashData(downloadedBytes));

        Assert.Equal(_mockSha256, downloadedSha256);
    }

    public async Task DisposeAsync()
    {
        _serverCts.Cancel();
        try
        {
            _listener.Stop();
            _listener.Close();
        }
        catch
        {
            // Listener kapatma
        }

        if (_serverTask != null)
        {
            try
            {
                await _serverTask;
            }
            catch
            {
                // Server task tamamlama
            }
        }

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
