using Akinci.Application.UnitTests.Fakes;
using Akinci.Application.UseCases.CalculateChunks;
using Akinci.Application.UseCases.PauseDownload;
using Akinci.Domain.Entities;
using Akinci.Domain.Enums;
using Akinci.Domain.ValueObjects;
using Xunit;

namespace Akinci.Application.UnitTests.UseCases;

public class PauseDownloadCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenDownloading_PausesTaskAndUpdatesRepository()
    {
        var repository = new InMemoryDownloadRepository();
        var task = DownloadTask.Create(
            DownloadUrl.Create("https://example.com/file.bin"),
            @"C:\Downloads\file.bin",
            1000,
            true);
        task.InitializeChunks(2);
        task.Start();
        await repository.AddAsync(task);

        var handler = new PauseDownloadCommandHandler(repository);
        var updatedTask = await handler.HandleAsync(new PauseDownloadCommand(task.Id));

        Assert.Equal(DownloadStatus.Paused, updatedTask.Status);
        var retrieved = await repository.GetByIdAsync(task.Id);
        Assert.Equal(DownloadStatus.Paused, retrieved!.Status);
    }
}

public class CalculateChunksQueryHandlerTests
{
    [Fact]
    public void Handle_CalculatesEvenChunksDistribution()
    {
        var handler = new CalculateChunksQueryHandler();
        var query = new CalculateChunksQuery(TotalBytes: 1000, RequestedConnections: 4);

        var chunks = handler.Handle(query);

        Assert.Equal(4, chunks.Count);
        Assert.Equal(0, chunks[0].Start);
        Assert.Equal(249, chunks[0].End);
        Assert.Equal(250, chunks[1].Start);
        Assert.Equal(499, chunks[1].End);
        Assert.Equal(500, chunks[2].Start);
        Assert.Equal(749, chunks[2].End);
        Assert.Equal(750, chunks[3].Start);
        Assert.Equal(999, chunks[3].End);
    }
}
