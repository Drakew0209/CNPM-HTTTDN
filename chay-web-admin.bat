@echo off
chcp 65001 >nul
title InternetCafe - Web Admin (5173)
cd /d "%~dp0InternetCafe\apps\admin-web"
echo ============================================================
echo   KHOI CHAY GIAO DIEN QUAN TRI PHONG NET (WEB ADMIN)
echo   Dia chi: http://127.0.0.1:5173
echo ============================================================
echo.
npm run dev:shared
pause