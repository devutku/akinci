namespace Akinci.Application.Events;

/// <summary>
/// Bir chunk'tan yeni bir veri bloğu diske yazıldığında tetiklenen olay.
/// </summary>
public sealed record ChunkProgressChangedEvent(
    Guid TaskId,
    int ChunkIndex,
    long BytesRead,
    long NewOffset);
