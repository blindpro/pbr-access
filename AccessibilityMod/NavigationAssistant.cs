using System.Collections.Generic;
using UnityEngine;
using InfimaGames.LowPolyShooterPack;

namespace AccessibilityMod
{
    /// <summary>
    /// Provides spatial awareness for blind/low-vision players:
    /// - Wall detection ahead with distance callouts
    /// - Doorway/opening detection to the left and right
    /// - Nearby loot announcements with item names
    /// - A distinct proximity beep for ammo that fits the player's current weapon
    /// - Wide long-range building scan with name/direction/distance callouts
    /// - Pickup confirmation announcements
    /// - Indoor/outdoor transition detection
    /// </summary>
    public class NavigationAssistant
    {
        // Wall detection
        private const float WallCheckDistance = 15f;
        private const float WallDistChangeThreshold = 2f; // re-announce when distance changes by this much
        private const float WallPeriodicInterval = 5f; // periodic re-announce if still facing wall
        private float _wallPeriodicTimer;
        private bool _wallAhead;
        private float _lastWallDist;
        private float _lastAnnouncedWallDist;

        // Doorway/opening detection
        private const float DoorCheckDistance = 8f;
        private const float DoorCheckAngle = 45f; // degrees left/right
        private const float DoorAnnounceInterval = 3f;
        private float _doorAnnounceTimer;

        // Loot detection
        private const float LootScanRadius = 7f;
        private const float LootAnnounceInterval = 2f;
        private float _lootAnnounceTimer;
        private AmmoBox _lastAnnouncedBox;
        private int _lastLootItemCount;

        // Pickup tracking
        private PickupsManager.Item _lastPickItem;
        private AmmoBox _lastPickBox;

        // Loot proximity audio cue (spatial 3D at loot position)
        private AudioClip _lootBeep;
        private AudioClip _ammoBeep; // distinct double-beep for weapon-compatible ammo
        private float _lootBeepTimer;
        private const float LootBeepMaxInterval = 1.5f; // far away beep rate
        private const float LootBeepMinInterval = 0.3f; // close beep rate

        // Wide long-range building scan
        private const float BuildingScanInterval = 3f;     // how often to sweep
        private const float BuildingMinDistance = 18f;      // ignore close hits (handled by wall check)
        private const float BuildingMaxDistance = 60f;      // how far to look
        private const float BuildingDistBucket = 10f;       // round distance to nearest 10m
        private const float BuildingReannounceInterval = 8f;
        private float _buildingScanTimer;
        private float _buildingReannounceTimer;
        private string _lastBuildingAnnounce;

        // On-demand survey (B key)
        private const float InteriorScanDistance = 20f;  // room-sized, not map-sized
        private const float DoorwayProbeAngle = 14f;     // how far off-axis we look for a frame
        private const float DoorwayProbeDistance = 6f;   // a frame that close means a doorway
        private const float SurveySweepStep = 22.5f;     // 16 rays around the circle
        private const float SurveyMergeArc = 45f;        // one building claims this much arc
        private const int SurveyMaxBuildings = 3;        // beyond this it stops being a sentence
        private const int InteriorLandmarkCount = 2;     // named buildings the indoor report still mentions
        private const float StandingInside = 3f;         // this close, its walls are the walls around you

        // Indoor/outdoor detection
        private const float CeilingCheckHeight = 20f;
        private bool _isIndoors;
        private bool _indoorStateSet;
        // Twice a second, because a change has to survive a second reading before it is
        // spoken and a threshold called two seconds after you walked through it is not a
        // threshold callout.
        private const float IndoorCheckInterval = 0.5f;
        private float _indoorCheckTimer;
        private bool _pendingIndoors;
        private string _insideOf;
        private const float ThresholdSearchRadius = 25f;

        // Weapon draw tracking
        private string _lastWeaponName;

        // Scan timing
        private const float ScanInterval = 0.15f;
        private float _scanTimer;

