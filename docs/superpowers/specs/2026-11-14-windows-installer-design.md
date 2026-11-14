# Akıncı Windows Kurulum Programı — Tasarım

_Tarih: 2026-11-14_

## Amaç ve neden

Bugün `publish.bat` ve GitHub sürüm iş akışı (`.github/workflows/release.yml`), Windows için bir klasör ya da zip üretiyor: `Akinci.exe`, `Akinci-CLI.exe`, `Akinci-BrowserBridge.exe`, `extension\`, `manifests\`, `install-host.bat`, `uninstall-host.bat`. Kullanıcı bu klasörü kendisi bir yere koyuyor, kısayolu kendisi oluşturuyor ve tarayıcı entegrasyonu için ayrıca uğraşıyor. Programı kaldırmanın da bir yolu yok.

Hedef, sıradan bir Windows kullanıcısının tek bir **Setup.exe** indirip sihirbazla Akıncı'yı kurabilmesi. Kurulum bitince Akıncı başlat menüsünde görünür, ilk açılışta hazır olur ve Denetim Masası'ndan temizce kaldırılabilir.

## Kullanıcının istedikleri

1. **Kurulum programı:** Tek bir Setup.exe ve ileri-ileri ilerleyen bir sihirbaz.
2. **VirusTotal API anahtarı sayfası:** Kullanıcının anahtarı varsa sihirbazda girer ve Akıncı anahtarı kullanmaya başlar. Yoksa sayfa kısaca VirusTotal'ın ne işe yaradığını ve ücretsiz anahtarın nasıl alınacağını anlatır. Adım atlanabilir.
3. **Kaldırma:** Program, Denetim Masası'ndaki "Programlar ve Özellikler" listesinden kaldırılabilir.

## Hedefler

- Tek dosya `Akinci-Setup-<sürüm>.exe`, sihirbaz arayüzü Türkçe ve İngilizce (Windows diline göre otomatik).
- Yönetici izni (UAC) istemeden, kullanıcı başına kurulum.
- Başlat menüsü kısayolu her zaman oluşturulur. Masaüstü kısayolu için bir onay kutusu olur, varsayılan olarak işaretli.
- VirusTotal sayfası: açıklama, anahtar giriş kutusu, anahtar alma bağlantısı. İsteğe bağlı.
- Tarayıcı entegrasyonu bugünkü gibi uygulamada kalır: ilk açılışta "Eklentiyi kur" penceresi çıkar (kullanıcı kararı, aşağıda).
- Son sayfada "Akıncı'yı başlat" seçeneği.
- Akıncı kapanırken devam eden indirmeler "duraklatıldı" olarak kaydedilir; bir sonraki açılışta kaldığı yerden devam ettirilebilir (kullanıcı kararı, aşağıda).
- Denetim Masası'ndan kaldırma: program dosyaları, kısayollar ve kayıt defteri girdileri silinir.
- Aynı kurulum programı yeni sürümü eskisinin üzerine kurar (güncelleme).
- Kurulum programı hem yerelde (`publish.bat`) hem GitHub sürüm iş akışında üretilir ve sürüme eklenir.

## Hedef dışı (bu iş kapsamında yapılmayacaklar)

- Otomatik güncelleme.
- MSI paketi ve kurumsal dağıtım senaryoları. Sessiz kurulum Inno Setup'ın kendi parametreleriyle yine de çalışır.
- Linux ve macOS kurulum programları. Bu platformların arşivleri bugünkü gibi kalır.
- Firefox entegrasyonu. Eklenti Manifest V3 service worker kullandığı için zaten Chrome ve Edge hedefli.
- Tüm kullanıcılar için (Program Files) kurulum.
- Kod imzalama. İmza sertifikası ücretli. İmzasız kurulum programında Windows SmartScreen "Windows bilgisayarınızı korudu" uyarısı gösterir, kullanıcı "Ek bilgi → Yine de çalıştır" der. Bu bilinen bir sınırlama olarak README'ye yazılacak.
- Tarayıcı eklentisini kurulum sırasında Chrome'a yüklemek. Chrome buna izin vermiyor. Eklenti mağazada onaylanınca uygulamanın "Eklentiyi Kur" akışı mağaza sayfasına yönlendirilecek; bu ayrı bir iş.

## Olmaması gerekenler (çalışsa bile başarısızlık sayılır)

- Kurulum veya kaldırma yönetici izni istememeli.
- Kaldırma, kullanıcının indirdiği dosyalara asla dokunmamalı.
- API anahtarı hiçbir yere gönderilmemeli, kurulum günlüğüne (log) yazılmamalı. Yalnızca kullanıcının bilgisayarında, uygulamanın ayar dosyasında saklanmalı.
- Kaldırmadan sonra kayıt defterinde Akıncı'ya ait Native Messaging girdisi kalmamalı.
- Kurulum sırasında Akıncı açıksa, açık indirmeler habersizce öldürülmemeli.

## Kısıtlar

- Araç: **Inno Setup 6** (kullanıcının kararı). Derleyici (`ISCC.exe`), yerelde ve CI'da kurulu olmalı.
- Uygulama .NET 10, self-contained ve single-file yayınlanıyor (`publish.bat` ile aynı parametreler). Kurulum programı büyük olacak (yaklaşık 100 MB ve üstü). Bu kabul edilebilir.
- Ayarlar `%USERPROFILE%\.akinci\settings.json` dosyasında, `SettingsService` tarafından okunuyor (`src/Akinci.UI/Services/SettingsService.cs`). Anahtar alanı `VirusTotalApiKey`.
- Uygulama kapatma düğmesinde sistem tepsisine küçülüyor (`MainWindow.AllowClose`). Pencereye gönderilen bir kapatma isteği uygulamayı sonlandırmayabilir.
- Chrome Native Messaging, Windows'ta manifest içindeki göreli `path` değerini manifest dosyasının bulunduğu klasöre göre çözer.
- İzin verilen eklenti kimlikleri: mağaza `coeeghhijegibfhhodhnjhjffaipckok`, paketlenmemiş `dhnoabdpnfdkljiknkocppmaoijplgbd`. Bugün iki yerde tutuluyor: `BrowserIntegrationService.AllowedExtensionIds` ve `src/Akinci.NativeHost/manifests/com.akinci.downloader.json`.

## Yaklaşımlar

| Yaklaşım | Artısı | Eksisi |
|---|---|---|
| **Inno Setup** (seçilen) | Özel sihirbaz sayfası (anahtar girişi), Türkçe dil dosyası, kısayol, Denetim Masası kaydı ve yöneticisiz kurulum hazır. Tek Setup.exe üretir. | Derleyicinin yerelde ve CI'da kurulu olması gerekir. Görünüm Akıncı'nın koyu temasıyla birebir aynı olmaz. |
| WiX (MSI) | Kurumsal standart, .NET projelerine entegre. | Özel giriş sayfası yazmak zahmetli ve hataya açık. |
| Kendi Avalonia sihirbazımız | Akıncı'yla birebir görünüm, tam kontrol. | Dosya kopyalama, Denetim Masası kaydı, kaldırma ve geri alma sıfırdan yazılmalı. En uzun ve en riskli yol. |

Velopack ve Squirrel gibi güncelleme odaklı araçlar sihirbaz sayfası sunmadığı için elendi. Kullanıcı Inno Setup'ı seçti.

## Tasarım

### Dosyalar

```text
installer/
├── akinci.iss                 # Inno Setup betiği
└── (isteğe bağlı) sihirbaz görselleri: büyük 164x314 ve küçük 55x55, Akıncı logosundan
publish.bat                    # 4. adım: ISCC varsa kurulum programını üretir
.github/workflows/release.yml  # Windows işi: Inno Setup kur, Setup.exe üret, sürüme ekle
src/Akinci.UI/Services/SettingsService.cs  # bekleyen anahtarı uygula (aşağıda)
src/Akinci.UI/Program.cs, App.axaml.cs     # tek örnek kilidi, --shutdown, kapanışta duraklatma (aşağıda)
src/Akinci.UI/ViewModels/MainWindowViewModel.cs  # açılışta yarım kalmış indirmeleri düzeltme
README.md                      # kurulum ve SmartScreen notu
```

### Kurulum yeri ve içerik

- `PrivilegesRequired=lowest`. Hedef klasör `{autopf}\Akinci`; yöneticisiz kurulumda bu `%LOCALAPPDATA%\Programs\Akinci` olur.
- Sabit bir `AppId` (GUID). Güncellemeler aynı kaydın üzerine kurulur.
- Sürüm, `Akinci.UI.csproj` içindeki `<Version>` değerinden alınır ve derlemede `/DAppVersion=...` ile betiğe aktarılır.
- Kurulan dosyalar: `Akinci.exe`, `Akinci-CLI.exe`, `Akinci-BrowserBridge.exe` ve `extension\` klasörü. Uygulamanın "Eklentiyi kur" akışı (`BrowserIntegrationService.FindIntegrationSources`) bu düzende eklentiyi ve köprüyü kurulum klasöründe bulur.
- `manifests\`, `install-host.bat`, `uninstall-host.bat` kurulum programına konmaz; zip dağıtımında kalırlar. (Not: zip'teki `install-host.bat` bugün çalışmıyor, çünkü kaydettiği `manifests\com.akinci.downloader.json` içindeki göreli yol `manifests\Akinci-BrowserBridge.exe`'yi gösteriyor ve orada dosya yok. Bu ayrı bir hata, bu işin kapsamında değil.)
- Kurulum ve kaldırma ekranındaki program simgesi: `src/Akinci.UI/Assets/akinci.ico`.

### Sihirbaz sayfaları (sırayla)

1. Hoş geldiniz.
2. Lisans: `LICENSE` (GPLv3, arayüz) ve `LICENSE-MIT` (çekirdek) birlikte gösterilir.
3. Kurulum klasörü (varsayılan önerilir, değiştirilebilir).
4. Ek görevler: "Masaüstü kısayolu oluştur" (varsayılan işaretli).
5. **VirusTotal sayfası** (aşağıda).
6. Kuruluyor.
7. Bitti: "Akıncı'yı başlat" (varsayılan işaretli).

### VirusTotal sayfası

- **Başlık:** "VirusTotal ile güvenlik taraması (isteğe bağlı)".
- **Açıklama:** Akıncı indirme bittiğinde dosyanın özetini (SHA-256) VirusTotal'a sorar ve 70'ten fazla antivirüs motorunun sonucunu gösterir. Bunun için ücretsiz bir VirusTotal API anahtarı gerekir. Dosyanın kendisi gönderilmez.
- **Anahtar alma:** "virustotal.com'da ücretsiz hesap açın, profil menüsünden API Key sayfasına gidin ve anahtarı kopyalayın." Tıklanabilir bir bağlantı tarayıcıda `https://www.virustotal.com/gui/join-us` adresini açar.
- **Giriş kutusu:** Anahtar, karakterleri gizlenerek girilir.
- **Not:** "Anahtarınız yoksa boş bırakıp devam edin; daha sonra Akıncı → Ayarlar'dan ekleyebilirsiniz." Güncelleme sırasında: "Boş bırakırsanız mevcut anahtarınız korunur."
- **Doğrulama:** Kutu doluysa ve değer 64 karakterlik bir onaltılık dize (hex) değilse, kullanıcı uyarılır ama yine de devam edebilir. VirusTotal anahtarları bu biçimde.

