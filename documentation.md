# Loot, Gear and Items — How the Game Actually Works

Everything here comes from reading the decompiled game code in `Decompiled/Assembly-CSharp/`,
mainly:

- `InfimaGames.LowPolyShooterPack/PickupsManager.cs` — the item table and every pick/drop path
- `InfimaGames.LowPolyShooterPack/AmmoBox.cs` — a single loot pile
- `InfimaGames.LowPolyShooterPack/LootPoint.cs` — where piles are placed
- `InfimaGames.LowPolyShooterPack/CharacterInventory.cs` — what you carry and wear
- `InfimaGames.LowPolyShooterPack/CharacterMultiplayer.cs` — health, healing, death drops
- `InfimaGames.LowPolyShooterPack/WeaponAttachmentManager.cs` + `Scope/Grip/Laser/Muzzle.cs` — attachments

Where a fact lives in a Unity asset rather than in code (exact item names, capacities, damage
numbers) that is called out explicitly.

**Vanilla vs. modded.** Sections 1–8 describe the shipped game. Where the accessibility mod now
changes that behaviour it is marked **[modded]** inline, and section 9 lists the keys.

---

## 1. Are there loot boxes?

**No — not in the monetisation sense.** There is no crate, case, key, gacha or roulette system
anywhere in the assembly. Nothing is purchased or opened for a random reward.

The word "box" in the code is `AmmoBox`, which is simply **a loot pile on the ground**. Every
pile is an `AmmoBox` component holding a `List<PickupsManager.Item> items`.

Two things create piles:

| Source | Where | Contents |
| --- | --- | --- |
| `LootPoint` (world spawn) | Hand-placed in the level. On `Start()` it raycasts 5.5 m down, snaps to the ground surface and spawns `pickupsManager.ammoBoxPrefab` there. | Random — see below |
| Death drop (`CharacterMultiplayer.DropAmmoBox`) | Where a player or bot dies | Exactly what they were wearing/holding: vest, backpack, helmet, equipped weapon |

Progression *is* saved (weapon grades, player level, coins via `ProgressionManager` /
`SteamCloudManager`), but it is earned from kills, rank, match time and `money` items you carry
at the end — never from opening anything.

---

## 2. Does the game intentionally spawn loot in piles?

**Yes, deliberately.** `AmmoBox.CreateRandomItems()` runs once per pile when the match starts
(`PickupsManager.OnMatchStarted`):

```csharp
int num = Random.Range(3, 6);          // 3, 4 or 5 items — never 6
for (int i = 0; i < num; i++)
{
    int num2 = Random.Range(0, pickupsManager.items.Length);
    items.Add(pickupsManager.items[num2]);   // uniform over the WHOLE item table
}
```

Consequences worth knowing:

- **Every pile has 3–5 items.** `Random.Range(int, int)` excludes the upper bound.
- **Items are drawn uniformly from the entire item table, with replacement.** There is no rarity
  weighting and no per-item-type balancing at all — a pile can be three helmets, or duplicates of
  the same item. Ammo, money, helmets and rifles are all equally likely.
- **The items are laid out physically.** `ShowObjects()` instantiates each item's 3D model into
  `objectsContainers[i]` — a fixed set of child transforms on the pile prefab. So one pile = several
  separate objects arranged around a point, each with its own `BoxCollider`.
- **Vanilla targeting is per-object, by camera ray.** `AmmoBox.Update()` picks whichever item's box
  collider the camera-forward line crosses (`LineIntersectsBoxLocal`), nearest first, and only within
  `pickRange` (1.5 m by default). That is exactly why a blind player cannot choose between items in a
  pile — hence `AccessibilityMod/LootMenu.cs`, which lists every item in every pile within range and
  lets Up/Down/Enter take them, and `PickupPatches.cs`, which raises `pickRange` to 3.5 m.
- Piles are activated/deactivated by distance (`ammoboxShowDistance = 30 m`, checked in 5 chunks
  every 0.2 s) purely for performance.

---

## 3. What you start with

