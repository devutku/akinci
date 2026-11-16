using System.Diagnostics;
using System.Text.Json;
using Microsoft.Win32;

namespace Akinci.UI.Services;

public static class BrowserIntegrationService
{
    private const string HostName = "com.akinci.downloader";

    /// <summary>
    /// Native Messaging Host'a bağlanmasına izin verilen eklenti kimlikleri.
    /// Chrome, allowed_origins içinde joker (chrome-extension://*/*) kabul etmez; kimlikler tek tek yazılmalıdır.
    /// Paketlenmemiş eklenti kimliği, extension/manifest.json içindeki "key" alanından türetilir.
    /// Chrome Web Store sürümü mağazanın atadığı kimliği kullanır.
    /// Değişirse src/Akinci.NativeHost/manifests/com.akinci.downloader.json da güncellenmelidir.
    /// </summary>
    public static readonly string[] AllowedExtensionIds =
    {
        "coeeghhijegibfhhodhnjhjffaipckok", // Chrome Web Store sürümü
        "dhnoabdpnfdkljiknkocppmaoijplgbd"  // Paketlenmemiş (unpacked) geliştirme sürümü
    };

    public static bool IsNativeHostRegistered()
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var chromeKey = Registry.CurrentUser.OpenSubKey($@"Software\Google\Chrome\NativeMessagingHosts\{HostName}");
                if (chromeKey != null) return true;

