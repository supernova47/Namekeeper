@echo off
setlocal EnableExtensions
chcp 65001 >nul
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildMod.ps1" %*
set "BUILD_RESULT=%ERRORLEVEL%"
echo.
if "%BUILD_RESULT%"=="0" (echo Build complete.) else (echo Build failed. Read BuildLog.txt.)
pause
exit /b %BUILD_RESULT%