`CharacterInventory.Restart()` — run at spawn and on respawn:

```csharp
weapon1 = pickupsManager.GetItem("Handgun 01");
weapon2 = null;
// all four attachment slots on both weapons: null
vest    = null;
helmet  = null;
bag     = pickupsManager.GetItem("Bag A (Lv1)");
inventory.Clear();
currentWeapon = 0;
```

So: **a pistol, an empty level-1 backpack, no armour, no attachments, an empty inventory.**
Health starts at 255 (`health = byte.MaxValue`).

### Why picking up a gun instantly puts a Glock 19 in your other slot

This is `PickupsManager.pick_weapon1_from_box`:

```csharp
if (characterInventory.weapon1 != null && characterInventory.weapon2 == null)
{
    characterInventory.weapon2 = characterInventory.weapon1;   // pistol slides to slot 2
    characterInventory.weapon1 = null;
}
characterInventory.weapon1 = currentAmmoBox.ReplaceItem(item, characterInventory.weapon1);
```

The Glock 19 **is** your starting `"Handgun 01"` (Glock 19 is its `short_description`, set in the
Unity asset). You never "get" a second gun — the pistol you always had is pushed from slot 1 into
the empty slot 2 so your new rifle can take slot 1.

**[modded]** `WeaponSlotPatches` reverses this: while slot 2 is empty the new weapon goes *there*
and the character switches to hold it, so the Glock stays in slot 1 and nothing is displaced. Once
both slots are full the vanilla rule returns — a pickup replaces whatever you are holding.

**[modded]** The pistol is also named properly now. Two readouts tested for the asset key
`"Handgun 01"` and treated it as a placeholder, so the ammo key answered *"No weapon"* while a
loaded pistol was in hand, and the draw announcement stayed silent for anyone who had not found
something better. Both use `short_description` — "Glock 19" — and the first draw after landing is
spoken.

Once slot 2 is occupied, the swap stops happening — a further slot-1 pickup replaces slot 1 and the
old weapon goes back into the pile (`ReplaceItem` swaps them, so the pile now contains your old gun).

**Which slot a pickup goes to is decided by which weapon you currently have out**
(`characterInventory.GetCurrentWeapon() == 0` → slot 1, else slot 2). `1` and `2`
(`OnTrySelectWeapon1/2` → `SetCurrentWeapon`) switch the active slot; slot 2 can only be selected
once you actually have a second weapon.

---

## 4. Armour — vests and helmets

### Does it get put on automatically?

**Yes.** Armour never sits in your bag. `pick_helmet_from_box` / `pick_vest_from_box` write straight
into the equipment slot:

```csharp
characterInventory.helmet = currentAmmoBox.ReplaceItem(item, characterInventory.helmet);
```

`ReplaceItem` is a **swap**: the new piece goes on you, and whatever you were wearing is put back in
the pile in its place (visible as a "droped"-tagged object). There is no confirmation and no
comparison — picking up a level 1 vest while wearing level 3 will downgrade you.

Then `CharacterInventory.Apply()` shows the model by activating the matching object in a body
collection:

| Slot | Collection | Setter |
| --- | --- | --- |
| vest | `"armor"` | `SetVest(objName)` — name is auto-swapped Male→Female for female bodies |
| helmet | `"hat"` | `SetHelmet(objName)` |
| bag | `"backbag"` | `SetBag(objName)` |

The HUD reads `helmet.value + "/" + helmet.capacity` and `"Lv " + level`, which is presented as
armour durability.

### ⚠️ What armour actually does to damage: nothing

This is the important finding. Trace the damage path:

`Projectile.cs:93` → `damage = equippedWeapon.weaponDamage;` → `component3.RPC_Damage(damage, ...)`
→ `CharacterMultiplayer.Damage(byte damage, ...)`:

```csharp
int num = health - damage;
if (num < 1) num = 1;
health = (byte)num;
```

