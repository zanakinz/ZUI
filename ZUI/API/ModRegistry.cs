using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ZUI.UI.ModContent;

namespace ZUI.API
{
    /// <summary>
    /// Central registry for managing custom UI panels and legacy menu buttons.
    /// Tracks context (current plugin/window) for content registration.
    /// </summary>
    public static class ModRegistry
    {
        // Legacy "List" Mode Storage
        private static readonly List<ModPlugin> RegisteredPlugins = new List<ModPlugin>();

        // Advanced "Custom UI" Mode Storage (PluginName -> WindowId -> Panel)
        private static readonly Dictionary<string, Dictionary<string, CustomModPanel>> CustomPanels = new Dictionary<string, Dictionary<string, CustomModPanel>>();

        // Initialization Queue for calls made before UI is ready
        private static readonly List<Action> _initQueue = new List<Action>();

        private static readonly object Lock = new object();

        // Context Tracking
        private static string _currentPlugin = null;
        private static string _currentCategory = null;
        private static string _currentWindowId = "Main";

        /// <summary>
        /// Processes queued UI requests once the UI system is initialized.
        /// </summary>
        public static void ProcessInitQueue()
        {
            lock (Lock)
            {
                if (_initQueue.Count > 0)
                {
                    Plugin.LogInstance.LogInfo($"[ModRegistry] Processing {_initQueue.Count} queued UI requests...");
                    foreach (var action in _initQueue)
                    {
                        try
                        {
                            action();
                        }
                        catch (Exception ex)
                        {
                            Plugin.LogInstance.LogError($"[ModRegistry] Error in queued action: {ex}");
                        }
                    }
                    _initQueue.Clear();
                }
            }
        }

        public static void SetPlugin(string pluginName)
        {
            if (string.IsNullOrWhiteSpace(pluginName))
            {
                Plugin.LogInstance.LogError("[ModRegistry] Plugin name cannot be null or empty.");
                return;
            }

            lock (Lock)
            {
                _currentPlugin = pluginName;
                _currentCategory = null;
                _currentWindowId = "Main";

                // Create legacy plugin entry if it doesn't exist
                if (!RegisteredPlugins.Exists(p => p.PluginName.Equals(pluginName, StringComparison.OrdinalIgnoreCase)))
                {
                    RegisteredPlugins.Add(new ModPlugin
                    {
                        PluginName = pluginName,
                        Categories = new List<ModCategory>()
                    });
                }
            }
        }

        public static void SetTargetWindow(string windowId)
        {
            _currentWindowId = string.IsNullOrWhiteSpace(windowId) ? "Main" : windowId;
        }

        public static void SetUITemplate(string templateName)
        {
            if (CheckQueue(() => SetUITemplate(templateName))) return;
            EnsurePanelExists(_currentWindowId, templateName, -1, -1);
        }

        public static void SetUICustom(int width, int height)
        {
            if (CheckQueue(() => SetUICustom(width, height))) return;
            EnsurePanelExists(_currentWindowId, null, width, height);
        }

        public static void HideTitleBar()
        {
            if (CheckQueue(() => HideTitleBar())) return;

            var panel = GetCurrentContextPanel();
            if (panel != null)
            {
                panel.SetTitleBarVisibility(false);
            }
        }

        public static void SetWindowTitle(string title)
        {
            if (CheckQueue(() => SetWindowTitle(title))) return;

            var panel = GetCurrentContextPanel();
            if (panel != null)
            {
                panel.SetWindowTitle(title);
            }
        }

        public static void CreateTab(string name, string tooltip)
        {
            if (CheckQueue(() => CreateTab(name, tooltip))) return;

            var panel = GetCurrentContextPanel();
            if (panel != null)
            {
                panel.CreateTab(name, tooltip);
            }
            else
            {
                Plugin.LogInstance.LogWarning("[ModRegistry] CreateTab called without a Custom UI context. Tabs are only supported in Custom Panels.");
            }
        }

        public static void OpenWindow(string pluginName, string windowId)
        {
            if (Plugin.UIManager == null || !Plugin.UIManager.IsInitialized) return;

            if (string.IsNullOrWhiteSpace(pluginName)) return;
            if (string.IsNullOrWhiteSpace(windowId)) windowId = "Main";

            lock (Lock)
            {
                if (CustomPanels.TryGetValue(pluginName, out var windows))
                {
                    if (windows.TryGetValue(windowId, out var panel))
                    {
                        if (panel != null)
                        {
                            panel.SetActive(true);
                            if (panel.UIRoot != null)
                                panel.UIRoot.transform.SetAsLastSibling();
                        }
                    }
                }
            }
        }

