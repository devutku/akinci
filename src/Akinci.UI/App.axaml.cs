using Akinci.Application.Ports;
using Akinci.Application.UseCases.CreateDownload;
using Akinci.Application.UseCases.PauseDownload;
using Akinci.Application.UseCases.RecoverInterruptedDownloads;
using Akinci.Application.UseCases.ResumeDownload;
using Akinci.Application.UseCases.ScanSecurity;
using Akinci.Application.UseCases.StartDownload;
using Akinci.Infrastructure.Adapters;
using Akinci.Infrastructure.Persistence;
using Akinci.Infrastructure.Resilience;
using Akinci.Infrastructure.Security;
using Akinci.Infrastructure.Storage;
using Akinci.UI.Services;
using Akinci.UI.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;

namespace Akinci.UI;

public partial class App : Avalonia.Application
{
    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Dependency Injection Konteynerini Kur
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        Services = serviceCollection.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindowVm = Services.GetRequiredService<MainWindowViewModel>();
            var settingsService = Services.GetRequiredService<SettingsService>();
            var notificationService = Services.GetRequiredService<DesktopNotificationService>();
            var ipcServer = Services.GetRequiredService<IpcServerService>();

            var throttler = Services.GetRequiredService<IBandwidthThrottler>();
            var mainWindow = new MainWindow(mainWindowVm, settingsService, notificationService, throttler);
            desktop.MainWindow = mainWindow;

            _desktop = desktop;
            _mainWindow = mainWindow;
            _mainWindowVm = mainWindowVm;

            // Kurulum programı / ikinci örnek tarafından gönderilen yerel kontrol komutları
            ipcServer.ShutdownRequested = () => Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(RequestGracefulShutdownAsync);
            ipcServer.ActivateRequested = () => Avalonia.Threading.Dispatcher.UIThread.Post(ShowMainWindow);

            // Arka plan IPC HTTP sunucusunu başlat (Tarayıcı eklentisi ve NativeHost dinleyici)
            ipcServer.Start();
            desktop.Exit += async (_, _) => await ipcServer.DisposeAsync();

            // Sistem Tepsisi (TrayIcon) Yapılandırması
            SetupTrayIcon(mainWindowVm);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private IClassicDesktopStyleApplicationLifetime? _desktop;
    private MainWindow? _mainWindow;
    private MainWindowViewModel? _mainWindowVm;
    private bool _shutdownInProgress;

    /// <summary>
    /// Devam eden indirmeleri duraklatıp kaydeder ve uygulamayı tamamen kapatır (tepsiye küçülmez).
    /// Tepsideki "Çıkış" ve kurulum programının gönderdiği "shutdown" komutu bu yolu kullanır.
    /// </summary>
    public async Task RequestGracefulShutdownAsync()
    {
        if (_shutdownInProgress || _desktop == null || _mainWindow == null || _mainWindowVm == null)
        {
            return;
        }

        _shutdownInProgress = true;

        // Kapanış sırasında kuyruk yeni indirme başlatmasın; ardından indirmeleri duraklat (en fazla 5 sn)
        _mainWindowVm.SuspendQueue();
        var pauseAll = _mainWindowVm.PauseAllAsync();
        await Task.WhenAny(pauseAll, Task.Delay(TimeSpan.FromSeconds(5)));

        // İptal edilen indirme işleyicilerinin son parça ofsetlerini kaydetmesine kısa bir süre tanı
        await Task.Delay(500);

        _mainWindow.AllowClose = true;
        _desktop.Shutdown();
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null)
        {
            return;
        }

        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private void SetupTrayIcon(MainWindowViewModel mainWindowVm)
    {
        var trayIcon = new TrayIcon
        {
            ToolTipText = "Akıncı - Çok Kanallı İndirme Yöneticisi",
            Icon = _mainWindow!.Icon,
            IsVisible = true
        };

        var menu = new NativeMenu();

        var showItem = new NativeMenuItem("Göster");
        showItem.Click += (_, _) => ShowMainWindow();
        menu.Add(showItem);

        menu.Add(new NativeMenuItemSeparator());

        var resumeAllItem = new NativeMenuItem("Tümünü Başlat");
        resumeAllItem.Click += async (_, _) => await mainWindowVm.ResumeAllAsync();
        menu.Add(resumeAllItem);

        var pauseAllItem = new NativeMenuItem("Tümünü Duraklat");
        pauseAllItem.Click += async (_, _) => await mainWindowVm.PauseAllAsync();
        menu.Add(pauseAllItem);

        menu.Add(new NativeMenuItemSeparator());

        var exitItem = new NativeMenuItem("Çıkış");
        exitItem.Click += async (_, _) => await RequestGracefulShutdownAsync();
        menu.Add(exitItem);

        trayIcon.Menu = menu;
        trayIcon.Clicked += (_, _) => ShowMainWindow();

        var icons = new TrayIcons { trayIcon };
        TrayIcon.SetIcons(this, icons);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // 1. Ayar Servisi
        var settingsService = new SettingsService();
        services.AddSingleton(settingsService);

        // 2. Altyapı Katmanı (Infrastructure)
        services.AddSingleton<IFileStorageService, RandomAccessFileStorageService>();

        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".akinci",
            "downloads.db");
        services.AddSingleton<IDownloadRepository>(_ => new SqliteDownloadRepository(dbPath));

        services.AddSingleton<IHttpMetadataService, ProtocolMetadataService>();
        services.AddSingleton<IChunkDownloader, ProtocolChunkDownloader>();
        services.AddSingleton<IFileHashCalculator, StreamingFileHashCalculator>();

        services.AddSingleton<IFileSecurityScanner>(sp =>
        {
            var settings = sp.GetRequiredService<SettingsService>();
            return new VirusTotalSecurityScanner(apiKey: settings.Current.VirusTotalApiKey);
        });

        services.AddSingleton<IBandwidthThrottler>(sp =>
        {
            var settings = sp.GetRequiredService<SettingsService>();
            return new TokenBucketThrottler(settings.Current.SpeedLimitBytesPerSecond);
        });

        // 3. Kullanım Senaryoları (Application Use Cases)
        services.AddTransient<CreateDownloadCommandHandler>();
        services.AddTransient<StartDownloadCommandHandler>(sp => new StartDownloadCommandHandler(
            sp.GetRequiredService<IDownloadRepository>(),
            sp.GetRequiredService<IChunkDownloader>(),
            sp.GetRequiredService<IFileStorageService>(),
            sp.GetRequiredService<IFileHashCalculator>(),
            sp.GetRequiredService<IFileSecurityScanner>(),
            sp.GetRequiredService<IBandwidthThrottler>()));
        services.AddTransient<PauseDownloadCommandHandler>();
        services.AddTransient<ResumeDownloadCommandHandler>(sp => new ResumeDownloadCommandHandler(
            sp.GetRequiredService<IDownloadRepository>(),
            sp.GetRequiredService<IChunkDownloader>(),
            sp.GetRequiredService<IFileStorageService>(),
            sp.GetRequiredService<IFileHashCalculator>(),
            sp.GetRequiredService<IFileSecurityScanner>(),
            sp.GetRequiredService<IBandwidthThrottler>()));
        services.AddTransient<ScanFileSecurityCommandHandler>();
        services.AddTransient<RecoverInterruptedDownloadsCommandHandler>();

        // 4. UI ve Bildirim Servisleri & ViewModels
        services.AddSingleton<DesktopNotificationService>();
        services.AddSingleton<INotificationService>(sp => sp.GetRequiredService<DesktopNotificationService>());
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<IpcServerService>();
    }
}