using UnityEngine;
using InfimaGames.LowPolyShooterPack;

namespace AccessibilityMod
{
    /// <summary>
    /// Gives the map spoken coordinates so it can be learned and talked about.
    /// - P key: current cell and the nearest landmark (P for position; M opens the
    ///   game's own pause menu)
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
        /// <summary>
        /// Singleton instance of the active MapGrid.
        /// </summary>
        public static MapGrid Instance { get; private set; }

        /// <summary>
        /// Initializes a new instance of the MapGrid class.
        /// </summary>
        public MapGrid()
        {
            Instance = this;
        }

        // 10 x 10, so a ~2100 metre map gives ~210 metre cells: coarse enough to name a
        // drop, fine enough that "D2" means one place.
        private const int Columns = 10;
        private const int Rows = 10;

        // How far inside a new cell you must be before it is announced. Without this,
        // walking a boundary ping-pongs between two names.
        private const float CellEntryMargin = 12f;

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

            if (!Input.GetKeyDown(KeyCode.P)) return;
            if (_onDemandCooldownTimer > 0f) return;

            _onDemandCooldownTimer = OnDemandCooldown;

            string cell = GetCell(focus);
            if (cell == null)
            {
                ScreenReaderManager.Speak("Off the map grid");
                return;
            }

            string report = cell;

            string landmark = Landmarks.DescribeNearest(focus, withDistance: true);
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
            if (Plugin.IsGameplayWarmingUp) return;

            string cell = GetCell(focus);
            if (cell == null || cell == _announcedCell) return;
            if (DistanceInsideCell(focus) < CellEntryMargin) return;

            _announcedCell = cell;

            string landmark = Landmarks.DescribeNearest(focus, withDistance: false);
            string report = landmark == null ? cell : $"{cell}, {landmark}";

            // interrupt: false so close-range wall and enemy callouts win.
            ScreenReaderManager.Speak(report, false);
        }

        // ---------------------------------------------------------------- grid

        /// <summary>
        /// A1 is the south west corner: columns run west to east as A to J, rows count
        /// up as you walk north. Numbers rising northward matches the compass the F key
        /// already speaks, so "heading to D8" and "heading north" agree.
        /// </summary>
        /// <summary>
        /// Gets the coordinate grid cell for a given 3D position.
        /// </summary>
        /// <param name="worldPos">The 3D position in the world.</param>
        /// <returns>The cell name (e.g. "D4") or null if off bounds.</returns>
        public string GetCell(Vector3 worldPos)
        {
            if (!EnsureBounds()) return null;

            float size = CellSize();
            float minX = _mapCenter.x - _mapHalfExtent;
            float minZ = _mapCenter.z - _mapHalfExtent;

            int col = Mathf.FloorToInt((worldPos.x - minX) / size);
            int row = Mathf.FloorToInt((worldPos.z - minZ) / size);

            if (col < 0 || col >= Columns || row < 0 || row >= Rows) return null;

            return $"{(char)('A' + col)}{row + 1}";
        }

        /// <summary>Metres from the nearest edge of the cell you're standing in.</summary>
        private float DistanceInsideCell(Vector3 worldPos)
        {
            float size = CellSize();
            float minX = _mapCenter.x - _mapHalfExtent;
            float minZ = _mapCenter.z - _mapHalfExtent;

            float intoColumn = Mathf.Repeat(worldPos.x - minX, size);
            float intoRow = Mathf.Repeat(worldPos.z - minZ, size);

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
    }
}
