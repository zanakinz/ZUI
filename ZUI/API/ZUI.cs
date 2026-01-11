using System;
using System.Collections.Generic;

namespace ZUI.API
{
    /// <summary>
    /// Clean, simple API wrapper for external plugins to register buttons with ZUI.
    /// This is the recommended way to interact with ZUI from external plugins.
    /// 
    /// For advanced usage, you can also use ModRegistry directly.
    /// </summary>
    public static class ZUI
    {
        /// <summary>
        /// Sets the plugin name context for subsequent registrations.
        /// All buttons and categories registered after this call will be grouped under this plugin.
        /// This must be called before adding categories or buttons.
        /// </summary>
        /// <param name="pluginName">The name of your plugin (e.g., "MyAwesomeMod")</param>
        /// <example>
        /// <code>
        /// ZUI.SetPlugin("MyMod");
        /// </code>
        /// </example>
        public static void SetPlugin(string pluginName)
        {
            ModRegistry.SetPlugin(pluginName);
        }

        /// <summary>
        /// Adds a category under the current plugin context.
        /// All buttons registered after this call will be grouped under this category.
        /// </summary>
        /// <param name="categoryName">The name of the category (e.g., "Player", "Admin", "Utility")</param>
        /// <example>
        /// <code>
        /// ZUI.SetPlugin("MyMod");
        /// ZUI.AddCategory("Player");
        /// ZUI.AddButton("Heal", ".heal");
        /// </code>
        /// </example>
        public static void AddCategory(string categoryName)
        {
            ModRegistry.AddCategory(categoryName);
        }

        /// <summary>
        /// Adds a button under the current plugin and category context.
        /// The button will execute the specified command when clicked.
        /// </summary>
        /// <param name="buttonText">The text to display on the button</param>
        /// <param name="command">The BloodCraft command to execute (e.g., ".heal", ".spawn npc")</param>
        /// <param name="tooltip">Optional tooltip text (not yet implemented)</param>
        /// <returns>True if registration successful, false if duplicate or invalid</returns>
        /// <example>
        /// <code>
        /// ZUI.SetPlugin("MyMod");
        /// ZUI.AddCategory("Player");
        /// ZUI.AddButton("Heal", ".heal");
        /// ZUI.AddButton("Teleport", ".tp home", "Teleports you to spawn");
        /// </code>
        /// </example>
        public static bool AddButton(string buttonText, string command, string tooltip = "")
        {
            return ModRegistry.AddButton(buttonText, command, tooltip);
        }

        /// <summary>
        /// Removes a registered button by text from the current plugin context.
        /// The plugin context must be set before calling this method.
        /// </summary>
        /// <param name="buttonText">The button text to remove</param>
        /// <returns>True if removed, false if not found</returns>
        /// <example>
        /// <code>
        /// ZUI.SetPlugin("MyMod");
        /// ZUI.RemoveButton("Heal");
        /// </code>
        /// </example>
        public static bool RemoveButton(string buttonText)
        {
            return ModRegistry.RemoveButton(buttonText);
        }

        /// <summary>
        /// Removes an entire plugin and all its categories/buttons.
        /// </summary>
        /// <param name="pluginName">The plugin name to remove</param>
        /// <returns>True if removed, false if not found</returns>
        /// <example>
        /// <code>
        /// ZUI.RemovePlugin("MyMod");
        /// </code>
        /// </example>
        public static bool RemovePlugin(string pluginName)
        {
            return ModRegistry.RemovePlugin(pluginName);
        }

        /// <summary>
        /// Gets all registered plugins with their categories and buttons.
        /// Useful for debugging or displaying registered content.
        /// </summary>
        /// <returns>Read-only list of registered plugins</returns>
        /// <example>
        /// <code>
        /// var plugins = ZUI.GetPlugins();
        /// foreach (var plugin in plugins)
        /// {
        ///     Console.WriteLine($"Plugin: {plugin.PluginName}");
        ///     foreach (var category in plugin.Categories)
        ///     {
        ///         Console.WriteLine($"  Category: {category.CategoryName}");
        ///         foreach (var button in category.Buttons)
        ///         {
        ///             Console.WriteLine($"    Button: {button.ButtonText}");
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        public static IReadOnlyList<ModRegistry.ModPlugin> GetPlugins()
        {
            return ModRegistry.GetPlugins();
        }

        /// <summary>
        /// Clears all registered plugins, categories, and buttons.
        /// WARNING: Use with extreme caution as this will remove all registrations from all plugins!
        /// </summary>
        /// <example>
        /// <code>
        /// ZUI.ClearAll(); // This will clear EVERYTHING!
        /// </code>
        /// </example>
        public static void ClearAll()
        {
            ModRegistry.ClearAll();
        }

        /// <summary>
        /// Event fired when buttons are added or removed.
        /// Subscribe to this event to be notified of changes.
        /// </summary>
        /// <example>
        /// <code>
        /// ZUI.OnButtonsChanged += () => Console.WriteLine("Buttons updated!");
        /// </code>
        /// </example>
        public static event Action OnButtonsChanged
        {
            add => ModRegistry.OnButtonsChanged += value;
            remove => ModRegistry.OnButtonsChanged -= value;
        }

        #region Type Aliases for Convenience
        
        /// <summary>
        /// Alias for ModRegistry.ModPlugin for convenience when using ZUI API.
        /// </summary>
        public class Plugin : ModRegistry.ModPlugin { }

        /// <summary>
        /// Alias for ModRegistry.ModCategory for convenience when using ZUI API.
        /// </summary>
        public class Category : ModRegistry.ModCategory { }

        /// <summary>
        /// Alias for ModRegistry.ModButton for convenience when using ZUI API.
        /// </summary>
        public class Button : ModRegistry.ModButton { }

        #endregion
    }
}