### Anahtarın uygulamaya aktarılması

Kurulum programı JSON düzenlemez. Kullanıcı bir anahtar girdiyse, kurulumun sonunda anahtarı tek satır olarak `%USERPROFILE%\.akinci\pending-virustotal-key` dosyasına yazar. `.akinci` klasörü yoksa oluşturulur.

`SettingsService` açılışta bu dosyayı görürse:
1. Değeri kırpıp `VirusTotalApiKey` alanına yazar. Varsa eski anahtarın üzerine yazılır, çünkü kullanıcı yeni bir anahtarı açıkça girdi.
2. Ayarları kaydeder.
3. Bekleyen dosyayı siler.

Böylece ilk kurulum ve güncelleme aynı yoldan geçer, mevcut ayarlar bozulmaz. Anahtar, kurulum günlüğüne yazılmaz.

CLI (`Akinci-CLI.exe`) anahtarı bugün olduğu gibi `--vt-key` parametresinden veya `VIRUSTOTAL_API_KEY` ortam değişkeninden okumaya devam eder. CLI'nin ayar dosyasını okuması kapsam dışı.

### Tarayıcı entegrasyonu (kullanıcı kararı)

- Kurulum programı Native Messaging kayıt defteri anahtarlarını **yazmaz**. Sebep: Uygulama ilk açılışta, köprü kayıtlı değilse (`MainWindow.CheckAndPromptExtensionInstallation`, `BrowserIntegrationService.IsNativeHostRegistered`) "Eklentiyi kur" penceresini gösteriyor. Mağaza onayına kadar kullanıcıyı eklentiye götüren tek otomatik yol bu. Kurulum anahtarları yazsaydı pencere hiç çıkmazdı.
- Kullanıcı pencerede "Kur" derse uygulama bugünkü gibi eklentiyi ve köprüyü `%USERPROFILE%\.akinci\extension` ve `\host` altına kopyalar ve Chrome/Edge anahtarlarını yazar.
- Kaldırma şu anahtarları, kimin yazdığına bakmadan siler: `HKCU\Software\Google\Chrome\NativeMessagingHosts\com.akinci.downloader`, `HKCU\Software\Microsoft\Edge\NativeMessagingHosts\com.akinci.downloader` ve varsa `HKCU\Software\Mozilla\NativeMessagingHosts\com.akinci.downloader`.

