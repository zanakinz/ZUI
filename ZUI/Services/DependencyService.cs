using BepInEx.Unity.IL2CPP;
using ZUI.Utils;
using System.Collections.Generic;

namespace ZUI.Services
{
    /// <summary>
    /// Service to detect if optional mod dependencies are available on the server.
    /// Since most dependencies are server-side mods, we detect them by their runtime behavior
    /// rather than checking if they're loaded locally.
    /// </summary>
    public static class DependencyService
    {
        // Plugin GUIDs for soft dependencies (for reference only - server-side mods won't be in local plugin list)
        private const string BLOODCRAFT_GUID = "io.zfolmt.Bloodcraft";
        private const string KINDRED_COMMANDS_GUID = "aa.odjit.KindredCommands";
        private const string KIN_PONDS_GUID = "KinPonds";
        private const string SCARLET_SIGNS_GUID = "ScarletSigns";

        // Runtime detection - assume available unless proven otherwise
        // Users can manually disable via config if needed
        private static bool _hasBloodCraft = false;  // Default: assume available
        private static bool _hasKindredCommands = false;
        private static bool _hasKinPonds = false;
        private static bool _hasScarletSigns = false;

        // Manual override flags for user configuration
        public static bool ForceDisableBloodCraft { get; set; } = false;
        public static bool ForceDisableKindredCommands { get; set; } = false;
        public static bool ForceDisableKinPonds { get; set; } = false;
        public static bool ForceDisableScarletSigns { get; set; } = false;

        /// <summary>
        /// BloodCraft provides: Familiars, Levels, Class, Quests, Prestige, Combat
        /// Note: This is a server-side mod, so we assume it's available by default
        /// </summary>
        public static bool HasBloodCraft => !ForceDisableBloodCraft && _hasBloodCraft;

        /// <summary>
        /// KindredCommands provides: User commands, Admin commands
        /// Note: This is a server-side mod, so we assume it's available by default
        /// </summary>
        public static bool HasKindredCommands => !ForceDisableKindredCommands && _hasKindredCommands;

        /// <summary>
        /// KinPonds provides: Pond commands
        /// Note: This is a server-side mod, so we assume it's available by default
        /// </summary>
        public static bool HasKinPonds => !ForceDisableKinPonds && _hasKinPonds;

        /// <summary>
        /// ScarletSigns provides: Sign creation/management
        /// Note: This is a server-side mod, so we assume it's available by default
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
        /// Log detected dependencies at startup
        /// </summary>
        public static void Initialize()
        {
            LogUtils.LogInfo("[DependencyService] ========================================");
            LogUtils.LogInfo("[DependencyService] Initializing Server-Side Mod Detection");
            LogUtils.LogInfo("[DependencyService] ========================================");
            LogUtils.LogInfo("[DependencyService] ");
            LogUtils.LogInfo("[DependencyService] NOTE: BloodCraft, KindredCommands, KinPonds, and ScarletSigns");
            LogUtils.LogInfo("[DependencyService] are SERVER-SIDE mods and will NOT appear in the client plugin list.");
            LogUtils.LogInfo("[DependencyService] ");
            LogUtils.LogInfo("[DependencyService] ZUI assumes these mods are available by default.");
            LogUtils.LogInfo("[DependencyService] If your server doesn't have a specific mod, you can disable");
            LogUtils.LogInfo("[DependencyService] related features in the ZUI config file.");
            LogUtils.LogInfo("[DependencyService] ");
            
            // Check for client-side plugins for debugging
            LogUtils.LogInfo("[DependencyService] Client-side plugins loaded:");
            try
            {
                var clientPlugins = new List<string>();
                foreach (var plugin in IL2CPPChainloader.Instance.Plugins)
                {
                    clientPlugins.Add(plugin.Key);
                }
                
                if (clientPlugins.Count > 0)
                {
                    foreach (var guid in clientPlugins)
                    {
                        LogUtils.LogInfo($"  ? {guid}");
                    }
                }
                else
                {
                    LogUtils.LogInfo("  (None)");
                }
            }
            catch (System.Exception ex)
            {
                LogUtils.LogError($"[DependencyService] Error listing plugins: {ex.Message}");
            }
            
            LogUtils.LogInfo("[DependencyService] ");
            LogUtils.LogInfo("[DependencyService] Server-side mod features (assumed available):");
            LogUtils.LogInfo($"  {(HasBloodCraft ? "?" : "?")} BloodCraft (Familiars, Levels, Class, Quests, Prestige, Combat)");
            LogUtils.LogInfo($"  {(HasKindredCommands ? "?" : "?")} KindredCommands (User & Admin commands)");
            LogUtils.LogInfo($"  {(HasKinPonds ? "?" : "?")} KinPonds (Pond commands)");
            LogUtils.LogInfo($"  {(HasScarletSigns ? "?" : "?")} ScarletSigns (Sign creation)");
            LogUtils.LogInfo("[DependencyService] ");
            
            if (ForceDisableBloodCraft || ForceDisableKindredCommands || ForceDisableKinPonds || ForceDisableScarletSigns)
            {
                LogUtils.LogWarning("[DependencyService] Some features manually disabled via config:");
                if (ForceDisableBloodCraft) LogUtils.LogWarning("  ? BloodCraft features disabled");
                if (ForceDisableKindredCommands) LogUtils.LogWarning("  ? KindredCommands features disabled");
                if (ForceDisableKinPonds) LogUtils.LogWarning("  ? KinPonds features disabled");
                if (ForceDisableScarletSigns) LogUtils.LogWarning("  ? ScarletSigns features disabled");
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
                    LogUtils.LogWarning($"[DependencyService] Marked BloodCraft as unavailable (command failed or server doesn't have it)");
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
