# Accessibility — Outstanding Work & Fairness Review

A pass over the mod (`AccessibilityMod/`) against the decompiled game (`Decompiled/Assembly-CSharp/`),
looking for information a **sighted** player receives that a **blind** player currently does not — and
for places where the mod, or the game underneath it, quietly puts a blind player at a disadvantage.

The mod is already deep: targeting locks, awareness callouts, hit markers, loot/inventory menus, zone
navigation, the map grid, landmark survey, door guide, heal slot, armour that actually mitigates. What
follows is what is *still* missing or unfair, ordered by how much it costs a player in a real match.

The guiding rule (from the brief): give the blind player the **same information**, delivered
**differently** — not an advantage, not a cut-down game.

---

## A. Fairness gaps — a sighted player knows this and a blind player cannot

### A1. Being shot at is silent — no incoming-fire / damage-direction callout ✅ DONE
A sighted player sees a red directional damage indicator the instant a bullet lands, and reacts —
turn, break line of sight, find cover. The blind player gets **only** `HudReader`'s "Low health" /
"Critical" thresholds (`HudReader.cs:82`), which fire late and say **nothing about direction or who**.
You can be shot from behind across a whole fight and never be told where it is coming from.

- **The data is right there.** `CharacterMultiplayer.Damage(byte damage, byte shooterActorId)`
  (`CharacterMultiplayer.cs:509`) carries the shooter's `ActorNumber`, and characters are already
  looked up by actor number (`CharacterMultiplayer.cs:277`, `GetPlayer`). `HitPatches` already patches
  `Damage` — but only reports hits where the main player is the *shooter*.
- **Do:** add a branch for hits where `__instance == main`. Resolve the shooter, compute the bearing
  relative to the player's facing (reuse `Bearings.Relative` / the 8-way vocabulary in
  `AudioTargeting.RelativeDirection`), and speak e.g. *"Taking fire, behind left"* — throttled like the
  hit-marker tally so automatic fire doesn't machine-gun the screen reader. Consider a distinct 3D
  "hurt" pip at the shooter's position, mirroring the behind-cover pip.
- **Watch:** the zone and grenades also route through this path (`shooterActorId == 0` for the zone —
  already the convention `ArmorPatches` uses); suppress the callout for actor 0 so the zone doesn't
  read as an attacker.
