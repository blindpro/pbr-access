using System;
using System.IO;
using BepInEx;
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
        public const string PluginVersion = "0.2.0";

        internal static new ManualLogSource Logger;
        internal static Harmony HarmonyInstance;

        public static float GameplayWarmupTimer = 0f;
        public static bool IsGameplayWarmingUp => GameplayWarmupTimer > 0f;

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo($"{PluginName} v{PluginVersion} loaded!");

            ScreenReaderManager.Initialize(Logger);
            Loc.Initialize();

            HarmonyInstance = new Harmony(PluginGUID);
            HarmonyInstance.PatchAll();

            // Create a separate hidden GameObject that survives scene loads.
            // The BepInEx_Manager object gets destroyed by this game on scene change,
            // so we need our own persistent object.
            var modObj = new GameObject("__AccessibilityMod__");
            modObj.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(modObj);
            modObj.AddComponent<ModRunner>();

            Logger.LogInfo("Mod runner created.");
            ScreenReaderManager.Speak("Accessibility mod loaded");
        }

        private void OnDestroy()
        {
            // Don't shutdown screen reader - ModRunner is still alive on its own object
        }
    }
}
