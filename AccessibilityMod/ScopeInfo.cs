using InfimaGames.LowPolyShooterPack;

namespace AccessibilityMod
{
    /// <summary>
    /// What the sight on the held weapon does to turning.
    ///
    /// The scope is the only attachment with real stats. While aiming, the game
    /// multiplies mouse look by GetMultiplierMouseSensitivity in Character.OnLook,
    /// and weapon spread by GetMultiplierSpread when it fires - a grip changes
    /// recoil, and silencers and lasers are sound and light. Nothing about a scope
    /// moves the shot ray, so the assist's geometry is unaffected by it; what it
    /// changes is how far a given input turns you.
    ///
    /// Both the arrow keys and the aim assist rotate the character directly rather
    /// than going through OnLook, which is what makes them work at all - the axis
    /// persistence bug and the assist's need for exact steps both rule out feeding
    /// the input path. The consequence is that this multiplier passed them by, so a
    /// scoped turn was as coarse as an unscoped one while a mouse player's turn got
    /// finer. Applying it here puts them back on the same terms.
    /// </summary>
    internal static class ScopeInfo
    {
        /// <summary>
        /// The look multiplier in force this instant: the scope's while aiming down
        /// it, otherwise one. Matches Character.OnLook, which only applies it while
        /// aiming.
        /// </summary>
        public static float Sensitivity(Character character)
        {
            if (character == null || !character.IsAiming()) return 1f;

            var scope = ScopeOf(character);
            if (scope == null) return 1f;

            float multiplier = scope.GetMultiplierMouseSensitivity();
            // A zero or negative multiplier would freeze turning outright.
            return multiplier > 0f ? multiplier : 1f;
        }

        private static ScopeBehaviour ScopeOf(Character character)
        {
            var weapon = character.GetEquippedWeapon();
            if (weapon == null) return null;

            var attachments = weapon.GetAttachmentManager();
            return attachments == null ? null : attachments.GetEquippedScope();
        }

        /// <summary>
        /// The spoken name of the fitted sight, or null when the weapon is on its
        /// ironsights. Read from the inventory item rather than the behaviour so it
        /// is the same name the loot list used when it was picked up.
        /// </summary>
        public static string FittedScopeName(CharacterInventory charInv)
        {
            if (charInv == null) return null;

            var item = charInv.GetCurrentWeapon() == 0 ? charInv.weapon1_scope : charInv.weapon2_scope;
            return item == null ? null : ItemText.Name(item);
        }
    }
}
