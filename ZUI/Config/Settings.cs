using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;

namespace ZUI.Config
{
    public class Settings
    {
        private static string CONFIG_PATH = Path.Combine(Paths.ConfigPath, PluginInfo.PLUGIN_NAME);
        private static readonly Dictionary<string, ConfigEntryBase> ConfigEntries = new();
        public const string UI_SETTINGS_GROUP = "UISettings";
        public const string FAM_SETTINGS_GROUP = "FamiliarSettings";
        public const string GENERAL_SETTINGS_GROUP = "GeneralOptions";


        public static bool ClearServerMessages =>
            (ConfigEntries[nameof(ClearServerMessages)] as ConfigEntry<bool>)?.Value ?? false;

        // --- NEW SECURITY FLAG ---
        public static bool AllowServerAudioDownloads =>
            (ConfigEntries[nameof(AllowServerAudioDownloads)] as ConfigEntry<bool>)?.Value ?? false;
        // -------------------------

        public static int FamStatsQueryIntervalInSeconds
        {
            get
            {
                var value = (ConfigEntries[nameof(FamStatsQueryIntervalInSeconds)] as ConfigEntry<int>)?.Value ?? 10;
                if (value < 5) value = 5;
                return value;
            }
        }

        public static bool UseHorizontalContentLayout =>
            (ConfigEntries[nameof(UseHorizontalContentLayout)] as ConfigEntry<bool>)?.Value ?? true;

        /// <summary>
        /// Delay in milliseconds between command executions.
        /// Set to 0 for instant commands (not recommended for laggy servers).
        /// Default is 50ms which provides near-instant response while preventing spam.
        /// </summary>
        public static int CommandDelayInMilliseconds
        {
            get
            {
                var value = (ConfigEntries[nameof(CommandDelayInMilliseconds)] as ConfigEntry<int>)?.Value ?? 50;
                if (value < 0) value = 0; // Don't allow negative values
                if (value > 5000) value = 5000; // Cap at 5 seconds max
                return value;
            }
        }

        [System.Obsolete("Use CommandDelayInMilliseconds instead. This property is kept for backward compatibility.")]
        public static int GlobalQueryIntervalInSeconds => 2;

        public static float UITransparency =>
            (ConfigEntries[nameof(UITransparency)] as ConfigEntry<float>)?.Value ?? 0.6f;

        public static string LastBindCommand
        {
            get => (ConfigEntries[nameof(LastBindCommand)] as ConfigEntry<string>)?.Value ?? "";
            set => ConfigEntries[nameof(LastBindCommand)].BoxedValue = value;
        }

        public static bool IsFamStatsPanelEnabled => (ConfigEntries[nameof(IsFamStatsPanelEnabled)] as ConfigEntry<bool>)?.Value ?? true;
        public static bool IsBoxPanelEnabled => (ConfigEntries[nameof(IsBoxPanelEnabled)] as ConfigEntry<bool>)?.Value ?? true;
        public static bool IsBindButtonEnabled => (ConfigEntries[nameof(IsBindButtonEnabled)] as ConfigEntry<bool>)?.Value ?? true;
        public static bool IsCombatButtonEnabled => (ConfigEntries[nameof(IsCombatButtonEnabled)] as ConfigEntry<bool>)?.Value ?? true;
        public static bool AutoEnableFamiliarEquipment => (ConfigEntries[nameof(AutoEnableFamiliarEquipment)] as ConfigEntry<bool>)?.Value ?? true;
        public static bool IsPrestigeButtonEnabled => (ConfigEntries[nameof(IsPrestigeButtonEnabled)] as ConfigEntry<bool>)?.Value ?? true;
        public static bool IsToggleButtonEnabled => (ConfigEntries[nameof(IsToggleButtonEnabled)] as ConfigEntry<bool>)?.Value ?? true;

        public static bool IsUILocked
        {
            get => (ConfigEntries[nameof(IsUILocked)] as ConfigEntry<bool>)?.Value ?? false;
            set => ConfigEntries[nameof(IsUILocked)].BoxedValue = value;
        }

        // Server Mod Availability (manual override for servers without specific mods)
        public static bool ServerHasBloodCraft => (ConfigEntries[nameof(ServerHasBloodCraft)] as ConfigEntry<bool>)?.Value ?? true;
        public static bool ServerHasKindredCommands => (ConfigEntries[nameof(ServerHasKindredCommands)] as ConfigEntry<bool>)?.Value ?? true;
        public static bool ServerHasKinPonds => (ConfigEntries[nameof(ServerHasKinPonds)] as ConfigEntry<bool>)?.Value ?? true;
        public static bool ServerHasScarletSigns => (ConfigEntries[nameof(ServerHasScarletSigns)] as ConfigEntry<bool>)?.Value ?? true;


