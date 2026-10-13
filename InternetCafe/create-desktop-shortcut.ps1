$WshShell = New-Object -ComObject WScript.Shell
$DesktopPath = [Environment]::GetFolderPath("Desktop")

# Create Shortcut on Desktop
$ShortcutPath = Join-Path $DesktopPath "InternetCafe Client.lnk"
$TargetExe = "$PSScriptRoot\dist\desktop-client\InternetCafe.Client.exe"
$WorkingDir = "$PSScriptRoot\dist\desktop-client"

$Shortcut = $WshShell.CreateShortcut($ShortcutPath)
$Shortcut.TargetPath = $TargetExe
$Shortcut.WorkingDirectory = $WorkingDir
$Shortcut.Description = "InternetCafe - Ứng dụng máy trạm Cybercafe"
$Shortcut.Save()

Write-Host "Da tao shortcut thanh cong tai: $ShortcutPath" -ForegroundColor Green

# Create Launcher .bat on Desktop that also auto-starts backend if not running
$BatPath = Join-Path $DesktopPath "Mo-InternetCafe.bat"
$BackendDir = "$PSScriptRoot\apps\backend\InternetCafe.Backend"

$BatContent = @"
@echo off
title InternetCafe Launcher
echo ===================================================
echo   KHOI DONG HE THONG CYBERCAFE INTERNETCAFE
echo ===================================================

:: Kiem tra Backend port 5055
netstat -ano | findstr 5055 >nul
if %errorlevel% neq 0 (
    echo [1/2] Dang khoi dong Backend Server...
    start /min "" dotnet run --project "$BackendDir\InternetCafe.Backend.csproj" --urls http://127.0.0.1:5055
    timeout /t 3 /nobreak >nul
) else (
    echo [1/2] Backend Server da san sang!
)

:: Khoi dong Desktop Client
echo [2/2] Dang mo App Desktop Client...
start "" "$TargetExe"

exit
"@

[System.IO.File]::WriteAllText($BatPath, $BatContent, [System.Text.Encoding]::UTF8)
Write-Host "Da tao file khoi dong thanh cong tai: $BatPath" -ForegroundColor Green
