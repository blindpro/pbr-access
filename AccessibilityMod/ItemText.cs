using InfimaGames.LowPolyShooterPack;

namespace AccessibilityMod
{
    /// <summary>
    /// One spoken name for an item, so a rifle is called the same thing in the loot
    /// list, the inventory and the HUD readouts. The asset carries three candidate
    /// names and only short_description is the human one - "Glock 19" rather than
    /// "Handgun 01" - so the fallbacks exist for items that leave it blank.
    /// </summary>
    internal static class ItemText
    {
        public static string Describe(PickupsManager.Item item)
        {
            if (item == null) return "empty";
            string name = Name(item);
            return item.level > 0 ? $"{name}, level {item.level}" : name;
        }

        public static string Name(PickupsManager.Item item)
        {
            if (item == null) return "empty";
            if (!string.IsNullOrEmpty(item.short_description)) return item.short_description;
            if (!string.IsNullOrEmpty(item.description)) return item.description;
            return item.name;
        }
    }
}
