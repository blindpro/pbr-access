using System.Reflection;
using HarmonyLib;
using UnityEngine;
using InfimaGames.LowPolyShooterPack;

namespace AccessibilityMod
{
    /// <summary>
    /// Locks the vertical axis of mouse look for the main player.
    ///
    /// Pitch belongs to the aim assist (see <see cref="AimAssist"/>): the arrow
    /// keys turn yaw only, so elevation is the one axis a screen-reader player has
    /// no deliberate control over, and the assist holds it on the target's body
    /// while they turn. A stray flick of vertical mouse movement fights that - it
    /// tilts the camera off the elevation the assist just found, and there is no
    /// key to put it back. So we keep the horizontal (yaw) component of mouse look,
    /// which is still a natural way to turn, and zero the vertical one.
    ///
    /// Character.OnLook writes both axes into axisLook (later read by CameraLook via
    /// GetInputLook); zeroing y in a postfix leaves x - and therefore left/right
    /// mouse turning - untouched. Fire (left click) and aim (right click) run
    /// through their own input actions and are not affected.
    /// </summary>
    [HarmonyPatch(typeof(Character), "OnLook")]
    public static class MouseLookPatches
    {
        private static readonly FieldInfo _axisLook =
            AccessTools.Field(typeof(Character), "axisLook");

        [HarmonyPostfix]
        static void OnLook_Postfix(Character __instance)
        {
            if (_axisLook == null) return;

            var look = (Vector2)_axisLook.GetValue(__instance);
            if (look.y == 0f) return;

            look.y = 0f;
            _axisLook.SetValue(__instance, look);
        }
    }
}
