# AKINCI
### Çok Kanallı, Yüksek Başarımlı & Güvenlik Korumalı İndirme Motoru

<p align="center">
  <img src="src/Akinci.UI/Assets/akinci-logo-dark.svg" alt="Akıncı Logo" width="220" />
</p>

<p align="center">
  <a href="CHANGELOG.md"><img src="https://img.shields.io/badge/s%C3%BCr%C3%BCm-v1.1.1-38bdf8.svg" alt="Sürüm 1.1.1" /></a>
  <a href="LICENSE-MIT"><img src="https://img.shields.io/badge/Core-MIT-green.svg" alt="Core MIT" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/UI-GPLv3-orange.svg" alt="UI GPLv3" /></a>
</p>

---

## ⚡ Genel Bakış

**Akıncı**, modern **.NET 10** platformunda, katı **Clean Core (Clean Architecture / Hexagonal Architecture)** prensiplerine göre tasarlanmış, çok kanallı (multi-connection) ve eşzamanlı parçalı indirme yöneticisidir.

Uygulamanın çekirdek iş kuralları (Domain ve Application katmanları); HTTP protokolü, disk G/Ç işlemleri, SQLite veri tabanı veya Avalonia UI gibi harici kütüphanelerden **tamamen izoledir**.

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

## 💿 Kurulum (Windows)

1. [Sürümler](https://github.com/devutku/akinci/releases) sayfasından `Akinci-Setup-<sürüm>.exe` dosyasını indirip çalıştırın.
2. Kurulum programı imzasız olduğu için Windows SmartScreen *"Windows bilgisayarınızı korudu"* uyarısı gösterebilir: **Ek bilgi → Yine de çalıştır** seçin.
3. Sihirbazı izleyin. Yönetici izni gerekmez; Akıncı `%LOCALAPPDATA%\Programs\Akinci` altına yalnızca sizin kullanıcınız için kurulur. VirusTotal API anahtarınız varsa sihirbazda girebilir, yoksa daha sonra **Ayarlar**'dan ekleyebilirsiniz.

Kaldırmak için **Denetim Masası → Programlar ve Özellikler** (veya **Ayarlar → Uygulamalar**) içinden *Akıncı*'yı seçin. Kaldırma, indirdiğiniz dosyalara dokunmaz; ayarların ve indirme geçmişinin de silinip silinmeyeceği sorulur.

Kurulum programı istemeyenler için aynı sürümde taşınabilir bir `.zip` paketi de bulunur.

---

## 🛠 Derleme ve Tek Dosya (Single-File) Dağıtım

Tüm bağımlılıkları içeren tek parça çalıştırılabilir dosya üretmek için:

### Windows (Tek Tıkla Derleme):
```bat
publish.bat
```
Çıktılar `dist/Akinci/` klasöründe üretilir:
* `dist\Akinci\Akinci.exe` (Masaüstü Uygulaması)
* `dist\Akinci\Akinci-CLI.exe` (Terminal Sürümü)
* `dist\Akinci\Akinci-BrowserBridge.exe` (Tarayıcı Köprüsü)
* `dist\Akinci-Setup-<sürüm>.exe` (Kurulum programı; [Inno Setup 6](https://jrsoftware.org/isinfo.php) kuruluysa üretilir: `winget install --id JRSoftware.InnoSetup -e`)

### Linux & macOS:
```bash
./publish.sh linux-x64
```

---

## 🌐 Tarayıcı Entegrasyonu Kurulumu

Eklenti **[Chrome Web Mağazası](https://chromewebstore.google.com/detail/coeeghhijegibfhhodhnjhjffaipckok)**'nda yayında (Chrome ve Edge):
1. Akıncı'nın ilk açılışta gösterdiği kurulum bildirimini onaylayın (veya **Ayarlar → Eklentiyi Kur**). Akıncı tarayıcı köprüsünü yapılandırır ve mağaza sayfasını açar.
2. Mağaza sayfasında **Chrome'a ekle** deyin. Edge'de önce *"Diğer mağazalardaki uzantılara izin ver"* seçeneğini onaylayın.

Geliştirme için: `src\Akinci.NativeHost\extension` klasörü Geliştirici Modunda *"Paketlenmemiş Öğe Yükle"* ile de eklenebilir (sabit geliştirme kimliğiyle).

---

## ⚖️ Lisanslama (Dual Licensing)

Akıncı, açık kaynak ekosistemini desteklerken nihai masaüstü uygulamasının klonlanıp kapatılmasını önleyen **çift lisanslama (dual licensing)** modelini benimser:

* **Kütüphane & Çekirdek Katmanları (`Akinci.Domain`, `Akinci.Application`, `Akinci.Infrastructure`):**
  .NET topluluğunun ve geliştiricilerin kendi projelerinde veya ticari yazılımlarında bir kütüphane/motor olarak özgürce kullanabilmesi için **[MIT Lisansı](LICENSE-MIT)** ile lisanslanmıştır.
* **Masaüstü Uygulaması & Dağıtımlar (`Akinci.UI`, CLI ve Tümleşik İkili Paketler):**
  Uygulamanın kendisi, kullanıcı arayüzü ve dağıtılabilir ikili paketleri **[GNU General Public License v3.0 (GPLv3)](LICENSE)** ile korunmaktadır.

> Bu sayede indirme motorunu bağımsız bir NuGet paketi olarak projelerinizde serbestçe kullanabilirsiniz; ancak Akıncı masaüstü uygulamasının arayüzü ve bütünleşik türevleri daima açık kaynak kalmak zorundadır.

---

## ⚠️ Sorumluluk Reddi (Disclaimer)

> **Uyarı:** Akıncı, eğitim ve açık veri aktarımı amaçlarıyla geliştirilmiş bir ağ aracıdır. Telif hakkıyla korunan veya platform kullanım şartlarına aykırı içeriklerin indirilmesinden kullanıcı sorumludur.
