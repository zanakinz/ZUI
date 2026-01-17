using System;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using ZUI.API;
using ZUI.Config;
using ZUI.Patches;
using ZUI.Services;
using ZUI.UI;
using ZUI.UI.CustomLib.Util;
using ZUI.UI.ModernLib;
using ZUI.Utils;
using HarmonyLib;
using ProjectM.Scripting;
using Unity.Entities;
using UnityEngine;

namespace ZUI
{
    [BepInProcess("VRising.exe")]
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BasePlugin
    {
        public static Plugin Instance { get; private set; }
        public static ManualLogSource LogInstance => Instance.Log;
        public static Settings Settings { get; private set; }
        private static World _client;
        public static EntityManager EntityManager => _client.EntityManager;
        //public static ServerGameManager ServerGameManager => _client.GetExistingSystemManaged<ServerScriptMapper>()._ServerGameManager;
        public static bool IsInitialized { get; private set; }
        public static bool IsGameDataInitialized { get; set; }
        public static BCUIManager UIManager { get; set; }
        public static CoreUpdateBehavior CoreUpdateBehavior { get; set; }
        public static bool IsClient { get; private set; }
        public static Entity LocalCharacter { get; set; }

        public static bool IsClientNull() => _client == null;
        public const bool IS_TESTING = false;

        // Public API for external mods - Short and sweet!
        /// <summary>
        /// Registers a button in the Mods menu. External mods can call ZUI.AddButton("Text", ".command")
        /// </summary>
        public static bool AddButton(string buttonText, string command, string tooltip = "") 
            => ModRegistry.AddButton(buttonText, command, tooltip);

        /// <summary>
        /// Removes a registered button from the Mods menu.
        /// </summary>
        public static bool RemoveButton(string buttonText) 
            => ModRegistry.RemoveButton(buttonText);

        /// <summary>
        /// Loads a sprite from your plugin's Sprites directory.
        /// Place images in: BepInEx/plugins/{YourPlugin.dll}/Sprites/{filename}
        /// Supported formats: PNG, JPG
        /// </summary>
        /// <param name="filename">The filename of the image (e.g., "icon.png")</param>
        /// <param name="pixelsPerUnit">Pixels per unit for the sprite (default: 100)</param>
        /// <returns>The loaded Sprite, or null if loading failed</returns>
        public static Sprite LoadSprite(string filename, float pixelsPerUnit = 100f)
            => Utils.SpriteLoader.LoadSprite(filename, pixelsPerUnit);

        public static void Reset()
        {
            _client = null;
            IsInitialized = false;
            IsGameDataInitialized = false;
        }

        private static Harmony _harmonyBootPatch;
        private static Harmony _harmonyChatPatch;
        private static Harmony _harmonyInitPatch;
        private static Harmony _harmonyCanvasPatch;
        private static Harmony _harmonyMenuPatch;
        internal static Harmony HarmonyVersionStringPatch;
        private static FrameTimer _uiInitializedTimer;

        public override void Load()
        {
            IsClient = Application.productName != "VRisingServer";
            LogUtils.Init(Log);
            Instance = this;


            if (!IsClient)
            {
                LogUtils.LogInfo($"{PluginInfo.PLUGIN_NAME}[{PluginInfo.PLUGIN_VERSION}] is a client mod! ({Application.productName})");
                return;
            }

            Settings = new Settings().InitConfig();
            Theme.Opacity = Settings.UITransparency;
            
            // Apply config overrides for server mod availability
            DependencyService.ForceDisableBloodCraft = !Settings.ServerHasBloodCraft;
            DependencyService.ForceDisableKindredCommands = !Settings.ServerHasKindredCommands;
            DependencyService.ForceDisableKinPonds = !Settings.ServerHasKinPonds;
            DependencyService.ForceDisableScarletSigns = !Settings.ServerHasScarletSigns;
            
            // Check for optional mod dependencies
            DependencyService.Initialize();

            UIManager = new BCUIManager();
            CoreUpdateBehavior = new CoreUpdateBehavior();
            CoreUpdateBehavior.Setup();
            //todo CoreUpdateBehavior.ExecuteOnUpdate = MessageService.ProcessAllMessages;

            IsInitialized = true;

            _harmonyBootPatch = Harmony.CreateAndPatchAll(typeof(GameManagerPatch));
            _harmonyMenuPatch = Harmony.CreateAndPatchAll(typeof(EscapeMenuPatch));
            _harmonyCanvasPatch = Harmony.CreateAndPatchAll(typeof(UICanvasSystemPatch));
            HarmonyVersionStringPatch = Harmony.CreateAndPatchAll(typeof(VersionStringPatch));
            _harmonyChatPatch = Harmony.CreateAndPatchAll(typeof(ClientChatPatch));
            _harmonyInitPatch = Harmony.CreateAndPatchAll(typeof(InitializationPatch));
            //_eclipsePatch = Harmony.CreateAndPatchAll(typeof(EclipseClientChatSystemPatch));

            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} version {PluginInfo.PLUGIN_VERSION} is loaded!");

            if(IS_TESTING)
                AddTestUI();
        }


        public override bool Unload()
        {
            _harmonyBootPatch.UnpatchSelf();
            _harmonyCanvasPatch.UnpatchSelf();
            _harmonyMenuPatch.UnpatchSelf();
            HarmonyVersionStringPatch.UnpatchSelf();
            _harmonyChatPatch.UnpatchSelf();
            _harmonyInitPatch.UnpatchSelf();
            //_eclipsePatch.UnpatchSelf();
            
            return true;
        }

        private void AddTestUI()
        {
            UIManager.SetupAndShowUI();
        }

        //run on game start
        public static void GameDataOnInitialize(World world)
        {
            if (!IsGameDataInitialized && IsClient)
            {
                _client = world;
                IsGameDataInitialized = true;
                // We only want to run this once, so unpatch the hook that initiates this callback.
                _harmonyBootPatch.UnpatchSelf();
                _uiInitializedTimer = new FrameTimer();

                _uiInitializedTimer.Initialise(() =>
                    {
                        _uiInitializedTimer.Stop();
                    },
                    TimeSpan.FromSeconds(5),
                    true).Start();
            }
        }

        public static void UIOnInitialize()
        {
            UIManager.SetupAndShowUI();
            ModRegistry.ProcessInitQueue();
            if (Settings.AutoEnableFamiliarEquipment)
                MessageService.EnqueueMessage(MessageService.BCCOM_ENABLEEQUIP);
            FamiliarStateService.Initialize();
            LogUtils.LogInfo($"UI Manager initialized");
        }
    }

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "Zanakinz.ZUI";
        public const string PLUGIN_NAME = "ZUI";
        public const string PLUGIN_VERSION = "1.1.0";
    }
}
