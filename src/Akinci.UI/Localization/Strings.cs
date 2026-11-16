using static Akinci.UI.Localization.Loc;

namespace Akinci.UI.Localization;

/// <summary>
/// Arayüz metinleri (Türkçe, İngilizce). XAML'de <c>{x:Static l:Strings.Ad}</c> ile kullanılır.
/// Yeni bir metin eklerken iki dili de aynı satırda yazın.
/// </summary>
public static class Strings
{
    // ── Ortak ───────────────────────────────────────────────────────────────
    public static string Cancel => T("İptal", "Cancel");
    public static string Browse => T("Seç...", "Browse...");
    public static string Save => T("Kaydet", "Save");

    // ── Ana pencere ─────────────────────────────────────────────────────────
    public static string MainWindowTitle => T("AKINCI - İndirme Yöneticisi", "AKINCI - Download Manager");
    public static string SearchPlaceholder => T("Filtrele veya ara (Ctrl+F)...", "Filter or search (Ctrl+F)...");
    public static string ActiveLabel => T("Aktif:", "Active:");
    public static string CompletedLabel => T("Biten:", "Done:");
    public static string PauseAll => T("Tümünü Duraklat", "Pause All");
    public static string ResumeAll => T("Tümünü Başlat", "Start All");
    public static string Settings => T("Ayarlar", "Settings");
    public static string AddDownloadButton => T("+ İndirme Ekle", "+ Add Download");
    public static string Minimize => T("Simge Durumuna Küçült", "Minimize");
    public static string MaximizeRestore => T("Büyüt / Önceki Boyut", "Maximize / Restore");
    public static string CloseToBackground => T("Kapat (Arka Plana Al)", "Close (keep running in background)");

    public static string SectionDownloads => T("İNDİRMELER", "DOWNLOADS");
    public static string FilterAll => T("Tümü", "All");
    public static string FilterDownloading => T("İndirilenler", "Downloading");
    public static string FilterPaused => T("Duraklatılanlar", "Paused");
    public static string FilterCompleted => T("Tamamlananlar", "Completed");

    public static string SectionCategories => T("KATEGORİLER", "CATEGORIES");
    public static string CategoryAll => T("Tüm Türler", "All Types");
    public static string CategoryVideo => T("Videolar", "Videos");
    public static string CategoryAudio => T("Müzik & Ses", "Music & Audio");
    public static string CategoryDocuments => T("Belgeler", "Documents");
    public static string CategoryPrograms => T("Programlar", "Programs");
    public static string CategoryArchives => T("Sıkıştırılmış", "Archives");
    public static string CategoryOther => T("Diğer", "Other");

    public static string EmptyTitle => T("Henüz bir indirme eklenmedi", "No downloads yet");
    public static string EmptyHint => T(
        "Bağlantı yapıştırmak için Ctrl + V tuşlarına basın veya yukarıdan '+ İndirme Ekle'ye tıklayın.",
        "Press Ctrl + V to paste a link, or click '+ Add Download' above.");

    public static string OpenFile => T("Dosyayı Aç", "Open File");
    public static string ShowInFolder => T("Klasörde Göster", "Show in Folder");
    public static string PauseDownload => T("İndirmeyi Duraklat", "Pause Download");
    public static string ResumeDownload => T("İndirmeye Devam Et", "Resume Download");
    public static string StartOrResumeDownload => T("İndirmeyi Başlat / Devam Et", "Start / Resume Download");
    public static string Restart => T("Yeniden Başlat", "Restart");
    public static string CopyUrl => T("İndirme Adresini Kopyala", "Copy Download URL");
    public static string CopySha256 => T("SHA-256 Değerini Kopyala", "Copy SHA-256 Hash");
    public static string OpenSecurityReport => T("Güvenlik Raporunu Aç", "Open Security Report");
    public static string DeleteDownloadMenu => T("İndirmeyi Sil...", "Delete Download...");
    public static string SecurityBadgeTooltip => T("VirusTotal Güvenlik Raporu (Tıkla ve Aç)", "VirusTotal security report (click to open)");

    public static string RunningInBackgroundTitle => T("Akıncı Arka Planda Çalışıyor", "Akıncı Is Running in the Background");
    public static string RunningInBackgroundBody => T(
        "İndirmeleriniz devam ediyor. Görev çubuğundan geri açabilirsiniz.",
        "Your downloads continue. You can reopen Akıncı from the system tray.");

