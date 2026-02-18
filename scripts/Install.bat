@echo off
setlocal EnableDelayedExpansion

echo ============================================================
echo  CursorCompanion - Setup
echo ============================================================
echo.
echo This script will optionally:
echo   1. Create a Desktop shortcut
echo   2. Add CursorCompanion to Windows autostart
echo   3. Add a Windows Firewall rule for UDP port 7777
echo.

set "APP_DIR=%~dp0"
if "%APP_DIR:~-1%"=="\" set "APP_DIR=%APP_DIR:~0,-1%"
set "EXE_PATH=%APP_DIR%\CursorCompanion.App.exe"

if not exist "%EXE_PATH%" (
    echo ERROR: CursorCompanion.App.exe not found in %APP_DIR%
    echo Make sure you are running Install.bat from the CursorCompanion folder.
    pause
    exit /b 1
)

:: ---- 1. Desktop Shortcut ----
set "SHORTCUT=%USERPROFILE%\Desktop\CursorCompanion.lnk"
echo [1/3] Desktop shortcut
set /p CREATE_SHORTCUT="Create a Desktop shortcut? (Y/N): "
if /i "!CREATE_SHORTCUT!"=="Y" (
    powershell -NoProfile -Command ^
        "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut('!SHORTCUT!'); $s.TargetPath = '!EXE_PATH!'; $s.WorkingDirectory = '!APP_DIR!'; $s.Description = 'CursorCompanion Desktop Pet'; $s.Save()"
    if exist "!SHORTCUT!" (
        echo   Created: !SHORTCUT!
    ) else (
        echo   WARNING: Shortcut creation may have failed.
    )
) else (
    echo   Skipped.
)
echo.

:: ---- 2. Autostart ----
echo [2/3] Autostart on login
set /p ADD_AUTOSTART="Add CursorCompanion to autostart? (Y/N): "
if /i "!ADD_AUTOSTART!"=="Y" (
    reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" ^
        /v "CursorCompanion" ^
        /t REG_SZ ^
        /d "\"!EXE_PATH!\"" ^
        /f >nul 2>&1
    if errorlevel 1 (
        echo   WARNING: Failed to add autostart registry entry.
    ) else (
        echo   Added to autostart.
    )
) else (
    echo   Skipped.
)
echo.

:: ---- 3. Firewall Rule ----
echo [3/3] Windows Firewall rule (UDP port 7777 - multiplayer)
echo   NOTE: Requires Administrator rights.
echo         If you skip this, hosting multiplayer may not work.
echo.
set /p ADD_FW="Add firewall rule (will request elevation)? (Y/N): "
if /i "!ADD_FW!"=="Y" (
    powershell -NoProfile -Command ^
        "Start-Process -FilePath 'netsh' -ArgumentList 'advfirewall firewall add rule name=\"CursorCompanion UDP 7777\" dir=in action=allow protocol=UDP localport=7777 description=\"CursorCompanion multiplayer\"' -Verb RunAs -Wait" ^
        >nul 2>&1
    if errorlevel 1 (
        echo   WARNING: Firewall rule was not added (elevation cancelled or failed).
    ) else (
        echo   Firewall rule added for UDP port 7777.
    )
) else (
    echo   Skipped.
)
echo.

echo ============================================================
echo  Setup complete! Run CursorCompanion.App.exe to start.
echo ============================================================
echo.
pause
endlocal
