using System.Collections.Generic;
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
        // Real skeleton joints, most valuable first: head and chest are what stays
        // exposed above cover, and a shot to any of them connects.
        //
        // These are the animated bone transforms, not offsets from the character
        // root. The game's own bots aim at GetBoneTransform(Head).position and hit
        // reliably; a fixed height above the root only agrees with the body while
        // the enemy is standing still on flat ground, and drifts into empty air the
        // moment they crouch, go prone, stand on a slope or lean through an
        // animation - which is exactly when a fight is happening.
        private static readonly HumanBodyBones[] SampleBones =
        {
            HumanBodyBones.Head,
            HumanBodyBones.UpperChest,
            HumanBodyBones.Chest,
            HumanBodyBones.Spine,
            HumanBodyBones.Hips,
        };

        // Used only when a character has no humanoid rig to ask.
        private static readonly float[] FallbackHeights = { 1.5f, 1.2f, 0.7f };

        // Skip the first stretch of the ray so the player's own body, arms and the
        // weapon in their hands can never be mistaken for cover.
        private const float SelfClearance = 0.35f;

        // Stop the ray short of the body so the enemy's own limbs and the surface
        // they are standing against don't count as blocking them.
        private const float SurfaceMargin = 0.4f;

        private static readonly RaycastHit[] _hits = new RaycastHit[64];

        /// <summary>
        /// The centre of mass to measure range and bearing from - the real chest
        /// joint where there is one, so it follows the body rather than hovering
        /// over it.
        /// </summary>
        public static Vector3 ChestOf(CharacterMultiplayer character)
        {
            var points = RigOf(character).Points;
            if (points != null && points.Length > 1 && points[1] != null)
                return points[1].position;

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
            var points = RigOf(target).Points;

            if (points != null && points.Length > 0)
            {
                foreach (var joint in points)
                {
                    if (joint == null) continue;
                    if (IsClearPath(player, origin, joint.position))
                    {
                        visiblePoint = joint.position;
                        return true;
                    }
                }
            }
            else
            {
                foreach (float height in FallbackHeights)
                {
                    Vector3 point = target.transform.position + Vector3.up * height;
                    if (IsClearPath(player, origin, point))
                    {
                        visiblePoint = point;
                        return true;
                    }
                }
            }

            visiblePoint = ChestOf(target);
            return false;
        }

        /// <summary>
        /// What is between us and them, in the terms a player would act on. "Behind
        /// cover" alone covered a waist-high fence and two storeys of concrete with
        /// the same three words, and those call for opposite decisions.
        /// </summary>
        public enum Cover
        {
            /// <summary>Every sampled joint is clear - the whole body is shootable.</summary>
            Open,
            /// <summary>Some joints clear: shootable, but a smaller target than it sounds.</summary>
            Partial,
            /// <summary>Nothing clear, but the space above them is - a wall or fence
            /// they will stand up over. Worth holding the aim where they will appear.</summary>
            Low,
            /// <summary>Nothing clear, and there is a roof over them. They are in a
            /// building, and shooting at the wall will never reach them.</summary>
            Indoors,
            /// <summary>Nothing clear, no roof: a building face, a hillside, a rock.</summary>
            Blocked,
        }

        // How far above the head to look for a ceiling. Tall enough to clear a
        // doorway and find the floor above, short enough that open sky stays open.
        private const float RoofProbe = 6f;

        // How far above the head has to be exposed before the obstruction counts as
        // something they will rise over rather than something that simply stops the
        // shot. Roughly the difference between crouching and standing.
        private const float LowCoverProbe = 0.9f;

        /// <summary>
        /// Sorts the obstruction into something speakable. Deliberately not on the
        /// hot path: the targeting scan asks <see cref="HasLineOfSight"/> about every
        /// enemy many times a second, while this runs once a second for the single
        /// nearest one, so it can afford the extra probes.
        ///
        /// Open and Partial are exactly the cases where <see cref="HasLineOfSight"/>
        /// returns true - both count some joint as clear - so the spoken cover state
        /// and the presence of targeting beeps can never contradict each other.
        /// </summary>
        public static Cover Classify(CharacterMultiplayer player, Vector3 origin,
            CharacterMultiplayer target)
        {
            var points = RigOf(target).Points;
            int clear = 0;
            int total = 0;

            if (points != null && points.Length > 0)
            {
                foreach (var joint in points)
                {
                    if (joint == null) continue;
                    total++;
                    if (IsClearPath(player, origin, joint.position)) clear++;
                }
            }
            else
            {
                foreach (float height in FallbackHeights)
                {
                    total++;
                    if (IsClearPath(player, origin, target.transform.position + Vector3.up * height))
                        clear++;
                }
            }

            if (total == 0) return Cover.Blocked;
            if (clear == total) return Cover.Open;
            if (clear > 0) return Cover.Partial;

            Vector3 head = HeadOf(target);
            if (IsBlockedAbove(player, head)) return Cover.Indoors;
            if (IsClearPath(player, origin, head + Vector3.up * LowCoverProbe)) return Cover.Low;
            return Cover.Blocked;
        }

        /// <summary>
        /// A ceiling over this point. Bodies are skipped for the same reason they are
        /// in <see cref="IsClearPath"/> - a team mate on a balcony is not a roof.
        /// </summary>
        private static bool IsBlockedAbove(CharacterMultiplayer player, Vector3 point)
        {
            int count = Physics.RaycastNonAlloc(point + Vector3.up * 0.1f, Vector3.up, _hits,
                RoofProbe, AudioTargeting.GetObstacleMask(), QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                if (IsBody(_hits[i].collider, player)) continue;
                return true;
            }

            return false;
        }

        /// <summary>The head joint, which is what has to clear cover for the rest of
        /// them to follow.</summary>
        private static Vector3 HeadOf(CharacterMultiplayer character)
        {
            // SampleBones leads with Head, so the first resolved joint is the head.
            var points = RigOf(character).Points;
            if (points != null && points.Length > 0 && points[0] != null)
                return points[0].position;

            return character.transform.position + Vector3.up * 1.6f;
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

        /// <summary>
        /// The rig lookups for one character. Both the hit bones and the skeleton
        /// joints are fixed for a character's lifetime, while targeting asks for
        /// them many times a second, so they are resolved once each. Keyed per
        /// character rather than held as a single slot, because the aim assist and
        /// the audio scan interleave different targets and would thrash it.
        /// </summary>
        private class Rig
        {
            public CharacterBone[] Bones;
            public Transform[] Points;

            public bool IsStale
            {
                get { return Points != null && Points.Length > 0 && Points[0] == null; }
            }
        }

        private static readonly Dictionary<int, Rig> _rigs = new Dictionary<int, Rig>();

        /// <summary>Dropped between matches so rigs from the last one are not kept alive.</summary>
        public static void ForgetRigs()
        {
            _rigs.Clear();
        }

        private static Rig RigOf(CharacterMultiplayer character)
        {
            int id = character.GetInstanceID();
            // A destroyed character leaves its cached transforms reading as null, so
            // a rig that has lost its joints is rebuilt rather than trusted.
            if (_rigs.TryGetValue(id, out Rig cached) && !cached.IsStale) return cached;

            var rig = new Rig { Bones = character.GetComponentsInChildren<CharacterBone>() };

            var thirdPerson = character.GetComponent<ThirdPerson>();
            var animator = thirdPerson != null ? thirdPerson.tps_animator : null;
            if (animator != null && animator.isHuman)
            {
                var points = new List<Transform>(SampleBones.Length);
                foreach (var bone in SampleBones)
                {
                    var joint = animator.GetBoneTransform(bone);
                    if (joint != null) points.Add(joint);
                }
                rig.Points = points.ToArray();
            }

            _rigs[id] = rig;
            return rig;
        }

        private static CharacterBone[] BonesOf(CharacterMultiplayer target)
        {
            return target == null ? null : RigOf(target).Bones;
        }

        /// <summary>How many damageable bones this character exposes - zero means
        /// nothing on them can be shot, which the lock could never explain by ear.</summary>
        public static int BoneCount(CharacterMultiplayer target)
        {
            var bones = BonesOf(target);
            return bones == null ? 0 : bones.Length;
        }

        /// <summary>How many real skeleton joints we found; zero means we are on the
        /// fixed-height fallback and aiming at estimated positions.</summary>
        public static int JointCount(CharacterMultiplayer target)
        {
            if (target == null) return 0;
            var points = RigOf(target).Points;
            return points == null ? 0 : points.Length;
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
