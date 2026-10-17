using System.Diagnostics;
using System.Text.Json;
using Microsoft.Win32;

namespace Akinci.UI.Services;

public static class BrowserIntegrationService
{
    private const string HostName = "com.akinci.downloader";

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

    public static bool SetupIntegration()
    {
        try
        {
            var userAkinciDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".akinci");
            var hostDir = Path.Combine(userAkinciDir, "host");
            var extensionDir = Path.Combine(userAkinciDir, "extension");
            var manifestsDir = Path.Combine(hostDir, "manifests");

            Directory.CreateDirectory(hostDir);
            Directory.CreateDirectory(extensionDir);
            Directory.CreateDirectory(manifestsDir);

            // 1. Kaynak dosyaları bul
            var currentExeDir = AppDomain.CurrentDomain.BaseDirectory;
            var searchPaths = new List<string>
            {
                currentExeDir,
                Path.Combine(currentExeDir, "..", "Akinci.NativeHost"),
                Path.Combine(currentExeDir, "Akinci.NativeHost"),
                Path.Combine(currentExeDir, "..", "..", "..", "..", "Akinci.NativeHost"),
                Path.Combine(currentExeDir, "..", "dist", "Akinci")
            };

            string? foundNativeHostExe = null;
            string? foundExtensionSrc = null;

            var binaryName = OperatingSystem.IsWindows() ? "Akinci-BrowserBridge.exe" : "Akinci-BrowserBridge";
            var fallbackBinary = OperatingSystem.IsWindows() ? "Akinci.NativeHost.exe" : "Akinci.NativeHost";

            foreach (var p in searchPaths)
            {
                var exeCandidate = Path.Combine(p, binaryName);
                if (!File.Exists(exeCandidate))
                {
                    exeCandidate = Path.Combine(p, fallbackBinary);
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
            var targetHostExe = Path.Combine(hostDir, binaryName);
            if (foundNativeHostExe != null && File.Exists(foundNativeHostExe))
            {
                File.Copy(foundNativeHostExe, targetHostExe, overwrite: true);

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

            // 2. Manifest nesnesi oluştur (Mutlak yol ile)
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
        if (!Directory.Exists(extDir)) return;

        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = extDir,
                    UseShellExecute = true
                });
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", $"\"{extDir}\"");
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start("xdg-open", $"\"{extDir}\"");
            }
        }
        catch { }
    }

    public static void OpenChromeExtensionsPage()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "chrome",
                    Arguments = "chrome://extensions",
                    UseShellExecute = true
                });
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", "-a \"Google Chrome\" \"chrome://extensions\"");
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start("google-chrome", "chrome://extensions");
            }
        }
        catch
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "msedge",
                        Arguments = "edge://extensions",
                        UseShellExecute = true
                    });
                }
            }
            catch { }
        }
    }
}
