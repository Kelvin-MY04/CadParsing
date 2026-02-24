@chcp 65001 > nul
@echo off
setlocal EnableDelayedExpansion

:: -- Configuration ----------------------------------------------------------
set "ACCORE_EXE=C:\Program Files\Autodesk\AutoCAD 2023\accoreconsole.exe"
set "ROOT=%~dp0"
set "PROJECT=%ROOT%CadParsing\CadParsing.csproj"
set "DLL=%ROOT%CadParsing\bin\Debug\net48\CadParsing.dll"
set "SCRIPT=%ROOT%RunProgram.scr"
set "DOWNLOAD_ROOT=C:\Users\pphyo\Downloads"
set "LOG_ROOT=C:\Users\pphyo\Downloads\export\logs"

:: -- [1/2] Build ------------------------------------------------------------
echo [1/2] Building CadParsing (net48 x64 Debug)...
dotnet build "%PROJECT%" -c Debug
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo BUILD FAILED. Aborting.
    exit /b 1
)

:: -- [2/2] Generate RunProgram.scr ------------------------------------------
echo [2/2] Writing RunProgram.scr...
powershell -NoProfile -Command "Set-Content -Path '%SCRIPT%' -Value 'NETLOAD', '%DLL%', 'LISTXREFLAYERS', 'BINDXREF', 'EXPLODEBLOCK', 'DETECTBORDER', 'EXPORTPDF' -Encoding ASCII"

:: -- Process each directory from dwg_path.txt --------------------------------
for /f "usebackq delims=" %%d in ("%ROOT%dwg_path.txt") do (
    if not exist "%%d\" (
        echo.
        echo [WARN] Directory not found, skipping:
        echo   %%d
    ) else (
        echo.
        echo [DIR] Processing directory: %%d
        set "REL_DIR=%%d"
        set "REL_DIR=!REL_DIR:%DOWNLOAD_ROOT%\=!"
        set "LOG_DIR=%LOG_ROOT%\!REL_DIR!"
        if not exist "!LOG_DIR!" mkdir "!LOG_DIR!"
        for /f "delims=" %%f in ('dir /b "%%d\*.dwg" 2^>nul') do (
            for /f "delims=" %%t in ('powershell -NoProfile -Command "Get-Date -Format 'yyyyMMdd_HHmmss'"') do set "TIMESTAMP=%%t"
            set "FILE_LOG_DIR=!LOG_DIR!\%%~nf"
            if not exist "!FILE_LOG_DIR!" mkdir "!FILE_LOG_DIR!"
            echo.
            echo [OK] DWG file: %%d\%%f
            echo [LOG] !FILE_LOG_DIR!\!TIMESTAMP!.log
            echo Running accoreconsole.exe...
            "%ACCORE_EXE%" /i "%%d\%%f" /s "%SCRIPT%" /l "ko-KR" > "!FILE_LOG_DIR!\!TIMESTAMP!.log" 2>&1
        )
    )
)

endlocal
