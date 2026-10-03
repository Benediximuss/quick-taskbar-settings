@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Taskbar-Universal-v22.ps1" -Mode Window
exit /b %errorlevel%