### Kapanışta indirmelerin korunması (kullanıcı kararı)

Bugün uygulama indirme sürerken kapanırsa (tepsiden "Çıkış" dahil) görev veritabanında `Downloading` olarak kalıyor. `App.axaml.cs` içindeki `Exit` işleyicisi yalnızca IPC sunucusunu kapatıyor, `MainWindowViewModel.InitializeAsync` de yüklenen durumu düzeltmiyor. Sonuç olarak görev bir sonraki açılışta devam etmiyor ve `MaxConcurrentDownloads` kuyruğunda yer kaplıyor. Bu iş kapsamında düzeltilecek:

- **Kapanırken:** Uygulama kapanmadan önce aktif indirmeleri duraklatır, yani iptal eder ve `Paused` olarak kaydeder. Parça ofsetleri `StartDownloadCommandHandler`'ın iptal yolu üzerinden zaten kaydediliyor. Kapanış, bu kayıtların bitmesini kısa bir süre (ör. birkaç saniye) bekler.
- **Açılışta:** Veritabanından `Downloading` durumunda yüklenen görevler `Paused` yapılır. Bu, uygulamanın çöktüğü veya zorla sonlandırıldığı durumları da kapsar.
- **Otomatik devam yok:** Duraklatılan indirmeler açılışta kendiliğinden başlamaz. Kullanıcı "Tümünü Başlat" ile ya da tek tek devam ettirir. _Builder'ın kararı olarak işaretlendi; davranış değiştirilmek istenirse kolay._

