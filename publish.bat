@echo off
setlocal
echo ========================================================
echo   AKINCI - Single-File Publish Script (win-x64)
echo ========================================================

set "ROOT=%~dp0"
set "OUTPUT_DIR=%ROOT%dist"

if exist "%OUTPUT_DIR%" rmdir /s /q "%OUTPUT_DIR%"
mkdir "%OUTPUT_DIR%"

echo [1/3] Publishing Akinci.UI (win-x64)...
dotnet publish "%ROOT%src\Akinci.UI\Akinci.UI.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "%OUTPUT_DIR%\Akinci.UI"
if errorlevel 1 goto :fail

echo [2/3] Publishing Akinci.Cli (win-x64)...
dotnet publish "%ROOT%src\Akinci.Cli\Akinci.Cli.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "%OUTPUT_DIR%\Akinci.Cli"
if errorlevel 1 goto :fail

echo [3/3] Publishing Akinci.NativeHost (win-x64)...
dotnet publish "%ROOT%src\Akinci.NativeHost\Akinci.NativeHost.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "%OUTPUT_DIR%\Akinci.NativeHost"
if errorlevel 1 goto :fail

echo Copying extension manifests and scripts...
xcopy /E /I /Y "%ROOT%src\Akinci.NativeHost\manifests" "%OUTPUT_DIR%\Akinci.NativeHost\manifests" > nul
xcopy /E /I /Y "%ROOT%src\Akinci.NativeHost\extension" "%OUTPUT_DIR%\Akinci.NativeHost\extension" > nul
copy /Y "%ROOT%src\Akinci.NativeHost\install-host.bat" "%OUTPUT_DIR%\Akinci.NativeHost\" > nul
copy /Y "%ROOT%src\Akinci.NativeHost\uninstall-host.bat" "%OUTPUT_DIR%\Akinci.NativeHost\" > nul

echo ========================================================
echo   SUCCESS! Single-file packages generated in dist/
echo     - dist\Akinci.UI\Akinci.UI.exe
echo     - dist\Akinci.Cli\Akinci.Cli.exe
echo     - dist\Akinci.NativeHost\Akinci.NativeHost.exe
echo ========================================================
exit /b 0

:fail
echo [ERROR] Publish failed!
exit /b 1
