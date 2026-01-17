using System;
using System.Collections.Generic;

namespace ZUI.API
{
    /// <summary>
    /// Clean, simple API wrapper for external plugins to register buttons with ZUI or create Custom UI Windows.
    /// This is the recommended way to interact with ZUI from external plugins.
    /// </summary>
    public static class ZUI
    {
        // ==============================================================================================
        // CONTEXT MANAGEMENT
        // ==============================================================================================

        /// <summary>
        /// Sets the plugin name context for subsequent registrations.
        /// </summary>
        public static void SetPlugin(string pluginName)
        {
            ModRegistry.SetPlugin(pluginName);
        }

        /// <summary>
        /// (Advanced) Sets the active window context for the current plugin.
        /// </summary>
        public static void SetTargetWindow(string windowId)
        {
            ModRegistry.SetTargetWindow(windowId);
        }

        public static bool RemovePlugin(string pluginName)
        {
            return ModRegistry.RemovePlugin(pluginName);
        }

        // ==============================================================================================
        // CUSTOM UI CREATION
        // ==============================================================================================

        /// <summary>
        /// Initialize a Custom UI Window using a pre-defined template (Small, Medium, Large).
        /// </summary>
        public static void SetUI(string templateName)
        {
            ModRegistry.SetUITemplate(templateName);
        }

        /// <summary>
        /// Initialize a Custom UI Window with specific dimensions.
        /// </summary>
        public static void SetUI(int width, int height)
        {
            ModRegistry.SetUICustom(width, height);
        }

        /// <summary>
        /// Hides the standard top title bar (and its default close button). 
        /// Use this if you want a cleaner look and plan to add your own Close Button via <see cref="AddCloseButton(string, float, float)"/>.
        /// </summary>
        public static void HideTitleBar()
        {
            ModRegistry.HideTitleBar();
        }

        // ==============================================================================================
        // CONTENT REGISTRATION
        // ==============================================================================================

        public static void AddCategory(string categoryName)
        {
            ModRegistry.AddCategory(categoryName);
        }

        public static void AddCategory(string categoryName, float x, float y)
        {
            ModRegistry.AddCategory(categoryName, x, y);
        }

        public static bool AddButton(string buttonText, string command)
        {
            return ModRegistry.AddButton(buttonText, command, "");
        }

        public static bool AddButton(string buttonText, string command, string tooltip)
        {
            return ModRegistry.AddButton(buttonText, command, tooltip);
        }

        public static bool AddButton(string buttonText, string command, float x, float y)
        {
            return ModRegistry.AddButton(buttonText, command, "", x, y);
        }

        public static void AddText(string text)
        {
            ModRegistry.AddText(text);
        }

        public static void AddText(string text, float x, float y)
        {
            ModRegistry.AddText(text, x, y);
        }

        // --- NEW: CLOSE BUTTONS ---

        /// <summary>
        /// Adds a button that closes the current window.
        /// (Template Mode: Adds to button list)
        /// </summary>
        public static void AddCloseButton(string text = "Close")
        {
            ModRegistry.AddCloseButton(text, -1, -1);
        }

        /// <summary>
        /// Adds a button that closes the current window at a specific position.
        /// (Canvas Mode: Perfect for custom 'X' buttons in the top right)
        /// </summary>
        public static void AddCloseButton(string text, float x, float y)
        {
            ModRegistry.AddCloseButton(text, x, y);
        }

        // ==============================================================================================
        // MANAGEMENT
        // ==============================================================================================

        public static bool AddButtonWithCallback(string buttonText, Action onClick, string tooltip = "")
        {
            return ModRegistry.AddButtonWithCallback(buttonText, onClick, tooltip);
        }

        public static bool RemoveButton(string buttonText)
        {
            return ModRegistry.RemoveButton(buttonText);
        }

        public static void RemoveElement(string elementId)
        {
            ModRegistry.RemoveButton(elementId);
        }

        public static void ClearAll()
        {
            ModRegistry.ClearAll();
        }

        public static IReadOnlyList<ModRegistry.ModPlugin> GetPlugins()
        {
            return ModRegistry.GetPlugins();
        }

        public static event Action OnButtonsChanged
        {
            add => ModRegistry.OnButtonsChanged += value;
            remove => ModRegistry.OnButtonsChanged -= value;
        }

        #region Type Aliases
        public class Plugin : ModRegistry.ModPlugin { }
        public class Category : ModRegistry.ModCategory { }
        public class Button : ModRegistry.ModButton { }
        #endregion
    }
}