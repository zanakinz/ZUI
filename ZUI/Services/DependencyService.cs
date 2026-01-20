using BepInEx.Unity.IL2CPP;
using ZUI.Utils;
using System.Collections.Generic;

namespace ZUI.Services
{
    /// <summary>
    /// Service to detect if optional mod dependencies are available on the server.
    /// Since most dependencies are server-side mods, we detect them by their runtime behavior
    /// or assume they are present based on configuration.
    /// </summary>
    public static class DependencyService
    {
        // Plugin GUIDs for soft dependencies (for reference)
        private const string BLOODCRAFT_GUID = "io.zfolmt.Bloodcraft";
        private const string KINDRED_COMMANDS_GUID = "aa.odjit.KindredCommands";
        private const string KIN_PONDS_GUID = "KinPonds";
        private const string SCARLET_SIGNS_GUID = "ScarletSigns";

        // Runtime detection flags
        // Default to TRUE because these are usually Server-Side mods that the Client cannot "see" via Reflection.
        // We assume they exist until the user disables them in Config or the server tells us otherwise.
        private static bool _hasBloodCraft = true;
        private static bool _hasKindredCommands = true;
        private static bool _hasKinPonds = true;
        private static bool _hasScarletSigns = true;

        // Manual override flags for user configuration (Set via Plugin.cs Settings)
        public static bool ForceDisableBloodCraft { get; set; } = false;
        public static bool ForceDisableKindredCommands { get; set; } = false;
        public static bool ForceDisableKinPonds { get; set; } = false;
        public static bool ForceDisableScarletSigns { get; set; } = false;

        /// <summary>
        /// BloodCraft provides: Familiars, Levels, Class, Quests, Prestige, Combat
        /// </summary>
        public static bool HasBloodCraft => !ForceDisableBloodCraft && _hasBloodCraft;

        /// <summary>
        /// KindredCommands provides: User commands, Admin commands
        /// </summary>
        public static bool HasKindredCommands => !ForceDisableKindredCommands && _hasKindredCommands;

        /// <summary>
        /// KinPonds provides: Pond commands
        /// </summary>
        public static bool HasKinPonds => !ForceDisableKinPonds && _hasKinPonds;

        /// <summary>
        /// ScarletSigns provides: Sign creation/management
        /// </summary>
        public static bool HasScarletSigns => !ForceDisableScarletSigns && _hasScarletSigns;

        /// <summary>
        /// Check if a plugin is loaded locally (client-side only)
        /// </summary>
        private static bool CheckLocalPlugin(string pluginGuid)
        {
            try
            {
                return IL2CPPChainloader.Instance.Plugins.ContainsKey(pluginGuid);
            }
            catch (System.Exception ex)
            {
                LogUtils.LogError($"[DependencyService] Error checking for {pluginGuid}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Log detected dependencies at startup.
        /// Updated to check local plugins immediately, but respects Server-Side assumption.
        /// </summary>
        public static void Initialize()
        {
            LogUtils.LogInfo("[DependencyService] ========================================");
            LogUtils.LogInfo("[DependencyService] Initializing Server-Side Mod Detection");
            LogUtils.LogInfo("[DependencyService] ========================================");
            LogUtils.LogInfo("[DependencyService] ");
            LogUtils.LogInfo("[DependencyService] NOTE: BloodCraft, KindredCommands, KinPonds, and ScarletSigns");
            LogUtils.LogInfo("[DependencyService] are typically SERVER-SIDE mods.");
            LogUtils.LogInfo("[DependencyService] ");
            LogUtils.LogInfo("[DependencyService] ZUI assumes these mods are available by default.");
            LogUtils.LogInfo("[DependencyService] You can disable them in the ZUI config file if needed.");
            LogUtils.LogInfo("[DependencyService] ");

            // Check for client-side plugins for debugging/hybrid scenarios
            LogUtils.LogInfo("[DependencyService] Client-side plugins loaded (Local):");
            try
            {
                // We check local plugins just to log them, but we don't let a "missing local plugin" 
                // disable the feature, because it might be on the server.
                if (CheckLocalPlugin(BLOODCRAFT_GUID)) LogUtils.LogInfo($"  [LOCAL] BloodCraft found.");
                if (CheckLocalPlugin(KINDRED_COMMANDS_GUID)) LogUtils.LogInfo($"  [LOCAL] KindredCommands found.");
                if (CheckLocalPlugin(KIN_PONDS_GUID)) LogUtils.LogInfo($"  [LOCAL] KinPonds found.");
                if (CheckLocalPlugin(SCARLET_SIGNS_GUID)) LogUtils.LogInfo($"  [LOCAL] ScarletSigns found.");
            }
            catch (System.Exception ex)
            {
                LogUtils.LogError($"[DependencyService] Error listing plugins: {ex.Message}");
            }

            LogUtils.LogInfo("[DependencyService] ");
            LogUtils.LogInfo("[DependencyService] Feature Status (Active in Menu):");
            LogUtils.LogInfo($"  {(HasBloodCraft ? "[YES]" : "[NO ]")} BloodCraft");
            LogUtils.LogInfo($"  {(HasKindredCommands ? "[YES]" : "[NO ]")} KindredCommands");
            LogUtils.LogInfo($"  {(HasKinPonds ? "[YES]" : "[NO ]")} KinPonds");
            LogUtils.LogInfo($"  {(HasScarletSigns ? "[YES]" : "[NO ]")} ScarletSigns");
            LogUtils.LogInfo("[DependencyService] ");

            if (ForceDisableBloodCraft || ForceDisableKindredCommands || ForceDisableKinPonds || ForceDisableScarletSigns)
            {
                LogUtils.LogWarning("[DependencyService] Some features manually disabled via config:");
                if (ForceDisableBloodCraft) LogUtils.LogWarning("  - BloodCraft disabled via Config");
                if (ForceDisableKindredCommands) LogUtils.LogWarning("  - KindredCommands disabled via Config");
                if (ForceDisableKinPonds) LogUtils.LogWarning("  - KinPonds disabled via Config");
                if (ForceDisableScarletSigns) LogUtils.LogWarning("  - ScarletSigns disabled via Config");
            }

            LogUtils.LogInfo("[DependencyService] ========================================");
        }

        /// <summary>
        /// Reset cached values (for testing or reload scenarios)
        /// </summary>
        public static void Reset()
        {
            _hasBloodCraft = true;
            _hasKindredCommands = true;
            _hasKinPonds = true;
            _hasScarletSigns = true;
        }

        /// <summary>
        /// Manually mark a mod as unavailable (called when a command fails, etc.)
        /// This allows runtime detection of missing server mods
        /// </summary>
        public static void MarkModUnavailable(string modName)
        {
            switch (modName.ToLower())
            {
                case "bloodcraft":
                    _hasBloodCraft = false;
                    LogUtils.LogWarning($"[DependencyService] Marked BloodCraft as unavailable (Server missing mod?)");
                    break;
                case "kindredcommands":
                    _hasKindredCommands = false;
                    LogUtils.LogWarning($"[DependencyService] Marked KindredCommands as unavailable");
                    break;
                case "kinponds":
                    _hasKinPonds = false;
                    LogUtils.LogWarning($"[DependencyService] Marked KinPonds as unavailable");
                    break;
                case "scarletsigns":
                    _hasScarletSigns = false;
                    LogUtils.LogWarning($"[DependencyService] Marked ScarletSigns as unavailable");
                    break;
            }
        }
    }
}