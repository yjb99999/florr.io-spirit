@echo off
chcp 65001 >nul
title Uninstall florr_teach

:: Check admin rights
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo Requesting admin rights...
    powershell "Start-Process cmd -ArgumentList '/c \"%~f0\"' -Verb RunAs"
    exit /b
)

echo ========================================
echo   Uninstalling florr_teach...
echo ========================================
echo.

:: 1. Kill process
echo [1/3] Killing florr_teach.exe...
taskkill /F /IM florr_teach.exe >nul 2>&1
if %errorLevel% equ 0 (
    echo     OK - process killed
) else (
    echo     -- process not running
)
echo.

:: 2. Delete folder
echo [2/3] Deleting install folder...
if exist "C:\Program Files\florr_teach" (
    rd /s /q "C:\Program Files\florr_teach"
    if exist "C:\Program Files\florr_teach" (
        echo     FAIL - please manually delete C:\Program Files\florr_teach
    ) else (
        echo     OK - folder deleted
    )
) else (
    echo     -- folder not found
)
echo.

:: 3. Delete autostart registry
echo [3/3] Removing autostart...
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "florr_teach" /f >nul 2>&1
if %errorLevel% equ 0 (
    echo     OK - autostart removed
) else (
    echo     -- autostart not found
)
echo.

echo ========================================
echo   Done!
echo ========================================
pause
