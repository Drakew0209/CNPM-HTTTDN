@echo off
chcp 65001 >nul
title InternetCafe - Backend API (5055)
cd /d "%~dp0InternetCafe\apps\backend\InternetCafe.Backend"
echo ============================================================
echo   KHOI CHAY BACKEND API (ASP.NET CORE 10.0)
echo   Dia chi API Cuc bo:  http://127.0.0.1:5055/api/v1
echo   Dia chi API Mang LAN: http://0.0.0.0:5055/api/v1
echo   Health Check:        http://127.0.0.1:5055/health
echo ============================================================
echo.
dotnet run --urls "http://0.0.0.0:5055"
pause