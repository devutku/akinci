using Akinci.Domain.Enums;

namespace Akinci.Application.Events;

/// <summary>
/// İndirme görevinin durumu değiştiğinde tetiklenen olay.
/// </summary>
public sealed record DownloadStatusChangedEvent(
    Guid TaskId,
    DownloadStatus OldStatus,
    DownloadStatus NewStatus,
    string? FailureReason = null);
