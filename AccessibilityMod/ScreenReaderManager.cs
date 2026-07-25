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
        private static bool _isMac;

        public static bool IsAvailable => _available;

        public static void Initialize(ManualLogSource logger)
        {
            if (_initialized) return;
            _initialized = true;

            _isMac = Application.platform == RuntimePlatform.OSXPlayer ||
                     Application.platform == RuntimePlatform.OSXEditor ||
                     System.IO.Path.DirectorySeparatorChar == '/';

            if (_isMac)
            {
                logger.LogInfo("Initializing VoiceOver screen reader backend for macOS...");
                try
                {
                    if (VoiceOverOutput.IsRunning())
                    {
                        logger.LogInfo("VoiceOver detected and running on macOS.");
                    }
                    else
                    {
                        logger.LogWarning("VoiceOver is not currently running on macOS. Output will still be attempted.");
                    }
                    _available = true;
                    logger.LogInfo("macOS VoiceOver backend initialized successfully.");
                }
                catch (Exception ex)
                {
                    logger.LogError($"Failed to initialize macOS VoiceOver: {ex}");
                    _available = false;
                }
                return;
            }

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
                    if (!SetDllDirectory(libPath))
                        logger.LogWarning($"SetDllDirectory failed, Win32 error: {Marshal.GetLastWin32Error()}");

                    // Tolk looks up its driver clients by bare name at Tolk_Load
                    // time, and SetDllDirectory alone is not enough to find them:
                    // it is process-global, anything else in the process can clobber
                    // it, and it is ignored outright once some module has called
                    // SetDefaultDllDirectories. When the lookup fails the driver just
                    // reports itself inactive, so NVDA silently drops out of detection
                    // and Tolk falls through to SAPI.
                    //
                    // Preloading each client by full path fixes that: the module is
                    // then already in the process, and Tolk's bare-name LoadLibrary
                    // matches it on base name and only bumps the refcount.
                    // Dependencies first, Tolk.dll last.
                    foreach (string dep in new[] { "nvdaControllerClient64.dll", "SAAPI64.dll", "Tolk.dll" })
                    {
                        string depPath = Path.Combine(libPath, dep);
                        logger.LogInfo($"Loading native library: {depPath}");
                        if (LoadLibrary(depPath) == IntPtr.Zero)
                            logger.LogError($"LoadLibrary failed for {dep}, Win32 error: {Marshal.GetLastWin32Error()}");
                        else
                            logger.LogInfo($"{dep} loaded successfully.");
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
                    // SAPI is only ever the last resort. Landing on it while a real
                    // screen reader is running means that reader's client library
                    // did not load - say so rather than reporting plain success.
                    if (sr == "SAPI")
                        logger.LogWarning("Using SAPI (Windows built-in speech). No screen reader was detected; "
                            + "if NVDA is running, check the LoadLibrary lines above for a failure.");
                    else
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
                if (_isMac)
                {
                    VoiceOverOutput.Speak(text, interrupt);
                }
                else
                {
                    DavyKager.Tolk.Output(text, interrupt);
                }
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
                if (_isMac)
                {
                    VoiceOverOutput.Stop();
                }
                else
                {
                    DavyKager.Tolk.Silence();
                }
            }
            catch (Exception) { }
        }

        public static void Shutdown()
        {
            if (!_initialized) return;
            try
            {
                if (_isMac)
                {
                    VoiceOverOutput.Shutdown();
                }
                else
                {
                    DavyKager.Tolk.Unload();
                }
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