### Açık uygulama (kurulum ve kaldırma sırasında)

Uygulamada bugün tek örnek kontrolü yok. Windows'un kapatma isteği (Restart Manager dahil) pencereyi yalnızca tepsiye küçültüyor (`MainWindow.OnClosing`, `AllowClose`). Bu yüzden kurulum programının uygulamayı tespit etmek ve kapatmak için kendi yolu olmalı:

- **Tek örnek kilidi:** Uygulama açılışta adlandırılmış bir mutex (ör. `Local\Akinci.SingleInstance`) alır. Inno Setup bunu `AppMutex` ile tanır ve uygulamanın açık olduğunu anlar. Yan fayda: Akıncı ikinci kez açılırsa yeni bir örnek başlamaz, mevcut pencere öne getirilir.
- **`Akinci.exe --shutdown`:** Çalışan örneğe "kapan" isteği gönderir. Çalışan örnek indirmeleri yukarıdaki gibi duraklatır ve tamamen çıkar (tepsiye küçülmez). İstek, yalnızca yerel olarak erişilebilen bir kanal üzerinden iletilir; yöntem builder'a bırakılmıştır (adlandırılmış kanal ya da mevcut `127.0.0.1` IPC sunucusunda `ipc.token` ile korunan bir uç).
- **Kurulum/kaldırma akışı:** Uygulama açıksa kullanıcıya "Akıncı açık. Devam etmek için kapatılacak; devam eden indirmeler duraklatılıp kaydedilecek." mesajı gösterilir.
  - Tamam derse `--shutdown` çalıştırılır ve mutex bırakılana kadar beklenir (ör. en fazla 15 saniye).
  - Uygulama bu sürede kapanmazsa kullanıcıya zorla kapatmak isteyip istemediği sorulur. Zorla kapatmada son kaydedilen ofsetten sonraki birkaç megabaytlık ilerleme tekrar indirilir. Açılıştaki düzeltme sayesinde görevler yine `Paused` olur.
  - İptal derse kurulum durur.

### Kaldırma

