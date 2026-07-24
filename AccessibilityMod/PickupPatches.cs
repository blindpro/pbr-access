using HarmonyLib;
using InfimaGames.LowPolyShooterPack;
using UnityEngine;
using UnityEngine.InputSystem;

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
        /// The game's own interact key also calls PickCurrentItem, which grabs
        /// whatever the camera ray found - the very ambiguity the loot list exists
        /// to remove. Only the loot list gets to pick, so pressing E opens the list
        /// instead of silently taking an item at the same time.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PickupsManager), "PickCurrentItem")]
        static bool PickupsManager_PickCurrentItem_Prefix()
        {
            return LootMenu.IsPicking;
        }

        /// <summary>
        /// Escape belongs to the loot list while it is open. The game reaches the
        /// pause menu two ways - a raw key read in PauseInputs and the input-system
        /// cursor toggle - and both are held off, so Escape closes the list instead
        /// of dropping the player into the pause menu on top of it.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(HP.Generics.PauseInputs), "Update")]
        static bool PauseInputs_Update_Prefix()
        {
            return !LootMenu.BlocksPause;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameManager), "OnLockCursor")]
        static bool GameManager_OnLockCursor_Prefix()
        {
            return !LootMenu.BlocksPause && !InventoryMenu.BlocksGameInventory;
        }

        /// <summary>
        /// Left click takes the selected item out of the loot list, and activates
        /// the selected row of the inventory, so it must not also pull the trigger.
        /// Only the press and the shot are held off - a release still gets through,
        /// so a burst that was being held when a list opened cannot be left running
        /// with the button already up.
        /// </summary>
        // Harmony003 fires on the read of context.phase - the getter of a struct
        // property takes this by reference, which the analyzer cannot tell apart
        // from an assignment. Nothing here writes to the context.
#pragma warning disable Harmony003
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Character), "OnTryFire")]
        static bool Character_OnTryFire_Prefix(InputAction.CallbackContext context)
        {
            if (!LootMenu.BlocksFire && !InventoryMenu.BlocksFire) return true;
            return context.phase == InputActionPhase.Canceled;
        }
#pragma warning restore Harmony003

        /// <summary>
        /// The game's own inventory screen is mouse-only drag and drop, and opening
        /// it unlocks the cursor. Whichever key it is bound to, it must not come up
        /// behind the accessible inventory.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameManager), "OnInventory")]
        static bool GameManager_OnInventory_Prefix()
        {
            return !InventoryMenu.BlocksGameInventory;
        }

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
