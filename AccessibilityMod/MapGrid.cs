using UnityEngine;
using InfimaGames.LowPolyShooterPack;

namespace AccessibilityMod
{
    /// <summary>
    /// Gives the map spoken coordinates so it can be learned and talked about.
    /// - M key: current cell and the nearest landmark
    /// - Auto-announces the cell as you cross into it
    ///
    /// The map has no authored place names (no NewLocationTrigger survives in the
    /// battle royale scene), so cells come from the same rectangle the game's own big
    /// map camera frames, and landmark names are read off the Synty prefab names of
    /// whatever is standing nearby. See map.md.
    ///
    /// Deliberately says nothing about loot. A square's box count is knowable and
    /// fixed, but a sighted player can't see it either, so speaking it would hand out
    /// an advantage rather than close a gap.
    /// </summary>
    public class MapGrid
    {
        // 10 x 10, so a ~2100 metre map gives ~210 metre cells: coarse enough to name a
        // drop, fine enough that "D2" means one place.
        private const int Columns = 10;
        private const int Rows = 10;

        // How far inside a new cell you must be before it is announced. Without this,
        // walking a boundary ping-pongs between two names.
        private const float CellEntryMargin = 12f;

        // Landmarks are only worth naming while they are the thing you'd walk to.
        private const float LandmarkSearchRadius = 80f;
        private const float LandmarkNearbyDistance = 40f;

        private const float OnDemandCooldown = 1f;
        private float _onDemandCooldownTimer;

        private string _announcedCell;

        // Map rectangle, resolved once per match off the game's own map camera.
        private bool _boundsResolved;
        private Vector3 _mapCenter;
        private float _mapHalfExtent;

        public void Tick()
        {
            var player = CharacterMultiplayer.GetMainPlayer();
            if (player == null || player.IsDead())
            {
                _announcedCell = null;
                return;
            }

            if (MatchmakingManager.Instance == null) return;
            if (MatchmakingManager.Instance.GetRoomStatus() != MatchmakingManager.RoomStatus.Playing) return;

            if (!EnsureBounds()) return;

            Vector3 focus = GetFocusPosition(player);

            HandleOnDemand(focus);
            MonitorCellChange(focus);
        }

        /// <summary>
        /// What the readout is about. On the plane that is the plane, not the character:
        /// the character isn't moved until it jumps, and the squares the plane is
        /// crossing are the whole reason to ask.
        /// </summary>
        private static Vector3 GetFocusPosition(CharacterMultiplayer player)
        {
            var parachute = player.GetComponent<CharacterParachute>();
            if (parachute != null && parachute.isOnAirplane && GameManager.Instance != null)
            {
                var airplaneManager = GameManager.Instance.GetComponent<AirplaneManager>();
                if (airplaneManager != null && airplaneManager.Airplane != null)
                    return airplaneManager.Airplane.transform.position;
            }

            return player.transform.position;
        }

        private void HandleOnDemand(Vector3 focus)
        {
            _onDemandCooldownTimer -= Time.deltaTime;

            if (!Input.GetKeyDown(KeyCode.M)) return;
            if (_onDemandCooldownTimer > 0f) return;

            _onDemandCooldownTimer = OnDemandCooldown;

            string cell = GetCell(focus);
            if (cell == null)
            {
                ScreenReaderManager.Speak("Off the map grid");
                return;
            }

            string report = cell;

            string landmark = DescribeNearestLandmark(focus, withDistance: true);
            if (landmark != null)
                report += ". " + landmark;

            ScreenReaderManager.Speak(report);
        }

        /// <summary>
        /// Calls the new square as you enter it, held back until you are properly inside
        /// so that walking a boundary doesn't chatter.
        /// </summary>
        private void MonitorCellChange(Vector3 focus)
        {
            string cell = GetCell(focus);
            if (cell == null || cell == _announcedCell) return;
            if (DistanceInsideCell(focus) < CellEntryMargin) return;

            _announcedCell = cell;

            string landmark = DescribeNearestLandmark(focus, withDistance: false);
            string report = landmark == null ? cell : $"{cell}, {landmark}";

            // interrupt: false so close-range wall and enemy callouts win.
            ScreenReaderManager.Speak(report, false);
        }

        // ---------------------------------------------------------------- grid

