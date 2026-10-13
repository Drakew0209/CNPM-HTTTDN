@echo off
title Đóng gói ClickOnce Auto-Update
chcp 65001 >nul
echo ============================================================
echo   ĐANG XUẤT BẢN BẢN CẬP NHẬT CLICKONCE AUTO-UPDATE
echo ============================================================
echo.
echo [1/2] Đang biên dịch ClickOnce Manifest và Application Files...

set MSBUILD="D:\VSCode\MSBuild\Current\Bin\MSBuild.exe"
if not exist %MSBUILD% (
    for /f "tokens=*" %%i in ('"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe') do set MSBUILD="%%i"
)

%MSBUILD% "%~dp0apps\desktop-client\InternetCafe.Client\InternetCafe.Client.csproj" /target:publish /p:PublishProfile=ClickOnceProfile /p:Configuration=Release

if %errorlevel% neq 0 (
    echo [LỖI] Xuất bản ClickOnce thất bại!
    pause
    exit /b %errorlevel%
)

copy /y "%~dp0dist\clickonce\index.html" "%~dp0dist\clickonce\Publish.html" >nul
powershell -NoProfile -Command "New-Item -ItemType Directory -Force '%~dp0..\docs\client' >nul; Copy-Item -Recurse -Force '%~dp0dist\clickonce\*' '%~dp0..\docs\client\'"

echo.
echo [2/2] XUẤT BẢN THÀNH CÔNG!
echo Bản cài đặt và cập nhật đã sẵn sàng tại:
echo %~dp0dist\clickonce\
echo.
echo ============================================================
echo ĐỂ KÍCH HOẠT AUTO-UPDATE LÊN GITHUB:
echo Bạn chỉ cần commit thư mục dist\clickonce và đẩy lên nhánh 'main'
echo (hoặc nhánh gh-pages) trên GitHub:
echo.
echo   git add InternetCafe/dist/clickonce
echo   git commit -m "feat: release new ClickOnce version"
echo   git push origin main
echo ============================================================
echo.
pause
