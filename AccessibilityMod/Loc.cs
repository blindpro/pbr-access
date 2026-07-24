using System.Collections.Generic;

namespace AccessibilityMod
{
    /// <summary>
    /// Central localization manager for the Accessibility Mod.
    /// Automatically detects game language using I2.Loc.LocalizationManager.
    /// </summary>
    public static class Loc
    {
        #region Fields

        private static bool _initialized = false;
        private static string _currentLang = "en";

        private static readonly Dictionary<string, string> _german = new();
        private static readonly Dictionary<string, string> _english = new();

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the localization manager. Call once at mod startup.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            InitializeStrings();
            RefreshLanguage();
            _initialized = true;
        }

        /// <summary>
        /// Updates the current language from the game's settings.
        /// </summary>
        public static void RefreshLanguage()
        {
            string gameLang = GetGameLanguage();

            switch (gameLang)
            {
                case "de":
                    _currentLang = "de";
                    break;
                default:
                    _currentLang = "en";
                    break;
            }
        }

        /// <summary>
        /// Retrieves a localized string by key.
        /// </summary>
        /// <param name="key">The translation key.</param>
        /// <returns>The localized string, or the key itself as fallback.</returns>
        public static string Get(string key)
        {
            if (!_initialized) Initialize();

            var dict = GetCurrentDictionary();

            if (dict.TryGetValue(key, out string value))
                return value;

            if (_english.TryGetValue(key, out string engValue))
                return engValue;

            return key;
        }

        /// <summary>
        /// Retrieves a localized string formatted with parameters.
        /// </summary>
        /// <param name="key">The translation key.</param>
        /// <param name="args">Formatting arguments.</param>
        /// <returns>The formatted localized string.</returns>
        public static string Get(string key, params object[] args)
        {
            string template = Get(key);
            try
            {
                return string.Format(template, args);
            }
            catch
            {
                return template;
            }
        }

        #endregion

        #region Private Methods

        private static string GetGameLanguage()
        {
            try
            {
                string code = I2.Loc.LocalizationManager.CurrentLanguageCode;
                if (!string.IsNullOrEmpty(code))
                {
                    if (code.Length >= 2)
                    {
                        return code.Substring(0, 2).ToLower();
                    }
                    return code.ToLower();
                }
            }
            catch
            {
                // Fallback if I2.Loc is not initialized or throws
            }
            return "en";
        }

        private static Dictionary<string, string> GetCurrentDictionary()
        {
            switch (_currentLang)
            {
                case "de":
                    return _german;
                default:
                    return _english;
            }
        }

        private static void Add(string key, string german, string english)
        {
            _german[key] = german;
            _english[key] = english;
        }

        private static void InitializeStrings()
        {
            // ===== Weapon Feedback (B2) =====
            Add("weapon_empty", "Leer", "Empty");
            Add("weapon_dry", "Trocken", "Dry");
            Add("weapon_reloading", "Lädt nach", "Reloading");
            Add("weapon_reloaded", "Nachgeladen", "Reloaded");
            Add("weapon_reloaded_ammo", "Nachgeladen, {0}", "Reloaded, {0}");

            // ===== Safe Zone (A5) =====
            Add("zone_appears_in", "Nächste Zone erscheint in {0} Sekunden", "Next zone appears in {0} seconds");
            Add("zone_shrinks_in", "Zone schrumpft in {0} Sekunden", "Zone shrinks in {0} seconds");
            Add("zone_shrinking_remaining", "Zone schrumpft, {0} Sekunden verbleibend", "Zone shrinking, {0} seconds remaining");
            Add("zone_shrinks_soon", "Zone schrumpft bald", "Zone shrinks soon");

            // ===== Parachute (A3) & Spectating (A4) =====
            Add("parachute_over_cell", "{0} Meter, über {1}", "{0} meters, over {1}");
            Add("parachute_over_landmark", "{0} Meter, über {1}, {2}", "{0} meters, over {1}, {2}");
            Add("spectating_start", "Zuschauen: {0}", "Spectating {0}");
            Add("spectating_died", "{0} ist gestorben", "{0} died");

            // ===== Game Pause/Resume =====
            Add("game_paused", "Spiel pausiert", "Game paused");
            Add("game_resumed", "Spiel fortgesetzt", "Game resumed");
        }

        #endregion
    }
}
