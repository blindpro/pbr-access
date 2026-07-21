using UnityEngine;
using InfimaGames.LowPolyShooterPack;

namespace AccessibilityMod
{
    /// <summary>
    /// The two geometry questions every combat cue depends on: "can I see them"
    /// and "would this shot actually land". Both used to be answered with a single
    /// naive raycast, and both answers were wrong in ways that cost fights.
    ///
    /// Line of sight was wrong because the ray started inside the player's own head
    /// and ran to one fixed point on the enemy's chest, so our own rig, our own held
    /// weapon, a squad mate walking past, or a hip-height fence in front of a fully
    /// visible torso all read as "behind cover".
    ///
    /// The shot test was wrong because it was invented rather than measured. The
    /// game decides damage in Projectile.Update: the bullet only hurts anyone if
    /// ComputeLookAtRaycast's single exact ray from the camera struck a CharacterBone
    /// collider. Weapon spread moves the visible tracer and nothing else. So a shot
    /// lands exactly when that one ray lands, and IsShotLanding reproduces it.
    /// </summary>
    internal static class Targeting
    {
        // Sampled up the body, most valuable first: head and chest are what is left
        // exposed above cover, and a shot to any of them still connects.
        private static readonly float[] BodyHeights = { 1.5f, 1.2f, 0.7f };

        // Skip the first stretch of the ray so the player's own body, arms and the
        // weapon in their hands can never be mistaken for cover.
        private const float SelfClearance = 0.35f;

        // Stop the ray short of the body so the enemy's own limbs and the surface
        // they are standing against don't count as blocking them.
        private const float SurfaceMargin = 0.4f;

        private static readonly RaycastHit[] _hits = new RaycastHit[64];

        public static Vector3 ChestOf(CharacterMultiplayer character)
        {
            return character.transform.position + Vector3.up * 1.2f;
        }

        /// <summary>
        /// True when any part of the enemy is exposed, reporting which point that
        /// was so the caller can aim and beep at somewhere actually shootable
        /// rather than at a chest tucked behind a wall.
        /// </summary>
        public static bool HasLineOfSight(CharacterMultiplayer player, Vector3 origin,
            CharacterMultiplayer target, out Vector3 visiblePoint)
        {
            foreach (float height in BodyHeights)
            {
                Vector3 point = target.transform.position + Vector3.up * height;
                if (IsClearPath(player, origin, point))
                {
                    visiblePoint = point;
                    return true;
                }
            }

            visiblePoint = ChestOf(target);
            return false;
        }

        /// <summary>
        /// Solid world geometry between here and there. People are deliberately not
        /// counted: a body standing in the way is not cover, and calling it cover
        /// made a squad mate crossing in front announce that the enemy had hidden.
        /// </summary>
        private static bool IsClearPath(CharacterMultiplayer player, Vector3 origin, Vector3 point)
        {
            Vector3 toPoint = point - origin;
            float dist = toPoint.magnitude;
            if (dist <= SelfClearance + SurfaceMargin) return true;

            Vector3 dir = toPoint / dist;
            Vector3 start = origin + dir * SelfClearance;
            float length = dist - SelfClearance - SurfaceMargin;

            int count = Physics.RaycastNonAlloc(start, dir, _hits, length,
                AudioTargeting.GetObstacleMask(), QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                if (IsBody(_hits[i].collider, player)) continue;
                return false;
            }

            return true;
        }

        private static bool IsBody(Collider col, CharacterMultiplayer player)
        {
            if (col == null) return true;
            if (player != null && col.transform.IsChildOf(player.transform)) return true;
            return col.GetComponentInParent<CharacterMultiplayer>() != null;
        }

        /// <summary>
        /// How far off the aim is from the target's actual silhouette, in degrees:
        /// zero or less means the crosshair is somewhere on their body.
        ///
        /// This exists because the game's hit ray is infinitely thin and the bone
        /// colliders are separate capsules with gaps between them - a ray-only lock
        /// blinks out between the shoulder and the chest while the aim has not moved.
        /// Measuring against the bones the game would damage keeps the lock honest
        /// without making it a pixel hunt.
        /// </summary>
        public static float AimError(Vector3 origin, Vector3 dir, CharacterMultiplayer target)
        {
            var bones = BonesOf(target);
            if (bones == null || bones.Length == 0) return float.MaxValue;

            float best = float.MaxValue;

            foreach (var bone in bones)
            {
                if (bone == null) continue;
                var col = bone.GetComponent<Collider>();
                if (col == null || !col.enabled) continue;

                Bounds bounds = col.bounds;
                Vector3 toBone = bounds.center - origin;
                float dist = toBone.magnitude;
                if (dist < 0.01f) continue;

                // Largest half-extent: the bone's own size, not a tuned constant, so
                // a distant enemy demands real precision and a close one does not.
                float radius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
                float angularRadius = Mathf.Atan2(radius, dist) * Mathf.Rad2Deg;

                float error = Vector3.Angle(dir, toBone) - angularRadius;
                if (error < best) best = error;
            }

            return best;
        }

        // Bones are fixed for the lifetime of a character, and the scan asks for them
        // several times a second - look them up once per target instead.
        private static CharacterMultiplayer _bonesOwner;
        private static CharacterBone[] _bonesCache;

        private static CharacterBone[] BonesOf(CharacterMultiplayer target)
        {
            if (target == null) return null;
            if (target == _bonesOwner && _bonesCache != null) return _bonesCache;

            _bonesOwner = target;
            _bonesCache = target.GetComponentsInChildren<CharacterBone>();
            return _bonesCache;
        }

        /// <summary>
        /// The game's own hit test, run ahead of time. Damage happens if and only if
        /// the camera's exact forward ray reaches a CharacterBone on this target
        /// before it reaches anything else, so that is precisely what we ask.
        /// </summary>
        public static bool IsShotLanding(CharacterMultiplayer player, Vector3 origin,
            Vector3 dir, float range, CharacterMultiplayer target)
        {
            // Match the mask the game fires with, so we agree about what stops a bullet.
            int mask = ~0;
            var thirdPerson = player.GetComponent<ThirdPerson>();
            if (thirdPerson != null && thirdPerson.raycastMask.value != 0)
                mask = thirdPerson.raycastMask.value;

            int count = Physics.RaycastNonAlloc(origin, dir, _hits, range, mask,
                QueryTriggerInteraction.Ignore);

            // The game turns our own bones into triggers for the duration of its
            // raycast; we cannot, so we discard our own rig by hand instead.
            int nearest = -1;
            float nearestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var col = _hits[i].collider;
                if (col == null) continue;
                if (player != null && col.transform.IsChildOf(player.transform)) continue;
                if (_hits[i].distance < nearestDist)
                {
                    nearestDist = _hits[i].distance;
                    nearest = i;
                }
            }

            if (nearest < 0) return false;

            // GetComponent, not GetComponentInParent: the game reads the bone off the
            // struck collider itself, so a hit on a non-bone collider parented to a
            // character does no damage either.
            var bone = _hits[nearest].collider.GetComponent<CharacterBone>();
            if (bone == null || bone.character == null) return false;

            return bone.character.GetComponent<CharacterMultiplayer>() == target;
        }
    }
}
