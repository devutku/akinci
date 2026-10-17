using Akinci.Domain.Enums;

namespace Akinci.Domain.Exceptions;

/// <summary>
/// Geçersiz bir durum geçişi yapılmak istendiğinde fırlatılır (Örn: Tamamlanmış indirmeyi duraklatmak).
/// </summary>
public sealed class InvalidStateTransitionException : DomainException
{
    public DownloadStatus CurrentStatus { get; }
    public DownloadStatus TargetStatus { get; }

    public InvalidStateTransitionException(DownloadStatus currentStatus, DownloadStatus targetStatus)
        : base($"'{currentStatus}' durumundan '{targetStatus}' durumuna geçiş yapılamaz.")
    {
        CurrentStatus = currentStatus;
        TargetStatus = targetStatus;
    }
}
