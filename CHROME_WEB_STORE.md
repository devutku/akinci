# Chrome Web Store Başvuru Rehberi

Akıncı tarayıcı eklentisini Chrome Web Store'a göndermek için gereken paket, metinler ve adımlar.
Mağaza panelindeki alanlar İngilizce doldurulabilir; aşağıdaki metinler doğrudan yapıştırılabilir.

## 1. Paketi oluştur

```powershell
powershell -ExecutionPolicy Bypass -File pack-extension.ps1
```

Çıktı: `dist\akinci-extension-<sürüm>.zip`. Betik, yalnızca yerel (unpacked) kurulum için olan `key` alanını paketten çıkarır.

## 2. Mağaza adımları (geliştirici hesabı sahibi yapar)

1. [Chrome Web Store Developer Dashboard](https://chrome.google.com/webstore/devconsole) üzerinden geliştirici hesabı aç (tek seferlik kayıt ücreti ve kimlik doğrulaması istenir).
2. **New item** → zip paketini yükle.
3. **Store listing**, **Privacy** ve **Distribution** sekmelerini aşağıdaki metinlerle doldur.
4. Ekran görüntüsü ve tanıtım görseli ekle (mağaza en az bir ekran görüntüsü ister: 1280x800 veya 640x400).
5. **Submit for review**.

### İlk yüklemeden sonra (önemli)

Mağaza eklentiye kendi kimliğini (item ID) atar. Akıncı'nın mağaza kimliği: **`coeeghhijegibfhhodhnjhjffaipckok`**.

**Durum:** Yayında — <https://chromewebstore.google.com/detail/coeeghhijegibfhhodhnjhjffaipckok> (ilk yayınlanan sürüm 1.0.2). Masaüstü uygulamasının "Eklentiyi Kur" adımı bu sayfayı açar (`BrowserIntegrationService.ChromeWebStoreUrl`). Yeni sürümler [6. bölümdeki](#6-otomatik-yayınlama-github-actions) iş akışıyla otomatik yüklenir; kurulum yapılmadıysa `pack-extension.ps1` ile paketi üretip panelden **Paket → Yeni paket yükle** yapın. Her iki durumda da `manifest.json` sürümü artırılmış olmalıdır.

Bu kimlik, masaüstü uygulamasının Native Messaging izin listesinde bulunmalıdır; yoksa mağazadan kurulan eklenti uygulamaya yalnızca `127.0.0.1` yedek yolu üzerinden ulaşır:

- `src/Akinci.UI/Services/BrowserIntegrationService.cs` → `AllowedExtensionIds`
- `src/Akinci.NativeHost/manifests/com.akinci.downloader.json` → `allowed_origins`

## 3. Store listing

Eklenti 1.2.0'dan itibaren çok dillidir: ad, açıklama ve arayüz metinleri `extension/_locales/<dil>/messages.json` dosyalarından gelir (`default_locale` İngilizce; tarayıcı dili Türkçe ise Türkçe). Mağazada görünen ad da kullanıcının diline göre bu dosyalardan seçilir; panelde dil başına ayrı açıklama girilebiliyorsa aşağıdaki metnin Türkçesi eklenebilir.

**Summary (kısa açıklama, en fazla 132 karakter):**

> Send downloads from Chrome to the Akıncı desktop download manager for faster, resumable, multi-connection downloads.

**Description:**

> Akıncı Downloader Integration connects Chrome to the Akıncı desktop download manager (https://github.com/devutku/akinci). The desktop app must be installed separately.
>
> What it does:
> • Automatically hands over downloads of common file types (archives, installers, disk images, videos, audio, PDFs) to Akıncı instead of Chrome's built-in downloader.
> • Adds a "Download with Akıncı" item to the right-click menu for links, images, video and audio.
> • Available in English and Turkish (follows the browser language).
> • Detects HLS (.m3u8) and DASH (.mpd) video streams on the page and shows a download button.
> • Shows a download button when you select and copy a download link.
> • Forwards the site's cookies with the download so downloads that require you to be signed in keep working.
>
> All data is sent only to the Akıncı app on your own computer. Nothing is sent to the developer or any third party.

**Category:** Productivity → Tools (veya Workflow & Planning)

## 4. Privacy sekmesi

**Single purpose:**

> Send downloads started in the browser to the Akıncı desktop download manager installed on the same computer.

**Permission justifications:**

| İzin | Gerekçe (panele yapıştır) |
|---|---|
| `downloads` | Detects downloads the user starts in Chrome and, for supported file types, cancels the browser download and hands the URL to the Akıncı desktop app instead. |
| `cookies` | Reads the cookies of the download's site and forwards them to the local Akıncı app so downloads that require a signed-in session work. Cookies are never sent anywhere else. |
| `webRequest` | Observes response headers (read-only, no blocking or modification) to detect HLS/DASH video stream manifests so a download button can be shown on the page. |
| `contextMenus` | Adds the "Download with Akıncı" item to the right-click menu for links and media. |
| `nativeMessaging` | Communicates with the Akıncı desktop app installed on the same computer, which performs the downloads. |
| Host permissions (`<all_urls>`) | Downloads and video streams can come from any website. Needed to read cookies for any download host, observe stream responses, and show the download button on any page. |
| Host permission `http://127.0.0.1:49500/*` | Local fallback channel to the Akıncı desktop app when Native Messaging is not available. Only reachable from the same computer. |

**Remote code:** No, I am not using remote code.

**Data usage** (paneldeki kategorilerden, eklentinin gerçekten işlediklerini işaretle):

- Authentication information — site cookies forwarded with downloads.
- Web history — URLs of downloads and the pages they were started from.
- Website content — detected stream URLs and the download link the user copies.

Ve şu beyanları onayla: veriler satılmıyor, eklentinin tek amacı dışında kullanılmıyor, kredi değerliliği veya borç verme amacıyla kullanılmıyor.

**Privacy policy URL:**

> https://github.com/devutku/akinci/blob/main/PRIVACY.md

## 5. İncelemede dikkat edilecekler

- Mağaza açıklaması, eklentinin gerçekte yaptığı her şeyi (özellikle indirmeleri otomatik olarak devralmayı ve çerezleri iletmeyi) açıkça söylemeli. Açıklama ile davranış arasındaki fark en sık ret nedenidir.
- Yeni izin eklenirse gerekçesi panelde ve `PRIVACY.md` içinde güncellenmeli.
- Eklenti uzaktan kod yüklememeli; tüm kod paketin içinde olmalı.

## 6. Otomatik yayınlama (GitHub Actions)

Kurulumdan sonra yeni bir eklenti sürümünü yayınlamak için tek yapmanız gereken `src/Akinci.NativeHost/extension/manifest.json` içindeki `version` değerini artırıp her zamanki gibi `v*` sürüm etiketini atmaktır. `.github/workflows/extension.yml` paketi `pack-extension.ps1` ile üretir, `publish-extension.mjs` ile Chrome Web Store API (v2) üzerinden yükler ve incelemeye gönderir. Bu sürüm mağazada zaten yayında veya incelemedeyse hiçbir şey yüklemeden geçer; bu yüzden eklentiyi değiştirmeyen masaüstü sürümleri mağazaya dokunmaz.

Kimlik bilgileri tanımlanmadıysa iş akışı başarısız olmaz, "atlandı" notu bırakır.

### Bir kerelik kurulum (yaklaşık 10-15 dakika)

Bu adımlar mağaza hesabının sahibi tarafından yapılır.

1. **Google Cloud projesi ve API**
   - <https://console.cloud.google.com/> adresinde yeni bir proje oluşturun (ör. `akinci-cws`).
   - Üstteki arama çubuğuna **Chrome Web Store API** yazın ve **Etkinleştir**'e tıklayın.
2. **Hizmet hesabı**
   - **IAM ve Yönetici → Hizmet Hesapları → Hizmet hesabı oluştur**. Ad: `akinci-cws-publisher`. Rol vermeyin, **Bitti** deyin.
   - Oluşan hesabın e-posta adresini kopyalayın (ör. `akinci-cws-publisher@akinci-cws.iam.gserviceaccount.com`).
   - Hesaba tıklayın → **Anahtarlar → Anahtar ekle → Yeni anahtar oluştur → JSON**. Bir `.json` dosyası iner.
3. **Chrome Web Store geliştirici paneli**
   - **Hesap** bölümünde hizmet hesabı alanına 2. adımdaki e-postayı ekleyip kaydedin. (Bir yayıncıya şu an yalnızca bir hizmet hesabı eklenebiliyor.)
   - **Yayıncı → Ayarlar** bölümündeki **Yayıncı kimliği**'ni (Publisher ID) kopyalayın.
4. **GitHub gizli değişkenleri**
   - <https://github.com/devutku/akinci/settings/secrets/actions> → **New repository secret**:
     - `CWS_SERVICE_ACCOUNT_KEY`: indirdiğiniz `.json` dosyasının **tam içeriği** (Not Defteri ile açıp hepsini yapıştırın).
     - `CWS_PUBLISHER_ID`: 3. adımdaki yayıncı kimliği.
   - Ardından bilgisayarınıza inen `.json` anahtar dosyasını silin; anahtar yalnızca GitHub'da şifreli olarak durmalı.
5. **Deneyin**
   - <https://github.com/devutku/akinci/actions/workflows/extension.yml> → **Run workflow**. Mağazadaki sürümden yeni bir sürüm varsa yüklenip incelemeye gönderilir; günlükte "Yayında / İncelemede" sürümleri ve sonuç yazar.

### Yerelde kullanım

```powershell
powershell -ExecutionPolicy Bypass -File pack-extension.ps1
$env:CWS_SERVICE_ACCOUNT_KEY = Get-Content -Raw anahtar.json
$env:CWS_PUBLISHER_ID = '<yayıncı kimliği>'
node publish-extension.mjs --status                                        # yalnızca durumu göster
node publish-extension.mjs dist\akinci-extension-1.1.0.zip --version 1.1.0 # yükle + incelemeye gönder
```

### Sorun giderme

- **`Erişim jetonu alınamadı`**: Anahtar yanlış veya silinmiş; yeni bir JSON anahtar oluşturup gizli değişkeni güncelleyin.
- **HTTP 403 / `PERMISSION_DENIED`**: Hizmet hesabı e-postası geliştirici panelinde **Hesap** bölümüne eklenmemiş ya da Chrome Web Store API projede etkin değil.
- **HTTP 400 sürüm hatası**: `manifest.json` sürümü mağazadakinden büyük değil; sürümü artırın.
- **"Anahtar oluşturma devre dışı"**: Google Workspace kuruluşuna bağlı projelerde `iam.disableServiceAccountKeyCreation` ilkesi açık olabilir; kişisel bir Google hesabıyla proje oluşturun veya ilkeyi kuruluş yöneticisine açtırın.
- Mağaza açıklaması, görseller ve izin gerekçeleri API ile değişmez; bunlar için panel kullanılır.
