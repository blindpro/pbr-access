using System.Collections.Generic;
using UnityEngine;

namespace AccessibilityMod
{
    /// <summary>
    /// Puts names to the buildings. The battle royale scene labels nothing — no
    /// NewLocationTrigger survives in it — but it is built from Synty POLYGON prefabs
    /// whose object names are already human words: SM_Bld_Church_01, SM_Bld_RadioTower_01.
    /// Reading those back off the colliders is the only place names this map will ever
    /// give us. See map.md.
    ///
    /// Shared by the map grid readout and the surroundings survey, so a building is
    /// called the same thing whichever key asked about it.
    /// </summary>
    public static class Landmarks
    {
        private const float SearchRadius = 80f;
        private const float NearbyDistance = 40f;

        // How far in from the sides of a footprint you must be to count as inside it.
        private const float FootprintInset = 2f;

        /// <summary>
        /// Is this point inside the building, rather than beside it?
        ///
        /// The bounds are axis aligned, so a building standing at an angle to the world
        /// drags a wedge of open ground in at each corner. Pulling the sides in keeps a
        /// player walking past a corner from being told they are indoors. Small buildings
        /// are inset proportionally: taking two metres off every side of a trailer would
        /// leave nothing to stand in.
        /// </summary>
        public static bool IsInside(Bounds footprint, Vector3 point)
        {
            float insetX = Mathf.Min(FootprintInset, footprint.extents.x * 0.5f);
            float insetZ = Mathf.Min(FootprintInset, footprint.extents.z * 0.5f);

            footprint.Expand(new Vector3(-insetX * 2f, 0f, -insetZ * 2f));
            return footprint.Contains(point);
        }

        /// <summary>A named building standing near a point.</summary>
        public struct Nearby
        {
            public string Name;
            public int Rank;
            public float Distance;

            /// <summary>Nearest point on the building: what to walk towards.</summary>
            public Vector3 Position;

            /// <summary>Every collider of it together, so Bounds.center is inside the walls.</summary>
            public Bounds Bounds;
        }

        /// <summary>
        /// Every named building inside the radius, nearest first, one entry per building
        /// rather than one per collider.
        ///
        /// A volume query, so it sees through hills, fences and roofs. That is the point:
        /// both the map readout and the surroundings survey want to know what is standing
        /// there, not what happens to be in line of sight from eye height.
        ///
        /// Distance and bearing are to the nearest corner of the building's bounds — the
        /// part you would actually walk to. A church's pivot can sit tens of metres from
        /// its wall, and two keys quoting different numbers for one church reads as a bug.
        /// </summary>
        public static List<Nearby> FindNearby(Vector3 worldPos, float radius)
        {
            // From the plane or under an open parachute we are hundreds of metres up,
            // where a sphere of this size touches nothing. Ask at ground level instead,
            // so the answer is about the place below us rather than the empty air.
            Vector3 queryPos = DropToGround(worldPos);

            var hits = Physics.OverlapSphere(queryPos, radius, GetMask(),
                QueryTriggerInteraction.Ignore);

            var found = new List<Nearby>();
            var index = new Dictionary<Transform, int>();

            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit == null) continue;

                if (!TryFind(hit.transform, out Transform root, out string name, out int rank)) continue;

                Vector3 closest = hit.bounds.ClosestPoint(queryPos);
                float distance = Vector3.Distance(queryPos, closest);

                // One church, however many colliders it was built from: the footprint
                // grows to hold them all, while distance and bearing stay pinned to
                // whichever piece of it is nearest.
                if (index.TryGetValue(root, out int existing))
                {
                    Nearby merged = found[existing];
                    merged.Bounds.Encapsulate(hit.bounds);

                    if (distance < merged.Distance)
                    {
                        merged.Distance = distance;
                        merged.Position = closest;
                    }

                    found[existing] = merged;
                    continue;
                }

                index[root] = found.Count;
                found.Add(new Nearby
                {
                    Name = name,
                    Rank = rank,
                    Distance = distance,
                    Position = closest,
                    Bounds = hit.bounds
                });
            }

            found.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            return found;
        }

        /// <summary>
        /// The building this point is standing inside, if any. Nearest first, so a porch
        /// that falls inside two footprints answers with the one you are actually in.
        /// </summary>
        public static bool TryFindContaining(Vector3 point, float radius, out Nearby building)
        {
            var nearby = FindNearby(point, radius);

            for (int i = 0; i < nearby.Count; i++)
            {
                if (!IsInside(nearby[i].Bounds, point)) continue;

                building = nearby[i];
                return true;
            }

            building = default(Nearby);
            return false;
        }

        /// <summary>
        /// Names the most distinctive building standing near a point, e.g. "church" or
        /// "radio tower", or null if nothing nearby is nameable. With withDistance, adds
        /// range and compass bearing; without, answers only when the landmark is close
        /// enough to be the thing you'd walk to.
        /// </summary>
        public static string DescribeNearest(Vector3 worldPos, bool withDistance)
        {
            var nearby = FindNearby(worldPos, SearchRadius);
            if (nearby.Count == 0) return null;

            // A landmark that says more wins outright; ties go to the closer one, and the
            // list already arrives closest first.
            Nearby best = nearby[0];
            for (int i = 1; i < nearby.Count; i++)
                if (nearby[i].Rank < best.Rank) best = nearby[i];

            if (!withDistance)
                return best.Distance <= NearbyDistance ? best.Name : null;

            int metres = Mathf.RoundToInt(best.Distance);
            // Dropping to the ground only changed our height, so the bearing is the same
            // one either point would give.
            return $"{best.Name} {metres} meters {GetCardinalTo(worldPos, best.Position)}";
        }

        /// <summary>
        /// Walks a collider and its parents looking for a known building prefab. Names
        /// like SM_Bld_Church_01_Glass mean the thing a ray actually struck is often a
        /// window or a door, so the useful name can be a level or two up.
        /// </summary>
        public static bool TryName(Transform transform, out string name, out int rank)
        {
            return TryFind(transform, out Transform _, out name, out rank);
        }

        /// <summary>
        /// As TryName, and also hands back the object that carried the name, which is
        /// what tells two churches apart when a query returns every collider of both.
        /// </summary>
        public static bool TryFind(Transform transform, out Transform root, out string name, out int rank)
        {
            root = null;
            name = null;
            rank = int.MaxValue;

            Transform current = transform;
            for (int depth = 0; depth < 3 && current != null; depth++)
            {
                string objectName = current.name;
                for (int i = 0; i < Known.Length; i++)
                {
                    if (objectName.IndexOf(Known[i].Token, System.StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    // Known is ordered most distinctive first, so the first hit at this
                    // depth is the best name this object can give.
                    root = current;
                    name = Known[i].Spoken;
                    rank = i;
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        /// <summary>What a ray hit should be called, or "building" when it isn't a prefab we know.</summary>
        public static string NameOr(Transform transform, string fallback)
        {
            return TryName(transform, out string name, out int _) ? name : fallback;
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
        private static readonly Landmark[] Known =
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
                    GetMask(), QueryTriggerInteraction.Ignore))
                return hit.point + Vector3.up;

            return worldPos;
        }

        private static int GetMask()
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
