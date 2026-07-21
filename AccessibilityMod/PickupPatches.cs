using HarmonyLib;
using InfimaGames.LowPolyShooterPack;
using UnityEngine;

namespace AccessibilityMod
{
    /// <summary>
    /// Makes item pickup much easier for blind/low-vision players:
    /// - Increases pick range from 1.5m to 3.5m
    /// - Auto-targets the nearest item in a box when within range,
    ///   removing the requirement to aim the camera directly at the item
    /// </summary>
    [HarmonyPatch]
    public static class PickupPatches
    {
        private const float AccessiblePickRange = 3.5f;

        /// <summary>
        /// After PickupsManager.Start, increase the pick range.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PickupsManager), "Start")]
        static void PickupsManager_Start_Postfix(PickupsManager __instance)
        {
            __instance.pickRange = AccessiblePickRange;
            Plugin.Logger.LogInfo($"PickupPatches: pickRange set to {AccessiblePickRange}");
        }

        /// <summary>
        /// After AmmoBox.Update, if the player is within range but no item was
        /// targeted by the camera ray, auto-target the first available item.
        /// This lets blind players pick up items without precise aiming.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AmmoBox), "Update")]
        static void AmmoBox_Update_Postfix(AmmoBox __instance)
        {
            // If the normal Update already targeted an item, nothing to do
            if (__instance.currentItem != null) return;

            // Check if within range
            if (!__instance.isNearPlayer) return;

            // Must have items
            if (__instance.items == null || __instance.items.Count == 0) return;

            // Auto-target the first item
            var item = __instance.items[0];
            if (item == null) return;

            var pickupsMgr = GameManager.Instance.GetComponent<PickupsManager>();
            if (pickupsMgr == null) return;

            __instance.currentItem = item;
            pickupsMgr.pickAmmoBox = __instance;
            pickupsMgr.pickItem = item;
            pickupsMgr.pickUI_visible = true;
        }
    }
}
