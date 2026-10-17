namespace Akinci.Application.Ports;

public enum NotificationType
{
    Information,
    Success,
    Warning,
    Error
}

/// <summary>
/// İndirme tamamlanma, hata ve VirusTotal güvenlik uyarılarını masaüstü bildirimi olarak gösteren port arayüzü.
/// </summary>
public interface INotificationService
{
    void ShowNotification(string title, string message, NotificationType type = NotificationType.Information);
}