        private static bool CheckQueue(Action action)
        {
            if (_currentPlugin == null)
            {
                Plugin.LogInstance.LogError("[ModRegistry] No plugin context set. Call SetPlugin() first.");
                return true;
            }

            if (Plugin.UIManager == null || !Plugin.UIManager.IsInitialized)
            {
                QueueAction(action);
                return true;
            }
            return false;
        }

        private static void QueueAction(Action action)
        {
            string pName = _currentPlugin;
            string wId = _currentWindowId;

            lock (Lock)
            {
                _initQueue.Add(() =>
                {
                    string prevPlugin = _currentPlugin;
                    string prevWin = _currentWindowId;

                    _currentPlugin = pName;
                    _currentWindowId = wId;

                    try
                    {
                        action();
                    }
                    finally
                    {
                        _currentPlugin = prevPlugin;
                        _currentWindowId = prevWin;
                    }
                });
            }
        }

        private static void EnsurePanelExists(string windowId, string template, int w, int h)
        {
            if (Plugin.UIManager == null || !Plugin.UIManager.IsInitialized) return;

            lock (Lock)
            {
                if (!CustomPanels.ContainsKey(_currentPlugin))
                    CustomPanels[_currentPlugin] = new Dictionary<string, CustomModPanel>();

                var pluginPanels = CustomPanels[_currentPlugin];

                if (!pluginPanels.ContainsKey(windowId))
                {
                    CustomModPanel newPanel;
                    if (!string.IsNullOrEmpty(template))
                    {
                        newPanel = new CustomModPanel(Plugin.UIManager.UiBase, _currentPlugin, windowId, template);
                    }
                    else
                    {
                        newPanel = new CustomModPanel(Plugin.UIManager.UiBase, _currentPlugin, windowId, w, h);
                    }

                    newPanel.SetActive(true);
                    newPanel.SetActive(false);

                    pluginPanels[windowId] = newPanel;
                    Plugin.UIManager.RegisterPanel(newPanel);

                    LegacyAddCategory("Windows");
                    string btnText = windowId == "Main" ? $"Open {_currentPlugin} UI" : $"Open {windowId}";

                    LegacyAddButtonWithCallback(btnText, () =>
                    {
                        if (pluginPanels.TryGetValue(windowId, out var p) && p != null)
                        {
                            p.SetActive(true);
                            p.UIRoot.transform.SetAsLastSibling();
                        }
                    }, "Opens the custom window");
                }
                else
                {
                    // --- FIX: Prevent Duplicate Elements/Tabs ---
                    // If the window already exists, we reset it before processing new packets.
                    // This clears out the old content so we don't just keep adding tabs every time the button is pressed.
                    pluginPanels[windowId].Reset();
                }
            }
        }

        private static CustomModPanel GetCurrentContextPanel()
        {
            if (_currentPlugin == null) return null;

            lock (Lock)
            {
                if (CustomPanels.TryGetValue(_currentPlugin, out var windows))
                {
                    if (windows.TryGetValue(_currentWindowId, out var panel))
                        return panel;
                }
            }
            return null;
        }

        // ==============================================================================================
        // CONTENT REGISTRATION METHODS
        // ==============================================================================================

        public static void AddCategory(string categoryName, float x = -1, float y = -1)
        {
            if (CheckQueue(() => AddCategory(categoryName, x, y))) return;

            var panel = GetCurrentContextPanel();
            if (panel != null)
            {
                panel.AddCategory(categoryName, x, y);
            }
            else
            {
                LegacyAddCategory(categoryName);
            }
        }

        public static void AddText(string text, float x = -1, float y = -1)
        {
            if (CheckQueue(() => AddText(text, x, y))) return;

            var panel = GetCurrentContextPanel();
            if (panel != null)
            {
                string id = $"txt_{text.GetHashCode()}";
                panel.AddText(id, text, x, y);
            }
        }

        public static void AddImage(Assembly assembly, string imageName, float x, float y, float w, float h)
        {
            if (CheckQueue(() => AddImage(assembly, imageName, x, y, w, h))) return;

            var panel = GetCurrentContextPanel();
            if (panel != null)
            {
                string id = $"img_{imageName}_{x}_{y}";
                panel.AddImage(id, assembly, imageName, x, y, w, h);
            }
        }

