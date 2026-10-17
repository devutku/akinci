using Akinci.Domain.Entities;

namespace Akinci.Application.Ports;

/// <summary>
/// İndirme görevlerinin durumlarını ve chunk ofsetlerini kalıcı depoya (SQLite, LiteDB, Memory) saklayan port arayüzü.
/// </summary>
public interface IDownloadRepository
{
    Task<DownloadTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DownloadTask>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(DownloadTask task, CancellationToken cancellationToken = default);

    Task UpdateAsync(DownloadTask task, CancellationToken cancellationToken = default);

    Task UpdateChunkOffsetAsync(Guid taskId, int chunkIndex, long newOffset, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
