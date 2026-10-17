# AKINCI
### Çok Kanallı, Yüksek Başarımlı & Güvenlik Korumalı İndirme Motoru

<p align="center">
  <img src="src/Akinci.UI/Assets/akinci-logo-dark.svg" alt="Akıncı Logo" width="220" />
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
