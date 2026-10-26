@echo off
title Chạy Postman API Test Suite (Newman)
chcp 65001 >nul
cd /d "%~dp0"
echo ============================================================
echo   TỰ ĐỘNG HÓA KIỂM THỬ API BẰNG POSTMAN & NEWMAN
echo   Dự án: Hệ thống Quản trị & Vận hành Internet Cafe
echo   Collection: testing\postman\InternetCafe_API.postman_collection.json
echo   Environment: testing\postman\InternetCafe_Local.postman_environment.json
echo ============================================================
echo.
echo [1/2] Đang kiểm tra kết nối tới Backend API (http://127.0.0.1:5055/health)...
curl -s http://127.0.0.1:5055/health >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo [LƯU Ý] Backend API chưa được bật!
    echo Vui lòng chạy file 'chay-backend.bat' trước khi chạy kiểm thử API.
    echo.
    pause
    exit /b 1
)

echo [OK] Backend API đang hoạt động!
echo.
echo [2/2] Bắt đầu chạy toàn bộ test suite bằng Newman CLI...
echo.
npx --yes newman run testing\postman\InternetCafe_API.postman_collection.json -e testing\postman\InternetCafe_Local.postman_environment.json --reporters cli
echo.
echo ============================================================
echo   HOÀN THÀNH KIỂM THỬ API!
echo ============================================================
pause
