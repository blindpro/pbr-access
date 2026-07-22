using UnityEngine;
using UnityEngine.AI;
using InfimaGames.LowPolyShooterPack;

namespace AccessibilityMod
{
    /// <summary>
    /// Press Y to ask whether the game's own pathfinding can get us through a door.
    ///
    /// The scene has a baked NavMesh: the bots walk it with a NavMeshAgent, and
    /// NavmeshPoint snaps objects onto it. If that bake covers building interiors,
    /// then finding a doorway is not a geometry problem at all - NavMesh.CalculatePath
    /// to a point inside returns corners that bend through the entrance, and the first
    /// corner is the door. If it only covers the outdoors, the path stops at the wall
    /// and doorways have to be found by hand.
    ///
    /// Nothing here helps a player mid-match; it exists to settle that one question
    /// before either approach gets built on. See map.md.
    /// </summary>
    public class NavMeshDiagnostics
    {
        private const float LandmarkSearchRadius = 60f;

        // How far off a wanted point the sampler may wander before its answer stops
        // being about that point. A building is tens of metres across, so this is
        // generous enough to find an interior floor and tight enough that a snap out
        // to the road is reported as the miss it is.
        private const float SampleRadius = 20f;

        public void Tick()
        {
            if (!Input.GetKeyDown(KeyCode.Y)) return;

            var player = CharacterMultiplayer.GetMainPlayer();
            if (player == null || player.IsDead())
            {
                ScreenReaderManager.Speak("Navmesh check unavailable", true);
                return;
            }

            Vector3 playerPos = player.transform.position;

            if (!NavMesh.SamplePosition(playerPos, out NavMeshHit standingOn, SampleRadius, NavMesh.AllAreas))
            {
                Report("Navmesh check. No navmesh anywhere near you. Either it is not "
                       + "baked in this scene or it does not reach here.");
                return;
            }

            float underfoot = Vector3.Distance(playerPos, standingOn.position);

            var nearby = Landmarks.FindNearby(playerPos, LandmarkSearchRadius);
            if (nearby.Count == 0)
            {
                Report($"Navmesh check. Standing {Metres(underfoot)} off the navmesh. "
                       + "No named building within "
                       + $"{(int)LandmarkSearchRadius} meters to test a route into.");
                return;
            }

            Landmarks.Nearby target = nearby[0];
            Vector3 middle = target.Bounds.center;

            // The interior floor, not the roof: the centre of a bounding box that
            // includes the roof can sit in mid-air, which the sampler would answer for
            // by dropping to whatever is below - possibly the ground outside.
            middle.y = target.Bounds.min.y + 1f;

            if (!NavMesh.SamplePosition(middle, out NavMeshHit inside, SampleRadius, NavMesh.AllAreas))
            {
                Report($"Navmesh check. {Capitalize(target.Name)} {Metres(target.Distance)} away. "
                       + "No navmesh within " + (int)SampleRadius + " meters of the middle of it. "
                       + "Interiors are not baked.");
                return;
            }

            // The whole question, in one line: did the nearest walkable point to the
            // middle of the building land inside its footprint, or out on the street?
            bool interiorBaked = target.Bounds.Contains(inside.position);
            float drift = Vector3.Distance(middle, inside.position);

            var path = new NavMeshPath();
            bool routed = NavMesh.CalculatePath(standingOn.position, inside.position,
                NavMesh.AllAreas, path);

            string route;
            if (!routed || path.status == NavMeshPathStatus.PathInvalid || path.corners.Length < 2)
            {
                route = "No route to it.";
            }
            else
            {
                Vector3 firstTurn = path.corners[1];
                string turn = DirectionTo(player.transform, firstTurn);
                string complete = path.status == NavMeshPathStatus.PathComplete
                    ? "Route complete" : "Route partial, it stops short";

                route = $"{complete}, {path.corners.Length} corners, "
                        + $"{Metres(PathLength(path))} total. "
                        + $"First turn {turn}, {Metres(Vector3.Distance(playerPos, firstTurn))} away.";
            }

            Report($"Navmesh check. {Capitalize(target.Name)} {Metres(target.Distance)} away. "
                   + (interiorBaked
                       ? $"Walkable inside its footprint, {Metres(drift)} from the middle. "
                       : $"Nearest walkable point is outside its footprint, {Metres(drift)} off. ")
                   + route);
        }

        private static float PathLength(NavMeshPath path)
        {
            float total = 0f;
            for (int i = 1; i < path.corners.Length; i++)
                total += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            return total;
        }

        /// <summary>Spoken and logged both: the log is what survives to be read after.</summary>
        private static void Report(string text)
        {
            Plugin.Logger.LogInfo(text);
            ScreenReaderManager.Speak(text, true);
        }

        private static string Metres(float distance)
        {
            return $"{Mathf.RoundToInt(distance)} meters";
        }

        private static string DirectionTo(Transform playerTransform, Vector3 target)
        {
            Vector3 delta = target - playerTransform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.01f) return "here";

            float angle = Vector3.SignedAngle(playerTransform.forward, delta, Vector3.up);

            if (angle >= -22.5f && angle < 22.5f) return "ahead";
            if (angle >= 22.5f && angle < 67.5f) return "front right";
            if (angle >= 67.5f && angle < 112.5f) return "right";
            if (angle >= 112.5f && angle < 157.5f) return "behind right";
            if (angle >= -67.5f && angle < -22.5f) return "front left";
            if (angle >= -112.5f && angle < -67.5f) return "left";
            if (angle >= -157.5f && angle < -112.5f) return "behind left";
            return "behind";
        }

        private static string Capitalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return char.ToUpper(text[0]) + text.Substring(1);
        }
    }
}
