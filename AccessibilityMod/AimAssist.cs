using System.Reflection;
using HarmonyLib;
using UnityEngine;
using InfimaGames.LowPolyShooterPack;

namespace AccessibilityMod
{
    /// <summary>
    /// Provides aim assist for accessibility:
    /// - Steers both the body (yaw) and the camera pivot (pitch) toward the
    ///   most-centered enemy that can actually be shot
    /// - Pulls hard while firing, and gently once the crosshair is already close,
    ///   so the last few degrees onto a target close themselves instead of having
    ///   to be found by ear
    ///
    /// Pitch is the reason this exists in its current form: the shot follows the
    /// camera, and the camera's pitch lives on the CameraLook transform, not on
    /// the character. Correcting yaw alone left the aim permanently above or below
    /// the target, so a lock never quite closed.
    /// </summary>
    public class AimAssist
    {
        // While firing, anything in this cone is fair game.
        private const float FireConeAngle = 30f;
        // Before firing the assist is only allowed to bite once the player has
        // already brought the crosshair close, so free look is never hijacked.
        private const float StickyConeAngle = 10f;
        private const float AssistMaxRange = 80f;

        // Degrees per second. Firing is a commitment, so it steers hard; the
        // pre-fire magnetism is a nudge, not a snap.
        private const float FireTurnSpeed = 120f;
        private const float StickyTurnSpeed = 35f;

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
            if (player == null || player.IsDead()) { _target = null; return; }

            if (MatchmakingManager.Instance == null) { _target = null; return; }
            if (MatchmakingManager.Instance.GetRoomStatus() != MatchmakingManager.RoomStatus.Playing)
            {
                _target = null;
                return;
            }

            var parachute = player.GetComponent<CharacterParachute>();
            if (parachute != null && parachute.isParachuting) { _target = null; return; }

            var character = player.GetComponent<Character>();
            if (character == null) return;

            // CameraLook owns the pitch pivot; without it there is nothing to steer.
            var cameraLook = player.GetComponentInChildren<CameraLook>();
            if (cameraLook == null) return;

            bool isFiring = (bool)_holdingButtonFire.GetValue(character);

            GetAim(player, cameraLook, out Vector3 origin, out Vector3 aimDir);

            _target = FindTarget(player, origin, aimDir, isFiring ? FireConeAngle : StickyConeAngle);
            if (_target == null) return;

            float speed = isFiring ? FireTurnSpeed : StickyTurnSpeed;
            Steer(player, cameraLook, origin, _target, speed * Time.deltaTime);
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
        /// Rotates at most <paramref name="maxStep"/> degrees toward the target on
        /// each axis, matching how the game itself applies look input: yaw is
        /// post-multiplied onto the character, pitch onto the camera pivot, and the
        /// CameraLook fields are kept in step so smoothed look mode agrees.
        /// </summary>
        private void Steer(CharacterMultiplayer player, CameraLook cameraLook, Vector3 origin,
            CharacterMultiplayer target, float maxStep)
        {
            Vector3 toTarget = _aimPoint - origin;

            // Yaw: turn the body, exactly as arrow-key turning does.
            Vector3 flatTo = new Vector3(toTarget.x, 0f, toTarget.z);
            Vector3 flatForward = player.transform.forward;
            flatForward.y = 0f;
            if (flatTo.sqrMagnitude > 0.0001f && flatForward.sqrMagnitude > 0.0001f)
            {
                float yawError = Vector3.SignedAngle(flatForward, flatTo, Vector3.up);
                if (Mathf.Abs(yawError) > SettleAngle)
                {
                    Quaternion turn = Quaternion.Euler(0f, Mathf.Clamp(yawError, -maxStep, maxStep), 0f);
                    player.transform.rotation *= turn;

                    if (_rotationCharacter != null)
                    {
                        var rot = (Quaternion)_rotationCharacter.GetValue(cameraLook);
                        _rotationCharacter.SetValue(cameraLook, rot * turn);
                    }
                }
            }

            // Pitch: recomputed after the yaw step so it measures against where the
            // camera now points. The game clamps this to its own limits next frame.
            Vector3 local = cameraLook.transform.InverseTransformDirection(toTarget);
            float horizontal = new Vector2(local.x, local.z).magnitude;
            float pitchError = -Mathf.Atan2(local.y, horizontal) * Mathf.Rad2Deg;
            if (Mathf.Abs(pitchError) > SettleAngle)
            {
                Quaternion look = Quaternion.Euler(Mathf.Clamp(pitchError, -maxStep, maxStep), 0f, 0f);
                cameraLook.transform.localRotation *= look;

                if (_rotationCamera != null)
                {
                    var rot = (Quaternion)_rotationCamera.GetValue(cameraLook);
                    _rotationCamera.SetValue(cameraLook, rot * look);
                }
            }
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
