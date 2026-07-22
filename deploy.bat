@echo off
setlocal

set GAME_DIR=C:\Program Files (x86)\Steam\steamapps\common\PolygonBitBattleRoyale
set PLUGIN_DIR=%GAME_DIR%\BepInEx\plugins\AccessibilityMod
set BUILD_DIR=%~dp0AccessibilityMod\bin\Debug\net472
set LIB_DIR=%~dp0lib\Tolk\x64
set RELEASE_DIR=%~dp0release
set PAYLOAD_DIR=%RELEASE_DIR%\PolygonBitAccessibility

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

echo.
echo Building player release in %PAYLOAD_DIR%...

rem Start from a clean payload so removed files never linger in a handout.
if exist "%PAYLOAD_DIR%" rmdir /s /q "%PAYLOAD_DIR%"
mkdir "%PAYLOAD_DIR%\BepInEx\plugins\AccessibilityMod\lib\x64"

rem BepInEx loader, taken from the working install. No config or logs -
rem BepInEx writes its own on first launch.
copy /y "%GAME_DIR%\winhttp.dll" "%PAYLOAD_DIR%\" >nul
copy /y "%GAME_DIR%\doorstop_config.ini" "%PAYLOAD_DIR%\" >nul
robocopy "%GAME_DIR%\BepInEx\core" "%PAYLOAD_DIR%\BepInEx\core" /e /njh /njs /ndl /nfl /np >nul
if errorlevel 8 (
    echo Failed to copy BepInEx core!
    exit /b 1
)

copy /y "%BUILD_DIR%\AccessibilityMod.dll" "%PAYLOAD_DIR%\BepInEx\plugins\AccessibilityMod\" >nul
copy /y "%BUILD_DIR%\AccessibilityMod.pdb" "%PAYLOAD_DIR%\BepInEx\plugins\AccessibilityMod\" >nul
copy /y "%LIB_DIR%\Tolk.dll" "%PAYLOAD_DIR%\BepInEx\plugins\AccessibilityMod\lib\x64\" >nul
copy /y "%LIB_DIR%\SAAPI64.dll" "%PAYLOAD_DIR%\BepInEx\plugins\AccessibilityMod\lib\x64\" >nul
copy /y "%LIB_DIR%\nvdaControllerClient64.dll" "%PAYLOAD_DIR%\BepInEx\plugins\AccessibilityMod\lib\x64\" >nul

copy /y "%~dp0release_readme.txt" "%PAYLOAD_DIR%\README.txt" >nul
copy /y "%~dp0release_readme.txt" "%RELEASE_DIR%\README.txt" >nul

echo Zipping...
if exist "%RELEASE_DIR%\PolygonBitAccessibility.zip" del /q "%RELEASE_DIR%\PolygonBitAccessibility.zip"
powershell -NoProfile -Command "Compress-Archive -Path '%PAYLOAD_DIR%\*' -DestinationPath '%RELEASE_DIR%\PolygonBitAccessibility.zip'"
if errorlevel 1 (
    echo Zip failed!
    exit /b 1
)

echo.
echo Deploy complete!
echo Hand players: %RELEASE_DIR%\PolygonBitAccessibility.zip
