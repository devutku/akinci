using Akinci.Application.Ports;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using AppNotificationType = Akinci.Application.Ports.NotificationType;
using AvaNotificationType = Avalonia.Controls.Notifications.NotificationType;

namespace Akinci.UI.Services;

/// <summary>
/// Avalonia WindowNotificationManager kullanarak modern toast masaüstü bildirimleri gösteren servis.
/// </summary>
public sealed class DesktopNotificationService : INotificationService
{
    private WindowNotificationManager? _notificationManager;

    public void AttachWindow(TopLevel topLevel)
    {
        _notificationManager = new WindowNotificationManager(topLevel)
        {
            Position = NotificationPosition.BottomRight,
            MaxItems = 4
        };
    }

    public void ShowNotification(string title, string message, AppNotificationType type = AppNotificationType.Information)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_notificationManager == null) return;

            var avaloniaType = type switch
            {
                AppNotificationType.Success => AvaNotificationType.Success,
                AppNotificationType.Warning => AvaNotificationType.Warning,
                AppNotificationType.Error => AvaNotificationType.Error,
                _ => AvaNotificationType.Information
            };

            _notificationManager.Show(new Notification(
                title,
                message,
                avaloniaType,
                expiration: TimeSpan.FromSeconds(6)));
        });
    }
}
