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
                // Mono's P/Invoke does not respect SetDllDirectory, so we must
                // preload Tolk.dll (and its dependencies) with LoadLibrary from
                // the explicit path before any P/Invoke call into Tolk.
                string libPath = Path.Combine(
                    Path.GetDirectoryName(typeof(ScreenReaderManager).Assembly.Location),
                    "lib", "x64");

                logger.LogInfo($"Tolk lib path: {libPath}");
                logger.LogInfo($"Tolk lib path exists: {Directory.Exists(libPath)}");

                if (Directory.Exists(libPath))
                {
                    // Set DLL search path so Tolk's own dependencies resolve
                    SetDllDirectory(libPath);

                    // Preload the native DLLs so Mono can find them
                    string tolkPath = Path.Combine(libPath, "Tolk.dll");
                    logger.LogInfo($"Loading native Tolk from: {tolkPath}");
                    IntPtr handle = LoadLibrary(tolkPath);
                    if (handle == IntPtr.Zero)
                    {
                        int err = Marshal.GetLastWin32Error();
                        logger.LogError($"LoadLibrary failed for Tolk.dll, Win32 error: {err}");
                    }
                    else
                    {
                        logger.LogInfo("Native Tolk.dll loaded successfully.");
                    }
                }
                else
                {
                    logger.LogError($"Tolk lib directory not found: {libPath}");
                }

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

                if (_available)
                    logger.LogInfo("Tolk initialized successfully, speech is available.");
                else
                    logger.LogWarning("Tolk initialized but no speech output available.");
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to initialize Tolk: {ex}");
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

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);
    }
}
