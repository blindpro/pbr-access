# Polygon Bit Battle Royale — Accessibility Mod

A BepInEx mod that makes Polygon Bit Battle Royale playable without sight. It provides:
- **Screen Reader Navigation:** NVDA, JAWS, System Access and SAPI narration for all menus and settings.
- **Altitudes & Parachute Feedback:** Periodic coordinates and landmarks below the player while free falling.
- **Weapon Feedback:** Firing, dry-fire, and reload progress voice feedback.
- **Safe Zone Timer:** Automated zone shrinkage countdown notifications.
- **Teammate & Spectator Narration:** Teammate status and active spectator target voiceovers.
- **Pause & Resume Feedback:** Instantly reads menu options when pausing/resuming.

Screen reader output goes through [Tolk](https://github.com/dkager/tolk), so NVDA, JAWS, System
Access and SAPI all work.

## For players

Grab `PolygonBitAccessibility.zip` from the release, extract it into the game folder next to
`PolygonBitBattleRoyale.exe`, and launch from Steam. BepInEx is bundled — there is nothing else to
install.

Full install steps, the key list and troubleshooting are in [`release_readme.txt`](release_readme.txt),
which is the README that ships inside the zip.

## For developers

### Building

You need the .NET SDK and a local copy of the game — the project references the game's managed
assemblies directly, so there is nothing to check in.

```
deploy.bat
```

That one script does everything:

1. builds `AccessibilityMod.csproj` against `GAME_DIR`
2. copies the mod and the Tolk natives into your game's `BepInEx\plugins\AccessibilityMod`
3. rebuilds `release\PolygonBitAccessibility\` — the drag-and-drop payload, BepInEx loader included
4. zips it to `release\PolygonBitAccessibility.zip`

`GAME_DIR` at the top of `deploy.bat` is the only thing you may need to change. The BepInEx core in
the release is copied from your own install, so players get exactly the loader version you tested
against.

`release/` is generated and gitignored — never edit it by hand. To change what players read, edit
`release_readme.txt` and re-run the script.

### Layout

| Path | What it is |
| --- | --- |
| `AccessibilityMod/` | the mod source — one file per feature area |
| `Decompiled/Assembly-CSharp/` | decompiled game code, for reference |
| `lib/Tolk/x64/` | Tolk and its screen reader clients |
| `documentation.md` | how the game's loot, gear and items actually work, read out of the decompiled source |
| `map.md` | the map grid and landmark names the mod speaks |
| `release_readme.txt` | player-facing README, copied into the release |

### Notes

- The mod is Harmony-patch based; `Plugin.cs` sets up the loader and `ModRunner.cs` drives the
  per-frame work.
- `documentation.md` is worth reading before touching loot or inventory code. It records behaviour
  that is surprising — bots never consume loot, `UseFuelItem` is a stub, weapon "Lv" is cosmetic.

## Credits

- [BepInEx](https://github.com/BepInEx/BepInEx) — mod loader, LGPL-2.1
- [Tolk](https://github.com/dkager/tolk) by Davy Kager — screen reader abstraction