    // ── Sistem tepsisi ──────────────────────────────────────────────────────
    public static string TrayTooltip => T("Akıncı - Çok Kanallı İndirme Yöneticisi", "Akıncı - Multi-connection Download Manager");
    public static string TrayShow => T("Göster", "Show");
    public static string TrayExit => T("Çıkış", "Exit");

    // ── İndirme satırı ──────────────────────────────────────────────────────
    public static string StatusPending => T("Beklemede", "Pending");
    public static string StatusDownloading => T("İndiriliyor", "Downloading");
    public static string StatusCompleted => T("Tamamlandı", "Completed");
    public static string StatusPaused => T("Duraklatıldı", "Paused");
    public static string StatusFailed => T("Hata Oluştu", "Failed");
    public static string StatusCanceled => T("İptal Edildi", "Canceled");
    public static string StatusError(string message) => T($"Hata: {message}", $"Error: {message}");

    public static string EtaFailed => T("Hata", "Error");
    public static string EtaCanceled => T("İptal", "Canceled");
    public static string EtaUnderOneSecond => T("Kalan: < 1 sn", "< 1 s left");
    public static string EtaSeconds(int seconds) => T($"Kalan: {seconds} sn", $"{seconds} s left");
    public static string EtaMinutes(int minutes, int seconds) => T($"Kalan: {minutes} dk {seconds} sn", $"{minutes} min {seconds} s left");
    public static string EtaHours(int hours, int minutes) => T($"Kalan: {hours} sa {minutes} dk", $"{hours} h {minutes} min left");

    public static string SizeUnknown => T("Boyut Belirsiz", "Unknown size");
    public static string TotalUnknown => T("Bilinmiyor", "Unknown");
    public static string DownloadedAmount(string amount) => T($"{amount} indirildi", $"{amount} downloaded");
    public static string ConnectionCount(int count) => T($"{count} bağlantı", count == 1 ? "1 connection" : $"{count} connections");
    public static string ChunkTitle(int number) => T($"Parça #{number}", $"Part #{number}");

    // ── Güvenlik rozeti ─────────────────────────────────────────────────────
    public static string SecurityAwaitingScan => T("Tarama Bekleniyor", "Awaiting scan");
    public static string SecurityNotFound => T("VirusTotal veritabanında kaydı yok (Bilinmeyen/Yeni dosya)", "Not in VirusTotal database (unknown/new file)");
    public static string SecurityNoApiKey => T("VirusTotal API anahtarı girilmedi (Tarama atlandı)", "No VirusTotal API key (scan skipped)");
    public static string SecurityRateLimited => T("VirusTotal API istek limiti aşıldı (Rate limit)", "VirusTotal API rate limit reached");
    public static string SecurityInvalidResponse => T("VirusTotal yanıtı çözümlenemedi", "Could not read the VirusTotal response");
    public static string SecurityConnectionError => T("VirusTotal bağlantı hatası", "VirusTotal connection error");
    public static string SecurityNoEngineResults => T("Hiçbir antivirüs motoru sonuç bildirmedi", "No antivirus engine reported a result");
    public static string SecurityThreat(int malicious, int total) => total > 0
        ? T($"{malicious}/{total} motor zararlı uyarısı verdi!", $"{malicious}/{total} engines flagged this file!")
        : T($"{malicious} motor zararlı uyarısı verdi!", $"{malicious} engines flagged this file!");
    public static string SecurityClean(int harmless, int total) =>
        T($"{harmless}/{total} antivirüs motorundan geçti, güvenli", $"Passed {harmless}/{total} antivirus engines, safe");