        public Settings InitConfig()
        {
            if (!Directory.Exists(CONFIG_PATH))
            {
                Directory.CreateDirectory(CONFIG_PATH);
            }

            InitConfigEntry(GENERAL_SETTINGS_GROUP, nameof(ClearServerMessages), true,
                "Clear server and command messages from chat");

            // --- NEW CONFIG ENTRY ---
            InitConfigEntry(GENERAL_SETTINGS_GROUP, nameof(AllowServerAudioDownloads), false,
                "Allow server to initiate audio file downloads. DISABLED by default for security. Only enable if you trust the server.");
            // ------------------------

            InitConfigEntry(GENERAL_SETTINGS_GROUP, nameof(FamStatsQueryIntervalInSeconds), 10,
                "Query interval for familiar stats update (no less than 10 sec)");
            InitConfigEntry(GENERAL_SETTINGS_GROUP, nameof(CommandDelayInMilliseconds), 50,
                "Delay in milliseconds between command executions. 0 = instant (may spam), 50 = near-instant (recommended), 2000 = old behavior. Max: 5000ms");

            // Server mod availability settings
            InitConfigEntry(GENERAL_SETTINGS_GROUP, nameof(ServerHasBloodCraft), false,
                "Set to false if your server doesn't have BloodCraft (disables Familiar, Levels, Class, Quests features)");
            InitConfigEntry(GENERAL_SETTINGS_GROUP, nameof(ServerHasKindredCommands), true,
                "Set to false if your server doesn't have KindredCommands (disables User/Admin command features)");
            InitConfigEntry(GENERAL_SETTINGS_GROUP, nameof(ServerHasKinPonds), true,
                "Set to false if your server doesn't have KinPonds (disables Pond commands)");
            InitConfigEntry(GENERAL_SETTINGS_GROUP, nameof(ServerHasScarletSigns), true,
                "Set to false if your server doesn't have ScarletSigns (disables Sign creation)");

            InitConfigEntry(UI_SETTINGS_GROUP, nameof(UseHorizontalContentLayout), true,
                "Use horizontal or vertical layout for main content panel");
            InitConfigEntry(FAM_SETTINGS_GROUP, nameof(LastBindCommand), "", "Last bind fam command stored");
            InitConfigEntry(FAM_SETTINGS_GROUP, nameof(AutoEnableFamiliarEquipment), true, "Automatically enable familiar equipment management via emote wheel");
            InitConfigEntry(UI_SETTINGS_GROUP, nameof(UITransparency), 0.6f,
                "Set transparency for all panels between 1.0f as opaque and 0f as transparent");
            InitConfigEntry(UI_SETTINGS_GROUP, nameof(IsFamStatsPanelEnabled), true, "Is fam stats panel enabled");
            InitConfigEntry(UI_SETTINGS_GROUP, nameof(IsBoxPanelEnabled), true, "Is box panel enabled");
            InitConfigEntry(UI_SETTINGS_GROUP, nameof(IsBindButtonEnabled), true, "Is bind button enabled");
            InitConfigEntry(UI_SETTINGS_GROUP, nameof(IsCombatButtonEnabled), true, "Is combat button enabled");
            InitConfigEntry(UI_SETTINGS_GROUP, nameof(IsPrestigeButtonEnabled), true, "Is prestige button enabled");
            InitConfigEntry(UI_SETTINGS_GROUP, nameof(IsToggleButtonEnabled), true, "Is toggle button enabled");
            InitConfigEntry(UI_SETTINGS_GROUP, nameof(IsUILocked), false, "Is UI locked (pin button state)");
            return this;
        }

        private static ConfigEntry<T> InitConfigEntry<T>(string section, string key, T defaultValue, string description)
        {
            // Bind the configuration entry and get its value
            var entry = Plugin.Instance.Config.Bind(section, key, defaultValue, description);

            // Check if the key exists in the configuration file and retrieve its current value
            var newFile = Path.Combine(Paths.ConfigPath, $"{PluginInfo.PLUGIN_GUID}.cfg");

            if (File.Exists(newFile))
            {
                var config = new ConfigFile(newFile, true);
                if (config.TryGetEntry(section, key, out ConfigEntry<T> existingEntry))
                {
                    // If the entry exists, update the value to the existing value
                    entry.Value = existingEntry.Value;
                }
            }

            ConfigEntries.Add(key, entry);
            return entry;
        }
    }
}