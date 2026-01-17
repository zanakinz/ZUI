using System;
using System.Collections.Generic;
using System.Linq;
using ZUI.UI.ModContent; // Required for CustomModPanel

namespace ZUI.API
{
    /// <summary>
    /// Public API for external mods to register custom buttons in ZUI's Mods menu or create custom UI windows.
    /// </summary>
    public static class ModRegistry
    {
        // Legacy "List" Mode Storage
        private static readonly List<ModPlugin> RegisteredPlugins = new();

        // Advanced "Custom UI" Mode Storage
        // Dictionary<PluginName, Dictionary<WindowID, CustomModPanel>>
        private static readonly Dictionary<string, Dictionary<string, CustomModPanel>> CustomPanels = new();

        // Initialization Queue for mods that load before ZUI is ready
        private static readonly List<Action> _initQueue = new();

        private static readonly object Lock = new();

        // Context Tracking
        private static string _currentPlugin = null;
        private static string _currentCategory = null; // Used for Legacy List Mode
        private static string _currentWindowId = "Main"; // Used for Custom UI Mode

        /// <summary>
        /// Called by ZUI.Plugin when the UI is fully initialized to process queued requests.
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

        /// <summary>
        /// Sets the plugin name context for subsequent button registrations.
        /// All buttons registered after this call will be grouped under this plugin name.
        /// </summary>
        /// <param name="pluginName">The name of the plugin</param>
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
                _currentCategory = null; // Reset category when switching plugins
                _currentWindowId = "Main"; // Reset window ID to default

