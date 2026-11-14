namespace Akinci.Infrastructure.Settings;

/// <summary>
/// Başka bir süreç (ör. kurulum programı) tarafından bırakılan tek satırlık bir ayar değerini
/// bir kez okuyup dosyayı silen yardımcı. Kurulum programı VirusTotal API anahtarını bu yolla
/// uygulamaya devreder; uygulama açılışta değeri ayarlarına alır.
/// </summary>
public static class PendingSettingFile
{
    /// <summary>
    /// Dosya varsa içeriğini kırpılmış olarak döndürür ve dosyayı siler.
    /// Dosya yoksa, içerik boşsa veya okuma/silme başarısız olursa <c>null</c> döner (istisna fırlatmaz).
    /// </summary>
    public static string? TryConsume(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var value = File.ReadAllText(filePath).Trim();
            File.Delete(filePath);
            return value.Length == 0 ? null : value;
        }
        catch
        {
            return null;
        }
    }
}
