using System;
using BepInEx.Logging;

namespace ZUI.Utils
{
    public static class LogUtils
    {
        private static ManualLogSource Log;

        public static void LogDebugError(Exception exception)
        {
            Log?.LogError(exception);
        }

        public static void Init(ManualLogSource log)
        {
            Log = log;
        }

        public static void LogInfo(string text)
        {
            Log?.LogInfo(text);
        }

        public static void LogError(string text)
        {
            Log?.LogError(text);
        }

        public static void LogWarning(string text)
        {
            Log?.LogWarning(text);
        }
    }
}
