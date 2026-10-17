using Akinci.Application.Ports;
using Akinci.Application.UseCases.CreateDownload;
using Akinci.Application.UseCases.PauseDownload;
using Akinci.Application.UseCases.ResumeDownload;
using Akinci.Application.UseCases.ScanSecurity;
using Akinci.Application.UseCases.StartDownload;
using Akinci.Infrastructure.Adapters;
using Akinci.Infrastructure.Persistence;
using Akinci.Infrastructure.Security;
using Akinci.Infrastructure.Storage;
using Akinci.UI.Services;
using Akinci.UI.ViewModels;
using Avalonia;
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
            desktop.MainWindow = new MainWindow(mainWindowVm, settingsService);
        }

        base.OnFrameworkInitializationCompleted();
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

        // 3. Kullanım Senaryoları (Application Use Cases)
        services.AddTransient<CreateDownloadCommandHandler>();
        services.AddTransient<StartDownloadCommandHandler>();
        services.AddTransient<PauseDownloadCommandHandler>();
        services.AddTransient<ResumeDownloadCommandHandler>();
        services.AddTransient<ScanFileSecurityCommandHandler>();

        // 4. ViewModels
        services.AddSingleton<MainWindowViewModel>();
    }
}