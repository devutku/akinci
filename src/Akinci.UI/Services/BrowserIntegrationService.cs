using System.Diagnostics;
using System.Text.Json;
using Microsoft.Win32;

namespace Akinci.UI.Services;

public static class BrowserIntegrationService
{
    private const string HostName = "com.akinci.downloader";

    public static bool IsNativeHostRegistered()
    {
        if (!OperatingSystem.IsWindows()) return false;

        try
        {
            using var chromeKey = Registry.CurrentUser.OpenSubKey($@"Software\Google\Chrome\NativeMessagingHosts\{HostName}");
            if (chromeKey != null) return true;

            using var edgeKey = Registry.CurrentUser.OpenSubKey($@"Software\Microsoft\Edge\NativeMessagingHosts\{HostName}");
            if (edgeKey != null) return true;
        }
        catch
        {
            // İzin hatası vb.
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

    public static bool SetupIntegration()
    {
        if (!OperatingSystem.IsWindows()) return false;

        try
        {
            var userAkinciDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".akinci");
            var hostDir = Path.Combine(userAkinciDir, "host");
            var extensionDir = Path.Combine(userAkinciDir, "extension");
            var manifestsDir = Path.Combine(hostDir, "manifests");

            Directory.CreateDirectory(hostDir);
            Directory.CreateDirectory(extensionDir);
            Directory.CreateDirectory(manifestsDir);

            // 1. Kaynak dosyaları bul (dist klasörü, derleme çıktısı veya proje dizini)
            var currentExeDir = AppDomain.CurrentDomain.BaseDirectory;
            var searchPaths = new[]
            {
                Path.Combine(currentExeDir, "..", "Akinci.NativeHost"),
                Path.Combine(currentExeDir, "Akinci.NativeHost"),
                Path.Combine(currentExeDir, "..", "..", "..", "..", "Akinci.NativeHost"),
                @"C:\Users\Admin\git\akinci\akinci\dist\Akinci.NativeHost",
                @"C:\Users\Admin\git\akinci\akinci\src\Akinci.NativeHost"
            };

            string? foundNativeHostExe = null;
            string? foundExtensionSrc = null;

            foreach (var p in searchPaths)
            {
                var exeCandidate = Path.Combine(p, "Akinci-BrowserBridge.exe");
                if (!File.Exists(exeCandidate))
                {
                    exeCandidate = Path.Combine(p, "Akinci.NativeHost.exe");
                }

                if (File.Exists(exeCandidate) && foundNativeHostExe == null)
                {
                    foundNativeHostExe = exeCandidate;
                }

                var extCandidate = Path.Combine(p, "extension");
                if (Directory.Exists(extCandidate) && foundExtensionSrc == null)
                {
                    foundExtensionSrc = extCandidate;
                }
            }

            // Host binary dosyasını kopyala
            var targetHostExe = Path.Combine(hostDir, "Akinci-BrowserBridge.exe");
            if (foundNativeHostExe != null && File.Exists(foundNativeHostExe))
            {
                File.Copy(foundNativeHostExe, targetHostExe, overwrite: true);
            }

            // Eklenti dosyalarını kopyala
            if (foundExtensionSrc != null && Directory.Exists(foundExtensionSrc))
            {
                foreach (var file in Directory.GetFiles(foundExtensionSrc, "*.*", SearchOption.AllDirectories))
                {
                    var rel = Path.GetRelativePath(foundExtensionSrc, file);
                    var dest = Path.Combine(extensionDir, rel);
                    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                    File.Copy(file, dest, overwrite: true);
                }
            }

            // 2. Chrome Manifest dosyasını oluştur (Tam mutlak yol ile)
            var chromeManifestPath = Path.Combine(manifestsDir, $"{HostName}.json");
            var manifestObj = new
            {
                name = HostName,
                description = "Akıncı Downloader Native Messaging Host for Chrome and Chromium-based browsers",
                path = targetHostExe,
                type = "stdio",
                allowed_origins = new[]
                {
                    "chrome-extension://*/*"
                }
            };

            var json = JsonSerializer.Serialize(manifestObj, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(chromeManifestPath, json);

            // 3. Windows Kayıt Defterine Yaz (Google Chrome & Microsoft Edge)
            using (var chromeKey = Registry.CurrentUser.CreateSubKey($@"Software\Google\Chrome\NativeMessagingHosts\{HostName}"))
            {
                chromeKey.SetValue("", chromeManifestPath);
            }

            using (var edgeKey = Registry.CurrentUser.CreateSubKey($@"Software\Microsoft\Edge\NativeMessagingHosts\{HostName}"))
            {
                edgeKey.SetValue("", chromeManifestPath);
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static void OpenExtensionFolder()
    {
        var extDir = GetPermanentExtensionPath();
        if (Directory.Exists(extDir))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = extDir,
                UseShellExecute = true
            });
        }
    }

    public static void OpenChromeExtensionsPage()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "chrome",
                Arguments = "chrome://extensions",
                UseShellExecute = true
            });
        }
        catch
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "msedge",
                    Arguments = "edge://extensions",
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }
}
