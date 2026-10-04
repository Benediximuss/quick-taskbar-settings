@echo off
setlocal
set "TASKBAR_TOGGLE_LAUNCHED_FROM_CMD=1"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Taskbar-Toggle.ps1"
set "TASKBAR_TOGGLE_EXIT=%errorlevel%"
if not "%TASKBAR_TOGGLE_EXIT%"=="0" (
    echo.
    pause >nul
)
exit /b %TASKBAR_TOGGLE_EXIT%
