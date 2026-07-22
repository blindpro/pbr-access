# Map & drop research

What the game actually knows about its own map, and what of that we can hand to a blind
player so they can pick a landing spot instead of taking whatever the plane gives them.

All findings are from `Decompiled/Assembly-CSharp` plus a string dump of the shipped
scene file (`PolygonBitBattleRoyale_Data/level2` — the battle royale map; `level0`/`level1`
are menu/lobby).

## Short answer

You guessed right: **there are no Fortnite-style named POIs in the battle royale map.**
The engine has the machinery for them and the map does not use it. But there is plenty
else to work with: a fixed-shape flight path with a knowable heading, and buildings whose
prefab names are readable enough to speak. Two other things the game knows — per-square
loot counts (§3) and where every bot will land (§4) — are reachable and deliberately left
alone, because a sighted player doesn't get them either.

---

## 1. There is no POI system in this map

The codebase ships a named-location system from the Synty/HP.Generics environment kit:

- `HP.Generics/NewLocationTrigger.cs` — a trigger volume with `locationName`, fires the
  "you have entered X" banner via `NewLocationCanvasManager`.
- `HP.Generics/MapLimit.cs` — "YOU'RE CLOSE TO THE MAP LIMIT" trigger. Same banner system.
- `HP.Generics/Minimap.cs` — declares `mapSize = 2100 x 2100` and a `minimapOrigin`
  transform; the player blip is `(pos - origin) / mapSize`.

The BR scene contains **zero** `NewLocationTrigger` instances — grepping `level2` for
`LocationTrigger` and for the default `"New Location"` string returns nothing. So the
banner path is dead here and there is no authored list of place names to read out.

**Implication:** any place names we speak have to be ones *we* invent. There are two
honest ways to do that, both cheap:

1. **Name landmarks from prefab names.** The map is built from Synty POLYGON Apocalypse
   assets whose object names are human-meaningful. Distinct building families present in
   `level2` include: `Church`, `Lighthouse`, `Diner`, `Motel`, `Cafe`, `AutoRepair`,
   `Market_Large/Medium`, `Shop_Large/Medium/Small`, `Warehouse_Brick`,
   `Warehouse_Concrete`, `Industrial_Large/Medium/Small`, `Commercial_Large/Medium/Small`,
   `HighRise_Large/Medium`, `Apartment`, `House`, `House_Burnt`, `Trailer`, `Junk_Shelter`,
   `Military_Tent`, `Quarantine_Tent`, `Bunker_Entrance`, `HeliPad`, `RadioTower`,
   `WaterTower`, `WaterTank`, `Cooling_Tower`, `SmokeStack`, `Crane`, `ContainerBridge`,
   `Pool`, `Pylon`.
   Strip the `SM_Bld_` prefix and the trailing `_01`/`_Glass`/`_Overgrowth` noise and you
   get speakable names: "church", "lighthouse", "radio tower", "warehouse district".
   A `Physics.OverlapSphere` + renderer-name lookup around a candidate point gives
   "you are aiming at a church and two houses". `NavigationAssistant.AnnounceSurroundings`
   already does the raycast-sweep half of this; it just does not name what it hits.
2. **Cluster the buildings themselves** and name each cluster by its most distinctive
   member. That yields stable, deserved names like "the warehouse district" without
   hand-authoring anything. (Loot positions would cluster more cleanly, but see the
   fairness note in §3 — box counts stay out of callouts.)

Grid coordinates are the fallback and are trivially available: `Minimap.minimapOrigin`
plus `mapSize` (2100 x 2100) gives a normalised 0–1 position, so an A1–J10 style callout
("you are dropping into F7") costs about ten lines.

---

## 2. The drop itself — what the player controls, and when

`InfimaGames.LowPolyShooterPack/AirplaneManager.cs` + `CharacterParachute.cs` + `Movement.cs`.

**Flight path.** The plane is *not* randomly routed each match in the usual sense. At
match start `MatchmakingManager.StartMatch` picks one number — `Random.Range(0f, 360f)` —
and applies it as a yaw to `AirplaneManager.airplaneRotation`
(`MatchmakingManager.cs:1270`). The plane, its `spawnPoint`, and its `targetPos` all hang
off that pivot. So the route is **the same line every match, spun to a random compass
bearing**, and that bearing is known to every client the instant the match starts. We can
speak it immediately: *"Plane heading north-east, crossing the map centre."*

**Timing.**

| Event | Source | Time |
|---|---|---|
| Match starts, you are on the plane | `CharacterParachute.OnMatchStarted` | t=0 |
| Jump becomes possible (`canJumpFromPlane`) | `AllowJumpFromPlane(20f)` | t=20s |
| Auto-jump if you never pressed jump | `AirplaneManager.Update` — fires when plane reaches `targetPos` | fixed point on the route |
| Parachute becomes openable | `AllowOpenParachute(10f)` after jumping | jump + 10s |
| Parachute force-opens | `y < minHeightUrgentParachute` (100) | altitude-based |