    // ── İndirme hataları (DownloadErrorText) ────────────────────────────────
    // Türkçe metinler çekirdek katmandaki istisna mesajlarıyla birebir aynıdır.
    public static string ErrorUrlEmpty => T("İndirme URL'i boş olamaz.", "The download URL is empty.");
    public static string ErrorUrlInvalid(string url) => T($"Geçersiz URL formatı: '{url}'.", $"Not a valid URL: '{url}'.");
    public static string ErrorUrlUnsupportedScheme(string scheme) => T(
        $"Desteklenmeyen URL şeması: '{scheme}'. Yalnızca HTTP, HTTPS, FTP ve FTPS desteklenir.",
        $"Unsupported URL scheme: '{scheme}'. Only HTTP, HTTPS, FTP and FTPS are supported.");
    public static string ErrorUserNameRequired(string authType) => T(
        $"{authType} kimlik doğrulama için kullanıcı adı boş olamaz.",
        $"{authType} authentication needs a user name.");
    public static string ErrorKerberosUserNameRequired => T(
        "Kerberos kimlik doğrulama için kullanıcı adı veya kullanıcı anahtarı boş olamaz.",
        "Kerberos authentication needs a user name or principal.");
    public static string ErrorInsufficientDiskSpace(string required, string available) => T(
        $"Hedef sürücüde yetersiz disk alanı. Gerekli: {required}, Kullanılabilir: {available}.",
        $"Not enough disk space on the target drive. Needed: {required}, available: {available}.");
    public static string ErrorHtmlInsteadOfFile => T(
        "Sunucu doğrudan dosya yerine bir web sayfası (HTML) döndürdü. Yapıştırdığınız bağlantı doğrudan dosya indirme linki (Direct Download Link) değil, bir web sayfasıdır.",
        "The server returned a web page (HTML) instead of the file. The link you pasted is a web page, not a direct download link.");
    public static string ErrorRangeNotHonored(int chunkIndex, long offset) => T(
        $"Sunucu kısmi içerik (HTTP 206 Partial Content) yerine dosyanın tamamını (HTTP 200 OK) döndürdü. " +
        $"Chunk #{chunkIndex} (Ofset: {offset}) için dosya bozulmasını önlemek amacıyla indirme durduruldu.",
        $"The server sent the whole file (HTTP 200 OK) instead of the requested part (HTTP 206 Partial Content). " +
        $"The download was stopped at part #{chunkIndex} (offset {offset}) so the file is not corrupted.");
    public static string ErrorServerStatus(int chunkIndex, int statusCode, string? reasonPhrase) => T(
        $"Chunk #{chunkIndex} indirilemedi. Sunucu HTTP {statusCode} ({reasonPhrase}) döndü.",
        $"Part #{chunkIndex} could not be downloaded. The server returned HTTP {statusCode} ({reasonPhrase}).");

    // ── Bildirimler ─────────────────────────────────────────────────────────
    public static string LinkCopiedTitle => T("Bağlantı Kopyalandı", "Link Copied");
    public static string LinkCopiedBody => T("İndirme bağlantısı panoya kopyalandı.", "The download link was copied to the clipboard.");
    public static string Sha256CopiedTitle => T("SHA-256 Kopyalandı", "SHA-256 Copied");
    public static string Sha256CopiedBody => T("Dosya hash değeri panoya kopyalandı.", "The file hash was copied to the clipboard.");
    public static string DownloadCompletedTitle => T("İndirme Tamamlandı", "Download Complete");
    public static string DownloadCompletedBody(string fileName) => T($"{fileName} başarıyla indirildi.", $"{fileName} was downloaded successfully.");
    public static string SecurityWarningTitle => T("Güvenlik Uyarısı!", "Security Warning!");
    public static string SecurityCleanTitle => T("Güvenlik Taraması Temiz", "Security Scan Clean");
    public static string DownloadErrorTitle => T("İndirme Hatası", "Download Error");
    public static string CannotOpenFileTitle => T("Dosya Açılamadı", "Could Not Open File");
    public static string CannotOpenFileBody(string message) => T($"Dosya çalıştırılamadı: {message}", $"The file could not be opened: {message}");
    public static string BrowserDownloadAddedTitle => T("Tarayıcıdan İndirme Eklendi", "Download Added from Browser");
    public static string BrowserDownloadAddedBody(string name) => T($"Yeni görev kuyruğa alındı: {name}", $"Added to the queue: {name}");
    public static string AddDownloadFailedTitle => T("İndirme Eklenemedi", "Could Not Add Download");