        public void Tick()
        {
            var player = CharacterMultiplayer.GetMainPlayer();
            if (player == null) return;

            // Forgetting the drawn weapon on death is what lets the next spawn
            // announce the pistol again.
            if (player.IsDead())
            {
                _lastWeaponName = null;

                // Same for the threshold: the next spawn starts outdoors somewhere else,
                // and should not be told it left the building this body died in.
                _indoorStateSet = false;
                _insideOf = null;
                return;
            }

            // Only run during gameplay
            if (MatchmakingManager.Instance == null) return;
            var status = MatchmakingManager.Instance.GetRoomStatus();
            if (status != MatchmakingManager.RoomStatus.Playing) return;

            // On-demand survey. Checked before the parachute skip (it helps pick a
            // landing spot) and before the scan gate, because GetKeyDown is true for
            // a single frame and the gate would swallow most presses.
            if (Input.GetKeyDown(KeyCode.B))
                AnnounceSurroundings(player);

            // Skip during parachuting
            var parachute = player.GetComponent<CharacterParachute>();
            if (parachute != null && parachute.isParachuting) return;

            _scanTimer -= Time.deltaTime;
            if (_scanTimer > 0f) return;
            _scanTimer = ScanInterval;

            EnsureBeeps(player);
            CheckWallAhead(player);
            CheckDoorways(player);
            CheckNearbyLoot(player);
            CheckLootProximityBeep(player);
            CheckBuildingsInDistance(player);
            CheckPickupConfirmation(player);
            CheckWeaponDraw(player);
            CheckIndoorOutdoor(player);
        }

        private void CheckWallAhead(CharacterMultiplayer player)
        {
            _wallPeriodicTimer -= ScanInterval;

            Vector3 origin = player.transform.position + Vector3.up * 1f;
            Vector3 forward = player.transform.forward;

            bool hit = Physics.Raycast(origin, forward, out RaycastHit hitInfo, WallCheckDistance,
                GetObstacleMask(), QueryTriggerInteraction.Ignore);

            if (hit)
            {
                float dist = Mathf.Round(hitInfo.distance);

                bool isNew = !_wallAhead;
                bool distChanged = Mathf.Abs(dist - _lastAnnouncedWallDist) >= WallDistChangeThreshold;
                bool periodicUpdate = _wallAhead && _wallPeriodicTimer <= 0f;

                _wallAhead = true;
                _lastWallDist = dist;

                if (isNew || distChanged || periodicUpdate)
                {
                    _lastAnnouncedWallDist = dist;
                    _wallPeriodicTimer = WallPeriodicInterval;

                    if (dist <= 1f)
                        ScreenReaderManager.Speak("Wall!");
                    else
                        ScreenReaderManager.Speak($"Wall ahead, {(int)dist} meters");
                }
            }
            else
            {
                if (_wallAhead)
                {
                    _wallAhead = false;
                    ScreenReaderManager.Speak("Clear");
                }
            }
        }

        private void CheckDoorways(CharacterMultiplayer player)
        {
            _doorAnnounceTimer -= ScanInterval;
            if (_doorAnnounceTimer > 0f) return;

            // Only check for doorways when near a wall
            if (!_wallAhead) return;

            Vector3 origin = player.transform.position + Vector3.up * 1f;

            // Check left
            string leftResult = CheckOpening(origin, player.transform, -DoorCheckAngle, DoorCheckDistance);
            // Check right
            string rightResult = CheckOpening(origin, player.transform, DoorCheckAngle, DoorCheckDistance);

            if (leftResult != null || rightResult != null)
            {
                _doorAnnounceTimer = DoorAnnounceInterval;

                if (leftResult != null && rightResult != null)
                    ScreenReaderManager.Speak($"{leftResult}. {rightResult}");
                else if (leftResult != null)
                    ScreenReaderManager.Speak(leftResult);
                else
                    ScreenReaderManager.Speak(rightResult);
            }
        }

        private string CheckOpening(Vector3 origin, Transform playerTransform, float angle, float maxDist)
        {
            Vector3 direction = Quaternion.Euler(0, angle, 0) * playerTransform.forward;
            string side = angle < 0 ? "left" : "right";

            // Cast at the angle - if no hit, there's an opening
            if (!Physics.Raycast(origin, direction, out RaycastHit _, maxDist,
                GetObstacleMask(), QueryTriggerInteraction.Ignore))
            {
                // Verify the wall IS blocking straight ahead at roughly same distance
                // to confirm it's a real opening (doorway/gap), not just open space
                if (_wallAhead && _lastWallDist < maxDist)
                {
                    return $"Opening {side}";
                }
            }
            else
            {
                // Also cast at a shallower angle to find doorway-sized gaps
                Vector3 shallowDir = Quaternion.Euler(0, angle * 0.5f, 0) * playerTransform.forward;
                if (!Physics.Raycast(origin, shallowDir, out RaycastHit _, maxDist,
                    GetObstacleMask(), QueryTriggerInteraction.Ignore))
                {
                    if (_wallAhead && _lastWallDist < maxDist)
                    {
                        return $"Opening {side}";
                    }
                }
            }

            return null;
        }

