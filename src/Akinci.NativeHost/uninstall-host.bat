@echo off
setlocal enabledelayedexpansion
chcp 65001 > nul

echo =======================================================
echo   Akıncı Tarayıcı Entegrasyonu Kaldırılıyor
echo =======================================================
echo.

echo [1/3] Google Chrome kaydı kaldırılıyor...
reg delete "HKCU\Software\Google\Chrome\NativeMessagingHosts\com.akinci.downloader" /f > nul 2>&1

echo [2/3] Microsoft Edge kaydı kaldırılıyor...
reg delete "HKCU\Software\Microsoft\Edge\NativeMessagingHosts\com.akinci.downloader" /f > nul 2>&1

echo [3/3] Mozilla Firefox kaydı kaldırılıyor...
reg delete "HKCU\Software\Mozilla\NativeMessagingHosts\com.akinci.downloader" /f > nul 2>&1

echo.
echo =======================================================
echo   Kayıt defteri temizlendi. Entegrasyon kaldırıldı.
echo =======================================================
pause
