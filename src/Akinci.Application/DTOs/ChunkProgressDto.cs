namespace Akinci.Application.DTOs;

/// <summary>
/// Belirli bir chunk'ın anlık ilerleme durumu.
/// </summary>
public sealed record ChunkProgressDto(
    int Index,
    long Start,
    long End,
    long CurrentOffset,
    long DownloadedBytes,
    double ProgressPercentage,
    bool IsCompleted);
