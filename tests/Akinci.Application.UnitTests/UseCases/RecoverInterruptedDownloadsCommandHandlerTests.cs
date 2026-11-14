using Akinci.Application.UnitTests.Fakes;
using Akinci.Application.UseCases.RecoverInterruptedDownloads;
using Akinci.Domain.Entities;
using Akinci.Domain.Enums;
using Akinci.Domain.ValueObjects;
using Xunit;

namespace Akinci.Application.UnitTests.UseCases;

public class RecoverInterruptedDownloadsCommandHandlerTests
{
    private readonly InMemoryDownloadRepository _repository = new();

    private async Task<DownloadTask> AddTaskAsync(long totalBytes = 1000, int connections = 2)
    {
        var task = DownloadTask.Create(
            DownloadUrl.Create("https://example.com/file.bin"),
            @"C:\Downloads\file.bin",
            totalBytes,
            supportsRangeRequests: true);
        task.InitializeChunks(connections);
        await _repository.AddAsync(task);
        return task;
    }

    [Fact]
    public async Task HandleAsync_PausesTasksLeftInDownloadingState()
    {
        var interrupted = await AddTaskAsync();
        interrupted.Start();
        var pending = await AddTaskAsync();

        var handler = new RecoverInterruptedDownloadsCommandHandler(_repository);
        int recovered = await handler.HandleAsync();

        Assert.Equal(1, recovered);
        var reloadedInterrupted = await _repository.GetByIdAsync(interrupted.Id);
        Assert.Equal(DownloadStatus.Paused, reloadedInterrupted!.Status);
        Assert.NotNull(reloadedInterrupted.PausedAt);
        var reloadedPending = await _repository.GetByIdAsync(pending.Id);
        Assert.Equal(DownloadStatus.Pending, reloadedPending!.Status);
    }

    [Fact]
    public async Task HandleAsync_LeavesCompletedFailedAndPausedTasksUntouched()
    {
        var completed = await AddTaskAsync(totalBytes: 100, connections: 1);
        completed.Start();
        completed.Chunks[0].AdvanceOffset(100);
        completed.Complete();

        var failed = await AddTaskAsync();
        failed.Start();
        failed.Fail("ağ hatası");

        var paused = await AddTaskAsync();
        paused.Start();
        paused.Pause();

        var handler = new RecoverInterruptedDownloadsCommandHandler(_repository);
        int recovered = await handler.HandleAsync();

        Assert.Equal(0, recovered);
        Assert.Equal(DownloadStatus.Completed, (await _repository.GetByIdAsync(completed.Id))!.Status);
        Assert.Equal(DownloadStatus.Failed, (await _repository.GetByIdAsync(failed.Id))!.Status);
        Assert.Equal(DownloadStatus.Paused, (await _repository.GetByIdAsync(paused.Id))!.Status);
    }

    [Fact]
    public async Task HandleAsync_PreservesChunkOffsets()
    {
        var task = await AddTaskAsync(totalBytes: 1000, connections: 2);
        task.Start();
        task.Chunks[0].AdvanceOffset(300);

        var handler = new RecoverInterruptedDownloadsCommandHandler(_repository);
        await handler.HandleAsync();

        var reloaded = await _repository.GetByIdAsync(task.Id);
        Assert.Equal(300, reloaded!.Chunks[0].CurrentOffset);
    }
}