        private void CheckNearbyLoot(CharacterMultiplayer player)
        {
            _lootAnnounceTimer -= ScanInterval;
            if (_lootAnnounceTimer > 0f) return;

            if (GameManager.Instance == null) return;
            var pickupsMgr = GameManager.Instance.GetComponent<PickupsManager>();
            if (pickupsMgr == null) return;

            Vector3 playerPos = player.transform.position;
            AmmoBox closestBox = null;
            float closestDist = float.MaxValue;

            foreach (var box in pickupsMgr.ammoBoxes)
            {
                if (box == null || box.items == null || box.items.Count == 0) continue;

                float dist = Vector3.Distance(playerPos, box.transform.position);
                if (dist < LootScanRadius && dist < closestDist)
                {
                    closestDist = dist;
                    closestBox = box;
                }
            }

            if (closestBox != null && closestBox.items.Count > 0)
            {
                // Only announce if it's a new box or items changed
                bool isNew = closestBox != _lastAnnouncedBox;
                bool itemsChanged = closestBox.items.Count != _lastLootItemCount;

                if (isNew || itemsChanged)
                {
                    _lootAnnounceTimer = LootAnnounceInterval;
                    _lastAnnouncedBox = closestBox;
                    _lastLootItemCount = closestBox.items.Count;

                    int dist = Mathf.RoundToInt(closestDist);
                    string direction = GetRelativeDirection(player.transform, closestBox.transform.position);

                    // List the items
                    var itemNames = new List<string>();
                    foreach (var item in closestBox.items)
                    {
                        if (item != null)
                        {
                            string name = !string.IsNullOrEmpty(item.short_description)
                                ? item.short_description : item.description;
                            if (!string.IsNullOrEmpty(name))
                                itemNames.Add(name);
                        }
                    }

                    if (itemNames.Count > 0)
                    {
                        string itemList = string.Join(", ", itemNames);
                        if (dist <= 1)
                            ScreenReaderManager.Speak($"Loot {direction}: {itemList}");
                        else
                            ScreenReaderManager.Speak($"Loot {direction}, {dist} meters: {itemList}");
                    }
                }
            }
            else
            {
                _lastAnnouncedBox = null;
                _lastLootItemCount = 0;
            }
        }

        private void EnsureBeeps(CharacterMultiplayer player)
        {
            const int sampleRate = 44100;

            // Generic loot beep: a single 880 Hz tone.
            if (_lootBeep == null)
            {
                float duration = 0.12f;
                int sampleCount = (int)(sampleRate * duration);
                float[] samples = new float[sampleCount];
                for (int i = 0; i < sampleCount; i++)
                {
                    float t = (float)i / sampleRate;
                    float envelope = 1f - (t / duration); // fade out
                    samples[i] = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.5f * envelope;
                }
                _lootBeep = AudioClip.Create("LootBeep", sampleCount, 1, sampleRate, false);
                _lootBeep.SetData(samples, 0);
            }

            // Ammo beep: a distinct higher-pitched DOUBLE beep so the player can
            // instantly tell "this is ammo that fits my gun" apart from other loot.
            if (_ammoBeep == null)
            {
                const float pulse = 0.05f;
                const float gap = 0.03f;
                const float freq = 1245f;
                float duration = pulse * 2f + gap;
                int sampleCount = (int)(sampleRate * duration);
                float[] samples = new float[sampleCount];
                for (int i = 0; i < sampleCount; i++)
                {
                    float t = (float)i / sampleRate;
                    float amp = 0f;
                    if (t < pulse)
                    {
                        float env = 1f - (t / pulse);
                        amp = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f * env;
                    }
                    else if (t >= pulse + gap && t < pulse * 2f + gap)
                    {
                        float lt = t - (pulse + gap);
                        float env = 1f - (lt / pulse);
                        amp = Mathf.Sin(2f * Mathf.PI * freq * lt) * 0.5f * env;
                    }
                    samples[i] = amp;
                }
                _ammoBeep = AudioClip.Create("AmmoBeep", sampleCount, 1, sampleRate, false);
                _ammoBeep.SetData(samples, 0);
            }
        }

