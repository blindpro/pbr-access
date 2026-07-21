using UnityEngine;
using InfimaGames.LowPolyShooterPack;

namespace AccessibilityMod
{
    /// <summary>
    /// Press T to hear why the lock tone is or is not sounding.
    ///
    /// "It still will not lock" can mean half a dozen different failures - the
    /// enemy is not being seen at all, cover is being reported where there is
    /// none, the rig has no bones to aim at, the aim assist is not engaging, or
    /// the aim is simply still wide - and they are indistinguishable by ear. This
    /// speaks the actual numbers so the failing stage can be named instead of
    /// guessed at.
    /// </summary>
    public class LockDiagnostics
    {
        private const float ReportRange = 120f;

        public void Tick()
        {
            if (!Input.GetKeyDown(KeyCode.T)) return;

            var player = CharacterMultiplayer.GetMainPlayer();
            if (player == null || player.IsDead())
            {
                ScreenReaderManager.Speak("Lock check unavailable", true);
                return;
            }

            // Targeting is switched off for the whole plane ride, so reporting a
            // range and an angle here reads as a fault when it is the intended
            // behaviour - there is no shot to take from the plane.
            var parachute = player.GetComponent<CharacterParachute>();
            if (parachute != null && parachute.isParachuting)
            {
                ScreenReaderManager.Speak("Lock check, targeting off until you land", true);
                return;
            }

            GetAim(player, out Vector3 origin, out Vector3 dir);

            // The most-centered hostile, whether or not anything else would accept
            // it as a target - the point is to explain why it was rejected.
            CharacterMultiplayer nearest = null;
            float nearestAngle = float.MaxValue;
            float nearestDist = 0f;

            foreach (var other in CharacterMultiplayer.characters)
            {
                if (!AudioTargeting.IsHostile(player, other)) continue;

                Vector3 chest = Targeting.ChestOf(other);
                float dist = Vector3.Distance(origin, chest);
                if (dist > ReportRange) continue;

                float angle = Vector3.Angle(dir, chest - origin);
                if (angle < nearestAngle)
                {
                    nearestAngle = angle;
                    nearest = other;
                    nearestDist = dist;
                }
            }

            if (nearest == null)
            {
                ScreenReaderManager.Speak("Lock check, no enemies in range", true);
                return;
            }

            bool visible = Targeting.HasLineOfSight(player, origin, nearest, out Vector3 _);
            float aimError = Targeting.AimError(origin, dir, nearest);
            bool shotLands = Targeting.IsShotLanding(player, origin, dir, ReportRange, nearest);
            int bones = Targeting.BoneCount(nearest);
            int joints = Targeting.JointCount(nearest);

            string report =
                $"Lock check. {Mathf.RoundToInt(nearestDist)} meters, " +
                $"{Mathf.RoundToInt(nearestAngle)} degrees off. " +
                (visible ? "Visible. " : "Blocked by cover. ") +
                (bones == 0 ? "No hit bones found. " : $"{bones} hit bones, ") +
                (joints == 0 ? "no skeleton, using fallback. " : $"{joints} joints. ") +
                (aimError == float.MaxValue
                    ? "Cannot measure aim."
                    : $"Aim {aimError:0.0} degrees off body. ") +
                (shotLands ? "Shot would hit." : "Shot would miss.");

            ScreenReaderManager.Speak(report, true);
        }

        private static void GetAim(CharacterMultiplayer player, out Vector3 origin, out Vector3 dir)
        {
            var thirdPerson = player.GetComponent<ThirdPerson>();
            if (thirdPerson != null && thirdPerson.fps_camera != null)
            {
                origin = thirdPerson.fps_camera.transform.position;
                dir = thirdPerson.fps_camera.transform.forward;
                return;
            }

            origin = player.transform.position + Vector3.up * 1.5f;
            dir = player.transform.forward;
        }
    }
}
