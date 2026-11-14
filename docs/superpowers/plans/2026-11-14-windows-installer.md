# Akıncı Windows Kurulum Programı — Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Akıncı'yı yönetici izni istemeden kuran, VirusTotal anahtarı sayfası olan ve Denetim Masası'ndan kaldırılabilen tek bir Inno Setup `Akinci-Setup-<sürüm>.exe` üretmek. Bunun için gereken uygulama tarafı değişikliklerini de yapmak: kapanışta indirmeleri korumak, tek örnek kilidi ve `--shutdown`.

**Architecture:** Kurulum programı Inno Setup betiği (`installer/akinci.iss`). Uygulamayla konuşmasının iki yolu var: girilen anahtarı bir "bekleyen dosya"ya yazar, uygulama açılışta onu içeri alır; uygulamanın açık olup olmadığını adlandırılmış bir mutex'ten anlar ve `Akinci.exe --shutdown` ile uygulamayı düzgünce kapatır. Kapanış isteği, mevcut `127.0.0.1` IPC sunucusuna `ipc.token` ile korunan yeni bir `/control/` uç noktası üzerinden iletilir. Yarım kalmış indirmeleri düzelten mantık test edilebilsin diye Application katmanında bir use case olarak yazılır.

**Tech Stack:** .NET 10, Avalonia 12, xUnit 2.9, Inno Setup 6 (Pascal Script), PowerShell 5.1 (`publish.bat` yardımcıları), GitHub Actions.

**Spec:** `docs/superpowers/specs/2026-11-14-windows-installer-design.md`

## Global Constraints

- Kurulum ve kaldırma yönetici izni istemez: `PrivilegesRequired=lowest`, hedef `{autopf}\Akinci`.
- Sabit `AppId`: `{{4C13BB6C-AE62-472E-B15A-DC329B2C10AF}`. Bu değer sürümler arasında asla değişmez.
- Sürüm `src/Akinci.UI/Akinci.UI.csproj` içindeki `<Version>` değerinden okunur (şu an `1.0.2`). Çıktı adı: `Akinci-Setup-<sürüm>.exe`.
- Mutex adı: `Local\Akinci.SingleInstance`. Hem uygulamada hem kurulum betiğindeki `CheckForMutexes` çağrısında birebir aynı yazılmalı. Inno'nun `AppMutex` yönergesi **kullanılmaz** (kendi uyarısını göstererek `[Code]` akışıyla çift mesaj üretir).
- Bekleyen anahtar dosyası: `%USERPROFILE%\.akinci\pending-virustotal-key` (uzantısız, tek satır, UTF-8).
- VirusTotal anahtar biçimi: 64 karakter onaltılık (`^[0-9a-fA-F]{64}$`). Biçim dışı değer uyarı verir ama engellemez.
- API anahtarı hiçbir yere gönderilmez, kurulum günlüğüne yazılmaz.
- Kaldırma, indirilen dosyalara asla dokunmaz. "Evet" denirse `.akinci` altında yalnızca şunlar silinir: `settings.json`, `downloads.db`, `downloads.db-wal`, `downloads.db-shm`, `ipc.token`, `pending-virustotal-key`, `host\`, `extension\`. Klasör boş kalırsa klasör de silinir.
- Kaldırma şu kayıt defteri anahtarlarını siler: `HKCU\Software\Google\Chrome\NativeMessagingHosts\com.akinci.downloader`, `HKCU\Software\Microsoft\Edge\NativeMessagingHosts\com.akinci.downloader`, `HKCU\Software\Mozilla\NativeMessagingHosts\com.akinci.downloader`. Kurulum bunları **yazmaz**.
- Sihirbaz dilleri: Türkçe (varsayılan) ve İngilizce, Windows diline göre otomatik.
- Kullanıcıya görünen tüm metinler Türkçe.

## Review Focus

1. **Kurulum sırasında indirme sürüyor:** Uygulama `--shutdown` ile kapanmalı, görev `Paused` kaydedilmeli, yeniden açılınca devam edebilmeli. → Task 3 Step 6 (elle doğrulama) ve Task 5 Step 6.
2. **IPC sunucusu başlayamadı (49500 portu dolu):** `--shutdown` iletilemez. Kurulum 15 saniye bekleyip zorla kapatmayı sormalı, takılı kalmamalı. → Task 4 Step 5.
3. **Anahtar kutusuna boşluklu veya satır sonlu değer yapıştırıldı:** Kırpılıp kaydedilmeli. → Task 2 testi `TryConsume_TrimsWhitespaceAndNewlines`.
4. **Güncellemede anahtar boş bırakıldı:** Mevcut anahtar korunmalı, bekleyen dosya yazılmamalı. → Task 2 testi `TryConsume_ReturnsNullWhenFileMissing` ve Task 4 Step 6.
5. **Uygulama çöktü veya zorla kapatıldı:** Açılışta `Downloading` görevler `Paused` olmalı, kuyruğu tıkamamalı. → Task 1 testleri.

---

### Task 1: Yarım kalmış indirmeleri açılışta düzelt

**Files:**
- Create: `src/Akinci.Application/UseCases/RecoverInterruptedDownloads/RecoverInterruptedDownloadsCommand.cs`
- Modify: `src/Akinci.UI/ViewModels/MainWindowViewModel.cs` (`InitializeAsync`, satır ~110; constructor)
- Modify: `src/Akinci.UI/App.axaml.cs` (`ConfigureServices`, handler kaydı)
- Test: `tests/Akinci.Application.UnitTests/UseCases/RecoverInterruptedDownloadsCommandHandlerTests.cs`

**Interfaces:**
- Produces: `public sealed class RecoverInterruptedDownloadsCommandHandler(IDownloadRepository repository)` ve `Task<int> HandleAsync(CancellationToken cancellationToken = default)`. Dönüş, `Paused` yapılan görev sayısı.

- [ ] **Step 1: Write the failing tests** (mevcut `Fakes/InMemoryDownloadRepository` ile)

```csharp
[Fact] public async Task HandleAsync_PausesTasksLeftInDownloadingState()
// İki görev: biri Start() edilmiş (Downloading), biri Pending. HandleAsync → 1 döner;
// repository'den okunan ilk görev Status == Paused ve PausedAt != null; ikincisi Pending kalır.