The jump key is the normal jump binding (`Character.OnTryJump` → `JumpFromPlane`, then a
second press → `OpenParachute`). **The player is never forced to ride to the end** — but
if they do nothing, `AirplaneManager` drops them at `targetPos`, which is the same spot
relative to the route every single match. Worth saying out loud: *"If you do not jump,
you will land at the default drop."*

**Glide range.** From `Movement.cs:244-252`, while parachuting the horizontal speed is
multiplied by `1 + 0.04 * y`, clamped to `flyControlMultiplierMinMax` = **1x to 3x**, and
only when you are moving within ~120° of your facing (`Dot(dir, forward) > -0.5`). Base
run speed is 6.8 m/s, so above y≈50 you glide at ~20 m/s. Vertical rates: `flyGravity = 2`
in freefall, `parachuteGravity = 1` once open — **an open parachute descends at half the
freefall rate, so opening early roughly doubles the ground you cover per metre of
altitude.** Given the plane's altitude we can compute and speak an actual reachable
radius, and say whether a chosen landmark is still in range.

**What this makes possible:** a pre-jump menu. Hold a key on the plane, cycle candidate
landing spots by landmark, hear "church, 340 metres right of the flight path, reachable"
— and get told when the plane is at the right moment to jump for it. All the inputs are
readable at runtime.

---

## 3. Loot density is fully knowable — and deliberately not spoken

`LootPoint.cs` spawns exactly one `AmmoBox` per loot point at `Start`. Every box registers
itself into `PickupsManager.ammoBoxes`, and boxes flagged `achievable` also land in
`PickupsManager.ammoBoxesAchievable` (`AmmoBox.Start`). Both are plain **public
`List<AmmoBox>`** — no reflection needed.

Scene counts from `level2`: **4,350 `AmmoBoxParent` objects**, from 53 distinct
`LootPoint_*` prefab variants (numbered up to `LootPoint_57`). Loot positions are static
scenery — identical every match. Contents are randomised per box (`AmmoBox.random_items`),
but *where* the boxes are never changes.

**Ruled out on fairness grounds.** All of the above is knowable, and speaking it was
tried and removed. A sighted player cannot see how much loot sits in a square either —
they learn it by playing. Handing a blind player a box count is not closing a gap, it is
opening one in the other direction, and the point of this mod is parity. Loot density
stays out of every callout: the map grid names places, and the player learns which ones
are worth dropping the same way everyone else does.

The per-box data is still the right source for *close-range* help, which is where the gap
is real: `NavigationAssistant` already uses `ammoBoxes` for the 7 m proximity beep, which
substitutes for seeing a box on the floor in front of you. That is the line — describing
what is in front of the player, not what is over the hill.

---

## 4. Bot landing points — ruled out, do not build this

The game knows exactly where every bot will land, and that knowledge is reachable.
**It is not going in the mod.** Recorded here only so the next person to find it knows the
call has already been made.

`AirplaneManager` holds a list of bot landing spots, and each bot commits to one of them
while you are still falling. A sighted player sees chutes in the sky and can guess roughly
where a few of them are heading — nobody gets a precise roster of where every enemy will
touch down. Reading one out of memory is not closing a gap, it is opening one, the same
way loot counts were (§3).

The deliberately vague version — "chutes to your left" from the same on-screen information
a sighted player has — would be parity, and is the only shape of this worth revisiting.
Exact coordinates, counts, or named destinations are not.

---

## 5. Safe zone: not knowable at drop time, but the *next* circle always is

`DamageZoneManager` timeline (all serialized fields, read them at runtime rather than
hardcoding):

- `appearsInDelay` 20 s → "safe zone will appear in" message
- `appearsInDuration` 60 s → circle appears (~t=80 s)
- `shrinkInDuration` 50 s → shrinking begins (~t=130 s).
  Note `shrinkInDurationFirst = 120f` exists but is **never read** — `OnShowShrinkInMsg`
  always uses `shrinkInDuration`. Do not trust the field name.
- `shrinkingDuration` 120 s → the lerp from old circle to target
- Damage: `hitPlayersDamage` 10 every `hitPlayersDuration` 5 s while outside.

The first circle is **not** a real shrink: `RPC_UpdateDamageZone(1, isFirstTime: 1)` skips
`ReduceTargetZoneCircle`, so circle #1 is just `damageZoneDefault` — the full default
circle, centred on the map's fixed default centre. Only from the second circle on does
`GetRandomCircle` run: radius x0.6, centre displaced by at most `R - 0.6R` = 0.4R.

Two usable facts for drop advice:

1. At jump time the first playable circle is unknowable, but every circle is **nested
   near the same fixed default centre** and shrinks 40% at a time. Distance from
   `damageZoneDefault.position` is therefore a genuine rotation-risk metric we can speak
   while the player is still choosing: *"that drop is 700 metres from map centre —
   long rotation."*
2. `DamageZoneManager.GetTargetDamageZonePos()` / `GetTargetDamageZoneRadius()` are
   **public and expose the *next* circle** as soon as it is chosen. `SafeZoneNav` currently
   reads only the *current* `damageZone`. Announcing the next circle during the "shrink in"
   window is a free, high-value upgrade — it is the single piece of information sighted
   players read off the map and we currently do not speak.

---

