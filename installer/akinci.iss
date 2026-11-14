; Akıncı Windows kurulum programı (Inno Setup 6)
; Derleme: installer\build-installer.ps1 (AppVersion, SourceDir ve LicenseFile tanımlarını geçirir)
; Tasarım: docs\superpowers\specs\2026-11-14-windows-installer-design.md

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\dist\Akinci"
#endif
#ifndef LicenseFile
  #define LicenseFile "obj\license.txt"
#endif

[Setup]
; AppId asla değiştirilmemeli: güncellemeler ve kaldırma bu kimliğe bağlı
AppId={{4C13BB6C-AE62-472E-B15A-DC329B2C10AF}
AppName=Akıncı
AppVersion={#AppVersion}
AppVerName=Akıncı {#AppVersion}
AppPublisher=Akıncı
AppPublisherURL=https://github.com/devutku/akinci
AppSupportURL=https://github.com/devutku/akinci/issues
AppUpdatesURL=https://github.com/devutku/akinci/releases
VersionInfoVersion={#AppVersion}
DefaultDirName={autopf}\Akinci
DefaultGroupName=Akıncı
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayName=Akıncı
UninstallDisplayIcon={app}\Akinci.exe
SetupIconFile=..\src\Akinci.UI\Assets\akinci.ico
LicenseFile={#LicenseFile}
OutputBaseFilename=Akinci-Setup-{#AppVersion}
; Açık uygulama [Code] içinde tek örnek kilidi ve --shutdown ile ele alınır
CloseApplications=no
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
; Spec: sihirbaz "Hoş geldiniz" sayfasıyla başlar (Inno 6 varsayılanı bu sayfayı gizler)
DisableWelcomePage=no
; Dil, Windows arayüz diline göre otomatik seçilir; eşleşme yoksa sorulur
ShowLanguageDialog=auto


[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
turkish.VtTitle=VirusTotal ile güvenlik taraması (isteğe bağlı)
english.VtTitle=Security scanning with VirusTotal (optional)
turkish.VtSubtitle=İndirilen dosyaları 70'ten fazla antivirüs motoruyla kontrol edin.
english.VtSubtitle=Check downloaded files with more than 70 antivirus engines.
turkish.VtDescription=Akıncı, bir indirme bittiğinde dosyanın özetini (SHA-256) VirusTotal'a sorar ve 70'ten fazla antivirüs motorunun sonucunu gösterir. Dosyanın kendisi gönderilmez. Bunun için ücretsiz bir VirusTotal API anahtarı gerekir: virustotal.com'da ücretsiz hesap açın, profil menüsünden API Key sayfasına gidin ve anahtarı kopyalayın.
english.VtDescription=When a download finishes, Akıncı looks up the file's SHA-256 hash on VirusTotal and shows the results of more than 70 antivirus engines. The file itself is not uploaded. This needs a free VirusTotal API key: create a free account on virustotal.com, open the API Key page from the profile menu and copy the key.
turkish.VtNoteNew=Anahtarınız yoksa boş bırakıp devam edin; daha sonra Akıncı → Ayarlar'dan ekleyebilirsiniz.
english.VtNoteNew=If you don't have a key, leave this empty and continue; you can add it later in Akıncı → Settings.
turkish.VtNoteUpgrade=Boş bırakırsanız mevcut anahtarınız korunur.
english.VtNoteUpgrade=If you leave this empty, your current key is kept.
turkish.VtKeyLabel=VirusTotal API anahtarı:
english.VtKeyLabel=VirusTotal API key:
turkish.VtLinkCaption=Ücretsiz VirusTotal hesabı aç
english.VtLinkCaption=Create a free VirusTotal account
turkish.VtKeyFormatWarning=Girilen değer bir VirusTotal API anahtarına benzemiyor (64 karakterlik onaltılık bir dize olmalı).%n%nYine de bu değerle devam edilsin mi?
english.VtKeyFormatWarning=The value you entered doesn't look like a VirusTotal API key (it should be a 64-character hexadecimal string).%n%nContinue with this value anyway?
turkish.AppRunning=Akıncı açık. Devam etmek için kapatılacak; devam eden indirmeler duraklatılıp kaydedilecek.
english.AppRunning=Akıncı is running. It will be closed to continue; active downloads will be paused and saved.
turkish.ForceClose=Akıncı kapanmadı. Zorla kapatılsın mı?%n%nSon birkaç saniyelik indirme ilerlemesi yeniden indirilebilir.
english.ForceClose=Akıncı did not close. Force it to close?%n%nThe last few seconds of download progress may be downloaded again.
turkish.AppStillRunning=Akıncı açık olduğu için kurulum devam edemiyor. Akıncı'yı sistem tepsisindeki simgeden "Çıkış" ile kapatıp kurulumu yeniden başlatın.
english.AppStillRunning=Setup can't continue while Akıncı is running. Close Akıncı with "Exit" from its system tray icon and run Setup again.
turkish.DeleteUserData=Ayarlarınız, indirme geçmişiniz ve VirusTotal anahtarınız da silinsin mi?%n%nİndirdiğiniz dosyalar silinmez.
english.DeleteUserData=Also delete your settings, download history and VirusTotal key?%n%nYour downloaded files are not deleted.
turkish.ExtensionRemovalHint=Tarayıcı eklentisini Chrome'un eklentiler sayfasından (chrome://extensions) kaldırabilirsiniz.
english.ExtensionRemovalHint=You can remove the browser extension from Chrome's extensions page (chrome://extensions).

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#SourceDir}\Akinci.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\Akinci-CLI.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\Akinci-BrowserBridge.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\extension\*"; DestDir: "{app}\extension"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Akıncı"; Filename: "{app}\Akinci.exe"
Name: "{autodesktop}\Akıncı"; Filename: "{app}\Akinci.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Akinci.exe"; Description: "{cm:LaunchProgram,Akıncı}"; Flags: nowait postinstall skipifsilent

[Code]
const
  MutexName = 'Local\Akinci.SingleInstance';
  UninstallKey = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{4C13BB6C-AE62-472E-B15A-DC329B2C10AF}_is1';
  VirusTotalJoinUrl = 'https://www.virustotal.com/gui/join-us';

var
  VtPage: TInputQueryWizardPage;

function AkinciDataDir: String;
begin
  Result := ExpandConstant('{%USERPROFILE}\.akinci');
end;

function IsHexKey(const Value: String): Boolean;
var
  I: Integer;
  C: Char;
begin
  Result := Length(Value) = 64;
  if not Result then
    exit;
  for I := 1 to Length(Value) do
  begin
    C := Value[I];
    if not (((C >= '0') and (C <= '9')) or ((C >= 'a') and (C <= 'f')) or ((C >= 'A') and (C <= 'F'))) then
    begin
      Result := False;
      exit;
    end;
  end;
end;

function WaitForAkinciExit(MaxWaitMs: Integer): Boolean;
var
  Waited: Integer;
begin
  Waited := 0;
  while CheckForMutexes(MutexName) and (Waited < MaxWaitMs) do
  begin
    Sleep(250);
    Waited := Waited + 250;
  end;
  Result := not CheckForMutexes(MutexName);
end;

{ Akıncı açıksa kullanıcıya sorar, --shutdown ile düzgünce kapatır; olmazsa zorla kapatmayı sorar.
  Akıncı kapalıysa veya kapatıldıysa True döner. }
function CloseRunningAkinci(const ExePath: String; Silent: Boolean): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  if not CheckForMutexes(MutexName) then
    exit;

  if not Silent then
    if MsgBox(CustomMessage('AppRunning'), mbConfirmation, MB_OKCANCEL) <> IDOK then
    begin
      Result := False;
      exit;
    end;

  if (ExePath <> '') and FileExists(ExePath) then
    Exec(ExePath, '--shutdown', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

  if WaitForAkinciExit(15000) then
    exit;

  if Silent then
  begin
    Result := False;
    exit;
  end;

  if MsgBox(CustomMessage('ForceClose'), mbConfirmation, MB_YESNO) = IDYES then
  begin
    Exec(ExpandConstant('{sys}\taskkill.exe'), '/IM Akinci.exe /F', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Result := WaitForAkinciExit(5000);
  end
  else
    Result := False;
end;

function InstalledAkinciExe: String;
var
  Location: String;
begin
  Result := '';
  if RegQueryStringValue(HKCU, UninstallKey, 'InstallLocation', Location) and (Location <> '') then
    Result := AddBackslash(Location) + 'Akinci.exe';
end;

procedure VtLinkClick(Sender: TObject);
var
  ErrorCode: Integer;
begin
  ShellExecAsOriginalUser('open', VirusTotalJoinUrl, '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
end;

procedure InitializeWizard;
var
  Note: String;
  Link: TNewStaticText;
begin
  if RegKeyExists(HKCU, UninstallKey) then
    Note := CustomMessage('VtNoteUpgrade')
  else
    Note := CustomMessage('VtNoteNew');

  VtPage := CreateInputQueryPage(wpSelectTasks,
    CustomMessage('VtTitle'), CustomMessage('VtSubtitle'),
    CustomMessage('VtDescription') + #13#10#13#10 + Note);
  VtPage.Add(CustomMessage('VtKeyLabel'), True);
  { Sessiz kurulum için anahtar AKINCI_VTKEY ortam değişkeninden alınabilir. Komut satırı parametresi
    kullanılmaz: Inno Setup komut satırını günlüğe yazar ve anahtar günlüğe düşerdi. }
  VtPage.Values[0] := GetEnv('AKINCI_VTKEY');

  Link := TNewStaticText.Create(VtPage);
  Link.Parent := VtPage.Surface;
  Link.Caption := CustomMessage('VtLinkCaption');
  Link.Cursor := crHand;
  Link.Font.Color := clHotLight;
  Link.Font.Style := [fsUnderline];
  Link.Top := VtPage.Edits[0].Top + VtPage.Edits[0].Height + ScaleY(12);
  Link.Left := VtPage.Edits[0].Left;
  Link.OnClick := @VtLinkClick;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  Key: String;
begin
  Result := True;
  if (VtPage <> nil) and (CurPageID = VtPage.ID) then
  begin
    Key := Trim(VtPage.Values[0]);
    if (Key <> '') and not IsHexKey(Key) then
      Result := MsgBox(CustomMessage('VtKeyFormatWarning'), mbConfirmation, MB_YESNO) = IDYES;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if not CloseRunningAkinci(InstalledAkinciExe, WizardSilent) then
    Result := CustomMessage('AppStillRunning');
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Key: String;
begin
  if CurStep = ssPostInstall then
  begin
    { Anahtar uygulamaya bekleyen dosya ile devredilir; asla Log()'a yazılmaz }
    Key := Trim(VtPage.Values[0]);
    if Key <> '' then
    begin
      ForceDirectories(AkinciDataDir);
      SaveStringToFile(AkinciDataDir + '\pending-virustotal-key', Key, False);
    end;
  end;
end;

function InitializeUninstall: Boolean;
begin
  Result := CloseRunningAkinci(ExpandConstant('{app}\Akinci.exe'), UninstallSilent);
  if not Result and not UninstallSilent then
    MsgBox(CustomMessage('AppStillRunning'), mbError, MB_OK);
end;

procedure DeleteUserDataFiles(const Dir: String);
begin
  DeleteFile(Dir + '\settings.json');
  DeleteFile(Dir + '\downloads.db');
  DeleteFile(Dir + '\downloads.db-wal');
  DeleteFile(Dir + '\downloads.db-shm');
  DeleteFile(Dir + '\ipc.token');
  DeleteFile(Dir + '\pending-virustotal-key');
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Dir: String;
  DeleteData: Boolean;
begin
  if CurUninstallStep <> usPostUninstall then
    exit;

  { Tarayıcı köprüsü kayıtları (uygulamanın "Eklentiyi kur" akışı yazar) }
  RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Google\Chrome\NativeMessagingHosts\com.akinci.downloader');
  RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Microsoft\Edge\NativeMessagingHosts\com.akinci.downloader');
  RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Mozilla\NativeMessagingHosts\com.akinci.downloader');

  { Uygulamanın oluşturduğu köprü ve eklenti kopyaları }
  Dir := AkinciDataDir;
  DelTree(Dir + '\host', True, True, True);
  DelTree(Dir + '\extension', True, True, True);

  if UninstallSilent then
    DeleteData := ExpandConstant('{param:DELETEUSERDATA|0}') = '1'
  else
    DeleteData := MsgBox(CustomMessage('DeleteUserData'), mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES;

  if DeleteData then
    DeleteUserDataFiles(Dir);

  { Yalnızca boşsa silinir; kullanıcının oraya koyduğu dosyalara dokunulmaz }
  RemoveDir(Dir);

  if not UninstallSilent then
    MsgBox(CustomMessage('ExtensionRemovalHint'), mbInformation, MB_OK);
end;
