# Akıncı Downloader Entegrasyonu — Gizlilik Politikası / Privacy Policy

_Son güncelleme / Last updated: 2026-11-14_

## Türkçe

**Akıncı Downloader Entegrasyonu** tarayıcı eklentisinin tek amacı, tarayıcıda başlatılan indirmeleri aynı bilgisayarda kurulu olan **Akıncı masaüstü indirme yöneticisine** aktarmaktır.

### Hangi veriler işlenir?

| Veri | Ne zaman | Neden |
|---|---|---|
| İndirme bağlantısı (URL), önerilen dosya adı ve bağlantının bulunduğu sayfanın adresi (referrer) | Desteklenen bir dosya türü indirildiğinde, "Akıncı ile İndir" menüsü veya sayfadaki Akıncı düğmesi kullanıldığında | Dosyayı Akıncı'nın indirebilmesi için |
| İndirilen sitenin çerezleri ve tarayıcının kullanıcı aracısı (User-Agent) | Yukarıdaki indirmelerle birlikte | Oturum açılması gereken veya korumalı sitelerden indirmenin çalışması için |
| Video akışı (HLS `.m3u8` / DASH `.mpd`) adresleri | Ziyaret edilen sayfa böyle bir akış yüklediğinde | Sayfada "indir" düğmesi göstermek için. Akış yalnızca kullanıcı düğmeye tıklarsa Akıncı'ya gönderilir. |
| Kullanıcının sayfada seçip kopyaladığı metin | Kopyalama sırasında | Metin bir indirme bağlantısıysa "indir" düğmesi göstermek için. Pano (clipboard) içeriği okunmaz. Bağlantı yalnızca kullanıcı düğmeye tıklarsa Akıncı'ya gönderilir. |

### Veriler nereye gönderilir?

Veriler **yalnızca aynı bilgisayardaki Akıncı masaüstü uygulamasına** gönderilir: Chrome'un yerel uygulama bağlantısı (Native Messaging) veya yalnızca bu bilgisayardan erişilebilen `127.0.0.1` adresi üzerinden.

- Eklenti hiçbir veriyi geliştiriciye veya üçüncü taraf bir sunucuya göndermez.
- Analitik, reklam veya izleme kodu içermez.
- Veriler satılmaz, paylaşılmaz ve indirme dışında hiçbir amaçla kullanılmaz.
- Eklenti uzaktan kod indirip çalıştırmaz.

Akıncı masaüstü uygulaması indirme listesini, devam ettirme için gereken oturum bilgileriyle birlikte bilgisayardaki yerel bir veritabanında (`%USERPROFILE%\.akinci\downloads.db`) saklar. Kullanıcı bir VirusTotal API anahtarı girerse uygulama, tamamlanan dosyanın yalnızca SHA-256 özetini VirusTotal'a sorar. Dosyanın kendisi veya yukarıdaki veriler gönderilmez.

### İletişim

Sorular için: https://github.com/devutku/akinci/issues

## English

The **Akıncı Downloader Entegrasyonu** browser extension has a single purpose: hand downloads started in the browser over to the **Akıncı desktop download manager** installed on the same computer.

**Data handled:** download URLs, suggested file names and referring page URLs; the cookies of the download's site and the browser's User-Agent (so authenticated downloads work); HLS/DASH stream URLs seen on visited pages (used only to show a download button; sent to the app only if the user clicks it); text the user selects and copies (checked locally to see whether it is a download link; the clipboard itself is never read).

**Where it goes:** only to the Akıncı desktop app on the same computer, via Chrome Native Messaging or `127.0.0.1`. The extension sends nothing to the developer or any third-party server, contains no analytics, ads or tracking, does not sell or share data, does not use it for any purpose other than downloading, and does not load remote code.

The desktop app stores the download list, including the session data needed to resume downloads, in a local database on the user's computer. If the user configures a VirusTotal API key, the app looks up only the SHA-256 hash of completed files on VirusTotal.

**Contact:** https://github.com/devutku/akinci/issues
