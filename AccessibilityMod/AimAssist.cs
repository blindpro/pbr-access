using System.Reflection;
using HarmonyLib;
using UnityEngine;
using InfimaGames.LowPolyShooterPack;

namespace AccessibilityMod
{
    /// <summary>
    /// Provides aim assist for accessibility, split along the line between what the
    /// player can do and what they cannot:
    ///
    /// - Yaw is theirs. The beeps say which way to turn and the arrow keys turn, so
    ///   finding the enemy left to right is the work, and the assist only adds a
    ///   gentle magnetism over the last few degrees where the beeps run out of
    ///   resolution. It never hauls the view around.
    /// - Pitch is the assist's, over a much wider cone. Not generosity: the arrow
    ///   keys turn yaw only, so there is no control bound to up and down at all,
    ///   and every degree of vertical error is a degree the player has no way to
    ///   remove. The assist holds the elevation while they do the turning, so
    ///   arriving in yaw is arriving on the body.
    ///
    /// This replaced a key that turned to face the enemy outright. That worked and
    /// it took the fight out of it - the aim was handed over rather than earned.
    /// The split keeps the hunt in the player's hands and quietly covers only the
    /// axis their keyboard cannot reach.
    ///
    /// Everything is measured on the shot ray itself - the fps camera's position
    /// and forward, which is the exact ray ThirdPerson.ComputeLookAtRaycast fires
    /// and the only thing that decides damage. Steering used to be judged against
    /// the body's forward and the CameraLook pivot's own frame instead, so the
    /// assist would centre the body, declare itself settled, and leave the shot ray
    /// pointing wherever the rig's offset put it. That leftover error is invisible
    /// to the assist but not to the lock tone, which is why the lock never closed.
    /// </summary>
    public class AimAssist
    {
        // How far off the enemy can be and still be worth correcting for. Wide,
        // because pitch is corrected across the whole of it - an enemy well off to
        // the side still has an elevation, and having it already right is what makes
        // the turn onto them land on the body instead of over their head.
        private const float AssistConeAngle = 60f;
        private const float AssistMaxRange = 80f;

        // Yaw magnetism only starts here - the player's own turn has to cover
        // everything outside it. This is roughly where the beeps stop being able to
        // resolve a difference, which is exactly where help should begin.
        private const float YawConeAngle = 12f;

        // Degrees per second of yaw. Deliberately small: at the edge it is a hint
        // that something is there to settle onto, and even at its firmest it is
        // slower than the arrow keys, so the player can always turn out of it.
        private const float YawSpeedFar = 12f;
        private const float YawSpeedNear = 45f;

        // Pitch is the assist's job outright, so it is free to be quick. It still
        // ramps, so a target crossing at the edge of the cone does not throw the
        // camera up and down.
        private const float PitchSpeedFar = 40f;
        private const float PitchSpeedNear = 120f;

        // Firing is a commitment: both axes tighten so a burst stays on the body.
        private const float FireYawSpeed = 90f;
        private const float FirePitchSpeed = 150f;
        private const float FireYawCone = 25f;

        // Below this the aim is already on target - stop, or it jitters. It has to
        // be tiny: the game only damages what its single camera ray strikes, and at
        // 60 m even half a degree of leftover error is half a meter of miss.
        private const float SettleAngle = 0.03f;

        // The enemy already being tracked keeps the assist through a wider cone
        // and wins ties, so the aim does not flick between two nearby enemies.
        private const float TargetStickyMultiplier = 1.6f;
        private const float TargetStickyBias = 0.5f;

        private static readonly FieldInfo _holdingButtonFire =
            AccessTools.Field(typeof(Character), "holdingButtonFire");
        private static readonly FieldInfo _rotationCharacter =
            AccessTools.Field(typeof(CameraLook), "rotationCharacter");
        private static readonly FieldInfo _rotationCamera =
            AccessTools.Field(typeof(CameraLook), "rotationCamera");

        private CharacterMultiplayer _target;

        // The exposed point on the target that the assist steers at. Aiming at a
        // fixed chest height drove the crosshair into whatever the enemy was
        // crouched behind; this is whichever part we can actually see.
        private Vector3 _aimPoint;

        public void Tick()
        {
            var player = CharacterMultiplayer.GetMainPlayer();
            if (player == null || player.IsDead()) { Forget(); return; }

            if (MatchmakingManager.Instance == null) { Forget(); return; }
            if (MatchmakingManager.Instance.GetRoomStatus() != MatchmakingManager.RoomStatus.Playing)
            {
                Forget();
                return;
            }

            // Nothing can be shot from the plane, so nothing is aimed from it either.
            var parachute = player.GetComponent<CharacterParachute>();
            if (parachute != null && parachute.isParachuting) { Forget(); return; }

            var character = player.GetComponent<Character>();
            if (character == null) return;

            // CameraLook owns the pitch pivot; without it there is nothing to steer.
            var cameraLook = player.GetComponentInChildren<CameraLook>();
            if (cameraLook == null) return;

            bool isFiring = (bool)_holdingButtonFire.GetValue(character);

            GetAim(player, cameraLook, out Vector3 origin, out Vector3 aimDir);

            _target = FindTarget(player, origin, aimDir, AssistConeAngle);
            if (_target == null) return;

            Vector3 toTarget = _aimPoint - origin;
            float offBy = Vector3.Angle(aimDir, toTarget);

            // Ramped on how close the player has already got, so the correction is
            // always proportionate to the work left rather than a constant tug.
            float centred = 1f - Mathf.Clamp01(offBy / AssistConeAngle);

            // Pitch first and always: it is the axis with no key bound to it.
            float pitchSpeed = isFiring
                ? FirePitchSpeed
                : Mathf.Lerp(PitchSpeedFar, PitchSpeedNear, centred);
            SteerPitch(cameraLook, aimDir, toTarget, pitchSpeed * Time.deltaTime);

            // Yaw only once they have brought the enemy close on their own.
            float yawCone = isFiring ? FireYawCone : YawConeAngle;
            if (offBy > yawCone) return;

            float yawSpeed = isFiring
                ? FireYawSpeed
                : Mathf.Lerp(YawSpeedFar, YawSpeedNear, 1f - Mathf.Clamp01(offBy / yawCone));
            SteerYaw(player, cameraLook, aimDir, toTarget, yawSpeed * Time.deltaTime);
        }

