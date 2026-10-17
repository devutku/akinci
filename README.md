# AKINCI (Downloader)
### Çok Kanallı, Yüksek Başarımlı & Güvenlik Korumalı İndirme Motoru

<p align="center">
  <img src="src/Akinci.UI/Assets/akinci-logo-dark.svg" alt="Akıncı Logo" width="220" />
</p>

<p align="center">
  <img src="src/Akinci.UI/Assets/akinci-banner.jpg" alt="Akıncı Sanatsal İllüstrasyon" width="850" style="border-radius: 12px;" />
</p>

---

## ⚡ Genel Bakış

**Akıncı**, modern **.NET 10** platformunda, katı **Clean Core (Clean Architecture / Hexagonal Architecture)** prensiplerine göre tasarlanmış, çok kanallı (multi-connection) ve eşzamanlı parçalı indirme yöneticisidir.

Uygulamanın çekirdek iş kuralları (Domain ve Application katmanları); HTTP protokolü, disk G/Ç işlemleri, SQLite veri tabanı veya Avalonia UI gibi harici kütüphanelerden **tamamen izoledir**.

---

## 🛡 Fark Yaratan Özellik (Killer Feature): Canlı VirusTotal Entegrasyonu

Akıncı, indirme işlemi tamamlandığında dosyayı diskte bekletirken akış (stream) üzerinden eşzamanlı olarak SHA-256 özetini hesaplar ve otomatik olarak **VirusTotal API v3** (`GET /api/v3/files/{hash}`) sorgusu gerçekleştirir.

* Dosya açılmadan önce **70+ antivirüs motorunun** analiz sonucu anında gösterilir:
  * 🟢 `70/70 antivirüs motorundan geçti, güvenli`
  * 🔴 `5/70 motor zararlı uyarısı verdi!`
* Kullanıcı tek tıkla doğrudan ilgili VirusTotal web raporuna ulaşabilir.

---

## 🦅 Desteklenen Protokoller ve Kimlik Doğrulama

| Protokol | Açıklama |
|---|---|
| **HTTP / HTTPS** | Yüksek başarımlı `SocketsHttpHandler`, Range başlıkları ve otomatik yönlendirme takibi |
| **FTP / FTPS** | `FluentFTP` tabanlı güvenli dosya aktarımı, parça bazlı REST offset indirme desteği |

### Kurumsal Kimlik Doğrulama (Authentication)
* **Basic Authentication**
* **NTLM** (Windows Ağ Doğrulaması)
* **Kerberos**
* **Negotiate (SPNEGO)**
* Windows Oturum Açma Bilgileri (DefaultCredentials)

---

## 🏛 Clean Core Mimari Katmanları

```plaintext
Akinci/
├── src/
│   ├── Akinci.Domain/             # Saf C#; harici paket almaz (Entities, Value Objects, Enums)
│   ├── Akinci.Application/        # Use Case'ler, DTO'lar, Port Arayüzleri (Hexagonal Ports)
│   ├── Akinci.Infrastructure/     # SocketsHttpHandler, RandomAccess I/O, SQLite WAL, VirusTotal API
│   ├── Akinci.Cli/                # Canlı ASCII parça izleyicili terminal uygulaması
│   ├── Akinci.UI/                 # Avalonia UI Koyu Tema (Dark), MVVM, Sistem Tepsisi
│   └── Akinci.NativeHost/         # Chrome/Edge/Firefox Native Messaging Host
└── tests/
    ├── Akinci.Domain.UnitTests/
    ├── Akinci.Application.UnitTests/
    └── Akinci.Infrastructure.IntegrationTests/
```

---

## 🚀 Görsel Varlık Kullanım Rehberi (SVG & JPG)

Akıncı mimarisinde grafik varlıklar amacına en uygun şekilde konumlandırılmıştır:

* **SVG (`src/Akinci.UI/Assets/akinci-logo-dark.svg`):**
  * **Kullanım Alanı:** Masaüstü başlık çubuğu (Header Brand), Boş Durum (Empty State) karşılama rozetleri, Yeni İndirme & Ayarlar diyalog başlıkları ve tarayıcı eklenti simgeleri.
  * **Nedeni:** Vektörel formatta her türlü DPI ekran çözünürlüğünde sıfır kalite kaybıyla keskin ve net çizim sağlar.
* **JPG (`src/Akinci.UI/Assets/akinci-banner.jpg`):**
  * **Kullanım Alanı:** Ayarlar penceresindeki "Akıncı Hakkında & Mimari" vitrin kartı, Dokümantasyon ve README başlık afişi.
  * **Nedeni:** Zengin renk geçişleri, poligon gölgeleri ve yüksek çözünürlüklü sanatsal illüstrasyon detaylarını en iyi performansta sunar.
* **ICO (`src/Akinci.UI/Assets/akinci.ico`):**
  * **Kullanım Alanı:** Windows uygulama simgesi, görev çubuğu ve Sistem Tepsisi (TrayIcon).

---

## 🛠 Derleme ve Tek Dosya (Single-File) Dağıtım

Tüm bağımlılıkları içeren tek parça çalıştırılabilir dosya üretmek için:

### Windows (Tek Tıkla Derleme):
```bat
publish.bat
```
Çıktılar `dist/` klasöründe üretilir:
* `dist\Akinci.UI\Akinci.UI.exe` (Masaüstü Arayüzü)
* `dist\Akinci.Cli\Akinci.Cli.exe` (Terminal Sürümü)
* `dist\Akinci.NativeHost\Akinci.NativeHost.exe` (Tarayıcı Köprüsü)

### Linux & macOS:
```bash
./publish.sh linux-x64
```

---

## 🌐 Tarayıcı Entegrasyonu Kurulumu

Chrome, Microsoft Edge ve Firefox ile sağ tık *"Akıncı ile İndir"* entegrasyonu için:
1. `src\Akinci.NativeHost\install-host.bat` dosyasını çalıştırın.
2. `src\Akinci.NativeHost\extension` klasörünü tarayıcınızın Geliştirici Modunda *"Paketlenmemiş Öğe Yükle"* seçeneğiyle ekleyin.