**`vest` and `helmet` are never consulted.** There is no damage reduction, no armour-durability
subtraction, no headshot or limb multiplier — `weaponDamage` is a flat byte per weapon prefab, and
health is a flat byte out of 255. Grenades (`GrenadeScript.cs:79`) and the shrinking zone
(`DamageZoneManager.cs:287`) go through the same unmitigated path.

So in the stock build, **armour is cosmetic + a HUD number.** Levels and durability are displayed
but never applied.

**[modded]** `ArmorPatches` gives it teeth **in offline (bot) matches only**: 8% damage reduction
per vest level plus 4% per helmet level, capped at 50%, never absorbing a hit entirely. It hooks
`RPC_Damage` — the single door bullets, grenades and the zone all come through, and the one that
both decides death and forwards the number that gets subtracted, so health loss and the death
threshold move together. Patching `Damage` as well would apply the reduction twice.

Three deliberate limits:
- **Offline only.** Death is decided on the *shooter's* client from its own copy of your health.
  Other players don't run the mod, so mitigating in a real room would leave you alive on your screen
  and dead on theirs.
- **Level, not durability.** `PickupsManager.items` holds one `Item` instance per kind and every
  pile hands out references to *those same instances* — decrementing `item.value` would wear that
  armour down for every other pile and every other character in the match. Levels are read-only.
- **Bots too.** They loot vests and helmets, and armour that worked for one side only would be a
  cheat rather than the mechanic the HUD has been describing. This does make armoured bots take
  more shots; say the word if you want it player-only.
- **Not the zone.** The damage zone passes `shooterActorId == 0`, which is skipped.

Note also the `if (num < 1) num = 1;` clamp — the `Damage` RPC can never kill you. Death is decided
separately by `RPC_Damage`, which calls `RPC_Dead` when `health - damage <= 0`.

### Related mod bug — fixed

`HudReader` read `charInv.vest.id > 0` / `charInv.helmet.id > 0`. Both are `null` until you pick
armour up, so the L key threw a `NullReferenceException` and fell into the catch-all short summary
whenever you were unarmoured — most of a match. It now reports what is worn, and what the
mitigation is worth.

---

## 5. Bags — "Bag A (Lv1)" and friends

Bags are the `ItemType.bag` items. You start with `"Bag A (Lv1)"`. What you hear as "bag 1a" is the
loot menu speaking an item's `short_description` plus its level (`LootMenu.Label` →
`"{name}, level {level}"`).

There are **six bag items** in the table — `CharacterBot.LootItem()` picks bots' upgrades from
indices `52..57` (vests are `49..51`, helmets are `58..71`). They differ only by `level` and
`capacity`.

**A bag does exactly one thing: it sets how many items fit in your inventory list.**

```csharp
// CharacterInventory.AddInventoryItem
if (inventory.Count >= bag.capacity)
{
    pickupsManager.OnShowBagFullMsg();   // the "bag full" banner, 3 seconds
    return false;                        // pickup silently refused
}
```

- Bags are equipped instantly on pickup, same swap rule as armour (`pick_bag_from_box` → `SetBag`).
- **Downgrading a bag destroys items.** `SetBag(Item)` does
  `inventory.RemoveRange(bag.capacity, inventory.Count - bag.capacity)` — anything past the new
  capacity is deleted outright, not dropped.
- HUD shows `inventory.Count + "/" + bag.capacity`.
- Weapons, attachments-on-guns, vest, helmet and the bag itself do **not** consume bag space —
  only the loose items in the `inventory` list do.
- When the mod says "Could not take X" (`LootMenu.cs:183`), a full bag is the usual reason.

Exact capacities per bag level live in the Unity item asset, not in code.

---

## 6. Attachments

Four slots **per weapon**, so eight slots total: `silencer`, `scope`, `grip`, `laser`.

### Do they auto-equip? Yes — onto whichever gun is active

`pick_silencer1_from_box`, `pick_scope1_from_box`, etc. are chosen by
`characterInventory.GetCurrentWeapon() == 0`. Same `ReplaceItem` swap as armour: the new attachment
goes on the gun, the old one goes back into the pile.