        /// <summary>Drops everything: dead, between matches, or back in the plane.</summary>
        private void Forget()
        {
            _target = null;
        }

        /// <summary>
        /// The most-centered enemy we could actually hit right now, preferring the
        /// one already being tracked.
        /// </summary>
        private CharacterMultiplayer FindTarget(CharacterMultiplayer player, Vector3 origin,
            Vector3 aimDir, float cone)
        {
            CharacterMultiplayer best = null;
            float bestScore = float.MaxValue;

            foreach (var other in CharacterMultiplayer.characters)
            {
                if (!AudioTargeting.IsHostile(player, other)) continue;

                float dist = Vector3.Distance(Targeting.ChestOf(other), origin);
                if (dist < 0.5f || dist > AssistMaxRange) continue;

                // Behind a wall means no shot, so there is nothing to assist.
                if (!Targeting.HasLineOfSight(player, origin, other, out Vector3 visible))
                    continue;

                bool tracked = other == _target;
                float angle = Vector3.Angle(aimDir, visible - origin);
                if (angle > (tracked ? cone * TargetStickyMultiplier : cone)) continue;

                float score = tracked ? angle * TargetStickyBias : angle;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = other;
                    _aimPoint = visible;
                }
            }

            return best;
        }

        /// <summary>
        /// Turns the body at most <paramref name="maxStep"/> degrees toward the
        /// target, exactly as arrow-key turning does, keeping CameraLook's stored
        /// rotation in step so smoothed look mode agrees.
        ///
        /// The error is the difference between where the shot ray currently points
        /// and where the target is, never between a rig transform and the target.
        /// The camera hangs off the body, so a degree of body yaw is a degree of ray
        /// yaw - which makes the error directly applicable, and makes any fixed
        /// offset between the rig and the camera something the loop drives out
        /// instead of settling into.
        /// </summary>
        private void SteerYaw(CharacterMultiplayer player, CameraLook cameraLook, Vector3 aimDir,
            Vector3 toTarget, float maxStep)
        {
            Vector3 flatTo = new Vector3(toTarget.x, 0f, toTarget.z);
            Vector3 flatAim = new Vector3(aimDir.x, 0f, aimDir.z);
            if (flatTo.sqrMagnitude < 0.0001f || flatAim.sqrMagnitude < 0.0001f) return;

            float yawError = Vector3.SignedAngle(flatAim, flatTo, Vector3.up);
            if (Mathf.Abs(yawError) <= SettleAngle) return;

            Quaternion turn = Quaternion.Euler(0f, Mathf.Clamp(yawError, -maxStep, maxStep), 0f);
            player.transform.rotation *= turn;

            if (_rotationCharacter != null)
            {
                var rot = (Quaternion)_rotationCharacter.GetValue(cameraLook);
                _rotationCharacter.SetValue(cameraLook, rot * turn);
            }
        }

        /// <summary>
        /// Tilts the camera pivot toward the target, as the difference of the two
        /// elevation angles. Elevation does not change when the body yaws, so this
        /// holds while the player turns underneath it - which is the whole point,
        /// since they are turning and cannot tilt. The game clamps the pivot to its
        /// own limits next frame.
        /// </summary>
        private void SteerPitch(CameraLook cameraLook, Vector3 aimDir, Vector3 toTarget,
            float maxStep)
        {
            if (toTarget.sqrMagnitude < 0.0001f || aimDir.sqrMagnitude < 0.0001f) return;

            float pitchError = Elevation(aimDir) - Elevation(toTarget);
            if (Mathf.Abs(pitchError) <= SettleAngle) return;

            Quaternion look = Quaternion.Euler(Mathf.Clamp(pitchError, -maxStep, maxStep), 0f, 0f);
            cameraLook.transform.localRotation *= look;

            if (_rotationCamera != null)
            {
                var rot = (Quaternion)_rotationCamera.GetValue(cameraLook);
                _rotationCamera.SetValue(cameraLook, rot * look);
            }
        }

        /// <summary>
        /// Degrees above the horizon, signed the way the game's pitch is: positive
        /// is looking down, matching Quaternion.Euler's x axis.
        /// </summary>
        private static float Elevation(Vector3 direction)
        {
            float horizontal = new Vector2(direction.x, direction.z).magnitude;
            return -Mathf.Atan2(direction.y, horizontal) * Mathf.Rad2Deg;
        }

        private static void GetAim(CharacterMultiplayer player, CameraLook cameraLook,
            out Vector3 origin, out Vector3 dir)
        {
            var tp = player.GetComponent<ThirdPerson>();
            if (tp != null && tp.fps_camera != null)
            {
                origin = tp.fps_camera.transform.position;
                dir = tp.fps_camera.transform.forward;
                return;
            }

            origin = cameraLook.transform.position;
            dir = cameraLook.transform.forward;
        }
    }
}
