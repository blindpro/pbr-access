using UnityEngine;
using UnityEngine.AI;
using InfimaGames.LowPolyShooterPack;

namespace AccessibilityMod
{
    /// <summary>
    /// Press Q to be walked through the nearest door: in from outside, and back out again
    /// when you are already inside. Press it again to stop.
    ///
    /// Finding a building was solved; getting through its door was not. A doorway is a
    /// metre-wide gap in a wall that every callout we have steps straight past, and
    /// hunting for one by ear means walking a wall until it stops. Getting back out is the
    /// same problem from the other side, and worse for being a room you cannot see the
    /// shape of.
    ///
    /// So we do not look for the door at all. The scene has a baked NavMesh, the bake
    /// covers interiors, and it is carved around the walls - measured, see map.md - which
    /// means a route between a point inside a building and a point outside it has no
    /// choice but to pass through the entrance. Ask for that route and the corner it turns
    /// at *is* the doorway. The game has been telling its bots where the doors are since
    /// launch.
    ///
    /// Guidance is a tone at the next corner rather than a stream of speech. Walking onto
    /// a sound is more precise than steering off "front left", and it leaves the screen
    /// reader free for the wall and enemy callouts that matter more.
    /// </summary>
    public class DoorGuide
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

        // Where to look for open ground when leaving: far enough out that the target is
        // clear of the walls, close enough that it is still this building's doorstep.
        private const float WayOutClearance = 6f;
        private const int WayOutBearings = 8;

        private bool _active;
        private bool _leaving;
        private string _targetName;
        private Bounds _targetFootprint;
        private Vector3 _goal;

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
            if (Landmarks.IsInside(_targetFootprint, player.transform.position) != _leaving)
            {
                ScreenReaderManager.Speak(_leaving
                    ? $"Out of the {_targetName}"
                    : $"Inside the {_targetName}");
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
        /// Inside a building, the way out is what you want; outside one, the way in.
        /// Which of those you are asking for is never ambiguous - you are either standing
        /// in a footprint or you are not - so the key does not need to be two keys.
        /// </summary>
        private void Start(CharacterMultiplayer player)
        {
            Vector3 position = player.transform.position;

            if (Landmarks.TryFindContaining(position, SearchRadius, out Landmarks.Nearby around))
            {
                StartLeaving(player, around);
                return;
            }

            StartEntering(player, position);
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
        private void StartEntering(CharacterMultiplayer player, Vector3 position)
        {
            var nearby = Landmarks.FindNearby(position, SearchRadius);

            if (nearby.Count == 0)
            {
                ScreenReaderManager.Speak($"No building within {(int)SearchRadius} meters");
                return;
            }

            for (int i = 0; i < nearby.Count; i++)
            {
                if (!TryFindWayIn(nearby[i], out Vector3 inside)) continue;

                Begin(player, nearby[i], inside, leaving: false);

                ScreenReaderManager.Speak($"Guiding into the {_targetName}, "
                    + $"{Mathf.RoundToInt(nearby[i].Distance)} meters "
                    + $"{Bearings.Relative(player.transform, nearby[i].Position)}");

                Recalculate(player);
                return;
            }

            ScreenReaderManager.Speak("No way in to anything nearby");
        }

        /// <summary>
        /// Guides back out of the building you are standing in, to the nearest open ground
        /// beyond its walls - which means back through the door you came in by, or a
        /// nearer one if the room has two.
        /// </summary>
        private void StartLeaving(CharacterMultiplayer player, Landmarks.Nearby around)
        {
            if (!TryFindWayOut(player, around, out Vector3 outside))
            {
                ScreenReaderManager.Speak($"No way out of the {around.Name} found");
                return;
            }

            Begin(player, around, outside, leaving: true);

            ScreenReaderManager.Speak($"Guiding out of the {_targetName}");
            Recalculate(player);
        }

        private void Begin(CharacterMultiplayer player, Landmarks.Nearby building,
            Vector3 goal, bool leaving)
        {
            _targetName = building.Name;
            _targetFootprint = building.Bounds;
            _goal = goal;
            _leaving = leaving;

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
                // Lower and longer than the loot tone, and single where the ammo tone is
                // a double, so three sounds in the same field stay distinct.
                _doorBeep = SpatialBeep.Tone("DoorBeep", 660f, 0.2f);
            }
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
        /// Open ground outside the building, picked by which one is actually the shortest
        /// walk rather than which is nearest as the crow flies. A ring of candidates round
        /// the outside of the footprint is tried, and the one with the shortest complete
        /// route wins - so a player at the back of a church is sent to the door they can
        /// reach, not to the patch of grass on the other side of the wall beside them.
        /// </summary>
        private static bool TryFindWayOut(CharacterMultiplayer player, Landmarks.Nearby building,
            out Vector3 outside)
        {
            outside = Vector3.zero;

            if (!NavMesh.SamplePosition(player.transform.position, out NavMeshHit standingOn,
                    FootSampleRadius, NavMesh.AllAreas))
                return false;

            float reach = Mathf.Max(building.Bounds.extents.x, building.Bounds.extents.z)
                          + WayOutClearance;
            float best = float.MaxValue;
            var path = new NavMeshPath();

            for (int i = 0; i < WayOutBearings; i++)
            {
                float angle = i * (360f / WayOutBearings);
                Vector3 candidate = building.Bounds.center
                    + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * reach;
                candidate.y = building.Bounds.min.y + 1f;

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit ground, WayOutClearance,
                        NavMesh.AllAreas))
                    continue;

                // Sampling can land back inside the building it is meant to be leaving.
                if (Landmarks.IsInside(building.Bounds, ground.position)) continue;

                if (!NavMesh.CalculatePath(standingOn.position, ground.position,
                        NavMesh.AllAreas, path))
                    continue;
                if (path.status != NavMeshPathStatus.PathComplete) continue;

                float length = PathLength(path);
                if (length >= best) continue;

                best = length;
                outside = ground.position;
            }

            return best < float.MaxValue;
        }

        private static float PathLength(NavMeshPath path)
        {
            float total = 0f;
            for (int i = 1; i < path.corners.Length; i++)
                total += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            return total;
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
            bool routed = NavMesh.CalculatePath(standingOn.position, _goal,
                NavMesh.AllAreas, path);

            if (!routed || path.status == NavMeshPathStatus.PathInvalid || path.corners.Length < 2)
            {
                ScreenReaderManager.Speak(_leaving
                    ? $"No route out of the {_targetName}"
                    : $"No route into the {_targetName}");
                _active = false;
                return;
            }

            // A partial route is worth following - it still walks you to the building -
            // but the player should be told once that it may not reach the way in.
            _routeShort = path.status == NavMeshPathStatus.PathPartial;
            if (_routeShort && !_saidRouteShort)
            {
                _saidRouteShort = true;
                ScreenReaderManager.Speak(_leaving
                    ? "Route may not reach all the way out"
                    : "Route may not reach all the way in", false);
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
