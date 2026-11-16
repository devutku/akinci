# Akıncı Windows kurulum programını (Inno Setup) derler.
#
# Kullanım:
#   powershell -NoProfile -ExecutionPolicy Bypass -File installer\build-installer.ps1 -SourceDir dist\Akinci -OutputDir dist
#
# -SourceDir: publish.bat çıktısı (Akinci.exe, Akinci-CLI.exe, Akinci-BrowserBridge.exe, extension\)
# -OutputDir: Akinci-Setup-<sürüm>.exe dosyasının yazılacağı klasör
#
# Inno Setup derleyicisi (ISCC.exe) bulunamazsa nasıl kurulacağını yazar ve 0 koduyla çıkar,
# böylece publish.bat zip/klasör çıktısıyla tamamlanabilir.

param(
    [Parameter(Mandatory = $true)][string]$SourceDir,
    [string]$OutputDir = (Join-Path $PSScriptRoot '..\dist'),
    # Boş bırakılırsa Akinci.UI.csproj içindeki <Version> kullanılır
    [string]$Version = ''
)

$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')

function Find-Iscc {
    $cmd = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    $candidates = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
    )
    foreach ($c in $candidates) {
        if ($c -and (Test-Path $c)) { return $c }
    }
    return $null
}

$iscc = Find-Iscc
if (-not $iscc) {
    Write-Host 'Inno Setup derleyicisi (ISCC.exe) bulunamadı; kurulum programı atlandı.'
    Write-Host 'Kurmak için:  winget install --id JRSoftware.InnoSetup -e'
    exit 0
}

# Sürüm: tek kaynak Akinci.UI.csproj
$version = $Version
if (-not $version) {
    [xml]$csproj = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'src\Akinci.UI\Akinci.UI.csproj')
    $version = ($csproj.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { $_ } | Select-Object -First 1)
}
if (-not $version) { throw 'Akinci.UI.csproj içinde <Version> bulunamadı.' }

# Windows dosya sürümü yalnızca sayı kabul eder: "1.1.0-beta.1" / "1.1.0+abc" → "1.1.0"
$numericVersion = ($version -split '[-+]')[0]
if ($numericVersion -notmatch '^\d+(\.\d+){0,3}$') { throw "Sürüm sayısal kısma ayrılamadı: $version" }

$sourceFull = (Resolve-Path $SourceDir).Path
foreach ($required in @('Akinci.exe', 'Akinci-CLI.exe', 'Akinci-BrowserBridge.exe', 'extension\manifest.json')) {
    if (-not (Test-Path (Join-Path $sourceFull $required))) {
        throw "Kaynak klasörde eksik dosya: $required ($sourceFull)"
    }
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
$outputFull = (Resolve-Path $OutputDir).Path

# Lisans sayfası: çekirdek (MIT) + arayüz (GPLv3) birlikte; kurulum iki dilde açıldığı için başlıklar iki dilli
$objDir = Join-Path $PSScriptRoot 'obj'
New-Item -ItemType Directory -Force -Path $objDir | Out-Null
$licensePath = Join-Path $objDir 'license.txt'
$utf8Bom = New-Object System.Text.UTF8Encoding $true
$separator = "`r`n`r`n" + ('=' * 72) + "`r`n`r`n"
$licenseText =
    "Akıncı core libraries (Domain, Application, Infrastructure): MIT License`r`n" +
    "Akıncı çekirdek kütüphaneleri (Domain, Application, Infrastructure): MIT Lisansı`r`n`r`n" +
    (Get-Content -Raw -Encoding UTF8 (Join-Path $root 'LICENSE-MIT')) +
    $separator +
    "Akıncı desktop app: GNU GPL v3`r`n" +
    "Akıncı masaüstü arayüzü: GNU GPL v3`r`n`r`n" +
    (Get-Content -Raw -Encoding UTF8 (Join-Path $root 'LICENSE'))
[System.IO.File]::WriteAllText($licensePath, $licenseText, $utf8Bom)

& $iscc "/DAppVersion=$version" "/DNumericVersion=$numericVersion" "/DSourceDir=$sourceFull" "/DLicenseFile=$licensePath" "/O$outputFull" (Join-Path $PSScriptRoot 'akinci.iss')
if ($LASTEXITCODE -ne 0) { throw "ISCC başarısız oldu (çıkış kodu $LASTEXITCODE)." }

Write-Host "Kurulum programı hazır: $(Join-Path $outputFull "Akinci-Setup-$version.exe")"
