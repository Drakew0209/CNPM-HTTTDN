@echo off
title Chạy Desktop Client (WPF)
chcp 65001 >nul
cd /d "%~dp0InternetCafe\apps\desktop-client"
echo ============================================================
echo   KHỞI CHẠY CLIENT DESKTOP (WPF .NET 9.0)
echo   * Tự động kết nối Backend API nếu đang mở
echo   * Tự động chuyển sang Standalone Offline Demo nếu chưa mở API
echo ============================================================
echo.
dotnet run --project InternetCafe.Client/InternetCafe.Client.csproj
pause