`CharacterInventory.Apply()` then pushes the item's `id` into the weapon's
`WeaponAttachmentManager` as an index and calls `UpdateWeapon()`:

```csharp
weaponAttachmentManager.muzzleIndex = weapon1_silencer == null ? 0  : weapon1_silencer.id;
weaponAttachmentManager.scopeIndex  = weapon1_scope    == null ? -1 : weapon1_scope.id;
weaponAttachmentManager.gripIndex   = weapon1_grip     == null ? -1 : weapon1_grip.id;
weaponAttachmentManager.laserIndex  = weapon1_laser    == null ? -1 : weapon1_laser.id;
```

Note the asymmetry: **muzzle defaults to index 0, not -1** — every gun always has some muzzle;
"no silencer" means the stock one. `-1` on the others means "none" (for the scope, `-1` = ironsights).

If a weapon's array doesn't have that index, `Apply()` clamps it, so an attachment can silently do
nothing on a gun that has no model for it.

### What each one actually does

| Attachment | Real effect | Where |
| --- | --- | --- |
| **Scope** | The only one with real stats. While **aiming**: spread × `multiplierSpread` (default **0.1** — a 10× accuracy gain), mouse sensitivity × `multiplierMouseSensitivity` (0.8), FOV × 0.9 (weapon 0.7), weapon sway × `swayMultiplier`. Not aiming: no effect at all. | `Scope.cs`; used at `Character.cs:450` (fire spread) and `Character.cs:1187/1199` (look), `SwayMotion.cs:74` |
| **Grip** | Reduces recoil: `SetRecoil(GripRecoil = 0.2f)` instead of `DefaultRecoil = 0.5f` — a flat alpha on every `RecoilMotion`. Binary: having *any* grip is what matters, the specific grip is irrelevant. | `Weapon.cs:539` → `CharacterMultiplayer.SetRecoil(bool, Weapon)` |
| **Silencer / muzzle** | Cosmetic + audio: swaps the firing `AudioClip`, muzzle-flash particles, smoke, flash light, and the firing socket position. **No stat change, and nothing in the code makes bots hear you less** — bot awareness is FOV + linecast based (`CharacterBot.IsInFOV`), not sound based. | `Muzzle.cs` |
| **Laser** | A visual beam. Auto-hides while running and while aiming; can be toggled (`LaserToggleInput`). No accuracy effect. | `Laser.cs` |

So mechanically: **scope > grip >>> silencer ≈ laser (cosmetic).**

**[modded]** Every scope stat applies *only while aiming*, and aiming was bound to a mouse button
and nothing else — so a scope could be found, carried, fitted and never once used. **X** toggles
aim down sights (`Character.Update` computes `aiming = holdingButtonAim && CanAim()`, so setting the
held flag is the whole of it), and the ammo readout names the fitted sight.

**[modded]** The aim assist now accounts for the scope. It did not before and could not have: it
steers the character directly rather than through `OnLook`, which is where the game applies the
sight's sensitivity multiplier. The *geometry* needed nothing — a scope doesn't move the shot ray,
and spread never decided damage — but the arrow-key turn and the assist's own pitch/yaw steps are
now scaled by that same multiplier, so scoped aiming is fine and slow for the player instead of the
assist slewing at full speed exactly where careful placement is the point.

Attachments can also be kept loose in your bag and dragged onto a weapon later — that path
(`OnEndDrag`, source `myInventory`) swaps the currently-fitted one back into the bag.

---

## 7. Healing

### The item

`ItemType.health`. It is **not** auto-used on pickup — it goes into your bag like any other loose
item (`pick_other_from_box_to_inventory`). Grenades, by contrast, *are* auto-used on pickup
(`UseGrenadeAuto()` is called right after any non-equipment pickup), and ammo is auto-used on reload
(`Character.OnTryPlayReload` → `UseAmmoAuto()`).

### Using one

`CharacterInventory.UseHealthItem`:

```csharp
if (component.isHealing) return false;          // one heal at a time
component.isHealing = true;
component.healing_add = (byte)item.value;       // amount, from the item asset
GameManager.Instance.usedHealths++;
```

