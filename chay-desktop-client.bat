@echo off
chcp 65001 >nul
title InternetCafe - Desktop Client
cd /d "%~dp0InternetCafe\apps\desktop-client"
echo ============================================================
echo   KHOI CHAY CLIENT DESKTOP (WPF .NET 10.0)
echo   * Tu dong ket noi Backend API neu dang mo
echo   * Tu dong chuyen sang Standalone Offline Demo neu chua mo API
echo ============================================================
echo.
echo Dang khoi dong giao dien may tram, vui long doi giay lat...
echo.
dotnet run --project InternetCafe.Client/InternetCafe.Client.csproj -- %*
pause