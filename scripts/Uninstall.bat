@echo off
setlocal EnableDelayedExpansion

echo ============================================================
echo  CursorCompanion - Uninstall / Cleanup
echo ============================================================
echo.
echo This script will remove:
echo   1. Desktop shortcut
echo   2. Autostart registry entry
echo   3. Windows Firewall rule for UDP 7777
echo.

:: ---- 1. Desktop Shortcut ----
echo [1/3] Removing Desktop shortcut...
set "SHORTCUT=%USERPROFILE%\Desktop\CursorCompanion.lnk"
if exist "%SHORTCUT%" (
    del /f /q "%SHORTCUT%"
    echo   Removed: %SHORTCUT%
) else (
    echo   Not found (already removed or never created).
)
echo.

:: ---- 2. Autostart ----
echo [2/3] Removing autostart registry entry...
reg query "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "CursorCompanion" >nul 2>&1
if errorlevel 1 (
    echo   Not found (already removed or never created).
) else (
    reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "CursorCompanion" /f >nul 2>&1
    if errorlevel 1 (
        echo   WARNING: Failed to remove registry entry.
    ) else (
        echo   Removed from autostart.
    )
)
echo.

:: ---- 3. Firewall Rule ----
echo [3/3] Removing Windows Firewall rule...
set /p REMOVE_FW="Remove firewall rule (will request elevation)? (Y/N): "
if /i "%REMOVE_FW%"=="Y" (
    powershell -NoProfile -Command ^
        "Start-Process -FilePath 'netsh' -ArgumentList 'advfirewall firewall delete rule name=\"CursorCompanion UDP 7777\"' -Verb RunAs -Wait" ^
        >nul 2>&1
    if errorlevel 1 (
        echo   WARNING: Firewall rule removal may have failed or was cancelled.
    ) else (
        echo   Firewall rule removed.
    )
) else (
    echo   Skipped.
)
echo.

echo ============================================================
echo  Cleanup complete.
echo  You may now delete the CursorCompanion folder manually.
echo ============================================================
echo.
pause
endlocal