Healing is a **channel, not instant**. `CharacterMultiplayer.UpdateHealing()` fills
`healingImg.fillAmount` at `Time.deltaTime * healingSpeed(1.0) * 1.3` per frame — roughly **0.77
seconds** — and only when the bar reaches 1.0 does `RPC_RestoreHealth(healing_add)` add the health
(capped at 255). Nothing cancels it, and a second heal is refused while one is running.

Also note: heals **cost you score**. `GameManager.ComputeScore` applies
`Mathf.Min(usedHealths * 0.03f, 0.15f)` as a penalty multiplier — up to −15% for 5+ heals.

### How you trigger it — and the accessibility gap

There are exactly two entry points in code:

1. **`PickupsManager.UseHealthAuto()`** — walks your inventory, uses the first `health` item, removes
   it. **It is not bound to any key or input action in code.** It is wired to an on-screen HUD
   button in the Unity scene. Compare `UseAmmoAuto`, which *is* called from `Character.cs:835/856`
   on reload.
2. **The inventory screen (drag & drop).** Open the in-game menu → Inventory
   (`GameManager.OnInventory` → `InGameInventoryButton`), then drag the health item onto the
   **Use** drop zone (`PickupsManager.OnEndDrag`, `dropZone == use_frame`). The same zone also
   accepts `grenade` and `fuel` items. **This screen is unusable without a mouse — see §8.5.**

There is **no keyboard heal binding in the vanilla input actions** (the full list is
fire, reload, inspect, aim, holster, grenade, melee, run, jump, inventory-next, lock-cursor, move,
look, map, inventory, select-weapon-1, select-weapon-2, interact, camera-mode, crouch, lean, lower,
laser-toggle), so in the stock game **healing requires the mouse.**

**[modded]** Heals are now a third weapon slot rather than a new one-shot key:

- **3** draws them — *"Heals, 2. Control to use"*, or *"No heals in bag"* if there are none.
- **Left Control** — the same key that fires — drinks one. The input controller stands down while
  the slot is out, so the fire key cannot fire and heal in the same frame.
- **1** or **2** puts the gun back. Using your last heal disarms automatically, so Control never
  becomes a dead key.
- The finished channel reports the new health: *"Healed. Health 92 percent."*

It routes through `PickupsManager.UseHealthAuto()` — the game's own path, which walks the bag, uses
the first health item, removes it and refreshes the HUD — so there is no second copy of that logic
to keep in step. `HealSlot.cs`.

Heals can also be used from the accessible inventory (**I**, then Enter on the item).

---

## 8.5. Why the inventory screen was unusable — and what replaced it

You said you can't really use the inventory. That is not a settings problem; the screen is
structurally unreachable from a keyboard:

- **There is not a single `Selectable` in it.** `MenuNavigator` walks `Selectable` components
  (buttons, toggles, sliders) — the thing it navigates simply does not exist here. Rows are bare
  `Image`s carrying a `UIDragHandler`.
- **Every operation is a mouse gesture** from a source rectangle to a target rectangle:
  `OnBeginDrag` → `OnEndDrag(dragHandler, dropZone)`. Fitting a scope means dragging an icon onto
  the correct one of ten small frames; there is no click-to-equip path at all.

**[modded]** `InventoryMenu.cs` therefore does not try to drive that UI. It reads
`CharacterInventory` — where the truth actually lives — and performs the same state changes
`OnEndDrag` performs for each drop zone, then calls `Apply()` and `PickupsManager.Init()` so the
visible screen agrees with what was spoken.

| Key | Action |
| --- | --- |
| **I** | Open / close |
| **Up / Down** | Move through equipment then bag |
| **Enter** | Hold a weapon · use a heal, grenade or ammo · fit an attachment · take one off |
| **Delete** / Backspace | Drop |
| **Left Arrow** / Escape | Close |

