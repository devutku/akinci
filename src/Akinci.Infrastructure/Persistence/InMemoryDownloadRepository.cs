using System.Collections.Concurrent;
using Akinci.Application.Ports;
using Akinci.Domain.Entities;

namespace Akinci.Infrastructure.Persistence;

/// <summary>
/// Sprint 2 için bellek tabanlı, thread-safe indirme deposu (Sprint 3'te SQLite/LiteDB ile kalıcı hale getirilecektir).
/// </summary>
public sealed class InMemoryDownloadRepository : IDownloadRepository
{
    private readonly ConcurrentDictionary<Guid, DownloadTask> _tasks = new();

    public Task<DownloadTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _tasks.TryGetValue(id, out var task);
        return Task.FromResult(task);
    }

    public Task<IReadOnlyList<DownloadTask>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DownloadTask> all = _tasks.Values.ToList();
        return Task.FromResult(all);
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
        _tasks.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
