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
the empty slot 2 so your new rifle can take slot 1. The mod's own code already treats this as a
special case: `HudReader.cs:327` and `NavigationAssistant.cs:588` both check
`weapon1.name != "Handgun 01"` to decide whether you have a *real* weapon.

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

So in the current build, **armour is cosmetic + a HUD number.** Levels and durability are displayed
but never applied. (Worth confirming against a newer build before relying on it, but nothing in this
assembly applies them.)

Note also the `if (num < 1) num = 1;` clamp — the `Damage` RPC can never kill you. Death is decided
separately by `RPC_Damage`, which calls `RPC_Dead` when `health - damage <= 0`.

### Related mod bug

`HudReader.cs:394-397` reads `charInv.vest.id > 0` / `charInv.helmet.id > 0`. Both are `null` until
you pick armour up, so the L key throws a `NullReferenceException` and falls into the catch-all
short summary whenever you are unarmoured. Should be `charInv.vest != null`.

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
   accepts `grenade` and `fuel` items.

There is **no keyboard heal binding in the vanilla input actions** (the full list is
fire, reload, inspect, aim, holster, grenade, melee, run, jump, inventory-next, lock-cursor, move,
look, map, inventory, select-weapon-1, select-weapon-2, interact, camera-mode, crouch, lean, lower,
laser-toggle) and **the accessibility mod does not add one** — its bound keys are
`LeftCtrl` fire, `F`, `H`, `Z`, `K`, `L`, `J`, `T`, `B`, `N`, `E`/arrows/Enter for the loot list.

**This is the missing piece for a blind player: healing currently requires the mouse.** The clean
fix is a one-line key in `HudReader`/`AccessibleInputController` calling
`GameManager.Instance.GetComponent<PickupsManager>().UseHealthAuto()`, then speaking the result
(and speaking "already healing" when `player.isHealing` is true). Nothing else needs patching —
`UseHealthAuto` is public and does the whole job.

---

## 8. Quick reference: the full item table

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
