# Akıncı tarayıcı eklentisini Chrome Web Store'a yüklenecek zip paketine dönüştürür.
#
# Kullanım:  powershell -ExecutionPolicy Bypass -File pack-extension.ps1
# Çıktı:     dist\akinci-extension-<sürüm>.zip
#
# Not: extension\manifest.json içindeki "key" alanı yalnızca paketlenmemiş (unpacked) yüklemede
# sabit bir eklenti kimliği sağlamak içindir. Chrome Web Store kendi kimliğini atar, bu yüzden
# alan pakete konmaz.

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$root = $PSScriptRoot
$extensionDir = Join-Path $root 'src\Akinci.NativeHost\extension'
$outDir = Join-Path $root 'dist'

$manifestPath = Join-Path $extensionDir 'manifest.json'
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
$manifestText = [System.IO.File]::ReadAllText($manifestPath, $utf8NoBom)

# "key" satırını çıkar (manifest'in geri kalanı birebir korunur)
$storeManifest = [regex]::Replace($manifestText, '(?m)^\s*"key"\s*:\s*"[^"]*",\r?\n', '')
if ($storeManifest -match '"key"\s*:') {
    throw 'manifest.json içindeki "key" alanı çıkarılamadı.'
}

# Geçerli JSON olduğunu doğrula ve sürümü oku
$version = ($storeManifest | ConvertFrom-Json).version

New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$zipPath = Join-Path $outDir "akinci-extension-$version.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

$zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    Get-ChildItem -Path $extensionDir -Recurse -File | ForEach-Object {
        # Zip içindeki yollar ileri eğik çizgi kullanmalı (Chrome Web Store gereksinimi)
        $entryName = $_.FullName.Substring($extensionDir.Length + 1).Replace('\', '/')

        if ($entryName -eq 'manifest.json') {
            $entry = $zip.CreateEntry($entryName, [System.IO.Compression.CompressionLevel]::Optimal)
            $writer = New-Object System.IO.StreamWriter($entry.Open(), $utf8NoBom)
            try { $writer.Write($storeManifest) } finally { $writer.Dispose() }
        }
        else {
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $zip, $_.FullName, $entryName, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
}
finally {
    $zip.Dispose()
}

Write-Host "Chrome Web Store paketi hazır: $zipPath"
