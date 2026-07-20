using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace AccessibilityMod
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInProcess("PolygonBitBattleRoyale.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.accessibility.polygonbitbr";
        public const string PluginName = "Accessibility Mod";
        public const string PluginVersion = "0.1.0";

        internal static new ManualLogSource Logger;
        internal static Harmony HarmonyInstance;

        private MenuNavigator _menuNavigator;

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo($"{PluginName} v{PluginVersion} loaded!");

            ScreenReaderManager.Initialize(Logger);

            HarmonyInstance = new Harmony(PluginGUID);
            HarmonyInstance.PatchAll();

            _menuNavigator = gameObject.AddComponent<MenuNavigator>();

            Logger.LogInfo("Harmony patches applied.");
            ScreenReaderManager.Speak("Accessibility mod loaded");
        }

        private void OnDestroy()
        {
            HarmonyInstance?.UnpatchSelf();
            ScreenReaderManager.Shutdown();
        }
    }
}
