@echo off
title InternetCafe - Dừng toàn bộ hệ thống
chcp 65001 >nul
cd /d "%~dp0"

echo ============================================================
echo   ĐANG DỪNG CÁC TIẾN TRÌNH HỆ THỐNG INTERNETCAFE...
echo ============================================================
echo.

echo [1/3] Đang tắt Desktop Client...
taskkill /F /IM InternetCafe.Client.exe >nul 2>&1

echo [2/3] Đang tắt Backend API (Port 5055)...
taskkill /F /IM InternetCafe.Backend.exe >nul 2>&1

echo [3/3] Đang giải phóng cổng Web Admin (Port 5173)...
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5173" ^| findstr "LISTENING"') do (
    taskkill /F /PID %%a >nul 2>&1
)

for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5055" ^| findstr "LISTENING"') do (
    taskkill /F /PID %%a >nul 2>&1
)

echo.
echo ============================================================
echo   ĐÃ DỪNG TOÀN BỘ CÁC DỊCH VỤ THÀNH CÔNG!
echo ============================================================
timeout /t 3 >nul
