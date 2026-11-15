# Kurulum sihirbazı görsellerini Akıncı banner'ından (src\Akinci.UI\Assets\akinci-banner.jpg) üretir.
#
# Kullanım: powershell -NoProfile -ExecutionPolicy Bypass -File installer\make-wizard-images.ps1
# Çıktı:    installer\assets\wizard-large-*.png (Hoş geldiniz / Bitiş sayfası, 164:314 oran)
#           installer\assets\wizard-small.png   (iç sayfaların sağ üst köşesi, kare)
# Banner değişmedikçe yeniden çalıştırmaya gerek yoktur; üretilen PNG'ler repoda tutulur.

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$bannerPath = Join-Path $root 'src\Akinci.UI\Assets\akinci-banner.jpg'
$outDir = Join-Path $PSScriptRoot 'assets'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$banner = [System.Drawing.Image]::FromFile($bannerPath)

function New-HQGraphics([System.Drawing.Bitmap]$bmp) {
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    return $g
}

# Büyük görsel: beyaz zemin, logonun tamamı (yazı dahil) dikeyde ortada, kenarlarda boşluk
# Boyutlar Inno Setup 6.6+ varsayılan alanlarına göre: %100, %150, %200 DPI
foreach ($size in @(@(202, 386), @(336, 643), @(430, 824))) {
    $w = $size[0]; $h = $size[1]
    $bmp = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $g = New-HQGraphics $bmp
    $g.Clear([System.Drawing.Color]::White)
    $logo = [int]($w * 0.88)
    $x = [int](($w - $logo) / 2)
    $y = [int](($h - $logo) / 2 - $h * 0.04)
    $g.DrawImage($banner, (New-Object System.Drawing.Rectangle $x, $y, $logo, $logo))
    $g.Dispose()
    $bmp.Save((Join-Path $outDir "wizard-large-$w`x$h.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

# Küçük görsel: yalnızca kuşlar (yazı bu boyutta okunmaz), beyaz zemin, kare
# Kaynak kırpma alanı: banner üzerinde yalnızca kuşlar (2048x2048 piksel koordinatları; yazı y≈1750'de başlar)
$crop = New-Object System.Drawing.Rectangle 170, 110, 1710, 1610
$size = 160
$bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$g = New-HQGraphics $bmp
$g.Clear([System.Drawing.Color]::White)
$pad = 6
$destW = $size - 2 * $pad
$destH = [int]($destW * $crop.Height / $crop.Width)
$g.DrawImage($banner, (New-Object System.Drawing.Rectangle $pad, ([int](($size - $destH) / 2)), $destW, $destH), $crop, [System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose()
$bmp.Save((Join-Path $outDir 'wizard-small.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

$banner.Dispose()
Get-ChildItem $outDir -Filter 'wizard-*.png' | ForEach-Object { "$($_.Name)  $([math]::Round($_.Length / 1KB)) KB" }
