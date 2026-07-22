using HarmonyLib;
using InfimaGames.LowPolyShooterPack;
using Photon.Pun;
using UnityEngine;

namespace AccessibilityMod
{
    /// <summary>
    /// Makes a vest and a helmet mean something.
    ///
    /// In the shipped game they mean nothing at all. Damage is
    /// health - weaponDamage, a flat byte off a flat byte, and neither slot is ever
    /// read on the way through; there is no headshot multiplier either. A level 3
    /// vest and bare skin die to the same number of bullets. The HUD still shows a
    /// level and a durability for both, so the screen promises a mechanic that the
    /// code does not have - which is worse than not having it, because loot
    /// decisions get made on the strength of it.
    ///
    /// Mitigation is derived from the item's level and nothing else. The obvious
    /// alternative - spending the durability the HUD displays - is not available:
    /// PickupsManager.items holds one Item instance per kind and every pile hands
    /// out references to those same instances, so decrementing item.value would wear
    /// down that armour for every other pile and every other character in the match.
    /// Levels are read-only and safe.
    ///
    /// Offline only, deliberately. Death is decided on the shooter's client from its
    /// own copy of the victim's health, and other players do not run this mod, so
    /// mitigating in a real multiplayer room would leave the player alive on their
    /// own screen and dead on everyone else's. In an offline room the local client
    /// is the only authority there is, so it stays consistent.
    /// </summary>
    [HarmonyPatch]
    public static class ArmorPatches
    {
        // Per level. A vest covers the torso, which is most of what a bullet finds,
        // so it carries the larger share.
        private const float VestPerLevel = 0.08f;
        private const float HelmetPerLevel = 0.04f;

        // However well equipped, armour is an edge and not a shield: a fight has to
        // stay winnable by whoever is shooting.
        private const float MaxReduction = 0.5f;

        /// <summary>
        /// RPC_Damage is the single door every source of damage comes through -
        /// bullets, grenades and the zone all call it - and it both decides death
        /// and forwards the number to the Damage RPC that subtracts it. Reducing it
        /// here therefore reduces the health lost and moves the death threshold by
        /// the same amount. Patching Damage as well would apply the reduction twice,
        /// since the value it receives has already been through here.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharacterMultiplayer), nameof(CharacterMultiplayer.RPC_Damage))]
        static void RPC_Damage_Prefix(CharacterMultiplayer __instance, ref byte damage,
            byte shooterActorId)
        {
            // Actor numbers start at 1; the damage zone passes 0. Armour is no help
            // against the storm closing in.
            if (shooterActorId == 0) return;

            if (!PhotonNetwork.OfflineMode) return;

            damage = Mitigate(__instance, damage);
        }

        /// <summary>
        /// What actually lands on this character, after their armour. Applies to
        /// everyone who wears it, players and bots alike - the bots do loot vests
        /// and helmets, and armour that only worked for one side would be a cheat
        /// rather than the mechanic the HUD has been describing all along.
        /// </summary>
        public static byte Mitigate(CharacterMultiplayer character, byte damage)
        {
            float reduction = ReductionFor(character);
            if (reduction <= 0f) return damage;

            int reduced = Mathf.RoundToInt(damage * (1f - reduction));

            // Never absorb a hit entirely: being shot has to cost something, or a
            // well-armoured player becomes unkillable rather than durable.
            return (byte)Mathf.Clamp(reduced, 1, damage);
        }

        /// <summary>The share of incoming damage this character's armour absorbs, 0 to 1.</summary>
        public static float ReductionFor(CharacterMultiplayer character)
        {
            if (character == null) return 0f;

            var charInv = character.GetComponent<CharacterInventory>();
            if (charInv == null) return 0f;

            float reduction = LevelOf(charInv.vest) * VestPerLevel
                            + LevelOf(charInv.helmet) * HelmetPerLevel;

            return Mathf.Clamp(reduction, 0f, MaxReduction);
        }

        /// <summary>True when armour is doing anything at all, for the readouts.</summary>
        public static bool IsActive => PhotonNetwork.OfflineMode;

        private static int LevelOf(PickupsManager.Item item)
        {
            if (item == null) return 0;
            // An unlevelled piece is still a piece of armour.
            return Mathf.Max(item.level, 1);
        }
    }
}