        private void CheckLootProximityBeep(CharacterMultiplayer player)
        {
            _lootBeepTimer -= ScanInterval;

            if (GameManager.Instance == null) return;
            var pickupsMgr = GameManager.Instance.GetComponent<PickupsManager>();
            if (pickupsMgr == null) return;

            Vector3 playerPos = player.transform.position;
            AmmoBox closestBox = null;
            float closestDist = float.MaxValue;

            foreach (var box in pickupsMgr.ammoBoxes)
            {
                if (box == null || box.items == null || box.items.Count == 0) continue;
                float dist = Vector3.Distance(playerPos, box.transform.position);
                if (dist < LootScanRadius && dist < closestDist)
                {
                    closestDist = dist;
                    closestBox = box;
                }
            }

            if (closestBox == null || closestDist >= LootScanRadius) return;

            // Beep faster when closer
            float t = Mathf.InverseLerp(LootScanRadius, 0.5f, closestDist);
            float interval = Mathf.Lerp(LootBeepMaxInterval, LootBeepMinInterval, t);

            if (_lootBeepTimer <= 0f)
            {
                _lootBeepTimer = interval;

                // Use the distinct ammo tone when this box holds ammo that fits
                // the player's current weapon; otherwise the generic loot tone.
                var inv = player.GetComponent<CharacterInventory>();
                bool hasCompatibleAmmo = BoxHasCompatibleAmmo(closestBox, inv);
                AudioClip clip = hasCompatibleAmmo ? _ammoBeep : _lootBeep;

                if (clip != null)
                {
                    // Play at the loot box's world position as 3D spatial audio
                    PlaySpatialBeep(clip, closestBox.transform.position, Mathf.Lerp(0.5f, 1f, t));
                }
            }
        }

        private void PlaySpatialBeep(AudioClip clip, Vector3 position, float volume)
        {
            // Create a temporary GameObject with an AudioSource at the loot position
            var tempObj = new GameObject("LootBeepTemp");
            tempObj.transform.position = position;
            var source = tempObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 1f; // fully 3D
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1f;
            source.maxDistance = LootScanRadius + 2f;
            source.volume = volume;
            source.Play();
            // Destroy after the clip finishes
            Object.Destroy(tempObj, clip.length + 0.1f);
        }

