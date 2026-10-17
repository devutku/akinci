using Akinci.Domain.Enums;
using Akinci.Domain.ValueObjects;

namespace Akinci.Application.DTOs;

/// <summary>
/// Tüm indirme görevinin anlık genel ve parça parça ilerleme verisi.
/// </summary>
public sealed record DownloadProgressDto(
    Guid TaskId,
    long DownloadedBytes,
    long? TotalBytes,
    double ProgressPercentage,
    DownloadSpeed Speed,
    DownloadStatus Status,
    IReadOnlyList<ChunkProgressDto> Chunks);
