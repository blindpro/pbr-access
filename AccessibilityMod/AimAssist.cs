using System.Reflection;
using HarmonyLib;
using UnityEngine;
using InfimaGames.LowPolyShooterPack;

namespace AccessibilityMod
{
    /// <summary>
    /// Provides aim assist for accessibility:
    /// - When firing, gently nudges aim toward the nearest visible enemy
    /// - Uses a cone in front of the player to find targets
    /// - Applies smooth rotation so it feels natural
    /// </summary>
    public class AimAssist
    {
        private const float AssistConeAngle = 25f; // degrees from center
        private const float AssistMaxRange = 50f;
        private const float AssistRotationSpeed = 5f; // degrees per second of nudge
        private const float AssistInterval = 0.05f; // how often to update

        private static readonly FieldInfo _holdingButtonFire =
            AccessTools.Field(typeof(Character), "holdingButtonFire");
        private static readonly FieldInfo _rotationCharacter =
            AccessTools.Field(typeof(CameraLook), "rotationCharacter");

        private float _timer;

        public void Tick()
        {
            var player = CharacterMultiplayer.GetMainPlayer();
            if (player == null || player.IsDead()) return;

            if (MatchmakingManager.Instance == null) return;
            if (MatchmakingManager.Instance.GetRoomStatus() != MatchmakingManager.RoomStatus.Playing) return;

            var character = player.GetComponent<Character>();
            if (character == null) return;

            // Only assist while firing
            bool isFiring = (bool)_holdingButtonFire.GetValue(character);
            if (!isFiring) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = AssistInterval;

            var target = FindNearestTarget(player);
            if (target == null) return;

            NudgeAimToward(player, target);
        }

        private CharacterMultiplayer FindNearestTarget(CharacterMultiplayer player)
        {
            Vector3 playerPos = player.transform.position + Vector3.up * 1.2f;
            Vector3 forward = player.transform.forward;

            CharacterMultiplayer bestTarget = null;
            float bestAngle = AssistConeAngle;

            foreach (var other in CharacterMultiplayer.characters)
            {
                if (other == null || other == player || other.IsDead()) continue;
                if (other.isMainPlayer) continue;

                Vector3 targetPos = other.transform.position + Vector3.up * 1.2f;
                Vector3 toTarget = targetPos - playerPos;
                float dist = toTarget.magnitude;

                if (dist > AssistMaxRange || dist < 0.5f) continue;

                float angle = Vector3.Angle(forward, toTarget);
                if (angle > AssistConeAngle) continue;

                // Prefer targets closer to crosshair center
                if (angle < bestAngle)
                {
                    // Quick line-of-sight check
                    if (!Physics.Raycast(playerPos, toTarget.normalized, dist * 0.9f,
                        GetObstacleMask(), QueryTriggerInteraction.Ignore))
                    {
                        bestAngle = angle;
                        bestTarget = other;
                    }
                }
            }

            return bestTarget;
        }

        private void NudgeAimToward(CharacterMultiplayer player, CharacterMultiplayer target)
        {
            Vector3 playerPos = player.transform.position + Vector3.up * 1.2f;
            Vector3 targetPos = target.transform.position + Vector3.up * 1.2f;
            Vector3 toTarget = (targetPos - playerPos).normalized;

            // Calculate desired rotation
            Quaternion desiredRot = Quaternion.LookRotation(toTarget);
            float maxStep = AssistRotationSpeed * AssistInterval;

            // Smoothly rotate toward target
            Quaternion currentRot = player.transform.rotation;
            Quaternion newRot = Quaternion.RotateTowards(currentRot, desiredRot, maxStep);
            player.transform.rotation = newRot;

            // Also update CameraLook so the rotation sticks
            var cameraLook = player.GetComponentInChildren<CameraLook>();
            if (cameraLook != null && _rotationCharacter != null)
            {
                Quaternion camRot = (Quaternion)_rotationCharacter.GetValue(cameraLook);
                camRot = Quaternion.RotateTowards(camRot, desiredRot, maxStep);
                _rotationCharacter.SetValue(cameraLook, camRot);
            }
        }

        private static int GetObstacleMask()
        {
            int mask = 1 << 0; // Default
            int ground = LayerMask.NameToLayer("Ground");
            if (ground >= 0) mask |= 1 << ground;
            int terrain = LayerMask.NameToLayer("Terrain");
            if (terrain >= 0) mask |= 1 << terrain;
            int building = LayerMask.NameToLayer("Building");
            if (building >= 0) mask |= 1 << building;
            int env = LayerMask.NameToLayer("Environment");
            if (env >= 0) mask |= 1 << env;
            return mask;
        }
    }
}
