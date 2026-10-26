@echo off
title InternetCafe - Dung toan bo he thong
chcp 65001 >nul
cd /d "%~dp0"

echo ============================================================
echo   DANG DUNG CAC TIEN TRINH HE THONG INTERNETCAFE...
echo ============================================================
echo.

echo [1/3] Dang tat Desktop Client...
taskkill /F /IM InternetCafe.Client.exe >nul 2>&1

echo [2/3] Dang tat Backend API (Port 5055)...
taskkill /F /IM InternetCafe.Backend.exe >nul 2>&1

echo [3/3] Dang giai phong cong Web Admin (Port 5173)...
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5173" ^| findstr "LISTENING"') do (
    taskkill /F /PID %%a >nul 2>&1
)

for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5055" ^| findstr "LISTENING"') do (
    taskkill /F /PID %%a >nul 2>&1
)

echo.
echo ============================================================
echo   DA DUNG TOAN BO CAC DICH VU THANH CONG!
echo ============================================================
ping 127.0.0.1 -n 3 >nul