    // ── Yeni indirme penceresi ──────────────────────────────────────────────
    public static string AddDialogTitle => T("Yeni İndirme Ekle - AKINCI", "Add Download - AKINCI");
    public static string AddDialogHeader => T("Yeni İndirme Görevi", "New Download");
    public static string AddDialogSubheader => T("HTTP, HTTPS, FTP veya FTPS bağlantısı girin.", "Enter an HTTP, HTTPS, FTP or FTPS link.");
    public static string UrlsLabel => T(
        "İndirme Bağlantıları (URL - Liste halinde birden fazla yapıştırabilirsiniz)*",
        "Download links (URL - you can paste several, one per line)*");
    public static string LinksDetected(int count) => T($"{count} Link Algılandı", count == 1 ? "1 link detected" : $"{count} links detected");
    public static string UrlPlaceholder => T(
        "https://... (Tek link veya liste halinde alt alta birden fazla link yapıştırın)",
        "https://... (paste one link, or several links one per line)");
    public static string SaveDirectoryLabel => T("Kayıt Dizini", "Save to");
    public static string CustomFileNameLabel => T("Özel Dosya Adı (İsteğe bağlı - Tek link için)", "Custom file name (optional, single link only)");
    public static string CustomFileNamePlaceholder => T(
        "Örn: ubuntu-desktop.iso (Boş bırakılırsa otomatik bulunur)",
        "e.g. ubuntu-desktop.iso (detected automatically if left empty)");
    public static string ConnectionsLabel => T("Paralel Bağlantı Sayısı (Parça)", "Parallel connections (parts)");
    public static string AuthHeader => T("Kimlik Doğrulama (Opsiyonel)", "Authentication (optional)");
    public static string AuthNone => T("Yok", "None");
    public static string UseWindowsCredentials => T("Mevcut Windows oturum bilgilerini kullan", "Use my current Windows sign-in");
    public static string UserNameLabel => T("Kullanıcı Adı", "Username");
    public static string UserNamePlaceholder => T("Kullanıcı adı", "Username");
    public static string PasswordLabel => T("Parola", "Password");
    public static string DomainLabel => T("Windows Domain", "Windows domain");
    public static string DomainPlaceholder => T("Domain adı (Opsiyonel)", "Domain name (optional)");
    public static string StartDownload => T("İndirmeyi Başlat", "Start Download");
    public static string StartDownloads(int count) => T($"{count} İndirmeyi Başlat", $"Start {count} Downloads");
    public static string ChooseDownloadFolder => T("İndirme Konumunu Seçin", "Choose download folder");

    // ── Ayarlar ─────────────────────────────────────────────────────────────
    public static string SettingsDialogTitle => T("Ayarlar - AKINCI", "Settings - AKINCI");
    public static string SettingsHeader => T("Uygulama Ayarları", "Settings");
    public static string SettingsSubheader => T(
        "İndirme tercihlerinizi ve VirusTotal entegrasyonunu yapılandırın.",
        "Configure your download preferences and the VirusTotal integration.");
    // Dil seçimi iki dilde de anlaşılsın diye her zaman çift dilli yazılır
    public static string LanguageLabel => "Dil / Language";
    public static string LanguageSystem => T("Sistem dili", "System language");
    public static string LanguageRestartHint => T(
        "Dil değişikliği Akıncı yeniden başlatıldığında uygulanır.",
        "A language change takes effect the next time Akıncı starts.");
    public static string DefaultDirectoryLabel => T("Varsayılan Kayıt Dizini", "Default download folder");
    public static string DefaultConnectionsLabel => T("Varsayılan Bağlantı / Parça Sayısı", "Default connections / parts");
    public static string MaxConcurrentLabel => T("Kuyruk: Eşzamanlı Maksimum Aktif İndirme", "Queue: maximum simultaneous downloads");
    public static string SpeedLimitLabel => T("Küresel Hız Sınırı (KB/s - 0 = Sınırsız)", "Global speed limit (KB/s, 0 = unlimited)");
    public static string SpeedLimitPlaceholder => T("Örn: 2048 (2 MB/s) veya 0 (Sınırsız)", "e.g. 2048 (2 MB/s) or 0 (unlimited)");
    public static string ClipboardTitle => T("📋 Akıllı Pano İzleyici", "📋 Smart clipboard monitor");
    public static string ClipboardDescription => T(
        "Akıncı açık ve odaktayken panoya kopyalanan indirme bağlantılarını algılar.",
        "Detects download links copied to the clipboard while Akıncı is open and focused.");
    public static string VirusTotalTitle => T("🛡 VirusTotal API Entegrasyonu", "🛡 VirusTotal API integration");
    public static string VirusTotalDescription => T(
        "İndirilen dosyaların 70+ antivirüs motoru ile taranması için API anahtarınızı girin.",
        "Enter your API key to check downloaded files against 70+ antivirus engines.");
    public static string VirusTotalPlaceholder => T("VirusTotal API Key (64 karakter)", "VirusTotal API key (64 characters)");
    public static string ExtensionTitle => T("🌐 Chrome & Edge İndirme Eklentisi", "🌐 Chrome & Edge download extension");
    public static string ExtensionDescription => T(
        "İndirmeleri otomatik yakalayan tarayıcı eklentisini kurun veya yeniden yapılandırın.",
        "Install or reconfigure the browser extension that captures downloads automatically.");
    public static string InstallExtensionButton => T("Eklentiyi Kur...", "Install Extension...");
    public static string AboutName => T("AKINCI İndirme Motoru", "AKINCI Download Engine");
    public static string AboutNameWithVersion(string version) => $"{AboutName} v{version}";
    public static string AboutDescription => T(
        "Katı Clean Core mimarisi, SocketsHttpHandler, FluentFTP, SQLite WAL persistence ve VirusTotal v3 analizi.",
        "Strict Clean Core architecture, SocketsHttpHandler, FluentFTP, SQLite WAL persistence and VirusTotal v3 analysis.");
    public static string ChooseDefaultFolder => T("Varsayılan İndirme Konumu Seçin", "Choose default download folder");

