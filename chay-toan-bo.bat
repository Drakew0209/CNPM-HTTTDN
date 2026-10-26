@echo off
chcp 65001 >nul
title InternetCafe - Khoi chay toan bo he thong
cd /d "%~dp0"

echo ============================================================
echo   HE THONG QUAN LY PHONG MAY INTERNETCAFE [ENTERPRISE]
echo   KHOI CHAY TOAN BO 1-CLICK: DATABASE + BACKEND + WEB + CLIENT
echo ============================================================
echo.

echo [0/4] Dang don dep tien trinh cu va giai phong cong 5055, 5173...
taskkill /F /IM InternetCafe.Client.exe >nul 2>&1
taskkill /F /IM InternetCafe.Backend.exe >nul 2>&1
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5173" ^| findstr "LISTENING"') do taskkill /F /PID %%a >nul 2>&1
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5055" ^| findstr "LISTENING"') do taskkill /F /PID %%a >nul 2>&1
ping 127.0.0.1 -n 2 >nul
echo       -^> Cac cong va tien trinh da san sang.
echo.

echo [1/4] Dang khoi dong SQL Server LocalDB [MSSQLLocalDB]...
where sqllocaldb >nul 2>&1
if %ERRORLEVEL% equ 0 (
    sqllocaldb start MSSQLLocalDB >nul 2>&1
) else if exist "C:\Program Files\Microsoft SQL Server\170\Tools\Binn\SqlLocalDB.exe" (
    "C:\Program Files\Microsoft SQL Server\170\Tools\Binn\SqlLocalDB.exe" start MSSQLLocalDB >nul 2>&1
) else if exist "C:\Program Files\Microsoft SQL Server\160\Tools\Binn\SqlLocalDB.exe" (
    "C:\Program Files\Microsoft SQL Server\160\Tools\Binn\SqlLocalDB.exe" start MSSQLLocalDB >nul 2>&1
) else if exist "C:\Program Files\Microsoft SQL Server\150\Tools\Binn\SqlLocalDB.exe" (
    "C:\Program Files\Microsoft SQL Server\150\Tools\Binn\SqlLocalDB.exe" start MSSQLLocalDB >nul 2>&1
)
echo       -^> SQL Server LocalDB da san sang.
echo.

echo [2/4] Dang khoi chay Backend API [Port 5055]...
start "InternetCafe - Backend API (5055)" cmd /k ""%~dp0chay-backend.bat""
echo       -^> Dang cho Backend API khoi dong (http://127.0.0.1:5055/health)...
set /a attempts=0
:WAIT_BACKEND
curl -s -f http://127.0.0.1:5055/health >nul 2>&1
if %ERRORLEVEL% equ 0 goto BACKEND_READY
set /a attempts+=1
if %attempts% geq 30 (
    echo       [CANH BAO] Backend mat nhieu thoi gian hon du kien. Tiep tuc cac buoc tiep theo...
    goto BACKEND_READY
)
ping 127.0.0.1 -n 2 >nul
goto WAIT_BACKEND

:BACKEND_READY
echo       -^> Backend API da san sang tai http://127.0.0.1:5055
echo.

echo [3/4] Dang khoi chay Web Admin Quan ly [Port 5173]...
start "InternetCafe - Web Admin (5173)" cmd /k ""%~dp0chay-web-admin.bat""
ping 127.0.0.1 -n 3 >nul
start http://127.0.0.1:5173
echo       -^> Web Admin da mo tai http://127.0.0.1:5173
echo.

echo [4/4] Dang khoi chay Desktop Client [May tram phong net]...
start "InternetCafe - Desktop Client" cmd /k ""%~dp0chay-desktop-client.bat""
echo       -^> Cua so Desktop Client da khoi dong.
echo.

echo ============================================================
echo   KHOI CHAY THANH CONG TOAN BO HE THONG!
echo.
echo   * Backend API:    http://127.0.0.1:5055
echo   * Web Admin:      http://127.0.0.1:5173
echo   * Desktop Client: Cua so may tram Kiosk Client
echo.
echo   THONG TIN TAI KHOAN TRAI NGHIEM [Mat khau chung: Demo@123]:
echo     1. Quan tri he thong:     admin
echo     2. Quan ly co so:         manager
echo     3. Thu ngan tai quay:     cashier
echo     4. Nhan vien truc ca:     staff
echo     5. Khach hang choi may:   gamer (Nguyen Van An)
echo ============================================================
echo.
echo Nhan phim bat ky de dong cua so thong bao nay (cac dich vu van tiep tuc chay).
pause >nul