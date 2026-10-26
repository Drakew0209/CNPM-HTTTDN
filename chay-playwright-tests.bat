@echo off
title Chạy Playwright E2E Tests (App + Web)
chcp 65001 >nul
cd /d "%~dp0"

echo ========================================================
echo   CHẠY KIỂM THỬ TỰ ĐỘNG GIAO DIỆN PLAYWRIGHT E2E
echo   Hệ Thống InternetCafe: Web Admin + Tích Hợp Client App
echo   Tổng cộng: 34 Test Cases E2E toàn diện
echo ========================================================
echo.

echo [1/3] Đang kiểm tra kết nối Backend API (http://127.0.0.1:5055/health)...
curl.exe -s http://127.0.0.1:5055/health >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo [LƯU Ý] Backend API chưa được bật!
    echo Vui lòng chạy file 'chay-backend.bat' hoặc 'chay-toan-bo.bat' trước.
    echo.
    pause
    exit /b 1
)
echo       -^> Backend API đang hoạt động!
echo.

echo [2/3] Đang thực thi toàn bộ 34 Playwright E2E Tests...
cd InternetCafe\apps\admin-web
call npx playwright test

echo.
echo [3/3] Hoàn tất kiểm thử Playwright E2E!
echo Báo cáo HTML trực quan đã được tạo tại:
echo   -^> InternetCafe\apps\admin-web\playwright-report\index.html
echo.
echo ========================================================
echo Để mở báo cáo HTML trực quan trên trình duyệt, chạy:
echo   cd InternetCafe\apps\admin-web ^&^& npx playwright show-report
echo ========================================================
pause