[Fact] public async Task HandleAsync_LeavesCompletedFailedAndPausedTasksUntouched()
// Completed, Failed, Paused birer görev → 0 döner, durumlar değişmez.

[Fact] public async Task HandleAsync_PreservesChunkOffsets()
// 1000 bayt, 2 parça, Start(), Chunks[0].AdvanceOffset(300) → HandleAsync sonrası Chunks[0].CurrentOffset == 300.
```

- [ ] **Step 2: Run to verify they fail**
Run: `dotnet test tests/Akinci.Application.UnitTests --filter RecoverInterruptedDownloads`
Expected: derleme hatası (`RecoverInterruptedDownloadsCommandHandler` yok).

- [ ] **Step 3: Implement the handler.** `GetAllAsync`, ardından `Status == DownloadStatus.Downloading` olan her görev için `task.Pause()` ve `UpdateAsync`. Diğer use case'lerle aynı klasör/namespace düzeni (`Akinci.Application.UseCases.RecoverInterruptedDownloads`).

- [ ] **Step 4: Run tests.** Aynı komut. Expected: 3 PASS.

- [ ] **Step 5: Wire into the UI.** `App.ConfigureServices` içinde `services.AddTransient<RecoverInterruptedDownloadsCommandHandler>()`. `MainWindowViewModel` constructor'ına parametre olarak eklenir. `InitializeAsync`, `GetAllAsync`'ten **önce** `await _recoverHandler.HandleAsync()` çağırır.

- [ ] **Step 6: Build and run all tests.**
Run: `dotnet build Akinci.slnx` (Akıncı açıksa önce kapat; açık uygulama DLL'leri kilitler) ve `dotnet test Akinci.slnx`.
Expected: 0 hata; 107 test geçer.

- [ ] **Step 7: Commit** — `fix: pause downloads left in downloading state at startup`

### Task 2: Bekleyen VirusTotal anahtarını içeri al

**Files:**
- Create: `src/Akinci.Infrastructure/Settings/PendingSettingFile.cs`
- Modify: `src/Akinci.UI/Services/SettingsService.cs` (constructor, `LoadSettings` sonrası)
- Test: `tests/Akinci.Infrastructure.IntegrationTests/PendingSettingFileTests.cs`

**Interfaces:**
- Produces: `public static class PendingSettingFile { public static string? TryConsume(string filePath); }`
  - Dosya yoksa `null` döner.
  - Dosya varsa içeriği okur, `Trim()` eder, dosyayı siler.
  - Kırpılmış değer boşsa `null` döner, ama dosya yine silinir.
  - Okuma veya silme hatası istisna fırlatmaz, `null` döner.

- [ ] **Step 1: Write the failing tests** (geçici klasörde)

```csharp
[Fact] public void TryConsume_ReturnsValueAndDeletesFile()        // "abc" yazılı → "abc", File.Exists == false
[Fact] public void TryConsume_TrimsWhitespaceAndNewlines()        // "  key123\r\n" → "key123"
[Fact] public void TryConsume_ReturnsNullWhenFileMissing()        // yok → null
[Fact] public void TryConsume_ReturnsNullAndDeletesWhenBlank()    // "   \n" → null, dosya silinmiş
```

- [ ] **Step 2: Run to verify they fail**
Run: `dotnet test tests/Akinci.Infrastructure.IntegrationTests --filter PendingSettingFile`
Expected: derleme hatası.

- [ ] **Step 3: Implement `PendingSettingFile.TryConsume`.** Akinci.UI, Infrastructure'ı zaten referans ediyor; yeni proje referansı gerekmez.

- [ ] **Step 4: Run tests.** Expected: 4 PASS.

- [ ] **Step 5: Use it in `SettingsService`.** `LoadSettings()` sonrasında `PendingSettingFile.TryConsume(Path.Combine(appDir, "pending-virustotal-key"))` çağrılır. Sonuç `null` değilse `_currentSettings.VirusTotalApiKey` atanır ve `SaveSettings(_currentSettings)` çağrılır. Anahtar loglanmaz.

- [ ] **Step 6: Manual check.**
  - `%USERPROFILE%\.akinci\pending-virustotal-key` dosyasına sahte bir değer yaz (`0000…` 64 karakter).
  - Uygulamayı aç.
  - `settings.json` içinde `VirusTotalApiKey` bu değer olmalı, bekleyen dosya silinmiş olmalı.
  - Sonra eski anahtarı geri yaz veya alanı temizle.

- [ ] **Step 7: Build, full test run, commit** — `feat: import VirusTotal API key handed over by the installer`

### Task 3: Tek örnek kilidi, `--shutdown` ve kapanışta indirmeleri duraklatma

**Files:**
- Create: `src/Akinci.UI/Services/SingleInstance.cs`
- Modify: `src/Akinci.UI/Program.cs` (`Main`)
- Modify: `src/Akinci.UI/Services/IpcServerService.cs` (yeni `/control/` öneki ve rotalar)
- Modify: `src/Akinci.UI/App.axaml.cs` (graceful shutdown, tepsideki "Çıkış")
- Modify: `src/Akinci.UI/MainWindow.axaml.cs` (dışarıdan öne getirme)

**Interfaces:**
- Produces: `internal static class SingleInstance`
  - `const string MutexName = @"Local\Akinci.SingleInstance"`.
  - `static bool TryAcquire(out Mutex mutex)`.
  - `static Task<bool> SendControlAsync(string command)`. `command` `"shutdown"` veya `"activate"`. İsteği `POST http://127.0.0.1:49500/control/{command}/` adresine, `X-Akinci-Token` başlığı `%USERPROFILE%\.akinci\ipc.token` içeriği olacak şekilde gönderir. Zaman aşımı 3 sn. Başarılıysa `true`.
