@echo off
title Đóng gói InternetCafe Portable
chcp 65001 >nul
echo ============================================================
echo   ĐANG ĐÓNG GÓI BẢN PORTABLE INTERNETCAFE DESKTOP CLIENT
echo ============================================================
echo.
echo [1/3] Đang biên dịch và xuất bản tự trị (Self-Contained win-x64)...
dotnet publish "%~dp0apps\desktop-client\InternetCafe.Client\InternetCafe.Client.csproj" -c Release -r win-x64 --self-contained true -o "%~dp0dist\InternetCafe-Client-Portable"

if %errorlevel% neq 0 (
    echo [LỖI] Quá trình publish thất bại!
    pause
    exit /b %errorlevel%
)

echo.
echo [2/3] Đang nén toàn bộ thành file ZIP di động...
powershell -NoProfile -Command "Compress-Archive -Path '%~dp0dist\InternetCafe-Client-Portable\*' -DestinationPath '%~dp0dist\InternetCafe-Client-Portable.zip' -Force"

echo.
echo [3/3] HOÀN TẤT THÀNH CÔNG!
echo File ZIP mới nhất đã được cập nhật tại:
echo %~dp0dist\InternetCafe-Client-Portable.zip
echo.
pause
