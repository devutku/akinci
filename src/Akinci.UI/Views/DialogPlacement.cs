using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Akinci.UI.Views;

/// <summary>
/// İçeriğe göre büyüyen (SizeToContent) diyalogların ekranın kullanılabilir alanına sığmasını sağlar.
/// Yüksek ölçekli ekranlarda (ör. 1920x1080 @ %125) uzun bir diyalog ortalandığında başlık çubuğu
/// ekranın üstünden, alt düğmeler görev çubuğunun arkasına taşabiliyordu.
/// </summary>
internal static class DialogPlacement
{
    /// <summary>Başlık çubuğu ve pencere kenarlıkları için ayrılan pay (DIP).</summary>
    private const double DecorationAllowance = 48;

    /// <summary>Pencere ile ekran kenarı arasında bırakılan boşluk (DIP).</summary>
    private const double ScreenMargin = 12;

    public static void KeepOnScreen(Window window)
    {
        window.Opened += (_, _) =>
        {
            var screen = window.Screens.ScreenFromWindow(window) ?? window.Screens.Primary;
            if (screen is null)
            {
                return;
            }

            var maxClientHeight = screen.WorkingArea.Height / screen.Scaling - DecorationAllowance - 2 * ScreenMargin;
            window.MaxHeight = Math.Max(200, maxClientHeight);

            // Yükseklik sınırı uygulandıktan sonra yerleşim güncellenir; ardından konumu düzelt.
            Dispatcher.UIThread.Post(() => Reposition(window, screen), DispatcherPriority.Background);
        };
    }

    private static void Reposition(Window window, Screen screen)
    {
        var scaling = screen.Scaling;
        var area = screen.WorkingArea;
        var margin = (int)Math.Round(ScreenMargin * scaling);

        var widthPx = (int)Math.Ceiling(window.ClientSize.Width * scaling);
        var heightPx = (int)Math.Ceiling((window.ClientSize.Height + DecorationAllowance) * scaling);

        var x = window.Position.X;
        var y = window.Position.Y;

        // Yükseklik küçüldüyse sahip pencereye göre yeniden ortala
        if (window.Owner is Window owner && window.WindowStartupLocation == WindowStartupLocation.CenterOwner)
        {
            var ownerScaling = owner.DesktopScaling;
            var ownerWidthPx = (int)Math.Ceiling(owner.ClientSize.Width * ownerScaling);
            var ownerHeightPx = (int)Math.Ceiling(owner.ClientSize.Height * ownerScaling);
            x = owner.Position.X + (ownerWidthPx - widthPx) / 2;
            y = owner.Position.Y + (ownerHeightPx - heightPx) / 2;
        }

        // Kullanılabilir alanın içine sıkıştır (başlık çubuğu her zaman görünür kalsın)
        x = Math.Clamp(x, area.X + margin, Math.Max(area.X + margin, area.Right - margin - widthPx));
        y = Math.Clamp(y, area.Y + margin, Math.Max(area.Y + margin, area.Bottom - margin - heightPx));

        window.Position = new PixelPoint(x, y);
    }
}