- Produces: `App.RequestGracefulShutdownAsync(): Task`.
  - Aktif indirmeleri duraklatır (`MainWindowViewModel.PauseAllAsync`). En fazla 5 sn bekler.
  - `MainWindow.AllowClose = true` yapar, `desktop.Shutdown()` çağırır.
  - Tepsideki "Çıkış" da bu yolu kullanır.
- Consumes: `IpcServerService` içindeki mevcut `_ipcToken`.

- [ ] **Step 1: `Program.Main` davranışı**

| Durum | Davranış |
|---|---|
| `--shutdown` + mutex alınamadı (başka örnek açık) | `SendControlAsync("shutdown")`, çıkış kodu 0 |
| `--shutdown` + mutex alındı (açık örnek yok) | Hemen çıkış kodu 0 (Avalonia başlatılmaz) |
| Argümansız + mutex alınamadı | `SendControlAsync("activate")`, çıkış kodu 0 |
| Argümansız + mutex alındı | Normal açılış. Mutex süreç ömrü boyunca tutulur. |

- [ ] **Step 2: IPC `/control/` uç noktası.**
  - `_listener.Prefixes.Add("http://127.0.0.1:49500/control/")`.
  - Kural: Yalnızca `POST`. `Origin` başlığı varsa **403** (tarayıcıdan gelen istek kabul edilmez, eklenti dahil). `X-Akinci-Token` `_ipcToken` ile eşleşmezse **403**.
  - `shutdown` → 200 yanıtı gönderilir, sonra UI iş parçacığında `App.RequestGracefulShutdownAsync()`.
  - `activate` → 200, ana pencere `Show()`, `WindowState = Normal`, `Activate()`.
  - Bilinmeyen komut → 404.