                using var edgeKey = Registry.CurrentUser.OpenSubKey($@"Software\Microsoft\Edge\NativeMessagingHosts\{HostName}");
                if (edgeKey != null) return true;
            }
            catch { }
            return false;
        }

        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (OperatingSystem.IsLinux())
        {
            var chromePath = Path.Combine(userHome, ".config", "google-chrome", "NativeMessagingHosts", $"{HostName}.json");
            var chromiumPath = Path.Combine(userHome, ".config", "chromium", "NativeMessagingHosts", $"{HostName}.json");
            return File.Exists(chromePath) || File.Exists(chromiumPath);
        }

        if (OperatingSystem.IsMacOS())
        {
            var chromePath = Path.Combine(userHome, "Library", "Application Support", "Google", "Chrome", "NativeMessagingHosts", $"{HostName}.json");
            return File.Exists(chromePath);
        }

        return false;
    }

    public static string GetPermanentExtensionPath()
    {
        var appDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".akinci");
        return Path.Combine(appDir, "extension");
    }

    public static string GetPermanentHostPath()
    {
        var appDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".akinci");
        return Path.Combine(appDir, "host");
    }

    /// <summary>Kurulum sonucu; başarısızsa kullanıcıya gösterilecek hata mesajını taşır.</summary>
    public sealed record IntegrationResult(bool Success, string? Error = null, bool NativeHostRegistered = false);

    /// <summary>Eklenti klasörü ve Native Host çalıştırılabilir dosyasının bulunduğu konumlar.</summary>
    public sealed record IntegrationSources(string? ExtensionDir, string? NativeHostExe);

    private static string HostBinaryName => OperatingSystem.IsWindows() ? "Akinci-BrowserBridge.exe" : "Akinci-BrowserBridge";
    private static string FallbackHostBinaryName => OperatingSystem.IsWindows() ? "Akinci.NativeHost.exe" : "Akinci.NativeHost";

    /// <summary>
    /// Uygulama klasöründen başlayıp üst klasörlere doğru eklenti ve Native Host kaynaklarını arar.
    /// İki düzeni destekler:
    ///  - Yayınlanmış paket (publish.bat): dist/Akinci/{extension, Akinci-BrowserBridge.exe}
    ///  - Kaynak kodundan çalıştırma: src/Akinci.NativeHost/{extension, bin/**/Akinci-BrowserBridge.exe}
    /// Derleme çıktısının derinliği (ör. bin/Debug/net10.0/win-x64) değişse de çalışır.
    /// </summary>
    public static IntegrationSources FindIntegrationSources(string baseDirectory)
    {
        string? extensionDir = null;
        string? hostExe = null;

        var dir = new DirectoryInfo(baseDirectory);
        for (int depth = 0; dir != null && depth < 10; depth++, dir = dir.Parent)
        {
            foreach (var root in new[] { dir.FullName, Path.Combine(dir.FullName, "Akinci.NativeHost") })
            {
                var extCandidate = Path.Combine(root, "extension");
                if (extensionDir == null && File.Exists(Path.Combine(extCandidate, "manifest.json")))
                {
                    extensionDir = extCandidate;
                }

                hostExe ??= FindHostBinary(root);
            }

            if (extensionDir != null && hostExe != null)
            {
                break;
            }
        }

        return new IntegrationSources(extensionDir, hostExe);
    }

    private static string? FindHostBinary(string root)
    {
        foreach (var name in new[] { HostBinaryName, FallbackHostBinaryName })
        {
            var candidate = Path.Combine(root, name);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        // Kaynak düzeni: en son derlenen bin/<Config>/<TFM>/[<RID>]/Akinci-BrowserBridge(.exe)
        var binDir = Path.Combine(root, "bin");
        if (!Directory.Exists(binDir))
        {
            return null;
        }

        try
        {
            return Directory.EnumerateFiles(binDir, HostBinaryName, SearchOption.AllDirectories)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    public static IntegrationResult SetupIntegration()
    {
        try
        {
            var userAkinciDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".akinci");
            var hostDir = Path.Combine(userAkinciDir, "host");
            var extensionDir = Path.Combine(userAkinciDir, "extension");
            var manifestsDir = Path.Combine(hostDir, "manifests");

            // 1. Kaynak dosyaları bul
            var sources = FindIntegrationSources(AppDomain.CurrentDomain.BaseDirectory);
            if (sources.ExtensionDir == null)
            {
                return new IntegrationResult(false,
                    "Eklenti dosyaları bulunamadı. Akıncı klasöründe 'extension' klasörü olduğundan emin olun.");
            }

            Directory.CreateDirectory(hostDir);
            Directory.CreateDirectory(extensionDir);
            Directory.CreateDirectory(manifestsDir);

            // Eklenti dosyalarını kopyala
            foreach (var file in Directory.GetFiles(sources.ExtensionDir, "*.*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(sources.ExtensionDir, file);
                var dest = Path.Combine(extensionDir, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                File.Copy(file, dest, overwrite: true);
            }

            // Native Host bulunamazsa eklenti yine de HTTP IPC (127.0.0.1) üzerinden çalışır;
            // var olmayan bir dosyaya işaret eden manifest kaydetmiyoruz.
            if (sources.NativeHostExe == null)
            {
                return new IntegrationResult(true);
            }

            // Host binary: Tek dosya yayınlanmış sürüm kalıcı klasöre kopyalanır. Kaynak derlemesinde
            // exe yanındaki DLL'lere ihtiyaç duyduğundan yerinde kullanılır.
            string targetHostExe;
            var hostDll = Path.ChangeExtension(sources.NativeHostExe, ".dll");
            if (File.Exists(hostDll))
            {
                targetHostExe = sources.NativeHostExe;
            }
            else
            {
                targetHostExe = Path.Combine(hostDir, HostBinaryName);
                File.Copy(sources.NativeHostExe, targetHostExe, overwrite: true);

                // Linux ve macOS için çalıştırma (execute) yetkisi ver
                if (!OperatingSystem.IsWindows())
                {
                    try
                    {
                        Process.Start("chmod", $"+x \"{targetHostExe}\"")?.WaitForExit();
                    }
                    catch { }
                }
            }

            // 2. Manifest nesnesi oluştur (Mutlak yol ile)
            var manifestObj = new
            {
                name = HostName,
                description = "Akıncı Downloader Native Messaging Host for Chrome and Chromium-based browsers",
                path = targetHostExe,
                type = "stdio",
                allowed_origins = AllowedExtensionIds.Select(id => $"chrome-extension://{id}/").ToArray()
            };
            var json = JsonSerializer.Serialize(manifestObj, new JsonSerializerOptions { WriteIndented = true });

            // 3. Platforma Özgü Kayıt İşlemleri
            if (OperatingSystem.IsWindows())
            {
                var chromeManifestPath = Path.Combine(manifestsDir, $"{HostName}.json");
                File.WriteAllText(chromeManifestPath, json);

                using var chromeKey = Registry.CurrentUser.CreateSubKey($@"Software\Google\Chrome\NativeMessagingHosts\{HostName}");
                chromeKey.SetValue("", chromeManifestPath);

                using var edgeKey = Registry.CurrentUser.CreateSubKey($@"Software\Microsoft\Edge\NativeMessagingHosts\{HostName}");
                edgeKey.SetValue("", chromeManifestPath);
            }
            else
            {
                var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var targetManifestDirs = new List<string>();

                if (OperatingSystem.IsLinux())
                {
                    targetManifestDirs.Add(Path.Combine(userHome, ".config", "google-chrome", "NativeMessagingHosts"));
                    targetManifestDirs.Add(Path.Combine(userHome, ".config", "chromium", "NativeMessagingHosts"));
                    targetManifestDirs.Add(Path.Combine(userHome, ".mozilla", "native-messaging-hosts"));
                }
                else if (OperatingSystem.IsMacOS())
                {
                    targetManifestDirs.Add(Path.Combine(userHome, "Library", "Application Support", "Google", "Chrome", "NativeMessagingHosts"));
                    targetManifestDirs.Add(Path.Combine(userHome, "Library", "Application Support", "Chromium", "NativeMessagingHosts"));
                    targetManifestDirs.Add(Path.Combine(userHome, "Library", "Application Support", "Mozilla", "NativeMessagingHosts"));
                }

                foreach (var dir in targetManifestDirs)
                {
                    try
                    {
                        Directory.CreateDirectory(dir);
                        File.WriteAllText(Path.Combine(dir, $"{HostName}.json"), json);
                    }
                    catch { }
                }
            }

            return new IntegrationResult(true, NativeHostRegistered: true);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new IntegrationResult(false, $"Yetki hatası: {ex.Message} Lütfen Akıncı'yı yönetici olarak çalıştırın.");
        }
        catch (Exception ex)
        {
            return new IntegrationResult(false, $"Eklenti kurulamadı: {ex.Message}");
        }
    }

    /// <summary>Akıncı tarayıcı eklentisinin Chrome Web Mağazası sayfası.</summary>
    public const string ChromeWebStoreUrl = "https://chromewebstore.google.com/detail/coeeghhijegibfhhodhnjhjffaipckok";

    /// <summary>
    /// Eklentinin Chrome Web Mağazası sayfasını açar: Chrome kuruluysa Chrome'da, değilse varsayılan tarayıcıda
    /// (Edge, "diğer mağazalardan eklentilere izin ver" ile Chrome Web Mağazası'ndan kurulum yapabilir).
    /// </summary>
    public static void OpenChromeWebStorePage()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                // "chrome", Windows'un App Paths kaydı üzerinden çözümlenir
                Process.Start(new ProcessStartInfo { FileName = "chrome", Arguments = ChromeWebStoreUrl, UseShellExecute = true });
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", $"-a \"Google Chrome\" \"{ChromeWebStoreUrl}\"")?.WaitForExit();
            }
            else
            {
                Process.Start("google-chrome", ChromeWebStoreUrl);
            }
            return;
        }
        catch
        {
            // Chrome yok: varsayılan tarayıcıya düş
        }

        try
        {
            if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", ChromeWebStoreUrl);
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start("xdg-open", ChromeWebStoreUrl);
            }
            else
            {
                Process.Start(new ProcessStartInfo { FileName = ChromeWebStoreUrl, UseShellExecute = true });
            }
        }
        catch { }
    }
}
