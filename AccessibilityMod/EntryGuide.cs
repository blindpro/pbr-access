using UnityEngine;
using UnityEngine.AI;
using InfimaGames.LowPolyShooterPack;

namespace AccessibilityMod
{
    /// <summary>
    /// Press Q to be walked into the nearest building. Press it again to stop.
    ///
    /// Finding a building was solved; getting through its door was not. A doorway is a
    /// metre-wide gap in a wall that every callout we have steps straight past, and
    /// hunting for one by ear means walking a wall until it stops.
    ///
    /// So we do not look for the door at all. The scene has a baked NavMesh, the bake
    /// covers interiors, and it is carved around the walls - measured, see map.md - which
    /// means a route to a point inside a building has no choice but to pass through the
    /// entrance. Ask for that route and the corner it turns at *is* the doorway. The game
    /// has been telling its bots where the doors are since launch.
    ///
    /// Guidance is a tone at the next corner rather than a stream of speech. Walking onto
    /// a sound is more precise than steering off "front left", and it leaves the screen
    /// reader free for the wall and enemy callouts that matter more.
    /// </summary>
    public class EntryGuide
    {
        // Q is free in both the game's bindings and the mod's. If it ever collides,
        // this is the only line that needs to change.
        private const KeyCode GuideKey = KeyCode.Q;

        private const float SearchRadius = 60f;

        // At your feet the mesh is either under you or it is not; hunting for an interior
        // floor is a different question and gets a wider budget.
        private const float FootSampleRadius = 2f;
        private const float InteriorSampleRadius = 20f;

        // Often enough to keep up with a running player, rare enough that pathfinding
        // stays cheap. The bots recalculate far more than this.
        private const float RecalcInterval = 0.4f;

        // A corner this close is behind you already; aim at the next one.
        private const float CornerReached = 2.5f;

        // Re-speak only on a real change, so the tone does the fine work.
        private const float SpokenDistanceStep = 5f;

        private const float BeepFarDistance = 30f;
        private const float BeepMaxInterval = 1.2f;
        private const float BeepMinInterval = 0.25f;

        private bool _active;
        private string _targetName;
        private Bounds _targetFootprint;
        private Vector3 _targetInside;

        private Vector3 _corner;
        private bool _haveCorner;
        private bool _routeShort;
        private bool _saidRouteShort;

        private string _spokenDirection;
        private float _spokenDistance;

        private float _recalcTimer;
        private float _beepTimer;
        private AudioClip _doorBeep;

        public void Tick()
        {
            var player = CharacterMultiplayer.GetMainPlayer();

            if (player == null || player.IsDead())
            {
                _active = false;
                return;
            }

            if (MatchmakingManager.Instance == null) return;
            if (MatchmakingManager.Instance.GetRoomStatus() != MatchmakingManager.RoomStatus.Playing) return;

            if (Input.GetKeyDown(GuideKey)) Toggle(player);
            if (!_active) return;

            // Arrival. The footprint is what decides it, the same test the threshold
            // callout uses, so "inside the shop" and "entered the shop" cannot disagree.
            if (Landmarks.IsInside(_targetFootprint, player.transform.position))
            {
                ScreenReaderManager.Speak($"Inside the {_targetName}");
                _active = false;
                return;
            }

            _recalcTimer -= Time.deltaTime;
            if (_recalcTimer <= 0f)
            {
                _recalcTimer = RecalcInterval;
                Recalculate(player);
            }

            if (_haveCorner) Beep(player);
        }

        private void Toggle(CharacterMultiplayer player)
        {
            if (_active)
            {
                _active = false;
                ScreenReaderManager.Speak("Guide off");
                return;
            }

            Start(player);
        }

        /// <summary>
        /// Picks the nearest building we can actually get into and starts guiding.
        ///
        /// Nearest is not always enterable - a trailer is a sealed prop with no floor
        /// inside it - so the candidates are walked in order and the first one with
        /// walkable ground inside its own footprint wins. That check is exactly what the
        /// diagnostic measured, and it is what stops the guide marching someone across a
        /// field towards a building with no way in.
        /// </summary>
        private void Start(CharacterMultiplayer player)
        {
            Vector3 position = player.transform.position;
            var nearby = Landmarks.FindNearby(position, SearchRadius);

            if (nearby.Count == 0)
            {
                ScreenReaderManager.Speak($"No building within {(int)SearchRadius} meters");
                return;
            }

            for (int i = 0; i < nearby.Count; i++)
            {
                if (Landmarks.IsInside(nearby[i].Bounds, position))
                {
                    ScreenReaderManager.Speak($"You are already in the {nearby[i].Name}");
                    return;
                }

                if (!TryFindWayIn(nearby[i], out Vector3 inside)) continue;

                _targetName = nearby[i].Name;
                _targetFootprint = nearby[i].Bounds;
                _targetInside = inside;
                _active = true;
                _haveCorner = false;
                _routeShort = false;
                _saidRouteShort = false;
                _spokenDirection = null;
                _spokenDistance = float.MaxValue;
                _recalcTimer = RecalcInterval;
                _beepTimer = 0f;

                if (_doorBeep == null)
                {
                    // Lower and longer than the loot tone, and single where the ammo tone
                    // is a double, so three sounds in the same field stay distinct.
                    _doorBeep = SpatialBeep.Tone("DoorBeep", 660f, 0.2f);
                }

                ScreenReaderManager.Speak($"Guiding into the {_targetName}, "
                    + $"{Mathf.RoundToInt(nearby[i].Distance)} meters "
                    + $"{Bearings.Relative(player.transform, nearby[i].Position)}");

                Recalculate(player);
                return;
            }

            ScreenReaderManager.Speak("No way in to anything nearby");
        }

