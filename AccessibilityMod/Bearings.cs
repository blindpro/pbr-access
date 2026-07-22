using UnityEngine;

namespace AccessibilityMod
{
    /// <summary>
    /// The eight directions, relative to where the player is facing.
    ///
    /// One copy, because three callouts saying "front left" about the same spot have to
    /// mean the same angle. Compass directions ("north east") are a different vocabulary
    /// and live in Landmarks, which is the only place that speaks them.
    /// </summary>
    public static class Bearings
    {
        /// <summary>Degrees from where the player faces, ignoring height.</summary>
        public static float AngleTo(Transform from, Vector3 target)
        {
            Vector3 delta = target - from.position;
            delta.y = 0f;

            // Standing on the thing you asked about: call it straight ahead rather than
            // letting SignedAngle guess off a zero-length vector.
            if (delta.sqrMagnitude < 0.01f) return 0f;

            return Vector3.SignedAngle(from.forward, delta, Vector3.up);
        }

        public static string Relative(Transform from, Vector3 target)
        {
            return FromAngle(AngleTo(from, target));
        }

        public static string FromAngle(float angle)
        {
            if (angle >= -22.5f && angle < 22.5f) return "ahead";
            if (angle >= 22.5f && angle < 67.5f) return "front right";
            if (angle >= 67.5f && angle < 112.5f) return "right";
            if (angle >= 112.5f && angle < 157.5f) return "behind right";
            if (angle >= -67.5f && angle < -22.5f) return "front left";
            if (angle >= -112.5f && angle < -67.5f) return "left";
            if (angle >= -157.5f && angle < -112.5f) return "behind left";
            return "behind";
        }
    }
}