        /// <summary>
        /// A1 is the north west corner: columns run west to east, rows run north to
        /// south, matching how the big map is drawn and how the F key already calls
        /// north.
        /// </summary>
        private string GetCell(Vector3 worldPos)
        {
            float size = CellSize();
            float minX = _mapCenter.x - _mapHalfExtent;
            float maxZ = _mapCenter.z + _mapHalfExtent;

            int col = Mathf.FloorToInt((worldPos.x - minX) / size);
            int row = Mathf.FloorToInt((maxZ - worldPos.z) / size);

            if (col < 0 || col >= Columns || row < 0 || row >= Rows) return null;

            return $"{(char)('A' + col)}{row + 1}";
        }

        /// <summary>Metres from the nearest edge of the cell you're standing in.</summary>
        private float DistanceInsideCell(Vector3 worldPos)
        {
            float size = CellSize();
            float minX = _mapCenter.x - _mapHalfExtent;
            float maxZ = _mapCenter.z + _mapHalfExtent;

            float intoColumn = Mathf.Repeat(worldPos.x - minX, size);
            float intoRow = Mathf.Repeat(maxZ - worldPos.z, size);

            return Mathf.Min(
                Mathf.Min(intoColumn, size - intoColumn),
                Mathf.Min(intoRow, size - intoRow));
        }

        private float CellSize()
        {
            return _mapHalfExtent * 2f / Columns;
        }

        /// <summary>
        /// The playable rectangle. The big map camera is the honest source: it is the
        /// orthographic camera the game renders the full-screen map with, so our squares
        /// line up with the map a sighted player is looking at. The starting damage zone
        /// is the fallback, since it always exists and always covers the play area.
        /// </summary>
        private bool EnsureBounds()
        {
            if (_boundsResolved) return true;
            if (GameManager.Instance == null) return false;

            var mapCamera = GameManager.Instance.bigMapCamera;
            if (mapCamera != null && mapCamera.orthographic && mapCamera.orthographicSize > 1f)
            {
                _mapCenter = mapCamera.transform.position;
                _mapHalfExtent = mapCamera.orthographicSize;
                _boundsResolved = true;
                return true;
            }

            var zoneMgr = GameManager.Instance.GetComponent<DamageZoneManager>();
            var defaultZone = zoneMgr == null ? null : zoneMgr.damageZoneDefault;
            if (defaultZone != null)
            {
                var capsule = defaultZone.GetComponent<CapsuleCollider>();
                if (capsule != null)
                {
                    _mapCenter = capsule.bounds.center;
                    _mapHalfExtent = capsule.bounds.extents.x;
                    _boundsResolved = true;
                    return true;
                }
            }

            return false;
        }

        // ----------------------------------------------------------- landmarks

        /// <summary>
        /// Names the most distinctive building standing near a point, e.g. "church" or
        /// "radio tower". Nothing in the scene is labelled, so this reads the Synty
        /// prefab names off the colliders around you.
        /// </summary>
        private static string DescribeNearestLandmark(Vector3 worldPos, bool withDistance)
        {
            // From the plane or under an open parachute we are hundreds of metres up,
            // where a sphere of this size touches nothing. Ask at ground level instead,
            // so the answer is about the place below us rather than the empty air.
            worldPos = DropToGround(worldPos);

            var hits = Physics.OverlapSphere(worldPos, LandmarkSearchRadius, GetLandmarkMask(),
                QueryTriggerInteraction.Ignore);

            string bestName = null;
            int bestRank = int.MaxValue;
            float bestDistance = float.MaxValue;
            Vector3 bestPos = Vector3.zero;

            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit == null) continue;

                if (!TryNameLandmark(hit.transform, out string name, out int rank)) continue;

                float distance = Vector3.Distance(worldPos, hit.transform.position);

                // A landmark that says more wins outright; ties go to the closer one.
                if (rank > bestRank) continue;
                if (rank == bestRank && distance >= bestDistance) continue;

                bestName = name;
                bestRank = rank;
                bestDistance = distance;
                bestPos = hit.transform.position;
            }

            if (bestName == null) return null;

            if (!withDistance)
                return bestDistance <= LandmarkNearbyDistance ? bestName : null;

