using HarmonyLib;
using InfimaGames.LowPolyShooterPack;

namespace AccessibilityMod
{
    /// <summary>
    /// Auditory feedback for weapon actions such as reloading, dry firing, and empty magazines.
    /// </summary>
    [HarmonyPatch]
    public static class WeaponFeedbackHandler
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), "Fire")]
        static void Fire_Postfix(Character __instance)
        {
            var mainPlayer = CharacterMultiplayer.GetMainPlayer();
            if (mainPlayer == null) return;
            if (__instance != mainPlayer.GetComponent<Character>()) return;

            var weapon = __instance.GetEquippedWeapon() as Weapon;
            if (weapon == null) return;

            // If the magazine is now completely empty
            if (weapon.GetAmmunitionCurrent() == 0)
            {
                int mags = weapon.GetCurrentMags();
                if (mags > 0)
                {
                    // Empty but has reserves: speak "Empty"
                    ScreenReaderManager.Speak(Loc.Get("weapon_empty"), true);
                }
                else
                {
                    // Empty and out of reserves: speak "Dry"
                    ScreenReaderManager.Speak(Loc.Get("weapon_dry"), true);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), "FireEmpty")]
        static void FireEmpty_Postfix(Character __instance)
        {
            var mainPlayer = CharacterMultiplayer.GetMainPlayer();
            if (mainPlayer == null) return;
            if (__instance != mainPlayer.GetComponent<Character>()) return;

            var weapon = __instance.GetEquippedWeapon() as Weapon;
            if (weapon == null) return;

            int mags = weapon.GetCurrentMags();
            if (mags <= 0)
            {
                // Trying to shoot with 0 ammo and 0 reserves
                ScreenReaderManager.Speak(Loc.Get("weapon_dry"), true);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), "PlayReloadAnimation")]
        static void PlayReloadAnimation_Postfix(Character __instance)
        {
            var mainPlayer = CharacterMultiplayer.GetMainPlayer();
            if (mainPlayer == null) return;
            if (__instance != mainPlayer.GetComponent<Character>()) return;

            ScreenReaderManager.Speak(Loc.Get("weapon_reloading"), true);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), "AnimationEndedReload")]
        static void AnimationEndedReload_Postfix(Character __instance)
        {
            var mainPlayer = CharacterMultiplayer.GetMainPlayer();
            if (mainPlayer == null) return;
            if (__instance != mainPlayer.GetComponent<Character>()) return;

            var weapon = __instance.GetEquippedWeapon() as Weapon;
            if (weapon == null)
            {
                ScreenReaderManager.Speak(Loc.Get("weapon_reloaded"), true);
                return;
            }

            int currentAmmo = weapon.GetAmmunitionCurrent();
            ScreenReaderManager.Speak(Loc.Get("weapon_reloaded_ammo", currentAmmo), true);
        }
    }
}