Rows are spoken with their slot, and **empty attachment mounts are listed too** — an empty scope
mount is information, and walking the list is how the shape of your kit gets learned without seeing
it. Two of the game's own rules are preserved rather than worked around: slot 1 is only emptied by
promoting slot 2 into it (`Apply()` reads `weapon1` unconditionally and would throw on a null), and
a swap that would overflow the bag is refused whole, so an attachment can't quietly cease to exist.

The mod also suppresses the game's own inventory key while this is open — that screen unlocks the
cursor, and it must not come up behind the accessible one.

---

## 9. Mod key reference

| Key | Action |
| --- | --- |
| Left Control | Fire — or use a heal when the heal slot is drawn |
| Left / Right Arrow | Turn (scaled by scope sensitivity while aiming) |
| **X** | Toggle aim down sights *(new)* |
| **3** | Draw heals; **1** / **2** put the weapon back *(new)* |
| **I** | Accessible inventory *(new)* |
| E | Loot list — Up/Down, Enter takes, Left/Escape closes |
| H | Health · Z ammo (+ fitted sight) · K kills & players · L full status · J height |
| F | Compass facing · B surroundings survey (names buildings: "church ahead 30 meters") · N safe zone · T lock diagnostics |
| **P** | Position: map square and nearest landmark — e.g. "D2. Church 40 meters north east" *(new)* |
| **Q** | Guide me through the nearest door — in from outside, back out when inside. A tone at the doorway; press again to stop *(new)* |

Crossing a threshold is announced by name as it happens: "entered the church", "left the
church". It is held for a second reading first, so an awning or a container passed under
does not produce a pair of callouts a second apart.

---

## 10. Quick reference: the full item table

`PickupsManager.ItemType` (17 types):

`money`, `health`, `ammo_sniper`, `ammo_smg_gun`, `ammo_assault`, `ammo_grenades_launchers`,
`ammo_shotgun`, `grenade`, `weapon`, `silencer_attachment`, `scope_attachment`, `grip_attachment`,
`laser_attachment`, `helmet`, `bag`, `vest`, `fuel`

Each `Item` carries: `id` (doubles as the attachment index), `name` (asset key, e.g.
`"Handgun 01"`), `description`, `short_description` (the spoken name, e.g. `"Glock 19"`), `image`,
`level`, `value`, `capacity`, `type`, `objName` (the body-collection object for wearables).

Known index ranges, from `CharacterBot.LootItem()`: **vests 49–51, bags 52–57, helmets 58–71.**

### Where each type ends up when picked up

| Type | Destination | Auto-used? |
| --- | --- | --- |
| weapon | slot 1 or 2 by active weapon; pistol slides to slot 2 the first time | equipped |
| helmet / vest / bag | its equipment slot, swapping out the old one into the pile | equipped |
| silencer / scope / grip / laser | that slot on the **active** weapon | fitted |
| health | bag | **no — manual** |
| grenade | bag, then `UseGrenadeAuto()` fires immediately (`+2 grenades`) | yes |
| ammo_* | bag; consumed by `UseAmmoAuto()` on reload, matched by weapon-name substring (`SMG`/`Handgun` → smg ammo, `Sniper`, `Assault`, `Shotgun`, `Rocket Launcher`/`Grenade Launcher`) | on reload |
| money | bag; counted at match end into coins | no |
| fuel | bag; `UseFuelItem` **is a stub that returns true and does nothing** | no-op |

### Other odds and ends

- **Bots do not consume loot.** `CharacterBot.LootItem()` grants itself random gear from the item
  table when near a pile; it never removes items from `AmmoBox.items`. Piles are not competitive.
- Bots also give themselves 100,000 magazines (`weapon.AddMags(100000)`).
- Weapon "Lv" in the HUD is `ProgressionManager.GetWeaponGrade` — a persistent, account-wide grade
  saved to Steam Cloud that goes up with kills. **It is display-only; nothing multiplies
  `weaponDamage` by it.**
- Death drops (`DropAmmoBox`) contain armour/bag/helmet/equipped weapon but **not** the dead
  player's loose inventory items.
