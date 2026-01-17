using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using VampireCommandFramework;
using CombatAnalytics.UI;
using CombatAnalytics.Services;

namespace CombatAnalytics
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("gg.deca.VampireCommandFramework")]
    [BepInDependency("Zanakinz.ZUI", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BasePlugin
    {
        public static Plugin Instance { get; private set; }
        private Harmony _harmony;

        public override void Load()
        {
            Instance = this;

            Log.LogInfo($"=== {PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION} Loading ===");

            // Register with ZUI Mods menu first
            RegisterWithZUI();

            // 1. Register VCF Commands
            try
            {
                Log.LogInfo("Registering VCF commands...");
                CommandRegistry.RegisterAll();
                Log.LogInfo("VCF Commands registered successfully.");
                
                // Log registered command types
                var commandTypes = Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => t.GetMethods().Any(m => m.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0));
                
                foreach (var type in commandTypes)
                {
                    Log.LogInfo($"  Command class found: {type.FullName}");
                    foreach (var method in type.GetMethods())
                    {
                        var attrs = method.GetCustomAttributes(typeof(CommandAttribute), false);
                        foreach (CommandAttribute attr in attrs)
                        {
                            Log.LogInfo($"    - Command: {attr.Name}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to register VCF commands: {ex}");
            }

            // 2. Apply Harmony patches for UI initialization
            // The UI will be initialized when the game's UI is ready (via UIInitializationPatch)
            try
            {
                _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
                Log.LogInfo($"Harmony patches applied. Patch count: {_harmony.GetPatchedMethods().Count()}");
                
                // Log all patched methods for debugging
                foreach (var method in _harmony.GetPatchedMethods())
                {
                    Log.LogInfo($"  Patched: {method.DeclaringType?.Name}.{method.Name}");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to apply Harmony patches: {ex}");
            }

            // 3. Log start
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            Log.LogInfo("Use command '.dps' to toggle the DPS meter.");
        }

        private void RegisterWithZUI()
        {
            try
            {
                var zuiAssembly = System.AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "ZUI");

                if (zuiAssembly == null)
                {
                    Log.LogWarning("ZUI assembly not found - skipping Mods menu registration");
                    return;
                }

                // Try to get ModRegistry type directly for callback registration
                var modRegistryType = zuiAssembly.GetType("ZUI.API.ModRegistry");
                if (modRegistryType != null)
                {
                    Log.LogInfo("Found ModRegistry type, attempting callback registration");
                    
                    var setPluginMethod = modRegistryType.GetMethod("SetPlugin",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var addCategoryMethod = modRegistryType.GetMethod("AddCategory",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var addButtonWithCallbackMethod = modRegistryType.GetMethod("AddButtonWithCallback",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    
                    if (setPluginMethod != null && addCategoryMethod != null && addButtonWithCallbackMethod != null)
                    {
                        Log.LogInfo("Using ModRegistry.AddButtonWithCallback (direct API)");
                        
                        setPluginMethod.Invoke(null, new object[] { PluginInfo.PLUGIN_NAME });
                        addCategoryMethod.Invoke(null, new object[] { "Combat Analytics" });
                        
                        // Create Action delegates for callbacks
                        var toggleAction = new Action(() => {
                            Log.LogInfo("[ZUI Button] Toggle DPS button clicked!");
                            API.CombatAnalyticsAPI.ToggleDpsPanel();
                        });
                        
                        var resetAction = new Action(() => {
                            Log.LogInfo("[ZUI Button] Reset DPS button clicked!");
                            API.CombatAnalyticsAPI.ResetDps();
                        });
                        
                        addButtonWithCallbackMethod.Invoke(null, new object[] { "Toggle DPS", toggleAction, "Toggle the DPS meter on/off" });
                        addButtonWithCallbackMethod.Invoke(null, new object[] { "Reset DPS", resetAction, "Reset DPS counter to zero" });
                        
                        Log.LogInfo("Registered with ZUI Mods menu using callbacks!");
                        return;
                    }
                    else
                    {
                        Log.LogWarning($"ModRegistry methods: SetPlugin={setPluginMethod != null}, AddCategory={addCategoryMethod != null}, AddButtonWithCallback={addButtonWithCallbackMethod != null}");
                    }
                }

                // Fallback to ZUI.API.ZUI wrapper
                Log.LogInfo("Falling back to ZUI.API.ZUI wrapper");
                var zuiApiType = zuiAssembly.GetType("ZUI.API.ZUI");
                if (zuiApiType == null)
                {
                    Log.LogError("Could not find ZUI.API.ZUI type");
                    return;
                }

                var setPluginMethod2 = zuiApiType.GetMethod("SetPlugin",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var addCategoryMethod2 = zuiApiType.GetMethod("AddCategory",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var addButtonMethod = zuiApiType.GetMethod("AddButton",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                if (setPluginMethod2 == null || addCategoryMethod2 == null || addButtonMethod == null)
                {
                    Log.LogError("Could not find ZUI API methods");
                    return;
                }

                setPluginMethod2.Invoke(null, new object[] { PluginInfo.PLUGIN_NAME });
                addCategoryMethod2.Invoke(null, new object[] { "Combat Analytics" });
                
                // Use chat commands as fallback
                addButtonMethod.Invoke(null, new object[] { "Toggle DPS", ".dps", "Toggle the DPS meter on/off" });
                addButtonMethod.Invoke(null, new object[] { "Reset DPS", ".dps reset", "Reset DPS counter to zero" });
                addButtonMethod.Invoke(null, new object[] { "Debug Info", ".dpsstatus", "Show debug information" });
                addButtonMethod.Invoke(null, new object[] { "Test", ".hello", "Simple test command" });

                Log.LogInfo("Registered with ZUI Mods menu using chat commands (fallback)!");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to register with ZUI: {ex.Message}");
                Log.LogError($"Stack trace: {ex.StackTrace}");
            }
        }

        public override bool Unload()
        {
            CommandRegistry.UnregisterAssembly();
            _harmony?.UnpatchSelf();
            StandaloneUIManager.Instance.Cleanup();
            return true;
        }
    }

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "CombatAnalytics";
        public const string PLUGIN_NAME = "Combat Analytics";
        public const string PLUGIN_VERSION = "1.0.0";
    }
}