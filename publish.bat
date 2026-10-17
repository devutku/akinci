@echo off
setlocal
echo ========================================================
echo   AKINCI - Single-File Publish Script (win-x64)
echo ========================================================

set "ROOT=%~dp0"
set "OUTPUT_DIR=%ROOT%dist"

set "TARGET_DIR=%OUTPUT_DIR%\Akinci"
if exist "%TARGET_DIR%" rmdir /s /q "%TARGET_DIR%"
mkdir "%TARGET_DIR%"

echo [1/3] Publishing Akinci Desktop (Akinci.exe)...
dotnet publish "%ROOT%src\Akinci.UI\Akinci.UI.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "%TARGET_DIR%"
if errorlevel 1 goto :fail

echo [2/3] Publishing Akinci CLI (Akinci-CLI.exe)...
dotnet publish "%ROOT%src\Akinci.Cli\Akinci.Cli.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "%TARGET_DIR%"
if errorlevel 1 goto :fail

echo [3/3] Publishing Akinci Browser Bridge (Akinci-BrowserBridge.exe)...
dotnet publish "%ROOT%src\Akinci.NativeHost\Akinci.NativeHost.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "%TARGET_DIR%"
if errorlevel 1 goto :fail

echo Copying extension manifests and scripts...
xcopy /E /I /Y "%ROOT%src\Akinci.NativeHost\manifests" "%TARGET_DIR%\manifests" > nul
xcopy /E /I /Y "%ROOT%src\Akinci.NativeHost\extension" "%TARGET_DIR%\extension" > nul
copy /Y "%ROOT%src\Akinci.NativeHost\install-host.bat" "%TARGET_DIR%\" > nul
copy /Y "%ROOT%src\Akinci.NativeHost\uninstall-host.bat" "%TARGET_DIR%\" > nul

echo ========================================================
echo   SUCCESS! User-friendly package generated in dist\Akinci
echo     - dist\Akinci\Akinci.exe (Masaüstü Uygulaması)
echo     - dist\Akinci\Akinci-CLI.exe (Terminal Sürümü)
echo     - dist\Akinci\Akinci-BrowserBridge.exe (Tarayıcı Köprüsü)
echo ========================================================
exit /b 0

:fail
echo [ERROR] Publish failed!
exit /b 1
