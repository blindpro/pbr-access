using UnityEngine;
using InfimaGames.LowPolyShooterPack;

namespace AccessibilityMod
{
    /// <summary>
    /// Guides the player toward the safe zone with spoken directional callouts.
    /// - N key: on-demand safe zone report (direction, distance, status)
    /// - Auto-announces when entering or leaving the safe zone
    /// - Periodic direction reminders when outside the zone while it shrinks
    /// </summary>
    public class SafeZoneNav
    {
        private const float OutsideAnnounceInterval = 15f;
        private float _outsideTimer;

        private bool _wasInsideZone;
        private bool _zoneStateSet;

        private const float OnDemandCooldown = 1f;
        private float _onDemandCooldownTimer;

        public void Tick()
        {
            var player = CharacterMultiplayer.GetMainPlayer();
            if (player == null || player.IsDead())
            {
                if (_zoneStateSet)
                {
                    _wasInsideZone = false;
                    _zoneStateSet = false;
                    _outsideTimer = 0f;
                    _onDemandCooldownTimer = 0f;
                }
                return;
            }

            if (MatchmakingManager.Instance == null) return;
            var status = MatchmakingManager.Instance.GetRoomStatus();
            if (status != MatchmakingManager.RoomStatus.Playing) return;

            HandleOnDemand(player);
            MonitorZoneStatus(player);
        }

        private void HandleOnDemand(CharacterMultiplayer player)
        {
            _onDemandCooldownTimer -= Time.deltaTime;

            if (!Input.GetKeyDown(KeyCode.N)) return;
            if (_onDemandCooldownTimer > 0f) return;

            _onDemandCooldownTimer = OnDemandCooldown;

            var zoneMgr = GetZoneManager();
            if (zoneMgr == null)
            {
                ScreenReaderManager.Speak("Zone info unavailable");
                return;
            }

            var capsule = GetActiveCapsule(zoneMgr);
            if (capsule == null)
            {
                ScreenReaderManager.Speak("No safe zone yet");
                return;
            }

            ScreenReaderManager.Speak(DescribeZone(player, capsule));
        }

        private void MonitorZoneStatus(CharacterMultiplayer player)
        {
            var zoneMgr = GetZoneManager();
            if (zoneMgr == null) return;

            var capsule = GetActiveCapsule(zoneMgr);
            if (capsule == null) return;

            bool inside = IsInside(player, capsule);

            if (_zoneStateSet && inside != _wasInsideZone)
            {
                ScreenReaderManager.Speak(inside ? "In safe zone" : "Left the safe zone!");
                _outsideTimer = 0f;
            }

            _wasInsideZone = inside;
            _zoneStateSet = true;

            if (inside || !zoneMgr.IsShrinking()) return;

            // Outside while the circle is closing: keep repeating the way back.
            _outsideTimer -= Time.deltaTime;
            if (_outsideTimer > 0f) return;
            _outsideTimer = OutsideAnnounceInterval;

            // interrupt: false so close-range wall and enemy callouts win.
            ScreenReaderManager.Speak(DescribeZone(player, capsule), false);
        }

        /// <summary>
        /// The one sentence both the N key and the shrink reminder speak, e.g.
        /// "Safe zone front right, 200 meters" or "In safe zone, 80 meters from the edge".
        /// </summary>
        private static string DescribeZone(CharacterMultiplayer player, CapsuleCollider capsule)
        {
            float radius = GetRadius(capsule);
            Vector3 playerPos = player.transform.position;
            Vector3 center = FlatCenter(capsule, playerPos.y);

            float distToCenter = Vector3.Distance(playerPos, center);
            string direction = GetRelativeDirection(player.transform, center);

            if (distToCenter <= radius)
            {
                int toEdge = Mathf.RoundToInt(radius - distToCenter);
                if (toEdge <= 5)
                    return "In safe zone, right at the edge";
                return $"In safe zone, {toEdge} meters from the edge";
            }

            int distToEdge = Mathf.RoundToInt(distToCenter - radius);
            if (distToEdge <= 5)
                return $"Safe zone {direction}, just ahead";
            return $"Safe zone {direction}, {distToEdge} meters";
        }

        /// <summary>
        /// Mirrors CharacterMultiplayer.IsInsideSafeZone: a flat circle test against
        /// the capsule's horizontal extent, ignoring height.
        /// </summary>
        private static bool IsInside(CharacterMultiplayer player, CapsuleCollider capsule)
        {
            Vector3 playerPos = player.transform.position;
            return Vector3.Distance(playerPos, FlatCenter(capsule, playerPos.y)) <= GetRadius(capsule);
        }

        private static float GetRadius(CapsuleCollider capsule)
        {
            return capsule.bounds.extents.x;
        }

        /// <summary>Zone centre pulled to the player's own height, so slopes don't inflate the distance.</summary>
        private static Vector3 FlatCenter(CapsuleCollider capsule, float y)
        {
            Vector3 center = capsule.bounds.center;
            center.y = y;
            return center;
        }

        private static CapsuleCollider GetActiveCapsule(DamageZoneManager zoneMgr)
        {
            var zoneTransform = zoneMgr.damageZone;
            if (zoneTransform == null) return null;
            var capsule = zoneTransform.GetComponent<CapsuleCollider>();
            if (capsule == null || !capsule.gameObject.activeSelf) return null;
            return capsule;
        }

        private static string GetRelativeDirection(Transform playerTransform, Vector3 targetPos)
        {
            Vector3 toTarget = targetPos - playerTransform.position;
            toTarget.y = 0;

            if (toTarget.sqrMagnitude < 0.01f)
                return "ahead";

            float angle = Vector3.SignedAngle(playerTransform.forward, toTarget, Vector3.up);
            return DirectionFromAngle(angle);
        }

        private static DamageZoneManager GetZoneManager()
        {
            if (GameManager.Instance == null) return null;
            return GameManager.Instance.GetComponent<DamageZoneManager>();
        }

        private static string DirectionFromAngle(float angle)
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