    // ── İndirmeyi sil penceresi ─────────────────────────────────────────────
    public static string DeleteDialogTitle => T("İndirmeyi Sil - AKINCI", "Delete Download - AKINCI");
    public static string DeleteHeader => T("İndirmeyi Sil", "Delete Download");
    public static string DeleteQuestion => T("Bu indirmeyi nasıl kaldırmak istiyorsunuz?", "How do you want to remove this download?");
    public static string RemoveFromList => T("Yalnızca Listeden Kaldır", "Remove from List Only");
    public static string RemoveFromListDescription => T(
        "İndirilen dosya diskinizde saklanmaya devam eder, yalnızca Akıncı listesinden temizlenir.",
        "The downloaded file stays on your disk. It is only removed from the Akıncı list.");
    public static string DeleteFileAndList => T("Dosyayı ve Listeyi Sil", "Delete File and Entry");
    public static string DeleteFileAndListDescription => T(
        "İndirilen dosya ve geçici parçalar bilgisayarınızdan kalıcı olarak silinir.",
        "The downloaded file and its temporary parts are permanently deleted from your computer.");
    public static string KeepDownload => T("Vazgeç", "Cancel");

    // ── Tarayıcı eklentisi penceresi ────────────────────────────────────────
    public static string ExtensionDialogTitle => T("Tarayıcı Entegrasyonu - AKINCI", "Browser Integration - AKINCI");
    public static string ExtensionDialogHeader => T("Chrome ve Edge İndirme Entegrasyonu", "Chrome and Edge Download Integration");
    public static string ExtensionDialogQuestion => T(
        "Tarayıcınızdaki indirmelerin otomatik olarak Akıncı'ya yönlendirilmesini ister misiniz?",
        "Do you want downloads in your browser to be handed over to Akıncı automatically?");
    public static string ExtensionFeatureCapture => T(
        "IDM Benzeri Otomatik Yakalama: İndirme butonuna bastığınız anda Akıncı devralır.",
        "Automatic capture: Akıncı takes over as soon as you click a download button.");
    public static string ExtensionFeatureSession => T(
        "Oturum & Çerez Aktarımı: Giriş yapılmış veya geri sayımlı sitelerin gerçek dosya linklerini hatasız yakalar.",
        "Session & cookie handover: captures the real file links of sites you are signed in to or that use countdowns.");
    public static string ExtensionFeatureSpeed => T(
        "32 Kanallı Hızlandırma: Dosyaları anında çok parçalı yüksek hızda indirir.",
        "32-connection acceleration: downloads files in many parts at once for high speed.");
    public static string ExtensionDialogStatus => T(
        "Tarayıcı köprüsü otomatik yapılandırılacak, ardından eklentinin Chrome Web Mağazası sayfası açılacak. Orada 'Chrome'a ekle' demeniz yeterli.",
        "The browser bridge is configured automatically, then the extension's Chrome Web Store page opens. Just click 'Add to Chrome' there.");
    public static string DontAskAgain => T("Bir Daha Sorma", "Don't Ask Again");
    public static string Later => T("Daha Sonra", "Later");
    public static string YesInstallExtension => T("Evet, Eklentiyi Kur", "Yes, Install Extension");

    public static string ExtensionFilesNotFound => T(
        "Eklenti dosyaları bulunamadı. Akıncı klasöründe 'extension' klasörü olduğundan emin olun.",
        "Extension files not found. Make sure the Akıncı folder contains an 'extension' folder.");
    public static string ExtensionPermissionError(string message) => T(
        $"Yetki hatası: {message} Lütfen Akıncı'yı yönetici olarak çalıştırın.",
        $"Permission error: {message} Please run Akıncı as administrator.");
    public static string ExtensionInstallFailed(string message) => T($"Eklenti kurulamadı: {message}", $"Could not install the extension: {message}");
    public static string ExtensionInstallFailedGeneric => T("Eklenti kurulamadı.", "Could not install the extension.");
}