        public static bool AddButton(string buttonText, string command, string tooltip = "", float x = -1, float y = -1)
        {
            if (CheckQueue(() => AddButton(buttonText, command, tooltip, x, y))) return true;

            var panel = GetCurrentContextPanel();
            if (panel != null)
            {
                string id = $"btn_{buttonText}";
                panel.AddButton(id, buttonText, command, x, y);
                return true;
            }
            else
            {
                return LegacyAddButton(buttonText, command, tooltip);
            }
        }

        public static bool AddButton(Assembly assembly, string buttonText, string command, string imageName, float x, float y, float w, float h)
        {
            if (CheckQueue(() => AddButton(assembly, buttonText, command, imageName, x, y, w, h))) return true;

            var panel = GetCurrentContextPanel();
            if (panel != null)
            {
                string id = $"btn_{buttonText}";
                panel.AddButton(id, assembly, buttonText, command, imageName, x, y, w, h);
                return true;
            }
            else
            {
                return LegacyAddButton(buttonText, command, "");
            }
        }

        public static void AddCloseButton(string text, float x = -1, float y = -1)
        {
            if (CheckQueue(() => AddCloseButton(text, x, y))) return;

            var panel = GetCurrentContextPanel();
            if (panel != null)
            {
                string id = $"btn_close_{text}";
                panel.AddCloseButton(id, text, x, y);
            }
        }

        public static bool AddButtonWithCallback(string buttonText, Action onClick, string tooltip = "")
        {
            if (CheckQueue(() => AddButtonWithCallback(buttonText, onClick, tooltip))) return true;

            var panel = GetCurrentContextPanel();
            if (panel != null)
            {
                string id = $"btn_{buttonText}";
                panel.AddButtonWithCallback(id, buttonText, onClick, -1, -1);
                return true;
            }

            return LegacyAddButtonWithCallback(buttonText, onClick, tooltip);
        }

        public static bool RemoveButton(string buttonText)
        {
            var panel = GetCurrentContextPanel();
            if (panel != null)
            {
                panel.RemoveElement($"btn_{buttonText}");
                panel.RemoveElement(buttonText);
                return true;
            }

            return LegacyRemoveButton(buttonText);
        }

        // ==============================================================================================
        // REFRESH & NOTIFICATION
        // ==============================================================================================

        /// <summary>
        /// Triggers a refresh across all registered UI panels.
        /// Primarily used by ImageDownloader when a background download completes.
        /// </summary>
        public static void NotifyChanges()
        {
            OnButtonsChanged?.Invoke();
        }

        public static bool RemovePlugin(string pluginName)
        {
            if (string.IsNullOrWhiteSpace(pluginName))
                return false;

            bool removedLegacy = false;
            bool removedCustom = false;

            lock (Lock)
            {
                // Remove Legacy
                removedLegacy = RegisteredPlugins.RemoveAll(p =>
                    p.PluginName.Equals(pluginName, StringComparison.OrdinalIgnoreCase)) > 0;

                // Remove Custom Panels
                if (CustomPanels.ContainsKey(pluginName))
                {
                    var windows = CustomPanels[pluginName];
                    foreach (var panel in windows.Values)
                    {
                        if (panel != null)
                        {
                            panel.SetActive(false);
                            panel.Destroy();
                        }
                    }
                    CustomPanels.Remove(pluginName);
                    removedCustom = true;
                }

                if (removedLegacy || removedCustom)
                {
                    Plugin.LogInstance.LogInfo($"[ModRegistry] Removed plugin '{pluginName}'");
                    if (_currentPlugin?.Equals(pluginName, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        _currentPlugin = null;
                        _currentCategory = null;
                        _currentWindowId = "Main";
                    }
                    NotifyChanges();
                }

                return removedLegacy || removedCustom;
            }
        }

        // ==============================================================================================
        // LEGACY IMPLEMENTATIONS
        // ==============================================================================================

        private static void LegacyAddCategory(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName)) return;
            if (_currentPlugin == null) return;

            lock (Lock)
            {
                _currentCategory = categoryName;

                var plugin = RegisteredPlugins.FirstOrDefault(p =>
                    p.PluginName.Equals(_currentPlugin, StringComparison.OrdinalIgnoreCase));

                if (plugin != null && !plugin.Categories.Exists(c =>
                    c.CategoryName.Equals(categoryName, StringComparison.OrdinalIgnoreCase)))
                {
                    plugin.Categories.Add(new ModCategory
                    {
                        CategoryName = categoryName,
                        Buttons = new List<ModButton>()
                    });
                }
            }
        }

