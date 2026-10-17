@echo off
setlocal enabledelayedexpansion
chcp 65001 > nul

echo =======================================================
echo   Akıncı Tarayıcı Entegrasyonu (Native Messaging Host)
echo =======================================================
echo.

set "SCRIPT_DIR=%~dp0"
set "CHROME_MANIFEST=%SCRIPT_DIR%manifests\com.akinci.downloader.json"
set "FIREFOX_MANIFEST=%SCRIPT_DIR%manifests\com.akinci.downloader.firefox.json"

echo [1/3] Google Chrome kaydı yapılıyor...
reg add "HKCU\Software\Google\Chrome\NativeMessagingHosts\com.akinci.downloader" /ve /t REG_SZ /d "%CHROME_MANIFEST%" /f > nul
if %errorlevel% equ 0 (
    echo       Chrome başarıyla kaydedildi.
) else (
    echo       Chrome kaydı başarısız oldu.
)

echo [2/3] Microsoft Edge kaydı yapılıyor...
reg add "HKCU\Software\Microsoft\Edge\NativeMessagingHosts\com.akinci.downloader" /ve /t REG_SZ /d "%CHROME_MANIFEST%" /f > nul
if %errorlevel% equ 0 (
    echo       Microsoft Edge başarıyla kaydedildi.
) else (
    echo       Microsoft Edge kaydı başarısız oldu.
)

echo [3/3] Mozilla Firefox kaydı yapılıyor...
reg add "HKCU\Software\Mozilla\NativeMessagingHosts\com.akinci.downloader" /ve /t REG_SZ /d "%FIREFOX_MANIFEST%" /f > nul
if %errorlevel% equ 0 (
    echo       Mozilla Firefox başarıyla kaydedildi.
) else (
    echo       Mozilla Firefox kaydı başarısız oldu.
)

echo.
echo =======================================================
echo   Kayıt işlemi tamamlandı!
echo   Tarayıcınızı yeniden başlatarak Akıncı eklentisini
echo   kullanmaya başlayabilirsiniz.
echo =======================================================
pause
