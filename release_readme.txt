Polygon Bit Battle Royale - Accessibility Mod
=============================================

A screen reader mod for Polygon Bit Battle Royale. It speaks the menus, your
health and ammo, the loot on the ground, the safe zone, and where you are on
the map, so the game can be played without sight.

Works with NVDA, JAWS, System Access and SAPI (Windows built-in speech).


INSTALLING
----------

1. Find your game folder. In Steam: right click Polygon Bit Battle Royale,
   choose Manage, then Browse local files. The folder is the one that has
   PolygonBitBattleRoyale.exe in it.

2. Copy everything inside this folder - winhttp.dll, doorstop_config.ini and
   the BepInEx folder - into that game folder.

   If Windows asks whether to merge or replace folders, say yes to merging.

3. Start the game normally from Steam. The first launch takes a few seconds
   longer than usual while the mod loader sets itself up. You should hear the
   menu being read out.

Have your screen reader running before you start the game.


UNINSTALLING
------------

Delete winhttp.dll, doorstop_config.ini and the BepInEx folder from the game
folder. The game goes back to normal.


KEYS
----

Left Control     Fire - or use a heal when the heal slot is drawn
Escape           Pause Menu - opens pause menu and announces buttons
Left / Right     Turn (slower while aiming, matched to the scope)
X                Toggle aim down sights
1 / 2            Weapon slots
3                Draw heals (1 or 2 puts the weapon back)
I                Accessible inventory
E                Loot list - Up/Down to browse, Enter takes, Left or Escape closes

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
- Check that winhttp.dll sits next to PolygonBitBattleRoyale.exe, not inside a
  sub-folder. This is the most common mistake.
- Look in BepInEx\LogOutput.log inside the game folder. If the mod loaded it
  will say "Accessibility Mod v... loaded!" near the top. That file is worth
  sending along if you report a problem.


CREDITS
-------

Screen reader support uses Tolk by Davy Kager.
Mod loading uses BepInEx (https://github.com/BepInEx/BepInEx), LGPL-2.1.
