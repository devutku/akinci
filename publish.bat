@echo off
setlocal enabledelayedexpansion
chcp 65001 > nul

echo ========================================================
echo   AKINCI - Tek Dosya (Single-File) Dağıtım Derleyicisi
echo ========================================================
echo.

set "OUTPUT_DIR=%~dp0dist"
if exist "%OUTPUT_DIR%" rd /s /q "%OUTPUT_DIR%"
mkdir "%OUTPUT_DIR%"

echo [1/3] Akıncı Masaüstü UI (Akinci.UI) Derleniyor (win-x64)...
dotnet publish src\Akinci.UI\Akinci.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "%OUTPUT_DIR%\Akinci.UI"
if %errorlevel% neq 0 goto :error

echo [2/3] Akıncı CLI (Akinci.Cli) Derleniyor (win-x64)...
dotnet publish src\Akinci.Cli\Akinci.Cli.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "%OUTPUT_DIR%\Akinci.Cli"
if %errorlevel% neq 0 goto :error

echo [3/3] Akıncı Native Messaging Host (Akinci.NativeHost) Derleniyor (win-x64)...
dotnet publish src\Akinci.NativeHost\Akinci.NativeHost.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "%OUTPUT_DIR%\Akinci.NativeHost"
if %errorlevel% neq 0 goto :error

:: Manifest ve kurulum komut dosyalarını kopyala
xcopy /E /I /Y "src\Akinci.NativeHost\manifests" "%OUTPUT_DIR%\Akinci.NativeHost\manifests" > nul
xcopy /E /I /Y "src\Akinci.NativeHost\extension" "%OUTPUT_DIR%\Akinci.NativeHost\extension" > nul
copy /Y "src\Akinci.NativeHost\install-host.bat" "%OUTPUT_DIR%\Akinci.NativeHost\" > nul
copy /Y "src\Akinci.NativeHost\uninstall-host.bat" "%OUTPUT_DIR%\Akinci.NativeHost\" > nul

echo.
echo ========================================================
echo   BAŞARILI! Dağıtım paketleri 'dist/' klasörüne üretildi:
echo     - dist\Akinci.UI\Akinci.UI.exe
echo     - dist\Akinci.Cli\Akinci.Cli.exe
echo     - dist\Akinci.NativeHost\Akinci.NativeHost.exe
echo ========================================================
exit /b 0

:error
echo.
echo [HATA] Derleme sırasında hata meydana geldi!
exit /b 1
