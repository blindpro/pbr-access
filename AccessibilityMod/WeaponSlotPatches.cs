using HarmonyLib;
using InfimaGames.LowPolyShooterPack;

namespace AccessibilityMod
{
    /// <summary>
    /// Stops the starting pistol from being shuffled out of slot 1.
    ///
    /// Picking up your first real weapon reads as the game handing you a second gun
    /// from nowhere, and it is easy to hear it as a bug. It is not - it is the Glock
    /// you spawned with. pick_weapon1_from_box, when slot 1 is full and slot 2 is
    /// empty, moves what you are holding across to slot 2 and puts the new weapon in
    /// slot 1, so the pistol appears in a slot you never put it in and announces
    /// itself as an arrival.
    ///
    /// The new weapon goes to the empty slot instead, and the character switches to
    /// hold it. Nothing is displaced, the slot a gun lives in stays where the player
    /// put it, and picking up a rifle still ends with the rifle in your hands. Once
    /// both slots are full the game's own rule takes over again: a pickup replaces
    /// whatever you are holding, which is what a player reaching for a gun means.
    /// </summary>
    [HarmonyPatch]
    public static class WeaponSlotPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PickupsManager), "pick_weapon1_from_box")]
        static bool pick_weapon1_from_box_Prefix(CharacterInventory characterInventory,
            AmmoBox currentAmmoBox, PickupsManager.Item item)
        {
            if (characterInventory == null || currentAmmoBox == null || item == null) return true;

            // Only the case the game reshuffles for. With slot 2 already occupied the
            // vanilla path replaces the held weapon, which is correct.
            if (characterInventory.weapon1 == null || characterInventory.weapon2 != null) return true;

            // Picking the weapon that is already in hand should do nothing at all.
            if (item == characterInventory.weapon1) return false;

            characterInventory.weapon2 = currentAmmoBox.ReplaceItem(item, characterInventory.weapon2);

            // SetCurrentWeapon refuses to select an empty slot, so this has to follow
            // the assignment. It calls Apply itself, which draws the new weapon.
            if (characterInventory.weapon2 != null)
                characterInventory.SetCurrentWeapon(1);

            return false;
        }
    }
}
