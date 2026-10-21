# Değişiklik Günlüğü (Changelog)

Bu projedeki tüm önemli değişiklikler bu dosyada belgelenmektedir.

Biçimlendirme [Keep a Changelog](https://keepachangelog.com/tr/1.0.0/) standardına dayanmaktadır ve bu proje [Semantic Versioning](https://semver.org/lang/tr/) kurallarına uymaktadır.

## [1.0.2] - 2026-10-21

### ✨ Eklenenler (Added)
- **Tarayıcı Eklentisi Sağ Üst Kopyalama Bildirimi (Copy Toast Overlay):** Web sayfalarında indirme bağlantısı kopyalandığında (metin seçip kopyalama veya sağ tık "Bağlantı adresini kopyala") sayfanın sağ üst köşesinde zarif, koyu cam efektli (glassmorphism) bir Akıncı pop-up kartı görüntülenir.
  - Kart üzerinde dosya/bağlantı adı, *"Akıncı ile İndir"* butonu ve 6 saniyelik animasyonlu otomatik kapanma çubuğu yer alır.
  - Fare üzerine getirildiğinde geri sayım duraklar; kullanıcı tıklarsa indirme Akıncı'ya aktarılır, tıklamazsa sessizce kaybolur ve kullanıcının akışını bölmez.
- **Manifest v3 Pano Yetkisi:** Eklenti `manifest.json` dosyasına `clipboardRead` yetkisi eklenerek pano erişimi sağlandı.

### ⚡ Değiştirilenler ve İyileştirmeler (Changed)
- **Masaüstü Pano İzleyicisi Sakinleştirildi (Non-intrusive Clipboard Monitor):**
  - Akıncı masaüstü uygulamasının arka planda veya simge durumundayken herhangi bir link kopyalandığında odağı zorla çalıp "Yeni İndirme Ekle" penceresi açması engellendi. Pano algılaması yalnızca Akıncı penceresi halihazırda açık ve odaktayken çalışacak şekilde sınırlandı.
  - `EnableClipboardMonitor` ayarı varsayılan olarak `false` değerine çekildi.

---

## [1.0.1] - 2026-10-20

### 🛡️ Güvenlik (Security)
- **Güvenli IPC Kimlik Doğrulaması:** Tarayıcı köprüsü (`Akinci.NativeHost`) ile masaüstü arka plan IPC dinleyicisi arasına oturum bazlı kriptografik token doğrulaması (`~/.akinci/ipc.token`) eklendi. Yetkisiz yerel süreçlerin veya tarayıcı sayfalarının indirme kuyruğuna komut enjekte etmesi engellendi.
- **Kısıtlı CORS Politikası:** IPC HTTP sunucusunda `Access-Control-Allow-Origin` başlığı yalnızca doğrulanmış yerel tarayıcı eklentisi kökenine (`chrome-extension://...`) izin verecek şekilde sıkılaştırıldı.
- **HTTP 200 Parça Bozulma Koruması:** Sunucu `Range` isteklerine `206 Partial Content` yerine beklenmedik şekilde `200 OK` (tüm içerik) döndürdüğünde, parça ofset çakışması sonucu oluşan dosya bozulmaları önlendi.

### ✨ Eklenenler (Added)
- **Dinamik İş Çalma (Work-Stealing Chunk Splitting):** Boşta kalan indirme iş parçacıklarının (workers), en yavaş ilerleyen aktif parçanın kalan bayt aralığını dinamik olarak bölüp devralması (`TryStealChunk`) sağlandı. Böylece indirme sonlarında tek parçaya takılıp kalma sorunu çözülerek son bayta kadar maksimum bant genişliği kullanımı sağlandı.
- **Bant Genişliği Sınırlayıcı (Token Bucket Throttler):** Mikrosaniye hassasiyetinde çalışan `TokenBucketThrottler` ve `IBandwidthThrottler` servisi eklendi. Kullanıcılar indirme hızını global olarak KB/s cinsinden sınırlayabilir.
- **Akıllı İndirme Kuyruğu Yöneticisi:** Eşzamanlı aktif indirme limiti (`MaxConcurrentDownloads`) eklendi. Tamamlanan, duraklatılan veya hata veren görevlerin ardından sıradaki indirmeler otomatik olarak sıraya alınıp başlatılır (`ProcessQueue`).
- **Kategori Gezinme Sistemi:** `FileCategory` domain modeli ve otomatik dosya türü sınıflandırıcı (Videolar, Ses & Müzik, Belgeler, Programlar, Arşivler, Diğer) eklendi. Sol menüde her kategori için anlık görev adetlerini gösteren sayaç rozetleri yerleştirildi.
- **Pano Bağlantı Dinleyicisi (Clipboard Monitor):** Panoya kopyalanan HTTP, HTTPS ve FTP indirme bağlantılarını otomatik olarak yakalayıp kullanıcıya indirme önerisi sunan arka plan servisi eklendi; Ayarlar ekranından açılıp kapatılabilir.
- **Gelişmiş Sağ Tık (Context Menu) Eylemleri:**
  - *Yeniden Başlat:* İndirmeyi sıfırlayarak baştan temiz indirme.
  - *İndirme Bağlantısını Kopyala:* Kaynak URL'yi tek tıkla panoya aktarma.
  - *SHA-256 Kopyala:* İndirilen dosyanın SHA-256 bütünlük özetini hesaplayıp panoya kopyalama.
  - *Güvenlik Raporunu Görüntüle:* VirusTotal v3 tehdit analizi ve tespit motoru ayrıntılarını açma.

### ⚡ Değiştirilenler ve İyileştirmeler (Changed)
- **SQLite Disk Yazma Throttling:** Multi-gigabit indirmelerde disk G/Ç darboğazını ve veritabanı kilitlenmelerini önlemek amacıyla SQLite ilerleme kayıtları 1.5 saniye / 2 MB periyoduna bağlandı.
- **SQLite UPSERT Desteği:** Dinamik iş çalma ile sonradan oluşturulan parçaların güvenle saklanması için `SqliteDownloadRepository` üzerinde UPSERT mekanizması devreye alındı.
- **Kültür Bağımsız Hız Ölçümü:** Metin tabanlı ayrıştırma (`double.Parse`) yerine doğrudan atomik bayt farkları üzerinden bölgesel ayarlardan (virgül/nokta) etkilenmeyen sayısal hız hesaplaması getirildi.
- **Arayüz Sidebar Düzenlemesi:** Kategori menüsü dikey taşmaları önlemek için `ScrollViewer` ile esnek hale getirildi; Avalonia 12 Fluent tema uyumluluğu artırıldı.

### 🐛 Düzeltilenler (Fixed)
- **Disk Boş Alan Doğrulaması:** İndirme başlamadan önce hedef sürücüde yeterli alan olup olmadığı denetlenir; yetersiz alanda indirme başlatılmayarak dosya bozulmaları önlenir.
- **Dosya Adı Çakışma Yönetimi:** Hedef dizinde aynı isimde dosya varsa, dosyanın üzerine yazmak yerine otomatik olarak `dosya (1).ext` biçiminde çakışmasız adlandırma yapılır.
- **İptal Belirteci (CancellationToken) İşleme:** Başlatma komutunda iptal edilen indirme görevlerinin hatalı şekilde tamamlandı durumuna geçmesi düzeltildi.

---

## [1.0.0] - 2026-10-18

### ✨ İlk Kararlı Sürüm (Initial Stable Release)
- **Clean Core Mimarisi:** Saf C# Domain ve Application katmanları; harici kütüphanelerden izole mimari yapı.
- **Çok Kanallı HTTP/HTTPS Motoru:** `SocketsHttpHandler` tabanlı, parça (chunk) bazlı eşzamanlı indirme.
- **FTP/FTPS Desteği:** `FluentFTP` entegrasyonu ve REST offset parçalı indirme kabiliyeti.
- **Kurumsal Kimlik Doğrulama:** Basic, NTLM, Kerberos, Negotiate ve Windows DefaultCredentials desteği.
- **SQLite WAL Kalıcılığı:** Kesintiye uğrayan indirmelerin kaldığı yerden devam edebilmesi (Resumable downloads).
- **VirusTotal v3 Analizi:** İndirilen dosyaların SHA-256 özetleri üzerinden otomatik güvenlik taraması.
- **Avalonia UI Masaüstü İstemcisi:** Modern koyu tema (Dark mode), sistem tepsisi (tray) ve mini pencere modu.
- **Akinci-CLI Terminal İstemcisi:** Canlı ASCII parça izleyicili komut satırı arayüzü.
- **Tarayıcı Entegrasyonu:** Chrome, Edge ve Firefox için Native Messaging Host ve WebExtension eklentisi.
- **Tek Dosya (Single-File) Dağıtım:** Tüm bağımlılıkları içeren tek parça çalıştırılabilir paket (`publish.bat`).
