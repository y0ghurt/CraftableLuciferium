@echo off
REM CraftableLuciferium Build Script (Batch Version)
REM This script compiles the RimWorld mod from the command line

setlocal enabledelayedexpansion

REM Configuration
set PROJECT_NAME=CraftableLuciferium
set SOURCE_DIR=CraftableLuciferium
set OUTPUT_DIR=..\..\..\Assemblies
set COMPILER_PATH=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set RIMWORLD_PATH=C:\Program Files (x86)\Steam\steamapps\common\RimWorld
set ASSEMBLY_CSHARP_PATH="%RIMWORLD_PATH%\RimWorldWin64_Data\Managed\Assembly-CSharp.dll"
set UNITY_ENGINE_PATH="%RIMWORLD_PATH%\RimWorldWin64_Data\Managed\UnityEngine.dll"

echo ==========================================
echo   CraftableLuciferium Build Script
echo ==========================================
echo.

REM Check if we're in the right directory
if not exist "%SOURCE_DIR%" (
    echo ERROR: Source directory '%SOURCE_DIR%' not found.
    echo Please run this script from the project root.
    pause
    exit /b 1
)

REM Check if compiler exists
if not exist "%COMPILER_PATH%" (
    echo ERROR: C# compiler not found at: %COMPILER_PATH%
    echo Please ensure .NET Framework 4.0+ is installed.
    pause
    exit /b 1
)

REM Check if RimWorld assemblies exist
if not exist %ASSEMBLY_CSHARP_PATH% (
    echo ERROR: RimWorld Assembly-CSharp.dll not found.
    echo Please ensure RimWorld is installed via Steam.
    pause
    exit /b 1
)

if not exist %UNITY_ENGINE_PATH% (
    echo ERROR: UnityEngine.dll not found.
    echo Please ensure RimWorld is installed via Steam.
    pause
    exit /b 1
)

echo All prerequisites met!
echo.

REM Create output directory if it doesn't exist
if not exist "%OUTPUT_DIR%" (
    echo Creating output directory: %OUTPUT_DIR%
    mkdir "%OUTPUT_DIR%"
)

REM Change to source directory
cd /d "%SOURCE_DIR%"

REM Build the project
echo Building %PROJECT_NAME%...
echo.

REM Execute compilation
"%COMPILER_PATH%" /target:library /out:%PROJECT_NAME%.dll /reference:%ASSEMBLY_CSHARP_PATH% /reference:%UNITY_ENGINE_PATH% /reference:System.dll /reference:System.Core.dll /reference:System.Xml.dll /reference:System.Data.dll *.cs Properties\*.cs

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Build completed successfully!
    echo.
    
    REM Show build results
    if exist "%PROJECT_NAME%.dll" (
        for %%F in ("%PROJECT_NAME%.dll") do (
            echo Output: %%~fF
            echo Size: %%~zF bytes
        )
        
        REM Copy to output directory
        echo.
        echo Copying to output directory: %OUTPUT_DIR%
        copy "%PROJECT_NAME%.dll" "%OUTPUT_DIR%\%PROJECT_NAME%.dll" >nul
        if exist "%OUTPUT_DIR%\%PROJECT_NAME%.dll" (
            echo Successfully copied to: %OUTPUT_DIR%\%PROJECT_NAME%.dll
        ) else (
            echo WARNING: Failed to copy to output directory
        )
        
        echo.
        echo Your RimWorld mod is ready to use!
    )
) else (
    echo.
    echo ERROR: Build failed with exit code: %ERRORLEVEL%
    pause
    exit /b 1
)

echo.
echo Build script completed.
pause
