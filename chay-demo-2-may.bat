@echo off
chcp 65001 >nul
title InternetCafe - KHOI CHAY DEMO 2 MAY TRAM (SPLIT SCREEN)
cd /d "%~dp0"

echo ============================================================
echo   HE THONG QUAN LY PHONG MAY INTERNETCAFE [ENTERPRISE]
echo   KICH BAN DEMO TRINH DIEN 2 MAY TRAM SONG SONG TREN 1 LAPTOP
echo ============================================================
echo.

echo [0/5] Dang don dep tien trinh cu va giai phong cong 5055, 5173...
taskkill /F /IM InternetCafe.Client.exe >nul 2>&1
taskkill /F /IM InternetCafe.Backend.exe >nul 2>&1
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5173" ^| findstr "LISTENING"') do taskkill /F /PID %%a >nul 2>&1
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5055" ^| findstr "LISTENING"') do taskkill /F /PID %%a >nul 2>&1
ping 127.0.0.1 -n 2 >nul
echo       -^> Cac cong va tien trinh da san sang.
echo.

echo [1/5] Dang khoi dong SQL Server LocalDB [MSSQLLocalDB]...
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

echo [2/5] Dang khoi chay Backend API [Port 5055 - Ho tro LAN 0.0.0.0]...
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

echo [3/5] Dang khoi chay Web Admin Quan ly [Port 5173]...
start "InternetCafe - Web Admin (5173)" cmd /k ""%~dp0chay-web-admin.bat""
ping 127.0.0.1 -n 3 >nul
start http://127.0.0.1:5173
echo       -^> Web Admin da mo tai http://127.0.0.1:5173
echo.

echo [4/5] Dang khoi chay May tram 1: PC01 (Chế độ Cửa sổ Windowed)...
start "InternetCafe Client - PC01" cmd /k "cd /d ""%~dp0InternetCafe\apps\desktop-client"" && dotnet run --project InternetCafe.Client/InternetCafe.Client.csproj -- --machine PC01 --windowed"
echo       -^> Cua so PC01 da bat.
echo.

echo [5/5] Dang khoi chay May tram 2: PC02 (Chế độ Cửa sổ Windowed)...
ping 127.0.0.1 -n 3 >nul
start "InternetCafe Client - PC02" cmd /k "cd /d ""%~dp0InternetCafe\apps\desktop-client"" && dotnet run --project InternetCafe.Client/InternetCafe.Client.csproj -- --machine PC02 --windowed"
echo       -^> Cua so PC02 da bat.
echo.

echo ============================================================
echo   KHOI CHAY HOAN TAT MO HINH DEMO 2 MAY TRAM!
echo.
echo   * Web Admin:        Trinh duyet (http://127.0.0.1:5173)
echo   * May tram PC01:    Cua so Client 1 (Khu Tieu chuan - 8.000d/h)
echo   * May tram PC02:    Cua so Client 2 (Khu VIP - 15.000d/h)
echo.
echo   KICH BAN THUYET TRINH GOI Y [Mat khau chung: Demo@123]:
echo     1. Trinh duyet: Dang nhap 'admin' de mo phong may
echo     2. May PC01:   Dang nhap 'gamer'  (Nguyen Van An) -> PC01 sang xanh InUse
echo     3. May PC02:   Dang nhap 'user02' (Tran Thi Binh) -> PC02 sang xanh InUse
echo     4. Bam F2 hoac nut "Cau hinh" tren man hinh khoa de doi may nhanh!
echo ============================================================
echo.
echo Nhan phim bat ky de dong cua so thong bao nay (he thong van tiep tuc chay).
pause >nul
