using Akinci.Application.DTOs;
using Akinci.Application.UnitTests.Fakes;
using Akinci.Application.UseCases.StartDownload;
using Akinci.Domain.Entities;
using Akinci.Domain.Enums;
using Akinci.Domain.ValueObjects;
using Xunit;

namespace Akinci.Application.UnitTests.UseCases;

public class StartDownloadCommandHandlerTests
{
    private readonly InMemoryDownloadRepository _repository = new();
    private readonly FakeChunkDownloader _chunkDownloader = new();
    private readonly FakeFileStorageService _fileStorage = new();

    private async Task<DownloadTask> CreateAndSaveTaskAsync(long totalBytes = 1024, int connections = 4)
    {
        var task = DownloadTask.Create(
            DownloadUrl.Create("https://example.com/data.bin"),
            @"C:\Downloads\data.bin",
            totalBytes,
            supportsRangeRequests: true);

        task.InitializeChunks(connections);
        await _repository.AddAsync(task);
        return task;
    }

    [Fact]
    public async Task HandleAsync_AllChunksDownloadSuccessfully_CompletesTask()
    {
        var task = await CreateAndSaveTaskAsync(totalBytes: 1024, connections: 4);
        var handler = new StartDownloadCommandHandler(_repository, _chunkDownloader, _fileStorage);

        var command = new StartDownloadCommand(task.Id);
        await handler.HandleAsync(command);

        var updatedTask = await _repository.GetByIdAsync(task.Id);
        Assert.NotNull(updatedTask);
        Assert.Equal(DownloadStatus.Completed, updatedTask.Status);
        Assert.True(updatedTask.IsAllChunksCompleted);
        Assert.Equal(1024, updatedTask.DownloadedBytes);

        // Verify disk contents
        Assert.True(_fileStorage.FileExists(task.DestinationFilePath));
        Assert.Equal(1024, _fileStorage.GetFileSize(task.DestinationFilePath));
    }

    [Fact]
    public async Task HandleAsync_WithProgressCallback_ReceivesReports()
    {
        var task = await CreateAndSaveTaskAsync(totalBytes: 512, connections: 2);
        var handler = new StartDownloadCommandHandler(_repository, _chunkDownloader, _fileStorage);

        var progressReports = new List<DownloadProgressDto>();
        var progress = new Progress<DownloadProgressDto>(p => progressReports.Add(p));

        var command = new StartDownloadCommand(task.Id, progress);
        await handler.HandleAsync(command);

        var updatedTask = await _repository.GetByIdAsync(task.Id);
        Assert.Equal(DownloadStatus.Completed, updatedTask!.Status);
    }

    [Fact]
    public async Task HandleAsync_WhenCancellationTokenCanceled_PausesTask()
    {
        var task = await CreateAndSaveTaskAsync(totalBytes: 10000, connections: 4);
        var handler = new StartDownloadCommandHandler(_repository, _chunkDownloader, _fileStorage);

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-canceled to simulate instant cancellation

        var command = new StartDownloadCommand(task.Id);
        await handler.HandleAsync(command, cts.Token);

        var updatedTask = await _repository.GetByIdAsync(task.Id);
        Assert.NotNull(updatedTask);
        Assert.Equal(DownloadStatus.Paused, updatedTask.Status);
    }

    [Fact]
    public async Task HandleAsync_WhenDownloaderFails_SetsTaskFailedAndRethrows()
    {
        var task = await CreateAndSaveTaskAsync(totalBytes: 1024, connections: 2);
        _chunkDownloader.SimulateFailure = true;

        var handler = new StartDownloadCommandHandler(_repository, _chunkDownloader, _fileStorage);
        var command = new StartDownloadCommand(task.Id);

        await Assert.ThrowsAsync<HttpRequestException>(() => handler.HandleAsync(command));

        var updatedTask = await _repository.GetByIdAsync(task.Id);
        Assert.NotNull(updatedTask);
        Assert.Equal(DownloadStatus.Failed, updatedTask.Status);
        Assert.Contains("Simüle edilmiş ağ kopması", updatedTask.FailureReason);
    }
}