                // Create legacy plugin entry if it doesn't exist (ensures it shows up in Mods List if fallback occurs)
                if (!RegisteredPlugins.Exists(p => p.PluginName.Equals(pluginName, StringComparison.OrdinalIgnoreCase)))
                {
                    RegisteredPlugins.Add(new ModPlugin
                    {
                        PluginName = pluginName,
                        Categories = new List<ModCategory>()
                    });
                    Plugin.LogInstance.LogInfo($"[ModRegistry] Registered plugin '{pluginName}'");
                }
            }
        }

        /// <summary>
        /// Sets the specific Window ID context if a plugin has multiple custom windows.
        /// Defaults to "Main" if not called.
        /// </summary>
        /// <param name="windowId">The unique identifier for the window</param>
        public static void SetTargetWindow(string windowId)
        {
            if (string.IsNullOrWhiteSpace(windowId))
            {
                _currentWindowId = "Main";
                return;
            }
            _currentWindowId = windowId;
        }

        /// <summary>
        /// Initializes a Custom UI Panel using a pre-defined template (Small, Medium, Large).
        /// </summary>
        public static void SetUITemplate(string templateName)
        {
            if (_currentPlugin == null)
            {
                Plugin.LogInstance.LogError("[ModRegistry] Cannot set UI: No plugin context set. Call SetPlugin() first.");
                return;
            }

            // If UI isn't ready, queue this action
            if (Plugin.UIManager == null || !Plugin.UIManager.IsInitialized)
            {
                QueueAction(() => SetUITemplate(templateName));
                return;
            }

            EnsurePanelExists(_currentWindowId, templateName, -1, -1);
        }

        /// <summary>
        /// Initializes a Custom UI Panel with explicit dimensions.
        /// </summary>
        public static void SetUICustom(int width, int height)
        {
            if (_currentPlugin == null)
            {
                Plugin.LogInstance.LogError("[ModRegistry] Cannot set UI: No plugin context set. Call SetPlugin() first.");
                return;
            }

            // If UI isn't ready, queue this action
            if (Plugin.UIManager == null || !Plugin.UIManager.IsInitialized)
            {
                QueueAction(() => SetUICustom(width, height));
                return;
            }

            EnsurePanelExists(_currentWindowId, null, width, height);
        }

        /// <summary>
        /// Hides the standard title bar for the current custom window context.
        /// </summary>
        public static void HideTitleBar()
        {
            if (_currentPlugin == null) return;

            if (Plugin.UIManager == null || !Plugin.UIManager.IsInitialized)
            {
                QueueAction(() => HideTitleBar());
                return;
            }

            var panel = GetCurrentContextPanel();
            if (panel != null)
            {
                panel.SetTitleBarVisibility(false);
            }
        }

        private static void QueueAction(Action action)
        {
            // Capture state variables for the closure to ensure context is preserved when the queue runs later
            string pName = _currentPlugin;
            string wId = _currentWindowId;

            lock (Lock)
            {
                _initQueue.Add(() =>
                {
                    // Restore context temporarily to ensure correct creation
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
                        // Restore previous state just in case
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

                // Check if panel already exists
                if (!pluginPanels.ContainsKey(windowId))
                {
                    CustomModPanel newPanel;
                    if (!string.IsNullOrEmpty(template))
                    {
                        // Template Mode
                        newPanel = new CustomModPanel(Plugin.UIManager.UiBase, _currentPlugin, windowId, template);
                    }
                    else
                    {
                        // Custom Dimension Mode
                        newPanel = new CustomModPanel(Plugin.UIManager.UiBase, _currentPlugin, windowId, w, h);
                    }

                    // IMPORTANT: Panel must be active initially so ConstructPanelContent() is called
                    // This ensures _textScrollContent and _buttonScrollContent are created
                    newPanel.SetActive(true);

                    // Now immediately hide it - content is created but panel is invisible
                    newPanel.SetActive(false);

                    pluginPanels[windowId] = newPanel;

                    // --- REGISTER WITH UIMANAGER FOR VISIBILITY CONTROL ---
                    // This ensures the panel hides when ESC menu opens, map opens, etc.
                    Plugin.UIManager.RegisterPanel(newPanel);

                    // --- AUTO-GENERATE OPEN BUTTON ---
                    // This creates a button in the main "Mods" list to open this custom window
                    // We access Legacy logic directly here to bypass the "Is Custom Panel Active" check

                    // Ensure the category exists
                    LegacyAddCategory("Windows");

                    string btnText = windowId == "Main" ? $"Open {_currentPlugin} UI" : $"Open {windowId}";

                    // We use the internal AddButtonWithCallback logic but purely for the legacy list
                    LegacyAddButtonWithCallback(btnText, () =>
                    {
                        // Callback to open the panel
                        if (pluginPanels.TryGetValue(windowId, out var p) && p != null)
                        {
                            p.SetActive(true);
                            // Bring to front
                            p.UIRoot.transform.SetAsLastSibling();
                        }
                    }, "Opens the custom window");

                    Plugin.LogInstance.LogInfo($"[ModRegistry] Created Custom Panel '{windowId}' for '{_currentPlugin}' (Hidden by default)");
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

        /// <summary>
        /// Adds a category. If a custom UI is active, adds it to the UI. Otherwise adds to the Mods menu.
        /// </summary>
        public static void AddCategory(string categoryName, float x = -1, float y = -1)
        {
            if (string.IsNullOrWhiteSpace(categoryName)) return;

            // Queueing logic
            if (Plugin.UIManager == null || !Plugin.UIManager.IsInitialized)
            {
                // Only queue if we suspect a UI creation is pending, essentially always queue if UI isn't ready
                QueueAction(() => AddCategory(categoryName, x, y));
                return;
            }

            var panel = GetCurrentContextPanel();
            if (panel != null)
            {
                // Custom UI Mode
                panel.AddCategory(categoryName, x, y);
            }
            else
            {
                // Legacy Mode
                LegacyAddCategory(categoryName);
            }
        }

        /// <summary>
        /// Adds text to the UI. Only works in Custom UI mode.
        /// </summary>
        public static void AddText(string text, float x = -1, float y = -1)
        {
            // Queueing logic
            if (Plugin.UIManager == null || !Plugin.UIManager.IsInitialized)
            {
                QueueAction(() => AddText(text, x, y));
                return;
            }

            var panel = GetCurrentContextPanel();
            if (panel != null)
            {
                // We use the text itself as part of the ID hash for tracking
                string id = $"txt_{text.GetHashCode()}";
                panel.AddText(id, text, x, y);
            }
            else
            {
                Plugin.LogInstance.LogWarning("[ModRegistry] AddText called without a SetUI() context. Legacy Mods menu does not support raw text.");
            }
        }

        /// <summary>
        /// Registers a button. If a custom UI is active, adds it to the UI. Otherwise adds to the Mods menu.
        /// </summary>
        public static bool AddButton(string buttonText, string command, string tooltip = "", float x = -1, float y = -1)
        {
            // Queueing logic
            if (Plugin.UIManager == null || !Plugin.UIManager.IsInitialized)
            {
                QueueAction(() => AddButton(buttonText, command, tooltip, x, y));
                return true;
            }

            var panel = GetCurrentContextPanel();
            if (panel != null)
            {
                // Custom UI Mode
                string id = $"btn_{buttonText}";
                panel.AddButton(id, buttonText, command, x, y);
                return true;
            }
            else
            {
                // Legacy Mode
                return LegacyAddButton(buttonText, command, tooltip);
            }
        }

        /// <summary>
        /// Registers a close button. Only works in Custom UI mode.
        /// </summary>
        public static void AddCloseButton(string text, float x = -1, float y = -1)
        {
            // Queueing logic
            if (Plugin.UIManager == null || !Plugin.UIManager.IsInitialized)
            {
                QueueAction(() => AddCloseButton(text, x, y));
                return;
            }

            var panel = GetCurrentContextPanel();
            if (panel != null)
            {
                string id = $"btn_close_{text}";
                panel.AddCloseButton(id, text, x, y);
            }
            else
            {
                Plugin.LogInstance.LogWarning("[ModRegistry] AddCloseButton called without a SetUI() context.");
            }
        }

        /// <summary>
        /// Registers a button with a direct callback. 
        /// </summary>
        public static bool AddButtonWithCallback(string buttonText, Action onClick, string tooltip = "")
        {
            // Queueing logic
            if (Plugin.UIManager == null || !Plugin.UIManager.IsInitialized)
            {
                QueueAction(() => AddButtonWithCallback(buttonText, onClick, tooltip));
                return true;
            }

            var panel = GetCurrentContextPanel();
            if (panel != null)
            {
                // Basic support for callbacks in Custom UI
                string id = $"btn_{buttonText}";
                // Use -1, -1 for template positioning
                panel.AddButtonWithCallback(id, buttonText, onClick, -1, -1);
                return true;
            }

            return LegacyAddButtonWithCallback(buttonText, onClick, tooltip);
        }

        /// <summary>
        /// Removes a button or element. Checks Custom UI first, then Legacy.
        /// </summary>
        public static bool RemoveButton(string buttonText)
        {
            var panel = GetCurrentContextPanel();
            if (panel != null)
            {
                // Try removing as button
                panel.RemoveElement($"btn_{buttonText}");
                // Try removing as category or text if the user passed that ID
                panel.RemoveElement(buttonText);
                return true;
            }

            // Fallback to Legacy
            return LegacyRemoveButton(buttonText);
        }

        // ==============================================================================================
        // LEGACY IMPLEMENTATIONS (Original Logic)
        // ==============================================================================================

        private static void LegacyAddCategory(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                Plugin.LogInstance.LogError("[ModRegistry] Category name cannot be null or empty.");
                return;
            }

            if (_currentPlugin == null)
            {
                Plugin.LogInstance.LogError("[ModRegistry] Cannot add category: No plugin context set. Call SetPlugin() first.");
                return;
            }

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
            if (string.IsNullOrWhiteSpace(buttonText))
            {
                Plugin.LogInstance.LogError("[ModRegistry] Button text cannot be null or empty.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(command))
            {
                Plugin.LogInstance.LogError("[ModRegistry] Command cannot be null or empty.");
                return false;
            }

            if (_currentPlugin == null)
            {
                Plugin.LogInstance.LogError("[ModRegistry] Cannot add button: No plugin context set. Call SetPlugin() first.");
                return false;
            }

            lock (Lock)
            {
                var plugin = RegisteredPlugins.FirstOrDefault(p =>
                    p.PluginName.Equals(_currentPlugin, StringComparison.OrdinalIgnoreCase));

                if (plugin == null)
                {
                    Plugin.LogInstance.LogError($"[ModRegistry] Plugin '{_currentPlugin}' not found.");
                    return false;
                }

                // If no category is set, create a default "Uncategorized" category
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

                var category = plugin.Categories.FirstOrDefault(c =>
                    c.CategoryName.Equals(_currentCategory, StringComparison.OrdinalIgnoreCase));

                if (category == null)
                {
                    Plugin.LogInstance.LogError($"[ModRegistry] Category '{_currentCategory}' not found in plugin '{_currentPlugin}'.");
                    return false;
                }

                // Check for duplicates within this category
                if (category.Buttons.Exists(b => b.ButtonText.Equals(buttonText, StringComparison.OrdinalIgnoreCase)))
                {
                    Plugin.LogInstance.LogWarning($"[ModRegistry] Button '{buttonText}' already exists in category '{_currentCategory}'.");
                    return false;
                }

                var button = new ModButton
                {
                    ButtonText = buttonText,
                    Command = command,
                    Tooltip = tooltip,
                    OnClick = null,
                    RegisteredAt = DateTime.UtcNow
                };

                category.Buttons.Add(button);

                // Notify listeners that buttons have changed
                OnButtonsChanged?.Invoke();
                return true;
            }
        }

        private static bool LegacyAddButtonWithCallback(string buttonText, Action onClick, string tooltip = "")
        {
            if (string.IsNullOrWhiteSpace(buttonText))
            {
                Plugin.LogInstance.LogError("[ModRegistry] Button text cannot be null or empty.");
                return false;
            }

            if (onClick == null)
            {
                Plugin.LogInstance.LogError("[ModRegistry] onClick callback cannot be null.");
                return false;
            }

            if (_currentPlugin == null)
            {
                Plugin.LogInstance.LogError("[ModRegistry] Cannot add button: No plugin context set. Call SetPlugin() first.");
                return false;
            }

            lock (Lock)
            {
                var plugin = RegisteredPlugins.FirstOrDefault(p =>
                    p.PluginName.Equals(_currentPlugin, StringComparison.OrdinalIgnoreCase));

                if (plugin == null)
                {
                    Plugin.LogInstance.LogError($"[ModRegistry] Plugin '{_currentPlugin}' not found.");
                    return false;
                }

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

                var category = plugin.Categories.FirstOrDefault(c =>
                    c.CategoryName.Equals(_currentCategory, StringComparison.OrdinalIgnoreCase));

                if (category == null)
                {
                    Plugin.LogInstance.LogError($"[ModRegistry] Category '{_currentCategory}' not found in plugin '{_currentPlugin}'.");
                    return false;
                }

                if (category.Buttons.Exists(b => b.ButtonText.Equals(buttonText, StringComparison.OrdinalIgnoreCase)))
                {
                    Plugin.LogInstance.LogWarning($"[ModRegistry] Button '{buttonText}' already exists in category '{_currentCategory}'.");
                    return false;
                }

                var button = new ModButton
                {
                    ButtonText = buttonText,
                    Command = null,
                    Tooltip = tooltip,
                    OnClick = onClick,
                    RegisteredAt = DateTime.UtcNow
                };

                category.Buttons.Add(button);
                OnButtonsChanged?.Invoke();
                return true;
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

                if (plugin == null)
                    return false;

                bool removed = false;
                foreach (var category in plugin.Categories)
                {
                    removed |= category.Buttons.RemoveAll(b =>
                        b.ButtonText.Equals(buttonText, StringComparison.OrdinalIgnoreCase)) > 0;
                }

                if (removed)
                {
                    Plugin.LogInstance.LogInfo($"[ModRegistry] Removed button '{buttonText}' from plugin '{_currentPlugin}'");
                    OnButtonsChanged?.Invoke();
                }

                return removed;
            }
        }

        /// <summary>
        /// Removes an entire plugin and all its categories/buttons AND Custom Panels.
        /// </summary>
        /// <param name="pluginName">The plugin name to remove</param>
        /// <returns>True if removed, false if not found</returns>
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
                    OnButtonsChanged?.Invoke();
                }

                return removedLegacy || removedCustom;
            }
        }

        /// <summary>
        /// Gets all registered plugins with their categories and buttons.
        /// </summary>
        /// <returns>Read-only list of registered plugins</returns>
        public static IReadOnlyList<ModPlugin> GetPlugins()
        {
            lock (Lock)
            {
                return RegisteredPlugins.AsReadOnly();
            }
        }

        /// <summary>
        /// Legacy method for backward compatibility. Returns a flat list of all buttons.
        /// </summary>
        /// <returns>Read-only list of all registered buttons</returns>
        [Obsolete("Use GetPlugins() for better organization. This method will be removed in a future version.")]
        public static IReadOnlyList<ModButton> GetButtons()
        {
            lock (Lock)
            {
                var allButtons = new List<ModButton>();
                foreach (var plugin in RegisteredPlugins)
                {
                    foreach (var category in plugin.Categories)
                    {
                        allButtons.AddRange(category.Buttons);
                    }
                }
                return allButtons.AsReadOnly();
            }
        }

        /// <summary>
        /// Clears all registered plugins, categories, and buttons. Use with caution.
        /// </summary>
        public static void ClearAll()
        {
            lock (Lock)
            {
                RegisteredPlugins.Clear();
                _initQueue.Clear();

                // Clear Custom Panels
                foreach (var plugin in CustomPanels)
                {
                    foreach (var win in plugin.Value)
                    {
                        if (win.Value != null)
                        {
                            win.Value.SetActive(false);
                            win.Value.Destroy();
                        }
                    }
                }
                CustomPanels.Clear();

                _currentPlugin = null;
                _currentCategory = null;
                _currentWindowId = "Main";

                Plugin.LogInstance.LogInfo("[ModRegistry] Cleared all registered plugins");
                OnButtonsChanged?.Invoke();
            }
        }

        /// <summary>
        /// Event fired when buttons are added or removed.
        /// </summary>
        public static event Action OnButtonsChanged;

        /// <summary>
        /// Represents a registered plugin with its categories.
        /// </summary>
        public class ModPlugin
        {
            public string PluginName { get; set; }
            public List<ModCategory> Categories { get; set; }
        }

        /// <summary>
        /// Represents a category within a plugin.
        /// </summary>
        public class ModCategory
        {
            public string CategoryName { get; set; }
            public List<ModButton> Buttons { get; set; }
        }

        /// <summary>
        /// Represents a registered mod button.
        /// </summary>
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