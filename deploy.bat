@echo off
setlocal

set GAME_DIR=C:\Program Files (x86)\Steam\steamapps\common\PolygonBitBattleRoyale
set PLUGIN_DIR=%GAME_DIR%\BepInEx\plugins\AccessibilityMod
set BUILD_DIR=%~dp0AccessibilityMod\bin\Debug\net472
set LIB_DIR=%~dp0lib\Tolk\x64

echo Building AccessibilityMod...
dotnet build "%~dp0AccessibilityMod\AccessibilityMod.csproj" /p:GameDir="%GAME_DIR%" -c Debug
if errorlevel 1 (
    echo Build failed!
    exit /b 1
)

echo Deploying to %PLUGIN_DIR%...
if not exist "%PLUGIN_DIR%\lib\x64" mkdir "%PLUGIN_DIR%\lib\x64"

copy /y "%BUILD_DIR%\AccessibilityMod.dll" "%PLUGIN_DIR%\"
copy /y "%BUILD_DIR%\AccessibilityMod.pdb" "%PLUGIN_DIR%\"

copy /y "%LIB_DIR%\Tolk.dll" "%PLUGIN_DIR%\lib\x64\"
copy /y "%LIB_DIR%\SAAPI64.dll" "%PLUGIN_DIR%\lib\x64\"
copy /y "%LIB_DIR%\nvdaControllerClient64.dll" "%PLUGIN_DIR%\lib\x64\"

echo Deploy complete!
