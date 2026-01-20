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
using ZUI.UI.Components; // Added for GifPlayer
using HarmonyLib;
using ProjectM.Scripting;
using Unity.Entities;
using UnityEngine;
using Il2CppInterop.Runtime.Injection;

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
        public static bool IsInitialized { get; private set; }
        public static bool IsGameDataInitialized { get; set; }
        public static BCUIManager UIManager { get; set; }
        public static CoreUpdateBehavior CoreUpdateBehavior { get; set; }
        public static bool IsClient { get; private set; }
        public static Entity LocalCharacter { get; set; }

        public static bool IsClientNull() => _client == null;
        public const bool IS_TESTING = false;

        // Public API
        public static bool AddButton(string buttonText, string command, string tooltip = "")
            => ModRegistry.AddButton(buttonText, command, tooltip);

        public static bool RemoveButton(string buttonText)
            => ModRegistry.RemoveButton(buttonText);

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

            // --- FIX: Register Custom MonoBehaviours for IL2CPP ---
            // These MUST be registered here to prevent crashes when adding them to GameObjects.
            ClassInjector.RegisterTypeInIl2Cpp<ImageDownloader>();
            ClassInjector.RegisterTypeInIl2Cpp<GifPlayer>();

            // --- Initialize Dependencies EARLY ---
            // We do this in Load() so the flags are ready before the UI is built.
            // Since DependencyService now defaults to TRUE (assuming server-side presence),
            // this guarantees buttons show as Available immediately.
            DependencyService.ForceDisableBloodCraft = !Settings.ServerHasBloodCraft;
            DependencyService.ForceDisableKindredCommands = !Settings.ServerHasKindredCommands;
            DependencyService.ForceDisableKinPonds = !Settings.ServerHasKinPonds;
            DependencyService.ForceDisableScarletSigns = !Settings.ServerHasScarletSigns;

            DependencyService.Initialize();

            // Setup Managers
            UIManager = new BCUIManager();
            CoreUpdateBehavior = new CoreUpdateBehavior();
            CoreUpdateBehavior.Setup();

            IsInitialized = true;

            // Apply Harmony Patches
            _harmonyBootPatch = Harmony.CreateAndPatchAll(typeof(GameManagerPatch));
            _harmonyMenuPatch = Harmony.CreateAndPatchAll(typeof(EscapeMenuPatch));
            _harmonyCanvasPatch = Harmony.CreateAndPatchAll(typeof(UICanvasSystemPatch));
            HarmonyVersionStringPatch = Harmony.CreateAndPatchAll(typeof(VersionStringPatch));
            _harmonyChatPatch = Harmony.CreateAndPatchAll(typeof(ClientChatPatch));
            _harmonyInitPatch = Harmony.CreateAndPatchAll(typeof(InitializationPatch));

            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} version {PluginInfo.PLUGIN_VERSION} is loaded!");

            if (IS_TESTING) AddTestUI();
        }

        public override bool Unload()
        {
            _harmonyBootPatch.UnpatchSelf();
            _harmonyCanvasPatch.UnpatchSelf();
            _harmonyMenuPatch.UnpatchSelf();
            HarmonyVersionStringPatch.UnpatchSelf();
            _harmonyChatPatch.UnpatchSelf();
            _harmonyInitPatch.UnpatchSelf();
            return true;
        }

        private void AddTestUI()
        {
            UIManager.SetupAndShowUI();
        }

        public static void GameDataOnInitialize(World world)
        {
            if (!IsGameDataInitialized && IsClient)
            {
                _client = world;
                IsGameDataInitialized = true;

                // Dependencies already initialized in Load(), no need to do it here.

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
        public const string PLUGIN_VERSION = "2.2.0";
    }
}