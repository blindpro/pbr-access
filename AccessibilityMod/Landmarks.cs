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

        /// <summary>
        /// Names the most distinctive building standing near a point, e.g. "church" or
        /// "radio tower", or null if nothing nearby is nameable. With withDistance, adds
        /// range and compass bearing; without, answers only when the landmark is close
        /// enough to be the thing you'd walk to.
        /// </summary>
        public static string DescribeNearest(Vector3 worldPos, bool withDistance)
        {
            // From the plane or under an open parachute we are hundreds of metres up,
            // where a sphere of this size touches nothing. Ask at ground level instead,
            // so the answer is about the place below us rather than the empty air.
            worldPos = DropToGround(worldPos);

            var hits = Physics.OverlapSphere(worldPos, SearchRadius, GetMask(),
                QueryTriggerInteraction.Ignore);

            string bestName = null;
            int bestRank = int.MaxValue;
            float bestDistance = float.MaxValue;
            Vector3 bestPos = Vector3.zero;

            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit == null) continue;

                if (!TryName(hit.transform, out string name, out int rank)) continue;

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
                return bestDistance <= NearbyDistance ? bestName : null;

            int metres = Mathf.RoundToInt(bestDistance);
            return $"{bestName} {metres} meters {GetCardinalTo(worldPos, bestPos)}";
        }

        /// <summary>
        /// Walks a collider and its parents looking for a known building prefab. Names
        /// like SM_Bld_Church_01_Glass mean the thing a ray actually struck is often a
        /// window or a door, so the useful name can be a level or two up.
        /// </summary>
        public static bool TryName(Transform transform, out string name, out int rank)
        {
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
