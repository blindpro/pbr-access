#!/usr/bin/env bash
set -e

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
GAME_DIR="${GAME_DIR:-$HOME/Library/Application Support/Steam/steamapps/common/PolygonBitBattleRoyale}"
PLUGIN_DIR="$GAME_DIR/BepInEx/plugins/AccessibilityMod"
BUILD_DIR="$SCRIPT_DIR/AccessibilityMod/bin/Debug/net472"
RELEASE_DIR="$SCRIPT_DIR/release"
PAYLOAD_DIR="$RELEASE_DIR/PolygonBitAccessibility"
BEPINEX_MAC_DIR="$SCRIPT_DIR/lib/BepInEx_mac"

echo "Building AccessibilityMod..."
dotnet build "$SCRIPT_DIR/AccessibilityMod/AccessibilityMod.csproj" /p:GameDir="$GAME_DIR" -c Debug

echo "Deploying BepInEx & AccessibilityMod to $GAME_DIR..."
mkdir -p "$GAME_DIR/BepInEx/core"
mkdir -p "$PLUGIN_DIR"

if [ -d "$BEPINEX_MAC_DIR" ]; then
    cp -R "$BEPINEX_MAC_DIR/"* "$GAME_DIR/" 2>/dev/null || true
    if [ -f "$GAME_DIR/run_bepinex.sh" ]; then
        chmod +x "$GAME_DIR/run_bepinex.sh"
    fi
fi

cp -f "$BUILD_DIR/AccessibilityMod.dll" "$PLUGIN_DIR/"
if [ -f "$BUILD_DIR/AccessibilityMod.pdb" ]; then
    cp -f "$BUILD_DIR/AccessibilityMod.pdb" "$PLUGIN_DIR/"
fi

echo ""
echo "Building player release in $PAYLOAD_DIR..."
rm -rf "$PAYLOAD_DIR"
mkdir -p "$PAYLOAD_DIR/BepInEx/plugins/AccessibilityMod"

if [ -d "$BEPINEX_MAC_DIR" ]; then
    cp -R "$BEPINEX_MAC_DIR/"* "$PAYLOAD_DIR/" 2>/dev/null || true
    if [ -f "$PAYLOAD_DIR/run_bepinex.sh" ]; then
        chmod +x "$PAYLOAD_DIR/run_bepinex.sh"
    fi
fi

cp -f "$BUILD_DIR/AccessibilityMod.dll" "$PAYLOAD_DIR/BepInEx/plugins/AccessibilityMod/"
if [ -f "$BUILD_DIR/AccessibilityMod.pdb" ]; then
    cp -f "$BUILD_DIR/AccessibilityMod.pdb" "$PAYLOAD_DIR/BepInEx/plugins/AccessibilityMod/"
fi

cp -f "$SCRIPT_DIR/release_readme.txt" "$PAYLOAD_DIR/README.txt"
cp -f "$SCRIPT_DIR/release_readme.txt" "$RELEASE_DIR/README.txt"

echo "Zipping..."
rm -f "$RELEASE_DIR/PolygonBitAccessibility.zip"
(cd "$PAYLOAD_DIR" && zip -r -q "$RELEASE_DIR/PolygonBitAccessibility.zip" .)

echo ""
echo "Deploy complete!"
echo "Hand players: $RELEASE_DIR/PolygonBitAccessibility.zip"
echo ""
echo "On macOS: launch with ./run_bepinex.sh from the game folder (not Steam directly)"
echo "On Windows: the mod loads automatically via winhttp.dll"