- Program dosyaları, kısayollar, Native Messaging kayıt anahtarları ve uygulamanın oluşturduğu kopyalar (`%USERPROFILE%\.akinci\host\`, `%USERPROFILE%\.akinci\extension\`) silinir.
- Kaldırma sırasında sorulur: "Ayarlarınız, indirme geçmişiniz ve VirusTotal anahtarınız da silinsin mi?" Varsayılan **Hayır**. Evet derse `%USERPROFILE%\.akinci\` altında **yalnızca uygulamanın oluşturduğu dosyalar** silinir: `settings.json`, `downloads.db`, `downloads.db-wal`, `downloads.db-shm`, `ipc.token`, `pending-virustotal-key`. Klasör boş kalırsa klasör de silinir. Kullanıcı indirme klasörünü `.akinci` altına ayarlamış olabileceği için klasörün tamamı silinmez. _Builder'ın kararı olarak işaretlendi._
- İndirilen dosyalara (`DefaultDownloadDirectory`) asla dokunulmaz.
- Chrome'a yüklenmiş eklenti kaldırılamaz. Kaldırma bittiğinde bilgi verilir: "Tarayıcı eklentisini Chrome'un eklentiler sayfasından kaldırabilirsiniz."

### Derleme

- **Yerel:** `publish.bat` mevcut üç yayın adımından sonra `ISCC.exe`'yi arar (`PATH`, ardından `%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe` ve `%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe`). Bulursa `dist\Akinci-Setup-<sürüm>.exe` üretir. Bulamazsa hata vermez; Inno Setup'ın nasıl kurulacağını (`winget install JRSoftware.InnoSetup`) yazar ve zip/klasör çıktısıyla bitirir.
- **CI:** `release.yml` Windows işinde, yayın adımlarından sonra Inno Setup kurulur (ör. `choco install innosetup -y`, ya da runner'da hazır varsa doğrudan kullanılır). Ardından kurulum programı derlenir ve `Akinci-Setup-<etiket>.exe` sürüme zip'in yanına eklenir.
- Kurulum programına girdi olarak `publish.bat` ile aynı klasör düzeni kullanılır; betik kaynak klasörünü bir tanımla (`/DSourceDir=...`) alır.

### Doğrulama

- Inno Setup derleyicisi yerelde kurulur. Bu, kullanıcının onayıyla `winget` üzerinden yapılır.
- Kurulum programı derlenir ve sihirbaz elle bir kez baştan sona geçilir. Her sayfanın ekran görüntüsü alınır, Türkçe karakterler ve metinler kontrol edilir.
- Kurulum sonrası kontrol edilecekler:
  - Dosyalar `%LOCALAPPDATA%\Programs\Akinci` altında.
  - Başlat menüsü ve masaüstü kısayolları var.
  - Uygulama ilk açılışta "Eklentiyi kur" penceresini gösteriyor; "Kur" denince eklenti ve köprü kurulum klasöründen bulunup kopyalanıyor.
  - Girilen anahtar, uygulama açıldıktan sonra `settings.json` dosyasında; `pending-virustotal-key` silinmiş.
  - "Programlar ve Özellikler"de Akıncı sürümüyle birlikte listeleniyor.
- Güncelleme: Aynı sürüm ya da daha yeni bir sürüm üzerine kurulur. Anahtar boş bırakılırsa mevcut anahtar korunuyor olmalı.
- Kaldırma: Dosyalar, kısayollar ve kayıt defteri anahtarları silinmiş olmalı. "Hayır" seçilirse `.akinci` klasörü yerinde kalmalı, "Evet" seçilirse silinmeli. İndirilen dosyalar yerinde kalmalı.
- Açık uygulama: İndirme sürerken kurulum başlatılır. Mesaj çıkıyor, `--shutdown` sonrası uygulama kapanıyor, görev `Paused` olarak kaydediliyor ve yeniden açılınca kaldığı yerden devam ettirilebiliyor.
- Zorla sonlandırma sonrası açılış: `Downloading` olarak kalmış görev `Paused` görünüyor ve kuyruğu tıkamıyor.
- İkinci örnek: Akıncı açıkken tekrar başlatılınca yeni pencere açılmıyor, mevcut pencere öne geliyor.
- Birim testleri: Bekleyen anahtar dosyası varsa anahtar uygulanıyor ve dosya siliniyor, yoksa ayarlar değişmiyor. Açılışta `Downloading` → `Paused` düzeltmesi. Not: Bugün UI projesinin test projesi yok. Bu mantık ya UI dışına (ör. Application katmanına) taşınabilir, ya da bir test projesi eklenebilir; seçim builder'ın.

## Builder'a bırakılanlar

- Inno Setup betiğinin iç yapısı (bölümler, Pascal Script).
- Sihirbaz görsellerinin olup olmayacağı ve nasıl görüneceği.
- `--shutdown` isteğinin iletildiği yerel kanal (yukarıdaki kısıtlarla).
- Kapanışta indirmelerin kaydedilmesi için beklenecek süre.
- CI'da Inno Setup'ın kurulma yöntemi.
- `SettingsService` testinin nerede duracağı.
