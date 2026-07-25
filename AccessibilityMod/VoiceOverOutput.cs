using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AccessibilityMod
{
    public static class VoiceOverOutput
    {
        [DllImport("libdl.dylib")]
        private static extern IntPtr dlopen(string path, int mode);

        [DllImport("libdl.dylib")]
        private static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_getClass")]
        private static extern IntPtr objc_getClass(string name);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "sel_registerName")]
        private static extern IntPtr sel_registerName(string name);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr objc_msgSend(IntPtr self, IntPtr op);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr objc_msgSend(IntPtr self, IntPtr op, IntPtr arg1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr objc_msgSend(IntPtr self, IntPtr op, IntPtr arg1, IntPtr arg2);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool objc_msgSend_bool(IntPtr self, IntPtr op);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool objc_msgSend_bool(IntPtr self, IntPtr op, IntPtr arg1);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern int objc_msgSend_int(IntPtr self, IntPtr op);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void NSAccessibilityPostNotificationDelegate(IntPtr element, IntPtr notification, IntPtr userInfo);

        private const int ErrAeEventNotPermitted = -1743;

        private static readonly Dictionary<string, IntPtr> _scriptCache = new Dictionary<string, IntPtr>();
        private static float _speakingUntil = 0f;
        private static bool _frameworksLoaded = false;

        private static void LoadFrameworks()
        {
            if (_frameworksLoaded) return;
            try
            {
                dlopen("/System/Library/Frameworks/Foundation.framework/Foundation", 2 | 8);
                dlopen("/System/Library/Frameworks/AppKit.framework/AppKit", 2 | 8);
                _frameworksLoaded = true;
            }
            catch { }
        }

        public static bool IsSupported()
        {
            return true;
        }

        public static bool IsRunning()
        {
            try
            {
                LoadFrameworks();
                IntPtr workspaceClass = objc_getClass("NSWorkspace");
                IntPtr selSharedWorkspace = sel_registerName("sharedWorkspace");
                IntPtr selIsVoiceOverEnabled = sel_registerName("isVoiceOverEnabled");

                IntPtr workspace = objc_msgSend(workspaceClass, selSharedWorkspace);
                if (workspace == IntPtr.Zero) return false;
                return objc_msgSend_bool(workspace, selIsVoiceOverEnabled);
            }
            catch
            {
                return false;
            }
        }

        private static IntPtr MakeNSString(string text)
        {
            IntPtr nsStringClass = objc_getClass("NSString");
            IntPtr selStringWithUTF8String = sel_registerName("stringWithUTF8String:");
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes((text ?? "") + "\0");
            IntPtr utf8Ptr = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, utf8Ptr, bytes.Length);
            IntPtr nsStr = objc_msgSend(nsStringClass, selStringWithUTF8String, utf8Ptr);
            Marshal.FreeHGlobal(utf8Ptr);
            return nsStr;
        }

        private static string EscapeAppleScriptString(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static bool ExecuteScript(string source)
        {
            IntPtr nsAppleScriptClass = objc_getClass("NSAppleScript");
            IntPtr selAlloc = sel_registerName("alloc");
            IntPtr selInitWithSource = sel_registerName("initWithSource:");
            IntPtr selCompileAndReturnError = sel_registerName("compileAndReturnError:");
            IntPtr selExecuteAndReturnError = sel_registerName("executeAndReturnError:");

            IntPtr uninitScript = objc_msgSend(nsAppleScriptClass, selAlloc);
            IntPtr script = objc_msgSend(uninitScript, selInitWithSource, MakeNSString(source));
            if (script == IntPtr.Zero) return false;

            IntPtr compileErrPtr = Marshal.AllocHGlobal(IntPtr.Size);
            Marshal.WriteIntPtr(compileErrPtr, IntPtr.Zero);
            bool compiled = objc_msgSend_bool(script, selCompileAndReturnError, compileErrPtr);
            IntPtr compileErr = Marshal.ReadIntPtr(compileErrPtr);
            Marshal.FreeHGlobal(compileErrPtr);

            if (!compiled)
            {
                if (compileErr != IntPtr.Zero)
                {
                    IntPtr selObjectForKey = sel_registerName("objectForKey:");
                    IntPtr errNumObj = objc_msgSend(compileErr, selObjectForKey, MakeNSString("NSAppleScriptErrorNumber"));
                    if (errNumObj != IntPtr.Zero)
                    {
                        int errNum = objc_msgSend_int(errNumObj, sel_registerName("intValue"));
                        if (errNum == ErrAeEventNotPermitted) return false;
                    }
                }
                return false;
            }

            IntPtr execErrPtr = Marshal.AllocHGlobal(IntPtr.Size);
            Marshal.WriteIntPtr(execErrPtr, IntPtr.Zero);
            IntPtr reply = objc_msgSend(script, selExecuteAndReturnError, execErrPtr);
            IntPtr execErr = Marshal.ReadIntPtr(execErrPtr);
            Marshal.FreeHGlobal(execErrPtr);

            if (reply == IntPtr.Zero && execErr != IntPtr.Zero)
            {
                IntPtr selObjectForKey = sel_registerName("objectForKey:");
                IntPtr errNumObj = objc_msgSend(execErr, selObjectForKey, MakeNSString("NSAppleScriptErrorNumber"));
                if (errNumObj != IntPtr.Zero)
                {
                    int errNum = objc_msgSend_int(errNumObj, sel_registerName("intValue"));
                    if (errNum == ErrAeEventNotPermitted) return false;
                }
            }

            return reply != IntPtr.Zero;
        }

        private static bool AnnounceViaWindow(string text)
        {
            try
            {
                IntPtr appClass = objc_getClass("NSApplication");
                IntPtr selSharedApplication = sel_registerName("sharedApplication");
                IntPtr app = objc_msgSend(appClass, selSharedApplication);
                if (app == IntPtr.Zero) return false;

                IntPtr selKeyWindow = sel_registerName("keyWindow");
                IntPtr window = objc_msgSend(app, selKeyWindow);
                if (window == IntPtr.Zero)
                {
                    IntPtr selMainWindow = sel_registerName("mainWindow");
                    window = objc_msgSend(app, selMainWindow);
                }
                if (window == IntPtr.Zero) return false;

                IntPtr dictClass = objc_getClass("NSMutableDictionary");
                IntPtr selDictionary = sel_registerName("dictionary");
                IntPtr dict = objc_msgSend(dictClass, selDictionary);

                IntPtr selSetObjectForKey = sel_registerName("setObject:forKey:");

                IntPtr appKitHandle = dlopen("/System/Library/Frameworks/AppKit.framework/AppKit", 2 | 8);
                if (appKitHandle == IntPtr.Zero) return false;

                IntPtr announcementKeyPtr = dlsym(appKitHandle, "NSAccessibilityAnnouncementKey");
                IntPtr priorityKeyPtr = dlsym(appKitHandle, "NSAccessibilityPriorityKey");
                IntPtr notificationPtr = dlsym(appKitHandle, "NSAccessibilityAnnouncementRequestedNotification");

                if (announcementKeyPtr == IntPtr.Zero || priorityKeyPtr == IntPtr.Zero || notificationPtr == IntPtr.Zero)
                    return false;

                IntPtr announcementKey = Marshal.ReadIntPtr(announcementKeyPtr);
                IntPtr priorityKey = Marshal.ReadIntPtr(priorityKeyPtr);
                IntPtr notification = Marshal.ReadIntPtr(notificationPtr);

                IntPtr numClass = objc_getClass("NSNumber");
                IntPtr selNumberWithInt = sel_registerName("numberWithInt:");
                IntPtr priorityNum = objc_msgSend(numClass, selNumberWithInt, (IntPtr)90);

                objc_msgSend(dict, selSetObjectForKey, MakeNSString(text), announcementKey);
                objc_msgSend(dict, selSetObjectForKey, priorityNum, priorityKey);

                IntPtr postNotifPtr = dlsym(appKitHandle, "NSAccessibilityPostNotificationWithUserInfo");
                if (postNotifPtr != IntPtr.Zero)
                {
                    var postNotif = (NSAccessibilityPostNotificationDelegate)Marshal.GetDelegateForFunctionPointer(
                        postNotifPtr, typeof(NSAccessibilityPostNotificationDelegate));
                    postNotif(window, notification, dict);
                    return true;
                }
            }
            catch { }
            return false;
        }

        private static float EstimateSpeakingSeconds(string text)
        {
            int words = 0;
            if (!string.IsNullOrEmpty(text))
            {
                string[] parts = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                words = parts.Length;
            }
            return Math.Max(0.4f, words / 3.0f);
        }

        public static bool Speak(string text, bool interrupt = false)
        {
            if (string.IsNullOrEmpty(text)) return false;
            LoadFrameworks();

            if (interrupt)
            {
                Stop();
            }

            string escaped = EscapeAppleScriptString(text);
            string source = "tell application \"VoiceOver\" to output \"" + escaped + "\"";
            bool sent = ExecuteScript(source);
            if (!sent)
            {
                sent = AnnounceViaWindow(text);
            }

            if (sent)
            {
                _speakingUntil = UnityEngine.Time.realtimeSinceStartup + EstimateSpeakingSeconds(text);
            }
            return sent;
        }

        public static bool Stop()
        {
            LoadFrameworks();
            ExecuteScript("tell application \"VoiceOver\" to output \"\"");
            _speakingUntil = 0f;
            return true;
        }

        public static bool IsSpeaking()
        {
            return UnityEngine.Time.realtimeSinceStartup < _speakingUntil;
        }

        public static void Shutdown()
        {
            foreach (var kvp in _scriptCache)
            {
                if (kvp.Value != IntPtr.Zero)
                {
                    IntPtr selRelease = sel_registerName("release");
                    objc_msgSend(kvp.Value, selRelease);
                }
            }
            _scriptCache.Clear();
            _speakingUntil = 0f;
        }
    }
}