## 6. Concrete proposals, ranked by value per line of code

1. ~~**Next-circle callout in `SafeZoneNav`.**~~ Done — `SafeZoneNav.CheckNextCircle`.
2. **Landmark drop picker on the plane.** Name candidate landing spots from nearby
   `SM_Bld_*` renderer names and expose them as a cyclable list while `isOnAirplane`,
   with "jump now" timing against the flight line. Named places only — no box counts,
   per §3.
3. **Flight-path announcement at match start.** Read `airplaneRotation.eulerAngles.y`,
   `Airplane.transform.position/forward`, `targetPos`, `speed`. One sentence, tells the
   player which half of the map they are being offered.
4. ~~**Grid coordinates everywhere.**~~ Done — `MapGrid`, on the P key and auto-announced
   on crossing a square. Extents come from `GameManager.bigMapCamera`, which turned out
   to be a better source than `Minimap` (see Not verified).
5. ~~**Landmark naming from prefab names.**~~ Done — `Landmarks`, shared by the grid
   readout and by both building callouts in `NavigationAssistant`, so the `B` survey and
   the passive distance scan say "church ahead" rather than "building ahead".
   `Landmarks.FindNearby` is now the shared source of buildings, not just of names: the
   `B` survey seeds itself from the same sphere `P` asks, and keeps its raycast sweep only
   for structures no prefab name covers. Rays alone made `B` miss what `P` had just
   named — a ridge in the way, a bungalow the confirming ray flew over, or a tower
   sitting in one of the ~20 m gaps between rays at that range.
6. ~~**Threshold callouts.**~~ Done — `NavigationAssistant.CheckIndoorOutdoor` names the
   building it is speaking about: "entered the church", "left the church". The ceiling ray
   was already there; what it lacked was a name and a second reading to confirm the change
   before speaking it.
7. **Getting through the door.** The open problem. Finding a building is solved; entering
   one is not, and a doorway is a metre-wide gap that the callouts step straight past.

   The scene has a baked NavMesh — `CharacterBot` drives a `NavMeshAgent`, `GameManager`
   sets `NavMesh.pathfindingIterationsPerFrame`, `NavmeshPoint` calls
   `NavMesh.SamplePosition`, and the game ships `UnityEngine.AIModule`. If that bake
   covers interiors, doors need no geometry at all: `NavMesh.CalculatePath` to a point
   inside returns corners that bend through the entrance, so corner 1 *is* the door, and
   `path.status` says up front whether a building can be entered — which is what stops a
   player circling a sealed prop forever.

   `NavMeshDiagnostics` (Y key) answers exactly that, and nothing else: it samples the
   floor at the centre of the nearest landmark's footprint and reports whether the walkable
   point it found lands inside the footprint or out on the street. Inside → build the
   routing. Outside → interiors are not baked, and doorways have to be found by hand, most
   likely by sweeping a dense ray fan across a wall face and keeping the contiguous no-hit
   spans that work out to a door's width rather than a building's edge.

## Runtime handles worth remembering

| What | Where |
|---|---|
| Airplane, route, speed | `GameManager.Instance.GetComponent<AirplaneManager>()` — also reachable via `MatchmakingManager.Instance.GetComponent<AirplaneManager>()`; both are used in game code |
| Player parachute state | `player.GetComponent<CharacterParachute>()` — `isOnAirplane`, `isParachuting`, `isParachuteOpen`, `canJumpFromPlane`, `canOpenParachute` |
| Loot boxes (close-range help only, per §3) | `GameManager.Instance.GetComponent<PickupsManager>().ammoBoxes` / `.ammoBoxesAchievable` |
| Zone, current and next | `GameManager.Instance.GetComponent<DamageZoneManager>()` — `damageZone`, `GetTargetDamageZonePos()`, `GetTargetDamageZoneRadius()`, `IsShrinking()` |
| Pathfinding | A baked NavMesh the bots walk — `CharacterBot.agent` (`NavMeshAgent`), `NavmeshPoint.GetNearestNavMeshPoint`. Queryable without touching the bots: `NavMesh.SamplePosition`, `NavMesh.CalculatePath` |
| Map extents | `GameManager.Instance.bigMapCamera` — world `transform.position` + `orthographicSize`. What `MapGrid` uses; it is the camera the game renders its own full-screen map with, so squares line up with what a sighted player reads |
| Glide tuning | `Movement` — `flyControlMultiplierMinMax`, `flyGravity`, `parachuteGravity`, `speedRunning` (private/serialized; reflection or just hardcode the observed 1–3x, 2.0, 1.0, 6.8) |

## Not verified

- Whether `bigMapCamera.orthographicSize` really frames the play area, and whether the
  map is square. `MapGrid` assumes both and falls back to the `damageZoneDefault` circle
  if the camera is unusable. If the map plane turns out non-square, columns will be
  stretched relative to rows and the grid needs a separate x extent.
- Whether the landmark names come out distinctive or repetitive in practice — if most of
  the map is generic `Commercial_*` boxes, `MapGrid.Landmarks` wants pruning.
- Exact plane altitude and `speed`, which set the real glide radius — read them in game
  rather than assuming.