        private static bool LegacyAddButton(string buttonText, string command, string tooltip = "")
        {
            lock (Lock)
            {
                var plugin = RegisteredPlugins.FirstOrDefault(p =>
                    p.PluginName.Equals(_currentPlugin, StringComparison.OrdinalIgnoreCase));

                if (plugin == null) return false;

                EnsureLegacyCategory(plugin);

                var category = plugin.Categories.FirstOrDefault(c =>
                    c.CategoryName.Equals(_currentCategory, StringComparison.OrdinalIgnoreCase));

                if (category == null || category.Buttons.Exists(b => b.ButtonText.Equals(buttonText, StringComparison.OrdinalIgnoreCase)))
                    return false;

                category.Buttons.Add(new ModButton
                {
                    ButtonText = buttonText,
                    Command = command,
                    Tooltip = tooltip,
                    RegisteredAt = DateTime.UtcNow
                });
                NotifyChanges();
                return true;
            }
        }

        private static bool LegacyAddButtonWithCallback(string buttonText, Action onClick, string tooltip = "")
        {
            lock (Lock)
            {
                var plugin = RegisteredPlugins.FirstOrDefault(p =>
                    p.PluginName.Equals(_currentPlugin, StringComparison.OrdinalIgnoreCase));

                if (plugin == null) return false;

                EnsureLegacyCategory(plugin);

                var category = plugin.Categories.FirstOrDefault(c =>
                    c.CategoryName.Equals(_currentCategory, StringComparison.OrdinalIgnoreCase));

                if (category == null || category.Buttons.Exists(b => b.ButtonText.Equals(buttonText, StringComparison.OrdinalIgnoreCase)))
                    return false;

                category.Buttons.Add(new ModButton
                {
                    ButtonText = buttonText,
                    OnClick = onClick,
                    Tooltip = tooltip,
                    RegisteredAt = DateTime.UtcNow
                });
                NotifyChanges();
                return true;
            }
        }

        private static void EnsureLegacyCategory(ModPlugin plugin)
        {
            if (_currentCategory == null)
            {
                _currentCategory = "Commands";
                if (!plugin.Categories.Exists(c => c.CategoryName == "Commands"))
                {
                    plugin.Categories.Add(new ModCategory
                    {
                        CategoryName = "Commands",
                        Buttons = new List<ModButton>()
                    });
                }
            }
        }

        private static bool LegacyRemoveButton(string buttonText)
        {
            if (string.IsNullOrWhiteSpace(buttonText) || _currentPlugin == null)
                return false;

            lock (Lock)
            {
                var plugin = RegisteredPlugins.FirstOrDefault(p =>
                    p.PluginName.Equals(_currentPlugin, StringComparison.OrdinalIgnoreCase));

                if (plugin == null) return false;

                bool removed = false;
                foreach (var category in plugin.Categories)
                {
                    removed |= category.Buttons.RemoveAll(b =>
                        b.ButtonText.Equals(buttonText, StringComparison.OrdinalIgnoreCase)) > 0;
                }

                if (removed) NotifyChanges();
                return removed;
            }
        }

        public static void ClearAll()
        {
            lock (Lock)
            {
                RegisteredPlugins.Clear();
                _initQueue.Clear();

                foreach (var plugin in CustomPanels.Values)
                {
                    foreach (var panel in plugin.Values)
                    {
                        if (panel != null)
                        {
                            panel.SetActive(false);
                            panel.Destroy();
                        }
                    }
                }
                CustomPanels.Clear();

                _currentPlugin = null;
                _currentCategory = null;
                _currentWindowId = "Main";

                Plugin.LogInstance.LogInfo("[ModRegistry] Cleared all registered plugins");
                NotifyChanges();
            }
        }

        public static IReadOnlyList<ModPlugin> GetPlugins()
        {
            lock (Lock)
            {
                return RegisteredPlugins.AsReadOnly();
            }
        }

        public static event Action OnButtonsChanged;

        public class ModPlugin
        {
            public string PluginName { get; set; }
            public List<ModCategory> Categories { get; set; }
        }

        public class ModCategory
        {
            public string CategoryName { get; set; }
            public List<ModButton> Buttons { get; set; }
        }

        public class ModButton
        {
            public string ButtonText { get; set; }
            public string Command { get; set; }
            public string Tooltip { get; set; }
            public Action OnClick { get; set; }
            public DateTime RegisteredAt { get; set; }
        }
    }
}