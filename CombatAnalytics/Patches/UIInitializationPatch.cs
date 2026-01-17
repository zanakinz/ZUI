using System;
using HarmonyLib;
using ProjectM.UI;

namespace CombatAnalytics.Patches
{
    [HarmonyPatch]
    public static class UIInitializationPatch
    {
        private static bool _initialized = false;
        private static int _callCount = 0;

        public static void Reset()
        {
            _initialized = false;
            _callCount = 0;
            Plugin.Instance.Log.LogInfo("UIInitializationPatch: State reset");
        }

        public static string GetStatus()
        {
            return $"Initialized: {_initialized}, Call count: {_callCount}";
        }

        /// <summary>
        /// Patch the CharacterHUDEntry.Awake method to initialize our UI after the game's UI is ready
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharacterHUDEntry), nameof(CharacterHUDEntry.Awake))]
        private static void CharacterHUDEntry_Awake_Prefix()
        {
            Plugin.Instance.Log.LogInfo($"CharacterHUDEntry.Awake PREFIX called! (Call #{++_callCount}, Initialized: {_initialized})");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterHUDEntry), nameof(CharacterHUDEntry.Awake))]
        private static void CharacterHUDEntry_Awake_Postfix()
        {
            Plugin.Instance.Log.LogInfo($"CharacterHUDEntry.Awake POSTFIX called! (Initialized: {_initialized})");
            
            if (_initialized)
            {
                Plugin.Instance.Log.LogInfo("Already initialized, skipping.");
                return;
            }

            try
            {
                Plugin.Instance.Log.LogInfo("=== Game UI is ready, initializing DPS panel... ===");
                _initialized = true; // Set this FIRST to prevent re-entry
                CombatAnalytics.Services.DpsService.Initialize();
                Plugin.Instance.Log.LogInfo("=== DPS panel initialization complete. ===");
            }
            catch (Exception ex)
            {
                Plugin.Instance.Log.LogError($"Failed to initialize DPS panel: {ex}");
            }
        }
    }
}

