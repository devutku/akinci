using Akinci.UI.Services;
using Avalonia;
using System;

namespace Akinci.UI;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        bool shutdownRequested = args.Any(a => string.Equals(a, "--shutdown", StringComparison.OrdinalIgnoreCase));

        if (!SingleInstance.TryAcquire(out var instanceMutex))
        {
            // Başka bir Akıncı örneği çalışıyor: ona kapanma veya öne gelme komutu gönder ve çık
            SingleInstance.SendControlAsync(shutdownRequested ? "shutdown" : "activate")
                .GetAwaiter().GetResult();
            instanceMutex.Dispose();
            return 0;
        }

        if (shutdownRequested)
        {
            // Kapatılacak çalışan bir örnek yok
            instanceMutex.ReleaseMutex();
            instanceMutex.Dispose();
            return 0;
        }

        try
        {
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            GC.KeepAlive(instanceMutex);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
