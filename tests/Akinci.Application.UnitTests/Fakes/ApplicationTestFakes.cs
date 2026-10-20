using Akinci.Application.DTOs;
using Akinci.Application.Ports;
using Akinci.Domain.Entities;
using Akinci.Domain.ValueObjects;

namespace Akinci.Application.UnitTests.Fakes;

public class InMemoryDownloadRepository : IDownloadRepository
{
    private readonly Dictionary<Guid, DownloadTask> _tasks = new();

    public Task<DownloadTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _tasks.TryGetValue(id, out var task);
        return Task.FromResult(task);
    }

    public Task<IReadOnlyList<DownloadTask>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DownloadTask> list = _tasks.Values.ToList();
        return Task.FromResult(list);
    }

    public Task AddAsync(DownloadTask task, CancellationToken cancellationToken = default)
    {
        _tasks[task.Id] = task;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(DownloadTask task, CancellationToken cancellationToken = default)
    {
        _tasks[task.Id] = task;
        return Task.CompletedTask;
    }

    public Task UpdateChunkOffsetAsync(Guid taskId, int chunkIndex, long newOffset, CancellationToken cancellationToken = default)
    {
        if (_tasks.TryGetValue(taskId, out var task))
        {
            var chunk = task.Chunks.FirstOrDefault(c => c.Index == chunkIndex);
            chunk?.SetOffset(newOffset);
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _tasks.Remove(id);
        return Task.CompletedTask;
    }
}

public class FakeHttpMetadataService : IHttpMetadataService
{
    public FileMetadataDto MetadataToReturn { get; set; } = new(
        ContentLength: 1024,
        SupportsRangeRequests: true,
        SuggestedFileName: "remote-file.zip",
        ETag: "\"12345\"",
        ContentType: "application/zip");

    public Task<FileMetadataDto> GetMetadataAsync(
        DownloadUrl url,
        DownloadCredentials? credentials = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(MetadataToReturn);
    }
}

public class FakeFileStorageService : IFileStorageService
{
    public Dictionary<string, byte[]> Files { get; } = new();
    public HashSet<string> PreAllocatedFiles { get; } = new();

    public Task PreAllocateAsync(string filePath, long totalBytes, CancellationToken cancellationToken = default)
    {
        PreAllocatedFiles.Add(filePath);
        Files[filePath] = new byte[totalBytes];
        return Task.CompletedTask;
    }

    public Task WriteAtAsync(string filePath, long fileOffset, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        if (!Files.ContainsKey(filePath))
        {
            Files[filePath] = new byte[fileOffset + data.Length];
        }

        var target = Files[filePath];
        if (target.Length < fileOffset + data.Length)
        {
            Array.Resize(ref target, (int)(fileOffset + data.Length));
            Files[filePath] = target;
        }

        data.Span.CopyTo(target.AsSpan((int)fileOffset));
        return Task.CompletedTask;
    }

    public bool FileExists(string filePath) => Files.ContainsKey(filePath);

    public long GetFileSize(string filePath) => Files.TryGetValue(filePath, out var data) ? data.Length : 0;

    public void DeleteFile(string filePath) => Files.Remove(filePath);

    public void CloseHandle(string filePath) { }
    public long? AvailableFreeSpace { get; set; } = null;
    public long? GetAvailableFreeSpace(string directoryPath) => AvailableFreeSpace;
}

public class FakeChunkDownloader : IChunkDownloader
{
    public byte FillByte { get; set; } = 0xAA;
    public bool SimulateFailure { get; set; }

    public async Task DownloadChunkAsync(
        DownloadUrl url,
        Chunk chunk,
        Func<long, ReadOnlyMemory<byte>, CancellationToken, Task> onDataReceived,
        DownloadCredentials? credentials = null,
        CancellationToken cancellationToken = default)
    {
        if (SimulateFailure)
        {
            throw new HttpRequestException("Simüle edilmiş ağ kopması.");
        }

        long remaining = chunk.RemainingBytes;
        long currentOffset = chunk.CurrentOffset;
        const int bufferSize = 256;

        while (remaining > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int toRead = (int)Math.Min(bufferSize, remaining);
            var buffer = new byte[toRead];
            Array.Fill(buffer, FillByte);

            await onDataReceived(currentOffset, buffer, cancellationToken);

            currentOffset += toRead;
            remaining -= toRead;
        }
    }
}
