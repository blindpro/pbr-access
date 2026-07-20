using System;
using System.IO;
using System.Runtime.InteropServices;
using BepInEx.Logging;
using UnityEngine;

namespace AccessibilityMod
{
    public static class ScreenReaderManager
    {
        private static bool _initialized;
        private static bool _available;

        public static bool IsAvailable => _available;

        public static void Initialize(ManualLogSource logger)
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                // Add our lib folder to the DLL search path so Tolk.dll and
                // vendor DLLs (nvdaControllerClient64.dll, SAAPI64.dll) can be found.
                string libPath = Path.Combine(
                    Path.GetDirectoryName(typeof(ScreenReaderManager).Assembly.Location),
                    "lib", "x64");
                if (Directory.Exists(libPath))
                    SetDllDirectory(libPath);

                DavyKager.Tolk.TrySAPI(true);
                DavyKager.Tolk.Load();

                string sr = DavyKager.Tolk.DetectScreenReader();
                if (sr != null)
                {
                    logger.LogInfo($"Screen reader detected: {sr}");
                    _available = true;
                }
                else
                {
                    logger.LogWarning("No screen reader detected. SAPI will be used as fallback.");
                    _available = DavyKager.Tolk.HasSpeech();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to initialize Tolk: {ex.Message}");
                _available = false;
            }
        }

        public static void Speak(string text, bool interrupt = true)
        {
            if (!_available || string.IsNullOrEmpty(text)) return;
            try
            {
                DavyKager.Tolk.Output(text, interrupt);
            }
            catch (Exception)
            {
                // Silently fail if screen reader disconnected mid-session
            }
        }

        public static void Silence()
        {
            if (!_available) return;
            try
            {
                DavyKager.Tolk.Silence();
            }
            catch (Exception) { }
        }

        public static void Shutdown()
        {
            if (!_initialized) return;
            try
            {
                DavyKager.Tolk.Unload();
            }
            catch (Exception) { }
            _initialized = false;
            _available = false;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetDllDirectory(string lpPathName);
    }
}
