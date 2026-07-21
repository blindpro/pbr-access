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
        private float _lootBeepTimer;
        private const float LootBeepMaxInterval = 1.5f; // far away beep rate
        private const float LootBeepMinInterval = 0.3f; // close beep rate

        // Indoor/outdoor detection
        private const float CeilingCheckHeight = 20f;
        private bool _isIndoors;
        private bool _indoorStateSet;
        private const float IndoorCheckInterval = 1f;
        private float _indoorCheckTimer;

        // Weapon draw tracking
        private string _lastWeaponName;

        // Scan timing
        private const float ScanInterval = 0.15f;
        private float _scanTimer;

        public void Tick()
        {
            var player = CharacterMultiplayer.GetMainPlayer();
            if (player == null || player.IsDead()) return;

            // Only run during gameplay
            if (MatchmakingManager.Instance == null) return;
            var status = MatchmakingManager.Instance.GetRoomStatus();
            if (status != MatchmakingManager.RoomStatus.Playing) return;

            // Skip during parachuting
            var parachute = player.GetComponent<CharacterParachute>();
            if (parachute != null && parachute.isParachuting) return;

            _scanTimer -= Time.deltaTime;
            if (_scanTimer > 0f) return;
            _scanTimer = ScanInterval;

            EnsureLootBeep(player);
            CheckWallAhead(player);
            CheckDoorways(player);
            CheckNearbyLoot(player);
            CheckLootProximityBeep(player);
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

        private void EnsureLootBeep(CharacterMultiplayer player)
        {
            if (_lootBeep != null) return;

            // Generate a short beep tone for loot proximity
            int sampleRate = 44100;
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
                if (_lootBeep != null)
                {
                    // Play at the loot box's world position as 3D spatial audio
                    PlaySpatialBeep(closestBox.transform.position, Mathf.Lerp(0.5f, 1f, t));
                }
            }
        }

        private void PlaySpatialBeep(Vector3 position, float volume)
        {
            // Create a temporary GameObject with an AudioSource at the loot position
            var tempObj = new GameObject("LootBeepTemp");
            tempObj.transform.position = position;
            var source = tempObj.AddComponent<AudioSource>();
            source.clip = _lootBeep;
            source.spatialBlend = 1f; // fully 3D
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1f;
            source.maxDistance = LootScanRadius + 2f;
            source.volume = volume;
            source.Play();
            // Destroy after the clip finishes
            Object.Destroy(tempObj, _lootBeep.length + 0.1f);
        }

        private void CheckPickupConfirmation(CharacterMultiplayer player)
        {
            if (GameManager.Instance == null) return;
            var pickupsMgr = GameManager.Instance.GetComponent<PickupsManager>();
            if (pickupsMgr == null) return;

            var currentPickItem = pickupsMgr.pickItem;
            var currentPickBox = pickupsMgr.pickAmmoBox;

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

            // Use CharacterInventory to get the actual weapon the player picked up
            // Skip the default "Handgun 01" that the game always assigns
            bool hasRealWeapon = charInv.weapon1 != null && charInv.weapon1.name != "Handgun 01";
            bool hasWeapon2 = charInv.weapon2 != null;
            if (!hasRealWeapon && !hasWeapon2) return;

            int slot = charInv.GetCurrentWeapon();
            var invItem = slot == 0 ? charInv.weapon1 : charInv.weapon2;
            if (invItem == null) return;

            string weaponName = !string.IsNullOrEmpty(invItem.short_description)
                ? invItem.short_description : invItem.name;

            if (weaponName != _lastWeaponName)
            {
                if (_lastWeaponName != null)
                    ScreenReaderManager.Speak(weaponName);

                _lastWeaponName = weaponName;
            }
        }

        private void CheckIndoorOutdoor(CharacterMultiplayer player)
        {
            _indoorCheckTimer -= ScanInterval;
            if (_indoorCheckTimer > 0f) return;
            _indoorCheckTimer = IndoorCheckInterval;

            Vector3 origin = player.transform.position + Vector3.up * 1.5f;

            // Cast upward to detect a ceiling
            bool hasCeiling = Physics.Raycast(origin, Vector3.up, out RaycastHit _, CeilingCheckHeight,
                GetObstacleMask(), QueryTriggerInteraction.Ignore);

            if (hasCeiling != _isIndoors || !_indoorStateSet)
            {
                _isIndoors = hasCeiling;

                if (_indoorStateSet) // Don't announce on first check
                {
                    if (_isIndoors)
                        ScreenReaderManager.Speak("Indoors");
                    else
                        ScreenReaderManager.Speak("Outdoors");
                }
                _indoorStateSet = true;
            }
        }

        private static string GetRelativeDirection(Transform playerTransform, Vector3 targetPos)
        {
            Vector3 toTarget = targetPos - playerTransform.position;
            toTarget.y = 0;

            float angle = Vector3.SignedAngle(playerTransform.forward, toTarget, Vector3.up);

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
