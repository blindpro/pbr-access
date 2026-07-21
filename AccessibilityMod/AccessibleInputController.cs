using System.Reflection;
using HarmonyLib;
using UnityEngine;
using InfimaGames.LowPolyShooterPack;

namespace AccessibilityMod
{
    /// <summary>
    /// Adds accessible input controls:
    /// - Left Control: fire weapon
    /// - Left/Right Arrow: turn character
    /// - F: announce compass facing direction
    ///
    /// E (pick up loot) is handled by <see cref="LootMenu"/>, which has to own the
    /// key because a pile of several items needs a list rather than one action.
    /// </summary>
    public class AccessibleInputController
    {
        private const float TurnSpeed = 120f; // degrees per second

        // Cached reflection for Character private fields
        private static readonly FieldInfo _holdingButtonFire =
            AccessTools.Field(typeof(Character), "holdingButtonFire");
        private static readonly FieldInfo _shotsFired =
            AccessTools.Field(typeof(Character), "shotsFired");
        private static readonly FieldInfo _cursorLocked =
            AccessTools.Field(typeof(Character), "cursorLocked");
        private static readonly FieldInfo _lastShotTime =
            AccessTools.Field(typeof(Character), "lastShotTime");
        private static readonly MethodInfo _fireMethod =
            AccessTools.Method(typeof(Character), "Fire");
        private static readonly MethodInfo _canPlayAnimationFire =
            AccessTools.Method(typeof(Character), "CanPlayAnimationFire");

        // CameraLook rotation state - we modify this directly to avoid
        // the axisLook persistence bug that causes continuous spinning
        private static readonly FieldInfo _rotationCharacter =
            AccessTools.Field(typeof(CameraLook), "rotationCharacter");

        private bool _wasFiring;

        public void Tick()
        {
            var player = CharacterMultiplayer.GetMainPlayer();
            if (player == null) return;

            var character = player.GetComponent<Character>();
            if (character == null) return;

            // Only process during active gameplay (cursor locked = in game)
            bool locked = (bool)_cursorLocked.GetValue(character);
            var thirdPerson = player.GetComponent<ThirdPerson>();
            if (!locked || thirdPerson == null || !thirdPerson.isActive)
            {
                // Release fire if we were holding it when gameplay ended
                if (_wasFiring)
                {
                    _holdingButtonFire.SetValue(character, false);
                    _shotsFired.SetValue(character, 0);
                    _wasFiring = false;
                }
                return;
            }

            HandleFire(character);
            HandleTurning(player);
            HandleFacingReadout(player);
        }

        private void HandleFire(Character character)
        {
            bool fireDown = Input.GetKeyDown(KeyCode.LeftControl);
            bool fireUp = Input.GetKeyUp(KeyCode.LeftControl);

            if (fireDown)
            {
                _holdingButtonFire.SetValue(character, true);
                _shotsFired.SetValue(character, 0);
                _wasFiring = true;

                // For non-automatic weapons, Fire() must be called explicitly
                // (automatic weapons fire continuously in Update via holdingButtonFire)
                var weapon = character.GetEquippedWeapon();
                if (weapon != null && !weapon.IsAutomatic()
                    && weapon.HasAmmunition()
                    && (bool)_canPlayAnimationFire.Invoke(character, null))
                {
                    float lastShot = (float)_lastShotTime.GetValue(character);
                    if (Time.time - lastShot > 60f / weapon.GetRateOfFire())
                    {
                        _fireMethod.Invoke(character, null);
                    }
                }
            }

            if (fireUp)
            {
                _holdingButtonFire.SetValue(character, false);
                _shotsFired.SetValue(character, 0);
                _wasFiring = false;
            }
        }

        private void HandleTurning(CharacterMultiplayer player)
        {
            bool leftHeld = Input.GetKey(KeyCode.LeftArrow);
            bool rightHeld = Input.GetKey(KeyCode.RightArrow);

            if (!leftHeld && !rightHeld) return;

            float turnAmount = TurnSpeed * Time.deltaTime;
            float yaw = 0f;

            if (leftHeld) yaw = -turnAmount;
            if (rightHeld) yaw = turnAmount;

            // Directly rotate the CameraLook's stored rotation target and the
            // player transform together. This avoids the axisLook persistence
            // bug — axisLook retains its value between frames when the mouse
            // isn't moving, causing CameraLook to keep applying the turn.
            Quaternion turnQuat = Quaternion.Euler(0f, yaw, 0f);
            player.transform.rotation *= turnQuat;

            var cameraLook = player.GetComponentInChildren<CameraLook>();
            if (cameraLook != null && _rotationCharacter != null)
            {
                Quaternion rot = (Quaternion)_rotationCharacter.GetValue(cameraLook);
                rot *= turnQuat;
                _rotationCharacter.SetValue(cameraLook, rot);
            }
        }

        private void HandleFacingReadout(CharacterMultiplayer player)
        {
            if (!Input.GetKeyDown(KeyCode.F)) return;

            float yaw = player.transform.eulerAngles.y;
            // Normalize to 0-360
            yaw = ((yaw % 360f) + 360f) % 360f;

            string cardinal = GetCardinalDirection(yaw);
            int degrees = Mathf.RoundToInt(yaw);

            ScreenReaderManager.Speak($"{cardinal}, {degrees} degrees");
        }

        private static string GetCardinalDirection(float yaw)
        {
            // 0 = North, 90 = East, 180 = South, 270 = West
            if (yaw >= 337.5f || yaw < 22.5f) return "North";
            if (yaw >= 22.5f && yaw < 67.5f) return "North East";
            if (yaw >= 67.5f && yaw < 112.5f) return "East";
            if (yaw >= 112.5f && yaw < 157.5f) return "South East";
            if (yaw >= 157.5f && yaw < 202.5f) return "South";
            if (yaw >= 202.5f && yaw < 247.5f) return "South West";
            if (yaw >= 247.5f && yaw < 292.5f) return "West";
            if (yaw >= 292.5f && yaw < 337.5f) return "North West";
            return "Unknown";
        }
    }
}
