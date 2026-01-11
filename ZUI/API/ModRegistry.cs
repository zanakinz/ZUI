using System;
using System.Collections.Generic;
using System.Linq;

namespace ZUI.API
{
    /// <summary>
    /// Public API for external mods to register custom buttons in ZUI's Mods menu.
    /// </summary>
    public static class ModRegistry
    {
        private static readonly List<ModPlugin> RegisteredPlugins = new();
        private static readonly object Lock = new();
        private static string _currentPlugin = null;
        private static string _currentCategory = null;

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

                // Create plugin entry if it doesn't exist
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
        /// Adds a category under the current plugin context.
        /// All buttons registered after this call will be grouped under this category.
        /// </summary>
        /// <param name="categoryName">The name of the category</param>
        public static void AddCategory(string categoryName)
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
                    // Removed verbose logging - only log plugin registration and errors
                }
            }
        }

        /// <summary>
        /// Registers a button under the current plugin and category context.
        /// </summary>
        /// <param name="buttonText">The text to display on the button</param>
        /// <param name="command">The full command to execute when clicked</param>
        /// <param name="tooltip">Optional tooltip text (not yet implemented)</param>
        /// <returns>True if registration successful, false if duplicate or invalid</returns>
        public static bool AddButton(string buttonText, string command, string tooltip = "")
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
                    RegisteredAt = DateTime.UtcNow
                };

                category.Buttons.Add(button);
                // Removed verbose button logging - only log plugin registration and errors
                
                OnButtonsChanged?.Invoke();
                return true;
            }
        }

        /// <summary>
        /// Removes a registered button by text from the current plugin context.
        /// </summary>
        /// <param name="buttonText">The button text to remove</param>
        /// <returns>True if removed, false if not found</returns>
        public static bool RemoveButton(string buttonText)
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
        /// Removes an entire plugin and all its categories/buttons.
        /// </summary>
        /// <param name="pluginName">The plugin name to remove</param>
        /// <returns>True if removed, false if not found</returns>
        public static bool RemovePlugin(string pluginName)
        {
            if (string.IsNullOrWhiteSpace(pluginName))
                return false;

            lock (Lock)
            {
                var removed = RegisteredPlugins.RemoveAll(p => 
                    p.PluginName.Equals(pluginName, StringComparison.OrdinalIgnoreCase)) > 0;

                if (removed)
                {
                    Plugin.LogInstance.LogInfo($"[ModRegistry] Removed plugin '{pluginName}'");
                    if (_currentPlugin?.Equals(pluginName, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        _currentPlugin = null;
                        _currentCategory = null;
                    }
                    OnButtonsChanged?.Invoke();
                }

                return removed;
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
                _currentPlugin = null;
                _currentCategory = null;
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
            public DateTime RegisteredAt { get; set; }
        }
    }
}
