using Akinci.Application.DTOs;
using Akinci.Domain.Entities;
using Akinci.Domain.ValueObjects;

namespace Akinci.Application.UseCases.CalculateChunks;

public sealed record CalculateChunksQuery(
    long TotalBytes,
    int RequestedConnections,
    bool SupportsRangeRequests = true);

public sealed class CalculateChunksQueryHandler
{
    public IReadOnlyList<ChunkProgressDto> Handle(CalculateChunksQuery query)
    {
        var dummyTask = DownloadTask.Create(
            DownloadUrl.Create("https://akinci.local/temp"),
            "temp.bin",
            query.TotalBytes,
            query.SupportsRangeRequests);

        dummyTask.InitializeChunks(query.RequestedConnections);

        return dummyTask.Chunks.Select(c => new ChunkProgressDto(
            c.Index,
            c.Range.Start,
            c.Range.End,
            c.CurrentOffset,
            c.DownloadedBytes,
            c.ProgressPercentage,
            c.IsCompleted
        )).ToList();
    }
}
