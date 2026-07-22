using System.Collections.Generic;
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
    /// The reading that settles it is the detour: a route that walks 19 metres to reach
    /// something 4 metres away has gone round the building to an opening, which is only
    /// possible if the bake knows the walls are there. A route that arrives in a straight
    /// line through a wall proves the opposite - that the bake ignored the buildings and
    /// its answers are worthless to us.
    ///
    /// Nothing here helps a player mid-match; it exists to settle that question before
    /// either approach gets built on. See map.md.
    /// </summary>
    public class NavMeshDiagnostics
    {
        private const float LandmarkSearchRadius = 60f;

        // Where the player stands, the mesh is either under their feet or it is not.
        // Anything past a stride is the answer "you are off it", which is the single
        // most useful thing this can say when a route comes back broken.
        private const float FootSampleRadius = 2f;

        // Hunting for the interior floor is a different question and gets a different
        // budget: a building is tens of metres across.
        private const float InteriorSampleRadius = 20f;

        // A route this much longer than the straight line has gone round something.
        private const float DetourRatio = 1.5f;

        public void Tick()
        {
            if (!Input.GetKeyDown(KeyCode.Y)) return;

            var player = CharacterMultiplayer.GetMainPlayer();
            if (player == null || player.IsDead())
            {
                ScreenReaderManager.Speak("Navmesh check unavailable", true);
                return;
            }

            var lines = new List<string> { "Navmesh check." };
            Vector3 playerPos = player.transform.position;

            // The threshold callout's own decision, plus the roof reading separately -
            // they can now disagree, and which one is talking is the thing worth knowing.
            lines.Add(NavigationAssistant.IsIndoors(player, out string room)
                ? $"Reads as indoors{(room == null ? "" : ", in the " + room)}."
                : "Reads as outdoors.");

            lines.Add(NavigationAssistant.HasCeiling(player, out RaycastHit roof)
                ? $"Roof overhead: {Landmarks.NameOr(roof.transform, "something unnamed")} "
                  + $"{Metres(roof.distance)} up."
                : "Nothing overhead.");

            if (!TryFindFooting(playerPos, out NavMeshHit standingOn, out string footing))
            {
                lines.Add(footing);
                Report(lines);
                return;
            }
            lines.Add(footing);

            var nearby = Landmarks.FindNearby(playerPos, LandmarkSearchRadius);
            if (nearby.Count == 0)
            {
                lines.Add($"No named building within {(int)LandmarkSearchRadius} meters to route into.");
                Report(lines);
                return;
            }

            Landmarks.Nearby target = nearby[0];
            lines.Add($"{Capitalize(target.Name)}, {Metres(target.Distance)} away.");

            // The interior floor, not the roof: the centre of a bounding box that includes
            // the roof can sit in mid-air.
            Vector3 middle = target.Bounds.center;
            middle.y = target.Bounds.min.y + 1f;

            if (!NavMesh.SamplePosition(middle, out NavMeshHit inside, InteriorSampleRadius, NavMesh.AllAreas))
            {
                lines.Add($"No navmesh within {(int)InteriorSampleRadius} meters of the middle of it. "
                          + "Interiors are not baked.");
                Report(lines);
                return;
            }

            lines.Add(target.Bounds.Contains(inside.position)
                ? $"Walkable inside its footprint, {Metres(Vector3.Distance(middle, inside.position))} from the middle."
                : $"Nearest walkable point is outside its footprint, "
                  + $"{Metres(Vector3.Distance(middle, inside.position))} off.");

            lines.Add(DescribeRoute(player, standingOn.position, inside.position));
            Report(lines);
        }

        /// <summary>
        /// Where the route will really start from. A path is calculated from the nearest
        /// mesh point, not from the player, so a player standing off the mesh gets a route
        /// that begins somewhere they are not - which is how a two corner, six metre path
        /// ends up with its first turn seven metres away.
        /// </summary>
        private static bool TryFindFooting(Vector3 playerPos, out NavMeshHit standingOn, out string report)
        {
            if (NavMesh.SamplePosition(playerPos, out standingOn, FootSampleRadius, NavMesh.AllAreas))
            {
                report = "You are on the navmesh.";
                return true;
            }

            if (!NavMesh.SamplePosition(playerPos, out standingOn, LandmarkSearchRadius, NavMesh.AllAreas))
            {
                report = "No navmesh anywhere near you. Either it is not baked in this scene "
                         + "or it does not reach here.";
                return false;
            }

            // Worth saying out loud: if standing where you are is off the mesh, then the
            // mesh does not cover this spot, and every distance below is measured from
            // somewhere else.
            report = $"You are off the navmesh. The route starts "
                     + $"{Metres(Vector3.Distance(playerPos, standingOn.position))} from you.";
            return true;
        }

        /// <summary>
        /// The verdict. Length against the straight line is what tells us whether the bake
        /// knows the building is solid, and that is the whole question.
        /// </summary>
        private static string DescribeRoute(CharacterMultiplayer player, Vector3 from, Vector3 to)
        {
            var path = new NavMeshPath();
            bool routed = NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path);

            if (!routed || path.status == NavMeshPathStatus.PathInvalid || path.corners.Length < 2)
                return "No route to it at all.";

            float straight = Vector3.Distance(from, to);
            float length = PathLength(path);

            // Measured from the path's own start, which is the only honest reference.
            Vector3 firstTurn = path.corners[1];
            string shape = $"{path.corners.Length} corners, {Metres(length)} against "
                           + $"{Metres(straight)} straight. First turn "
                           + $"{DirectionTo(player.transform, firstTurn)}, "
                           + $"{Metres(Vector3.Distance(from, firstTurn))} along.";

            // A partial route never arrived, so nothing about walls can be read off it -
            // of course it is shorter than the straight line, it stopped early. What it
            // can say is how close it got before it gave up.
            if (path.status == NavMeshPathStatus.PathPartial)
            {
                float shortBy = Vector3.Distance(path.corners[path.corners.Length - 1], to);
                return $"Route stops short, {shape} Gave up {Metres(shortBy)} from it. "
                       + "No verdict on walls from a route that never arrived.";
            }

            bool blocked = Physics.Linecast(from + Vector3.up, to + Vector3.up,
                GetSolidMask(), QueryTriggerInteraction.Ignore);

            string verdict;
            if (length > straight * DetourRatio)
                verdict = "It goes the long way round, so the bake knows the walls are there "
                          + "and this route is using an opening.";
            else if (blocked && path.corners.Length > 2)
                // The reading that matters, and the one the first version of this got
                // wrong: bending round solid geometry, even by a little, is the bake
                // proving it knows the geometry is there.
                verdict = "It bends around solid geometry rather than crossing it, so the "
                          + "bake knows the walls are there.";
            else if (blocked)
                verdict = "It arrives dead straight through solid wall, so the bake "
                          + "ignored the buildings and cannot find doors for us.";
            else
                verdict = "Straight there, but nothing solid is in the way, so this tells "
                          + "us nothing either way. Try it from behind the building.";

            return $"Route complete, {shape} {verdict}";
        }

        private static float PathLength(NavMeshPath path)
        {
            float total = 0f;
            for (int i = 1; i < path.corners.Length; i++)
                total += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            return total;
        }

        /// <summary>Spoken and logged both: the log is what survives to be read after.</summary>
        private static void Report(List<string> lines)
        {
            string text = string.Join(" ", lines.ToArray());
            Plugin.Logger.LogInfo(text);
            ScreenReaderManager.Speak(text, true);
        }

        private static int GetSolidMask()
        {
            int mask = 1 << 0; // Default: where the buildings sit
            int building = LayerMask.NameToLayer("Building");
            if (building >= 0) mask |= 1 << building;
            int env = LayerMask.NameToLayer("Environment");
            if (env >= 0) mask |= 1 << env;
            return mask;
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
