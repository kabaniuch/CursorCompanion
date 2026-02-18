@echo off
setlocal EnableDelayedExpansion

echo ============================================================
echo  CursorCompanion - Release Build
echo ============================================================
echo.

set "SCRIPT_DIR=%~dp0"
set "PROJECT_FILE=%SCRIPT_DIR%src\CursorCompanion.App\CursorCompanion.App.csproj"
set "OUTPUT_DIR=%SCRIPT_DIR%dist\CursorCompanion"
set "SCRIPTS_DIR=%SCRIPT_DIR%scripts"

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ERROR: dotnet SDK not found in PATH.
    echo Install .NET 8 SDK from https://dotnet.microsoft.com/download
    exit /b 1
)

if exist "%OUTPUT_DIR%" (
    echo Cleaning previous output...
    rmdir /s /q "%OUTPUT_DIR%"
)

echo Publishing self-contained win-x64 release...
echo.

dotnet publish "%PROJECT_FILE%" ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=false ^
    -p:PublishTrimmed=false ^
    -p:DebugType=none ^
    -p:DebugSymbols=false ^
    --output "%OUTPUT_DIR%"

if errorlevel 1 (
    echo.
    echo ERROR: dotnet publish failed.
    exit /b 1
)

echo.
echo Copying Install.bat and Uninstall.bat...
copy /y "%SCRIPTS_DIR%\Install.bat" "%OUTPUT_DIR%\Install.bat" >nul
copy /y "%SCRIPTS_DIR%\Uninstall.bat" "%OUTPUT_DIR%\Uninstall.bat" >nul

echo.
echo ============================================================
echo  Build complete!
echo  Output: %OUTPUT_DIR%
echo.
echo  To distribute:
echo    powershell Compress-Archive dist\CursorCompanion dist\CursorCompanion.zip
echo.
echo  Users unzip and run CursorCompanion.App.exe
echo  or run Install.bat for shortcuts + autostart.
echo ============================================================
echo.

endlocal
