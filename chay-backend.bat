@echo off
title Chạy Backend API (ASP.NET Core 9.0)
chcp 65001 >nul
cd /d "%~dp0InternetCafe\apps\backend\InternetCafe.Backend"
echo ============================================================
echo   KHỞI CHẠY BACKEND API (ASP.NET CORE 9.0)
echo   Địa chỉ API: http://127.0.0.1:5055/api/v1
echo   Swagger / Health: http://127.0.0.1:5055/health
echo ============================================================
echo.
dotnet run --urls "http://127.0.0.1:5055"
pause
