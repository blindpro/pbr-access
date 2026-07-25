Polygon Bit Battle Royale - Accessibility Mod
=============================================

A screen reader mod for Polygon Bit Battle Royale. It speaks the menus, your
health and ammo, the loot on the ground, the safe zone, and where you are on
the map, so the game can be played without sight.

Works with NVDA, JAWS, System Access and SAPI on Windows, and VoiceOver on macOS.


INSTALLING
----------

1. Find your game folder. In Steam: right click Polygon Bit Battle Royale,
   choose Manage, then Browse local files.
   - On Windows: The folder contains PolygonBitBattleRoyale.exe.
   - On macOS: The folder contains PolygonBitBattleRoyale.app.

2. Copy everything inside this folder into that game folder:
   - Windows: winhttp.dll, doorstop files, and the BepInEx folder.
   - macOS: libdoorstop.dylib, run_bepinex.sh, and the BepInEx folder.

   If your operating system asks whether to merge or replace folders, say yes to merging.

3. Launch the game:
   - Windows: start the game normally from Steam.
   - macOS: open a terminal in the game folder and run ./run_bepinex.sh
     (do NOT launch from Steam directly — BepInEx needs the shell script).

   The first launch takes a few seconds longer than usual while the mod loader
   sets itself up. You should hear the menu being read out.

Have your screen reader (NVDA/JAWS/SAPI on Windows, or VoiceOver on macOS) running before you start the game.


UNINSTALLING
------------

Delete the mod files from the game folder:
- Windows: winhttp.dll, doorstop files, and the BepInEx folder.
- macOS: libdoorstop.dylib, run_bepinex.sh, and the BepInEx folder.

The game goes back to normal.


KEYS
----

Left Control     Fire - or use a heal when the heal slot is drawn
Escape           Pause Menu - opens pause menu and announces buttons
Left / Right     Turn (slower while aiming, matched to the scope)
X                Toggle aim down sights
1 / 2            Weapon slots
3                Draw heals (1 or 2 puts the weapon back)
I                Accessible inventory - Up/Down browse, Enter uses, Delete drops
                 Mouse works too: the wheel browses and left click uses
E                Loot list - Up/Down to browse, Enter takes, Left or Escape closes
                 Mouse works too: the wheel browses and left click takes

H                Health
Z                Ammo, and the sight fitted to your weapon
K                Kills and players left
L                Full status
J                Height

F                Compass facing
B                Survey your surroundings - names buildings, e.g. "church ahead 30 meters"
N                Safe zone
P                Position - map square and nearest landmark, e.g. "D2. Church 40 meters north east"
T                Lock diagnostics


IF IT DOES NOT TALK
-------------------

- Make sure your screen reader was already running before the game started.
- On Windows, check that winhttp.dll sits next to PolygonBitBattleRoyale.exe, not inside a
  sub-folder.
- On macOS, check that libdoorstop.dylib, run_bepinex.sh, and the BepInEx folder all sit
  inside the game directory next to PolygonBitBattleRoyale.app. Make sure run_bepinex.sh
  is executable (chmod +x run_bepinex.sh) and that you run it from the game folder.
- Look in BepInEx/LogOutput.log inside the game folder. If the mod loaded it
  will say "Accessibility Mod v... loaded!" near the top. That file is worth
  sending along if you report a problem.


CREDITS
-------

Screen reader support uses Tolk by Davy Kager (Windows) and native VoiceOver integration (macOS).
Mod loading uses BepInEx (https://github.com/BepInEx/BepInEx), LGPL-2.1.