- [ ] **Step 3: `RequestGracefulShutdownAsync` ve tepsi "Çıkış".** Duraklatma, `DownloadItemViewModel.PauseAsync` üzerinden yapılır. Bu, `_downloadCts.Cancel()` ve `PauseDownloadCommandHandler` ile durumu kaydeder. Toplam bekleme `Task.WhenAny(pauseAll, Task.Delay(5000))`.

- [ ] **Step 4: Build.** Run: `dotnet build Akinci.slnx`. Expected: 0 hata.

- [ ] **Step 5: Manual check — ikinci örnek.** Uygulama açıkken `Akinci.exe` tekrar çalıştırılır. Yeni pencere açılmamalı, mevcut pencere öne gelmeli, ikinci süreç hemen çıkmalı (`Get-Process Akinci` tek kayıt).

- [ ] **Step 6: Manual check — `--shutdown` sırasında indirme.**
  - Büyük bir dosya indirmesi başlat (ör. yerel test sunucusu ya da bilinen bir ISO).
  - `Akinci.exe --shutdown` çalıştır. Süreç 5 sn içinde bitmeli.
  - Yeniden açınca görev "Duraklatıldı" görünmeli ve "Devam" ile kaldığı yerden sürmeli.

- [ ] **Step 7: Manual check — güvenlik.** `curl -X POST -H "Origin: https://example.com" http://127.0.0.1:49500/control/shutdown/` → 403. Tokensiz → 403. Uygulama açık kalmalı.

- [ ] **Step 8: Full test run, commit** — `feat: single instance lock and graceful --shutdown that pauses downloads`

### Task 4: Inno Setup betiği

**Files:**
- Create: `installer/akinci.iss`
- Create: `installer/build-installer.ps1`
  - Sürümü `Akinci.UI.csproj`'dan okur ve `ISCC.exe`'yi bulur: `PATH`, ardından `%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe`, ardından `%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe`.
  - `ISCC /DAppVersion=<v> /DSourceDir=<publish klasörü> /O<dist> installer\akinci.iss` çalıştırır.
  - ISCC yoksa kurulum talimatını (`winget install JRSoftware.InnoSetup`) yazıp **0** koduyla çıkar.