- **Implemented:** `HitPatches.Damage_Postfix` now branches when `__instance == main` and speaks
  *"Taking fire, <bearing>"* via the new `IncomingFireAnnouncer` (actor 0 and self-damage skipped,
  bursts throttled to 1.5s, a new direction re-fires after 0.6s so a flanker isn't masked). Read-only
  announcement, safe online. Builds clean.

### A2. Winning / the match ending is never announced ✅ DONE
Death is spoken (`HudReader.cs:117`, *"You died. Finished rank X"*). **Victory and match-end are not.**
When the match finishes, `WinnersManager.Show()` puts up the top-3 / squad winners, the player's final
rank, kills, and a **30-second countdown to the next match** (`WinnersManager.cs`,
`timeBeforeNextMatch = 30f`). That screen is almost all `Text`, and `MenuNavigator` only reads
`Selectable`s (`MenuNavigator.cs:43`), so the whole result screen is silent.

- **Do:** watch `MatchmakingManager.GetRoomStatus()` for `Finish` (the mod already gates everything on
  `Playing`; nothing handles `Finish`). On transition, speak the outcome — *"Victory!"* if the player
  is a winner / last alive, else *"Match over. Rank N, K kills."* — and announce the *"Next match in N
  seconds"* countdown so the player isn't stranded on an unreadable screen.
- Also read the surviving-to-the-end case: right now if you *win* (last one standing) rather than die,
  nothing at all is said.
- **Implemented:** `HudReader.MonitorMatchEnd` watches `RoomStatus.Finish`. Win/loss is decided the
  game's own way (`!player.IsDead()` at Finish, as in `GameManager.OnMatchFinished`): *"Victory! You
  win with N kills"* or *"Match over. You finished rank R, K kills. <winner> won"*, each followed by the
  live countdown read from `WinnersManager.nextMatchTimeTxt`, with 10s/5s reminders. Builds clean.
  *Known limitation:* if the game destroys the dead player's object while spectating, the Finish callout
  won't fire for that player (they already got the death/rank callout); revisit alongside A4 (spectate).

### A3. Parachute drop — you can't choose where you land
`HudReader` announces the plane, the jump, chute open, height, and "Landed" (`HudReader.cs:137`), but
only the **vertical** story. A sighted player spends the descent steering across the map toward a chosen
landmark (loot density, distance from others). The blind player free-falls blind horizontally — no
readout of which landmark/grid square they're drifting toward or how to steer to one.

- **Do:** during `isParachuting`, periodically speak the ground landmark / map square directly below
  (reuse `Landmarks.FindNearby` + `MapGrid`), and let the turn keys (already wired in
  `AccessibleInputController`) plus a "heading toward X" cue guide the drop. Even a simple *"Drifting
  toward church, 120 meters"* would close most of the gap.

### A4. Death → spectate is unnarrated
After dying you enter spectate (`GameManager.cs:343` `Spectate()`, `spectatingPlayerNameTxt`,
`isSpectating`). The mod says nothing about entering spectate or **whose** view you're now watching, and
none of the combat/nav callouts run for the spectated player. Minor for solo, more relevant for squads
where you want to follow a living teammate.

- **Do:** announce *"Spectating <name>"* on the spectate transition and when the spectated target
  changes.

### A5. No shrink-timer countdown — only "soon" ✅ DONE
`HudReader.MonitorZone` speaks qualitative warnings ("appearing soon", "shrinking soon", "shrinking
now" — `HudReader.cs:227`). But the game shows sighted players an actual **numeric countdown**:
`DamageZoneManager.appearsInTimer` / `shrinkInTimer` with `appearsInTimerTxt` / `shrinkInTimerTxt`
(`DamageZoneManager.cs:20,22,42,44`). "Shrinking soon" without a number is much weaker than "shrinks in
20 seconds" when deciding whether there's time for one more loot pile.

- **Do:** surface the timer value (on the `N` key at least, ideally as periodic callouts as it counts
  down through thresholds — 30s / 10s).

### A6. Map-limit boundary warning
Separate from the safe zone, the game has an out-of-bounds warning (`MapLimit.cs` → *"YOU'RE CLOSE TO
THE MAP LIMIT — TURN BACK"*). `SafeZoneNav` handles the **safe zone**, which is a different circle. If
`MapLimit` triggers exist in this map, walking into the world edge is unannounced.
- **Do:** verify a `MapLimit` collider is present in the shipped map; if so, hook
  `NewLocationCanvasManager.TextFromZoneLimitTriggerEnter/Exit` (`NewLocationCanvasManager.cs`) and speak
  it. (Note: the game's *named-location* banners — `NewLocationTrigger` — do **not** exist in this map;
  `MapGrid.cs:12` and `Landmarks.cs:8` confirm the mod built its own grid/landmarks because of that. So
  only the map-limit path is worth wiring.)

---

## B. Combat readability — right idea, incomplete coverage

### B1. Only the single nearest enemy is ever tracked
`AudioTargeting` deliberately tracks **one** most-centered target and speaks **one** nearest enemy
(`AudioTargeting.cs` — "always targets the single most-centered eligible enemy"). This keeps the audio
readable, which is correct for the lock tone. But for *situational awareness* a sighted player sees the
**whole room**. Being flanked — one enemy ahead, one behind — is invisible until the nearer one is dealt
with.
- **Do (careful, opt-in):** consider a periodic multi-enemy summary (count + rough bearings, e.g.
  *"3 enemies: ahead, left, behind right"*), separate from the single-target lock audio so it doesn't
  muddy the aim cue. Keep it low-frequency.

### B2. Reload / truly-empty is thin ✅ DONE
`AudioTargeting` says *"Reload"* once, but only when you have a lock on a target and the gun is empty
(`AudioTargeting.cs:245`). Outside that exact moment there's no clean *"out of ammo"* / *"reloading"* /
*"reloaded"* feedback, and no distinction between "empty mag, reserves available" and "completely dry,
no reserve mags" — a critical difference the `Z` readout only gives on demand.
- **Do:** announce empty-mag and reload-complete events proactively (patch the game's reload path), and
  call out when reserve mags hit zero.

### B3. Auto-equip can silently downgrade you, unseen
Documented game behaviour (`documentation.md` §4/§5): walking over a pile auto-swaps armour, helmet,
bag and attachments with **no confirmation and no comparison** — a level-1 vest replaces your level-3,
a smaller bag **destroys** overflow items. A sighted player sees the icon change; the blind player finds
out via the on-demand `L` status. The accessible loot menu (`E`) is the controlled path, but auto-equip
on proximity can still happen.
- **Do:** when an equipment swap happens, announce it *with the comparison* — *"Vest downgraded, level 3
  to level 1"* / *"Bag full, item lost"* — so a bad auto-swap is at least heard the instant it costs
  something. (`PickupPatches` / `WeaponSlotPatches` are the place; `CharacterInventory.SetBag`'s
  `RemoveRange` is the destructive one to guard.)

---

## C. Smaller items / polish

- **C1. Grenade throw feedback.** Grenades auto-use on pickup (`+2 grenades`, `documentation.md` §10).
  Is the *throw* (game's grenade key) confirmed by voice, and its landing direction given? A sighted
  player sees the arc. Also: no **incoming**-grenade warning (a sighted player sees it land near them).
- **C2. Squad / teammate awareness.** `IsSquadMember` is used to exclude friendlies from targeting
  (`AudioTargeting.cs:325`), but there's no callout for **where your teammates are**, teammate downs, or
  revives. Only matters in squad mode — confirm whether the target audience plays squads.
- **C3. Kill feed.** Sighted players read the feed (who killed whom) to gauge where fights are. Not
  essential, but nearby eliminations are a threat/opportunity signal that's currently unavailable.
- **C4. Interrupt priority audit.** With incoming-fire (A1) added, several callouts compete. Do a pass
  on `ScreenReaderManager.Speak(..., interrupt)` priorities so a life-or-death cue (taking fire, zone,
  close enemy) always wins over loot/landmark chatter. Worth a dedicated priority tier rather than the
  current boolean.
- **C5. Menu result-screen labels.** `MenuNavigator` reads `Selectable`s and one large-font heading
  (`DetectMenuName`, `MenuNavigator.cs:363`). Screens that are mostly static `Text` (results, some
  banners) aren't read. A "read all visible text on this screen" fallback key would cover them.

---

## D. Verify / open questions
- Confirm whether `MapLimit` colliders exist in the shipped map (A6).
- Confirm target audience plays **squad** matches (affects A4, C2 priority).
- A1's incoming-fire callout needs the same **offline-only** caveat thinking as `ArmorPatches` if any
  of it ever touches networked state — but as a *read-only* announcement it's safe online too.
- Decide the policy on B1/C3: how much battlefield awareness is "same info, differently" vs. an
  advantage a sighted player doesn't actually have (they can't see through walls either). Keep parity
  with what the screen genuinely shows.

---

### Quick reference — where each fix lives
| Item | Primary file(s) | Game hook |
| --- | --- | --- |
| A1 incoming fire | `HitPatches.cs` | `CharacterMultiplayer.Damage` (already patched) |
| A2 match end | new / `HudReader.cs` | `MatchmakingManager.RoomStatus.Finish`, `WinnersManager` |
| A3 drop steering | `HudReader.cs` / `NavigationAssistant.cs` | `CharacterParachute`, `Landmarks`, `MapGrid` |
| A4 spectate | `HudReader.cs` | `GameManager.Spectate` / `spectatingPlayerNameTxt` |
| A5 zone timer | `SafeZoneNav.cs` / `HudReader.cs` | `DamageZoneManager.shrinkInTimer` |
| A6 map limit | new small patch | `NewLocationCanvasManager.TextFromZoneLimit…` |
| B1 multi-enemy | `AudioTargeting.cs` | `CharacterMultiplayer.characters` |
| B2 reload | `AudioTargeting.cs` / new | `Character` reload path |
| B3 downgrade warn | `PickupPatches.cs` / `WeaponSlotPatches.cs` | `CharacterInventory.SetBag/ReplaceItem` |