            int metres = Mathf.RoundToInt(bestDistance);
            string cardinal = GetCardinalTo(worldPos, bestPos);
            return $"{bestName} {metres} meters {cardinal}";
        }

        /// <summary>
        /// Walks a collider and its parents looking for a known building prefab. Names
        /// like SM_Bld_Church_01_Glass mean the collider itself is often a window or a
        /// door, so the useful name can be a level or two up.
        /// </summary>
        private static bool TryNameLandmark(Transform transform, out string name, out int rank)
        {
            name = null;
            rank = int.MaxValue;

            Transform current = transform;
            for (int depth = 0; depth < 3 && current != null; depth++)
            {
                string objectName = current.name;
                for (int i = 0; i < Landmarks.Length; i++)
                {
                    if (objectName.IndexOf(Landmarks[i].Token, System.StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    // Landmarks is ordered most distinctive first, so the first hit at
                    // this depth is the best name this object can give.
                    name = Landmarks[i].Spoken;
                    rank = i;
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private struct Landmark
        {
            public readonly string Token;
            public readonly string Spoken;

            public Landmark(string token, string spoken)
            {
                Token = token;
                Spoken = spoken;
            }
        }

        /// <summary>
        /// Ordered most distinctive first: a church is worth calling from across a
        /// square, a house is only worth mentioning when nothing better is standing
        /// there. Tokens are the Synty prefab names found in the scene.
        /// </summary>
        private static readonly Landmark[] Landmarks =
        {
            new Landmark("Lighthouse", "lighthouse"),
            new Landmark("Church", "church"),
            new Landmark("RadioTower", "radio tower"),
            new Landmark("Cooling_Tower", "cooling tower"),
            new Landmark("SmokeStack", "smokestack"),
            new Landmark("WaterTower", "water tower"),
            new Landmark("Crane", "crane"),
            new Landmark("HeliPad", "helipad"),
            new Landmark("Bunker_Entrance", "bunker"),
            new Landmark("ContainerBridge", "container bridge"),
            new Landmark("Diner", "diner"),
            new Landmark("Motel", "motel"),
            new Landmark("Cafe", "cafe"),
            new Landmark("AutoRepair", "auto repair shop"),
            new Landmark("Pool", "swimming pool"),
            new Landmark("Quarantine", "quarantine tent"),
            new Landmark("Military_Tent", "military tent"),
            new Landmark("WaterTank", "water tank"),
            new Landmark("Market_Large", "big market"),
            new Landmark("Market", "market"),
            new Landmark("Warehouse", "warehouse"),
            new Landmark("HighRise", "high rise"),
            new Landmark("Industrial", "industrial building"),
            new Landmark("Commercial", "commercial building"),
            new Landmark("Shop", "shop"),
            new Landmark("Apartment", "apartment block"),
            new Landmark("House_Burnt", "burnt house"),
            new Landmark("Junk_Shelter", "shack"),
            new Landmark("Trailer", "trailer"),
            new Landmark("House", "house"),
        };

        /// <summary>
        /// Straight down to whatever is underneath, so a query made from the air is
        /// about the ground it would land on. Stays put if there's nothing below.
        /// </summary>
        private static Vector3 DropToGround(Vector3 worldPos)
        {
            const float maxDrop = 2000f;

            if (Physics.Raycast(worldPos, Vector3.down, out RaycastHit hit, maxDrop,
                    GetLandmarkMask(), QueryTriggerInteraction.Ignore))
                return hit.point + Vector3.up;

            return worldPos;
        }

        private static int GetLandmarkMask()
        {
            int mask = 1 << 0; // Default: where the Synty buildings sit
            int building = LayerMask.NameToLayer("Building");
            if (building >= 0) mask |= 1 << building;
            int env = LayerMask.NameToLayer("Environment");
            if (env >= 0) mask |= 1 << env;
            return mask;
        }

        /// <summary>World compass bearing, matching the F key's reading of north.</summary>
        private static string GetCardinalTo(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.01f) return "here";

            float yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            yaw = ((yaw % 360f) + 360f) % 360f;

            if (yaw >= 337.5f || yaw < 22.5f) return "north";
            if (yaw < 67.5f) return "north east";
            if (yaw < 112.5f) return "east";
            if (yaw < 157.5f) return "south east";
            if (yaw < 202.5f) return "south";
            if (yaw < 247.5f) return "south west";
            if (yaw < 292.5f) return "west";
            return "north west";
        }
    }
}