**Interfaces:**
- Consumes: `/DSourceDir` klasöründe `Akinci.exe`, `Akinci-CLI.exe`, `Akinci-BrowserBridge.exe`, `extension\`. `publish.bat` çıktısı (`dist\Akinci`) ile aynı düzen.
- Consumes: Task 3'teki mutex adı ve `--shutdown`.
- Produces: `dist\Akinci-Setup-<sürüm>.exe`.
- Komut satırı parametreleri (sessiz test için):
  - `/VTKEY=<anahtar>`: anahtar sayfasını doldurur.
  - `/DELETEUSERDATA=1`: kaldırmada veri silme sorusuna "Evet" der. Sessiz kaldırmada varsayılan "Hayır".

- [ ] **Step 1: `[Setup]` ve `[Languages]`.**
  - `[Setup]`: `AppId` ve `PrivilegesRequired` Global Constraints'teki değerler. `AppName=Akıncı`, `AppVersion={#AppVersion}`, `AppPublisher=Akıncı`, `DefaultDirName={autopf}\Akinci`, `DefaultGroupName=Akıncı`, `UninstallDisplayIcon={app}\Akinci.exe`, `SetupIconFile=..\src\Akinci.UI\Assets\akinci.ico`, `OutputBaseFilename=Akinci-Setup-{#AppVersion}`, `CloseApplications=no`, `Compression=lzma2/max`, `SolidCompression=yes`, `WizardStyle=modern`, `LicenseFile=license.txt`.
  - `license.txt`, betik derlenmeden önce `build-installer.ps1` tarafından `LICENSE-MIT` + ayraç + `LICENSE` birleştirilerek `installer\obj\` altına üretilir. `LicenseFile` buna işaret eder.
  - `[Languages]`: `turkish` (`compiler:Languages\Turkish.isl`, ilk sırada) ve `english` (`compiler:Default.isl`).

- [ ] **Step 2: `[Files]`, `[Icons]`, `[Tasks]`, `[Run]`.**
  - `[Files]`: dört kaynak (`recursesubdirs` ile `extension\`).
  - `[Icons]`: Başlat menüsü kısayolu her zaman. Masaüstü kısayolu `Tasks: desktopicon`.
  - `[Tasks]`: `desktopicon` varsayılan işaretli (`Flags:` yok).
  - `[Run]`: `{app}\Akinci.exe`, `Flags: nowait postinstall skipifsilent`, açıklama "Akıncı'yı başlat".

- [ ] **Step 3: VirusTotal sayfası (`[Code]`).**
  - `CreateInputQueryPage(wpSelectTasks, ...)` ile oluşturulur, tek alan, `Password = True`.
  - Metinler spec'teki "VirusTotal sayfası" bölümünden birebir alınır. Kayıt sayfası bağlantısı: `https://www.virustotal.com/gui/join-us`. `ShellExec` ile açılan bir `TNewStaticText` veya buton olabilir.
  - Önceki kurulum varsa (`IsUpgrade`: `HKCU\...\Uninstall\{AppId}_is1` mevcut) not metni "Boş bırakırsanız mevcut anahtarınız korunur." olur.
  - `NextButtonClick`: değer doluysa ve regex'e uymuyorsa `MsgBox(..., mbConfirmation, MB_YESNO)`. "Hayır" sayfada tutar.
  - `/VTKEY=` parametresi verilmişse alan önceden doldurulur.

- [ ] **Step 4: Anahtarı devret.**
  - `CurStepChanged(ssPostInstall)` içinde alan doluysa `ForceDirectories(ExpandConstant('{%USERPROFILE}\.akinci'))` ve `SaveStringToFile(...\pending-virustotal-key, Trim(value), False)`.
  - Değer hiçbir `Log()` çağrısına verilmez.

- [ ] **Step 5: Açık uygulama.**
  - `InitializeSetup` ve `InitializeUninstall` içinde `CheckForMutexes('Local\Akinci.SingleInstance')` kontrol edilir.
  - Mutex varsa spec'teki mesaj gösterilir: "Akıncı açık. Devam etmek için kapatılacak; devam eden indirmeler duraklatılıp kaydedilecek." Tamam/İptal.
  - Tamam → çalışan sürümün yolu bulunur ve `--shutdown` ile çalıştırılır (`Exec`). Ardından 250 ms aralıkla en fazla 15 sn mutex'in kalkması beklenir.
    - Kurulumda yol, önceki kurulumun `HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\{AppId}_is1` kaydındaki `InstallLocation` + `Akinci.exe`.
    - Kaldırmada yol `{app}\Akinci.exe`.
    - Yol bulunamazsa (ör. Akıncı zip'ten çalışıyor) doğrudan zorla kapatma sorusuna geçilir.
  - Kalkmazsa "Akıncı kapanmadı. Zorla kapatılsın mı?" (Evet/Hayır). Evet → `taskkill /IM Akinci.exe /F`. Hayır veya İptal → kurulum/kaldırma `False` döner.
  - Sessiz modda soru sorulmaz: `--shutdown` denenir, kapanmazsa kurulum iptal olur.

- [ ] **Step 6: Kaldırma.**
  - `[Registry]` bölümünde üç Native Messaging anahtarı `Flags: deletekey uninsdeletekey dontcreatekey` ile listelenir.
  - `CurUninstallStepChanged(usPostUninstall)`:
    - Veri silme sorusu sorulur (varsayılan Hayır, `/DELETEUSERDATA=1` → Evet).
    - Evet ise Global Constraints'teki dosya listesi silinir, `host\` ve `extension\` `DelTree` ile silinir.
    - Hayır ise yalnızca `host\` ve `extension\` silinir (spec: uygulamanın oluşturduğu kopyalar).
    - Klasör boşsa `RemoveDir`.
    - Son olarak bilgi mesajı (sessiz modda gösterilmez): "Tarayıcı eklentisini Chrome'un eklentiler sayfasından kaldırabilirsiniz."

- [ ] **Step 7: Derle.**
  - Inno Setup yerelde yoksa önce kullanıcıdan onay alınır, sonra `winget install --id JRSoftware.InnoSetup -e --scope user` çalıştırılır.
  - Run: `publish.bat` (Task 5 öncesinde elle: üç `dotnet publish` + `powershell -File installer\build-installer.ps1 -SourceDir dist\Akinci`).
  - Expected: `dist\Akinci-Setup-1.0.2.exe` oluşur, ISCC uyarısız biter.

- [ ] **Step 8: Sessiz kurulum testi.**

```powershell
.\dist\Akinci-Setup-1.0.2.exe /VERYSILENT /SUPPRESSMSGBOXES /VTKEY=<64 hex sahte değer> /LOG=$env:TEMP\akinci-setup.log
```

Beklenenler:
  - `%LOCALAPPDATA%\Programs\Akinci\Akinci.exe` ve `extension\manifest.json` mevcut.
  - Başlat menüsü ve masaüstü kısayolu mevcut.
  - `HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\{4C13BB6C-AE62-472E-B15A-DC329B2C10AF}_is1` var ve `DisplayVersion=1.0.2`.
  - `pending-virustotal-key` mevcut.
  - Günlükte anahtar geçmiyor: `Select-String -Path $env:TEMP\akinci-setup.log -Pattern <değer>` sonuçsuz.

- [ ] **Step 9: Sihirbazı elle geç.**
  - Setup.exe'yi normal çalıştır, her sayfanın ekran görüntüsünü al.
  - Kontrol edilecekler: Türkçe karakterler, lisans, VirusTotal metinleri ve bağlantısı, biçim uyarısı (`abc` gir → uyarı), masaüstü onay kutusu, "Akıncı'yı başlat".
  - Açılan uygulamada ayarlarda anahtar görünüyor, ilk açılışta "Eklentiyi kur" penceresi çıkıyor.

- [ ] **Step 10: Güncelleme ve kaldırma.**
  - Aynı Setup'ı anahtarsız tekrar çalıştır. Mevcut anahtar korunmalı, sayfada "korunur" notu görünmeli.
  - Uygulama açıkken kaldırma başlat. Shutdown akışı çalışmalı.
  - "Hayır" ile kaldır: dosyalar, kısayollar ve üç kayıt anahtarı gitmiş olmalı, `.akinci\settings.json` ve `downloads.db` kalmalı.
  - Tekrar kur, sonra `unins000.exe /VERYSILENT /DELETEUSERDATA=1` ile kaldır: listelenen dosyalar silinmiş olmalı. `.akinci` içine elle konmuş `test.txt` yerinde kalmalı.

- [ ] **Step 11: Commit** — `feat: Inno Setup installer with VirusTotal key page and clean uninstall`

### Task 5: Derleme entegrasyonu ve belgeler

**Files:**
- Modify: `publish.bat` (yeni 4. adım)
- Modify: `.github/workflows/release.yml` (Windows işi)
- Modify: `README.md` (kurulum bölümü + SmartScreen notu)

**Interfaces:**
- Consumes: Task 4'teki `installer/build-installer.ps1 -SourceDir <klasör> -OutputDir <klasör>`.

- [ ] **Step 1: `publish.bat`.** Üç yayın adımından ve kopyalamalardan sonra `powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%installer\build-installer.ps1" -SourceDir "%TARGET_DIR%" -OutputDir "%OUTPUT_DIR%"` çalıştırılır. Başarı mesajına Setup.exe satırı eklenir. ISCC yoksa betik uyarıp devam eder.

- [ ] **Step 2: `release.yml`.**
  - Windows işinde paketlemeden sonra Inno Setup kurulur: `choco install innosetup -y --no-progress`. Runner'da `ISCC.exe` zaten varsa bu adım atlanabilir.
  - Ardından `build-installer.ps1` çalıştırılır.
  - `Upload Artifact` ve `create-github-release` `files:` listesine `Akinci-Setup-*.exe` eklenir.
  - Dosya adı sürümü csproj'dan alır. Etiketle uyumsuzluk kabul edilir; spec tek kaynak olarak csproj'u belirliyor.

- [ ] **Step 3: README.** Kısa bir "Kurulum (Windows)" bölümü:
  - Setup.exe'yi indir ve çalıştır.
  - SmartScreen uyarısında "Ek bilgi → Yine de çalıştır".
  - Yönetici izni gerekmez.
  - Denetim Masası'ndan kaldırılabilir.
  - Zip dağıtımı da var.

- [ ] **Step 4: Verify locally.** Run: `publish.bat`. Expected: `dist\Akinci\` ve `dist\Akinci-Setup-1.0.2.exe` oluşur. ISCC geçici olarak `PATH`'ten çıkarılınca (yeniden adlandırmadan) betik talimatı yazıp yine başarıyla biter.

- [ ] **Step 5: Verify workflow syntax.** `release.yml`'ı YAML olarak ayrıştır (ör. `python -c "import yaml,sys;yaml.safe_load(open(...))"` ya da `actionlint` varsa).
  - Gerçek çalıştırma yalnızca etiket push'unda olur. Bu adımda etiket atılmaz. CI doğrulanmamış olarak raporlanır.

- [ ] **Step 6: Uçtan uca son kontrol.**
  - Yeni Setup.exe ile temiz bir kurulum yap.
  - Uygulamada bir indirme başlat, kurulumu tekrar çalıştır (güncelleme).
  - İndirme duraklatılıp kaydedilmeli, güncelleme bitmeli, açılan uygulamada indirme devam ettirilebilmeli.

- [ ] **Step 7: Full test run and commit** — `build: produce Windows installer in publish.bat and release workflow`