        /// <summary>
        /// True if the box contains any ammo item whose type matches a weapon the
        /// player currently holds (i.e. ammo they could actually load into their gun).
        /// </summary>
        private static bool BoxHasCompatibleAmmo(AmmoBox box, CharacterInventory inv)
        {
            if (box == null || box.items == null || inv == null) return false;

            foreach (var item in box.items)
            {
                if (item == null) continue;
                if (!item.type.ToString().Contains("ammo")) continue;
                if (AmmoFitsWeapon(inv.weapon1, item.type) || AmmoFitsWeapon(inv.weapon2, item.type))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Mirrors CharacterInventory.UseAmmoItem's weapon-name to ammo-type matching.
        /// </summary>
        private static bool AmmoFitsWeapon(PickupsManager.Item weapon, PickupsManager.ItemType ammoType)
        {
            if (weapon == null || string.IsNullOrEmpty(weapon.name)) return false;
            string n = weapon.name;

            if ((n.Contains("SMG") || n.Contains("Handgun")) && ammoType == PickupsManager.ItemType.ammo_smg_gun)
                return true;
            if (n.Contains("Sniper") && ammoType == PickupsManager.ItemType.ammo_sniper)
                return true;
            if (n.Contains("Assault") && ammoType == PickupsManager.ItemType.ammo_assault)
                return true;
            if (n.Contains("Shotgun") && ammoType == PickupsManager.ItemType.ammo_shotgun)
                return true;
            if ((n.Contains("Rocket Launcher") || n.Contains("Grenade Launcher")) && ammoType == PickupsManager.ItemType.ammo_grenades_launchers)
                return true;

            return false;
        }

        /// <summary>
        /// Sweeps a wide arc at long range as the player walks, announcing large
        /// vertical structures (buildings) with a direction and distance, e.g.
        /// "Building in the distance, 50 meters left". Complements the short-range
        /// wall check by giving advance warning of buildings across open ground.
        /// </summary>
        private void CheckBuildingsInDistance(CharacterMultiplayer player)
        {
            _buildingScanTimer -= ScanInterval;
            if (_buildingReannounceTimer > 0f) _buildingReannounceTimer -= ScanInterval;

            if (_buildingScanTimer > 0f) return;
            _buildingScanTimer = BuildingScanInterval;

            // Indoors, everything is close walls - skip the distance scan.
            if (_isIndoors) return;

            // Raise the origin so terrain bumps and low cover don't count.
            Vector3 origin = player.transform.position + Vector3.up * 3f;
            int mask = GetObstacleMask();

            float bestDist = float.MaxValue;
            float bestAngle = 0f;
            Transform bestHit = null;
            bool found = false;

            // Sweep a wide fan relative to where the player faces.
            for (float angle = -135f; angle <= 135f; angle += 22.5f)
            {
                Vector3 dir = Quaternion.Euler(0, angle, 0) * player.transform.forward;

                if (!Physics.Raycast(origin, dir, out RaycastHit hit, BuildingMaxDistance,
                    mask, QueryTriggerInteraction.Ignore))
                    continue;

                if (hit.distance < BuildingMinDistance) continue; // near obstacles handled elsewhere
                if (!IsLikelyBuilding(origin, dir, hit)) continue;

                if (hit.distance < bestDist)
                {
                    bestDist = hit.distance;
                    bestAngle = angle;
                    bestHit = hit.transform;
                    found = true;
                }
            }

            if (!found)
            {
                _lastBuildingAnnounce = null;
                return;
            }

            // Reuse the shared 8-way vocabulary by projecting a point in that direction.
            Vector3 target = player.transform.position
                + (Quaternion.Euler(0, bestAngle, 0) * player.transform.forward) * bestDist;
            string direction = GetRelativeDirection(player.transform, target);

            // Name it if the prefab is one we know, so this says "church" where it used
            // to say "building".
            string what = Landmarks.NameOr(bestHit, "building");

            int bucket = Mathf.RoundToInt(bestDist / BuildingDistBucket) * (int)BuildingDistBucket;
            string key = what + direction + bucket;

            // Don't repeat the same callout until the cooldown elapses.
            if (key == _lastBuildingAnnounce && _buildingReannounceTimer > 0f) return;

            _lastBuildingAnnounce = key;
            _buildingReannounceTimer = BuildingReannounceInterval;

            string prefix = bucket >= 40 ? $"{what} in the distance" : what;
            // interrupt: false so near-wall safety callouts always take priority.
            ScreenReaderManager.Speak($"{Capitalize(prefix)}, {bucket} meters {direction}", false);
        }

        /// <summary>
        /// Confirms a hit is a tall vertical face (building) rather than a slope or
        /// terrain, by casting a second ray higher and checking it hits at a similar
        /// distance. A slope's higher ray travels farther before hitting.
        /// </summary>
        private bool IsLikelyBuilding(Vector3 origin, Vector3 dir, RaycastHit lowHit)
        {
            Vector3 highOrigin = origin + Vector3.up * 4f;
            if (Physics.Raycast(highOrigin, dir, out RaycastHit highHit, lowHit.distance + 5f,
                GetObstacleMask(), QueryTriggerInteraction.Ignore))
            {
                if (Mathf.Abs(highHit.distance - lowHit.distance) < 3f)
                    return true;
            }
            return false;
        }

        private void CheckPickupConfirmation(CharacterMultiplayer player)
        {
            if (GameManager.Instance == null) return;
            var pickupsMgr = GameManager.Instance.GetComponent<PickupsManager>();
            if (pickupsMgr == null) return;

            var currentPickItem = pickupsMgr.pickItem;
            var currentPickBox = pickupsMgr.pickAmmoBox;

            // The loot list announces its own takes - it knows which item the
            // player chose, which this heuristic cannot see.
            if (LootMenu.SuppressesPickupSpeech)
            {
                _lastPickItem = null;
                _lastPickBox = null;
                return;
            }

            // Only announce when an item was actually removed from a box's items list.
            // Checking pickItem alone causes false positives when the player looks away
            // (pickItem goes null in LateUpdate even though nothing was picked up).
            if (_lastPickBox != null && _lastPickItem != null
                && _lastPickBox.items != null
                && !_lastPickBox.items.Contains(_lastPickItem))
            {
                string name = !string.IsNullOrEmpty(_lastPickItem.short_description)
                    ? _lastPickItem.short_description : _lastPickItem.description;
                ScreenReaderManager.Speak($"Picked up {name}");

                // Force re-announce remaining loot
                _lastAnnouncedBox = null;
                _lootAnnounceTimer = 0.5f;

                // Clear so we don't re-trigger
                _lastPickItem = null;
                _lastPickBox = null;
            }

            // Track current state - only update when there's a real target
            if (currentPickItem != null)
            {
                _lastPickItem = currentPickItem;
                _lastPickBox = currentPickBox;
            }
        }

        private void CheckWeaponDraw(CharacterMultiplayer player)
        {
            var charInv = player.GetComponent<CharacterInventory>();
            if (charInv == null) return;

            // The spawn pistol used to be skipped here as if it were a placeholder,
            // so a player who had not found a gun yet was never told they were
            // holding one. It is a loaded weapon and it is what they will fight with
            // until they find better.
            int slot = charInv.GetCurrentWeapon();
            var invItem = slot == 0 ? charInv.weapon1 : charInv.weapon2;
            if (invItem == null) return;

            string weaponName = ItemText.Name(invItem);
            if (weaponName == _lastWeaponName) return;

            // Announced on the first draw too - landing with a pistol in hand is
            // worth knowing, and it is the moment the player can first use it.
            ScreenReaderManager.Speak(_lastWeaponName == null ? $"{weaponName} ready" : weaponName);
            _lastWeaponName = weaponName;
        }

        /// <summary>
        /// Calls the threshold, by name where the building has one: "entered the church"
        /// rather than "indoors". Crossing into a building is a moment worth marking —
        /// it is where the walls, the loot and the danger all change at once.
        /// </summary>
        private void CheckIndoorOutdoor(CharacterMultiplayer player)
        {
            _indoorCheckTimer -= ScanInterval;
            if (_indoorCheckTimer > 0f) return;
            _indoorCheckTimer = IndoorCheckInterval;

            bool hasCeiling = HasCeiling(player, out RaycastHit _);

            // First check of a life only records where we are; there is no threshold to
            // announce when you have just landed.
            if (!_indoorStateSet)
            {
                _isIndoors = hasCeiling;
                _pendingIndoors = hasCeiling;
                _indoorStateSet = true;
                if (hasCeiling) _insideOf = BuildingHere(player);
                return;
            }

            if (hasCeiling == _isIndoors)
            {
                _pendingIndoors = hasCeiling;
                return;
            }

            // Hold the change for a second check before speaking it. A tree, an awning or
            // a container passed under flips the ceiling ray for one reading, and
            // "entered the house, left the house" a second apart is worse than silence.
            if (hasCeiling != _pendingIndoors)
            {
                _pendingIndoors = hasCeiling;
                return;
            }

            _isIndoors = hasCeiling;

            if (_isIndoors)
            {
                _insideOf = BuildingHere(player);
                ScreenReaderManager.Speak(_insideOf == null ? "Indoors" : $"Entered the {_insideOf}");
                return;
            }

            ScreenReaderManager.Speak(_insideOf == null ? "Outdoors" : $"Left the {_insideOf}");
            _insideOf = null;
        }

        /// <summary>
        /// The overhead probe the threshold callout decides on: something solid within
        /// reach above the player's head means a roof. Public so the diagnostic reports
        /// this reading rather than a lookalike of its own that could disagree with it.
        /// </summary>
        public static bool HasCeiling(CharacterMultiplayer player, out RaycastHit hit)
        {
            Vector3 origin = player.transform.position + Vector3.up * 1.5f;
            return Physics.Raycast(origin, Vector3.up, out hit, CeilingCheckHeight,
                GetObstacleMask(), QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// What building the player is standing in, or null if nothing named is close
        /// enough to be the one around them. Inside a church every wall and pillar is a
        /// collider a few metres off, so the nearest landmark is the right answer.
        /// </summary>
        private static string BuildingHere(CharacterMultiplayer player)
        {
            var nearby = Landmarks.FindNearby(player.transform.position, ThresholdSearchRadius);

            for (int i = 0; i < nearby.Count; i++)
                if (nearby[i].Bounds.Contains(player.transform.position))
                    return nearby[i].Name;

            // Not inside any footprint - a porch, an archway, a doorway half-crossed.
            // The nearest is still the building it belongs to, if it is within touching
            // distance.
            if (nearby.Count > 0 && nearby[0].Distance <= StandingInside) return nearby[0].Name;

            return null;
        }

        /// <summary>
        /// On-demand survey, bound to B. Indoors it reads the room out: which way
        /// the exits are and how far the walls are. Outdoors it sweeps the full
        /// circle for buildings, the same idea as the passive distance scanner but
        /// covering every direction at once instead of only what is ahead.
        /// </summary>
        private void AnnounceSurroundings(CharacterMultiplayer player)
        {
            Vector3 origin = player.transform.position + Vector3.up * 1.5f;
            bool indoors = Physics.Raycast(origin, Vector3.up, CeilingCheckHeight,
                GetObstacleMask(), QueryTriggerInteraction.Ignore);

            // The same query the map key asks, so anything P can name is available to
            // both branches. Indoors matters as much as out: a tree, an awning or a
            // bridge overhead is enough to take the indoor path, and the church across
            // the field is still the thing worth knowing about.
            var nearby = Landmarks.FindNearby(player.transform.position, BuildingMaxDistance);

            // Explicitly requested, so it interrupts whatever else was talking.
            ScreenReaderManager.Speak(indoors
                ? DescribeInterior(player, nearby)
                : DescribeExterior(player, nearby));
        }

        /// <summary>
        /// Reads the surrounding room: exits first, because those are what the
        /// player acts on, then the walls that box them in. A clear line flanked by
        /// close walls is a doorway; a clear line with nothing beside it is open space.
        /// </summary>
        private string DescribeInterior(CharacterMultiplayer player, List<Landmarks.Nearby> nearby)
        {
            Vector3 origin = player.transform.position + Vector3.up * 1f;
            Transform t = player.transform;
            int mask = GetObstacleMask();

            var exits = new List<string>();
            var walls = new List<string>();

            // Clockwise from straight ahead, so the report reads as a turn of the
            // head rather than starting behind the player.
            for (float sweep = 0f; sweep < 360f; sweep += 45f)
            {
                float angle = sweep > 180f ? sweep - 360f : sweep;
                Vector3 dir = Quaternion.Euler(0, angle, 0) * t.forward;
                string where = DirectionFromAngle(angle);

                if (Physics.Raycast(origin, dir, out RaycastHit hit, InteriorScanDistance,
                    mask, QueryTriggerInteraction.Ignore))
                {
                    walls.Add($"wall {where} {Mathf.RoundToInt(hit.distance)} meters");
                    continue;
                }

                bool flankedLeft = Physics.Raycast(origin,
                    Quaternion.Euler(0, angle - DoorwayProbeAngle, 0) * t.forward,
                    DoorwayProbeDistance, mask, QueryTriggerInteraction.Ignore);
                bool flankedRight = Physics.Raycast(origin,
                    Quaternion.Euler(0, angle + DoorwayProbeAngle, 0) * t.forward,
                    DoorwayProbeDistance, mask, QueryTriggerInteraction.Ignore);

                exits.Add(flankedLeft && flankedRight ? $"doorway {where}" : $"opening {where}");
            }

            var parts = new List<string> { "Indoors" };
            // Which building this is, and what else is out there, before the geometry.
            parts.AddRange(NameLandmarks(player, nearby, InteriorLandmarkCount));
            if (exits.Count == 0)
                parts.Add("no exits in reach");
            else
                parts.AddRange(exits);
            parts.AddRange(walls);

            return string.Join(". ", parts.ToArray());
        }

        /// <summary>
        /// Reports the nearest few buildings all the way round.
        ///
        /// Named landmarks come straight from the landmark query, so whatever the map key
        /// can name is in here too. This used to be sixteen rays and nothing else, which
        /// meant a church the map key was happily naming went unmentioned whenever a
        /// ridge ate the ray, whenever the building was short enough for the confirming
        /// ray to fly over it, or whenever it simply sat in one of the twenty-metre gaps
        /// between rays at that range.
        ///
        /// The sweep still runs, doing the one thing the query cannot: calling the
        /// structures no prefab name covers, which is most of the map.
        /// </summary>
        private string DescribeExterior(CharacterMultiplayer player, List<Landmarks.Nearby> nearby)
        {
            Transform t = player.transform;
            var candidates = new List<Candidate>();

            for (int i = 0; i < nearby.Count; i++)
                candidates.Add(new Candidate(nearby[i].Name, nearby[i].Distance,
                    BearingTo(t, nearby[i].Position)));

            Vector3 origin = t.position + Vector3.up * 3f;
            int mask = GetObstacleMask();

            for (float angle = -180f; angle < 180f; angle += SurveySweepStep)
            {
                Vector3 dir = Quaternion.Euler(0, angle, 0) * t.forward;

                if (!Physics.Raycast(origin, dir, out RaycastHit hit, BuildingMaxDistance,
                    mask, QueryTriggerInteraction.Ignore))
                    continue;
                // Anything with a name is already listed, at a better distance than the
                // ray happened to strike.
                if (Landmarks.TryName(hit.transform, out string _, out int _)) continue;
                if (!IsLikelyBuilding(origin, dir, hit)) continue;

                candidates.Add(new Candidate("building", hit.distance, angle));
            }

            candidates.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            var found = new List<Candidate>();
            for (int i = 0; i < candidates.Count && found.Count < SurveyMaxBuildings; i++)
            {
                // One structure claims a wide arc, so the several rays that crossed a
                // warehouse don't become three warehouses. Only its own name is claimed:
                // a house in front of the church must not silence the church.
                if (AlreadyClaimed(found, candidates[i])) continue;
                found.Add(candidates[i]);
            }

            if (found.Count == 0)
                return $"Outdoors. No buildings within {(int)BuildingMaxDistance} meters";

            var parts = new List<string> { "Outdoors" };
            for (int i = 0; i < found.Count; i++)
            {
                int metres = Mathf.RoundToInt(found[i].Distance);
                // Pressed against its wall: "house here" beats "house ahead 0 meters".
                parts.Add(metres <= StandingInside
                    ? $"{found[i].Name} here"
                    : $"{found[i].Name} {DirectionFromAngle(found[i].Bearing)} {metres} meters");
            }

            return string.Join(". ", parts.ToArray());
        }

        /// <summary>One thing the survey could mention, from the landmark query or the sweep.</summary>
        private struct Candidate
        {
            public readonly string Name;
            public readonly float Distance;
            public readonly float Bearing; // degrees from where the player faces

            public Candidate(string name, float distance, float bearing)
            {
                Name = name;
                Distance = distance;
                Bearing = bearing;
            }
        }

        private static bool AlreadyClaimed(List<Candidate> reported, Candidate candidate)
        {
            for (int i = 0; i < reported.Count; i++)
            {
                if (reported[i].Name != candidate.Name) continue;
                if (Mathf.Abs(Mathf.DeltaAngle(reported[i].Bearing, candidate.Bearing)) <= SurveyMergeArc)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Names the closest landmarks in the player's own terms, skipping repeats so
        /// three houses on one street don't use up the whole sentence.
        /// </summary>
        private static List<string> NameLandmarks(CharacterMultiplayer player,
            List<Landmarks.Nearby> nearby, int max)
        {
            var said = new List<string>();
            var names = new List<string>();

            for (int i = 0; i < nearby.Count && said.Count < max; i++)
            {
                if (names.Contains(nearby[i].Name)) continue;
                names.Add(nearby[i].Name);

                int metres = Mathf.RoundToInt(nearby[i].Distance);
                if (metres <= StandingInside)
                {
                    said.Add(nearby[i].Name);
                    continue;
                }

                said.Add($"{nearby[i].Name} " +
                         $"{GetRelativeDirection(player.transform, nearby[i].Position)} {metres} meters");
            }

            return said;
        }

        private static string GetRelativeDirection(Transform playerTransform, Vector3 targetPos)
        {
            return DirectionFromAngle(BearingTo(playerTransform, targetPos));
        }

        /// <summary>Degrees from where the player faces, ignoring height.</summary>
        private static float BearingTo(Transform playerTransform, Vector3 targetPos)
        {
            Vector3 toTarget = targetPos - playerTransform.position;
            toTarget.y = 0;

            // Standing in the doorway of the thing you asked about: call it straight ahead
            // rather than letting SignedAngle guess off a zero-length vector.
            if (toTarget.sqrMagnitude < 0.01f) return 0f;

            return Vector3.SignedAngle(playerTransform.forward, toTarget, Vector3.up);
        }

        /// <summary>First letter up, for a landmark name that has to start a sentence.</summary>
        private static string Capitalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return char.ToUpper(text[0]) + text.Substring(1);
        }

        /// <summary>Shared 8-way vocabulary, for a bearing relative to player forward.</summary>
        private static string DirectionFromAngle(float angle)
        {
            if (angle >= -22.5f && angle < 22.5f) return "ahead";
            if (angle >= 22.5f && angle < 67.5f) return "front right";
            if (angle >= 67.5f && angle < 112.5f) return "right";
            if (angle >= 112.5f && angle < 157.5f) return "behind right";
            if (angle >= -67.5f && angle < -22.5f) return "front left";
            if (angle >= -112.5f && angle < -67.5f) return "left";
            if (angle >= -157.5f && angle < -112.5f) return "behind left";
            return "behind";
        }

        private static int GetObstacleMask()
        {
            // Use Default layer + any other solid layers
            // Ignore triggers, players, pickups
            // Layer 0 = Default (buildings, terrain, walls)
            // We also cast against common solid layers
            int mask = 1 << 0; // Default
            int ground = LayerMask.NameToLayer("Ground");
            if (ground >= 0) mask |= 1 << ground;
            int terrain = LayerMask.NameToLayer("Terrain");
            if (terrain >= 0) mask |= 1 << terrain;
            int building = LayerMask.NameToLayer("Building");
            if (building >= 0) mask |= 1 << building;
            int env = LayerMask.NameToLayer("Environment");
            if (env >= 0) mask |= 1 << env;
            return mask;
        }
    }
}
