@chcp 65001 > nul
@echo off
setlocal EnableDelayedExpansion

:: -- Configuration ----------------------------------------------------------
set "ACCORE_EXE=C:\Program Files\Autodesk\AutoCAD 2023\accoreconsole.exe"
set "ROOT=%~dp0"
set "PROJECT=%ROOT%CadParsing\CadParsing.csproj"
set "DLL=%ROOT%CadParsing\bin\Debug\net48\CadParsing.dll"
set "SCRIPT=%ROOT%RunProgram.scr"

:: -- Read DWG path from dwg_path.txt (UTF-8, no Korean bytes in .bat needed) 
for /f "usebackq delims=" %%a in ("%ROOT%dwg_path.txt") do (
    if not defined DWG set "DWG=%%a"
)

:: -- Guard: DWG file must exist ---------------------------------------------
if not exist "%DWG%" (
    echo.
    echo [ERROR] DWG file not found:
    echo   %DWG%
    exit /b 1
)
echo [OK] DWG file found.

:: -- [1/3] Build ------------------------------------------------------------
echo [1/3] Building CadParsing (net48 x64 Debug)...
dotnet build "%PROJECT%" -c Debug
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo BUILD FAILED. Aborting.
    exit /b 1
)

:: -- [2/3] Generate RunProgram.scr ------------------------------------------
echo [2/3] Writing RunProgram.scr...
powershell -NoProfile -Command "Set-Content -Path '%SCRIPT%' -Value 'NETLOAD', '%DLL%', 'LISTXREFLAYERS', 'BINDXREF', 'EXPLODEBLOCK', 'DETECTBORDER', 'EXPORTPDF' -Encoding ASCII"

:: -- [3/3] Run accoreconsole with DWG ---------------------------------------
echo [3/3] Running accoreconsole.exe with DWG...
"%ACCORE_EXE%" /i "%DWG%" /s "%SCRIPT%" /l "ko-KR"

endlocal