        /// <summary>
        /// Walkable ground inside the building, or nothing. Sampling near the middle of
        /// the footprint at floor height finds an interior floor if one is baked; if the
        /// nearest walkable point comes back outside the footprint, the building is solid
        /// and there is nothing to walk into.
        /// </summary>
        private static bool TryFindWayIn(Landmarks.Nearby building, out Vector3 inside)
        {
            inside = Vector3.zero;

            // The floor, not the roof: the centre of a box that includes the roof can sit
            // in mid-air.
            Vector3 middle = building.Bounds.center;
            middle.y = building.Bounds.min.y + 1f;

            if (!NavMesh.SamplePosition(middle, out NavMeshHit hit, InteriorSampleRadius, NavMesh.AllAreas))
                return false;

            if (!Landmarks.IsInside(building.Bounds, hit.position)) return false;

            inside = hit.position;
            return true;
        }

        /// <summary>
        /// Re-asks for the route and aims at the first corner that is still ahead of us.
        /// Recalculating beats stepping through a stored corner list: the player wanders,
        /// gets shot at, backs out and comes round the other side, and a fresh route is
        /// always about where they are now.
        /// </summary>
        private void Recalculate(CharacterMultiplayer player)
        {
            Vector3 position = player.transform.position;

            if (!NavMesh.SamplePosition(position, out NavMeshHit standingOn, FootSampleRadius, NavMesh.AllAreas)
                && !NavMesh.SamplePosition(position, out standingOn, InteriorSampleRadius, NavMesh.AllAreas))
            {
                ScreenReaderManager.Speak("Guide lost, no path from here");
                _active = false;
                return;
            }

            var path = new NavMeshPath();
            bool routed = NavMesh.CalculatePath(standingOn.position, _targetInside,
                NavMesh.AllAreas, path);

            if (!routed || path.status == NavMeshPathStatus.PathInvalid || path.corners.Length < 2)
            {
                ScreenReaderManager.Speak($"No route into the {_targetName}");
                _active = false;
                return;
            }

            // A partial route is worth following - it still walks you to the building -
            // but the player should be told once that it may not reach the way in.
            _routeShort = path.status == NavMeshPathStatus.PathPartial;
            if (_routeShort && !_saidRouteShort)
            {
                _saidRouteShort = true;
                ScreenReaderManager.Speak("Route may not reach all the way in", false);
            }

            _corner = path.corners[path.corners.Length - 1];
            _haveCorner = true;

            // The first corner still in front of us. Corners we have already walked past
            // are skipped rather than tracked, which is what keeps this stateless.
            for (int i = 1; i < path.corners.Length; i++)
            {
                if (Vector3.Distance(position, path.corners[i]) <= CornerReached) continue;

                _corner = path.corners[i];
                break;
            }

            Announce(player);
        }

        /// <summary>
        /// Speaks the corner only when it has actually changed. The tone is doing the
        /// steering; speech here is for the moment the direction changes, which is the
        /// moment a player needs to know they are turning.
        /// </summary>
        private void Announce(CharacterMultiplayer player)
        {
            string direction = Bearings.Relative(player.transform, _corner);
            float distance = Vector3.Distance(player.transform.position, _corner);

            bool turned = direction != _spokenDirection;
            bool closed = Mathf.Abs(distance - _spokenDistance) >= SpokenDistanceStep;
            if (!turned && !closed) return;

            _spokenDirection = direction;
            _spokenDistance = distance;

            // interrupt: false, so a wall or an enemy callout always wins over this.
            ScreenReaderManager.Speak($"{direction}, {Mathf.RoundToInt(distance)} meters", false);
        }

        private void Beep(CharacterMultiplayer player)
        {
            _beepTimer -= Time.deltaTime;
            if (_beepTimer > 0f) return;

            float distance = Vector3.Distance(player.transform.position, _corner);
            float closeness = Mathf.InverseLerp(BeepFarDistance, 1f, distance);

            _beepTimer = Mathf.Lerp(BeepMaxInterval, BeepMinInterval, closeness);

            SpatialBeep.PlayAt(_doorBeep, _corner, Mathf.Lerp(0.6f, 1f, closeness),
                BeepFarDistance + 5f);
        }
    }
}
