using System;
using System.Collections.Generic;
using System.Reflection;

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

        public static void SetPlugin(string pluginName)
        {
            ModRegistry.SetPlugin(pluginName);
        }

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

        public static void SetUI(string templateName)
        {
            ModRegistry.SetUITemplate(templateName);
        }

        public static void SetUI(int width, int height)
        {
            ModRegistry.SetUICustom(width, height);
        }

        public static void SetTitle(string title)
        {
            ModRegistry.SetWindowTitle(title);
        }

        public static void HideTitleBar()
        {
            ModRegistry.HideTitleBar();
        }

        // ==============================================================================================
        // TABS (NEW)
        // ==============================================================================================

        public static void CreateTab(string name, string tooltip = "")
        {
            ModRegistry.CreateTab(name, tooltip);
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

        public static bool AddButton(string buttonText, string command, float x, float y, float width, float height)
        {
            var assembly = Assembly.GetCallingAssembly();
            // Pass null for imageName to trigger default styling
            return ModRegistry.AddButton(assembly, buttonText, command, null, x, y, width, height);
        }

        public static bool AddButton(string buttonText, string command, string imageName, float x, float y, float width, float height)
        {
            var assembly = Assembly.GetCallingAssembly();
            return ModRegistry.AddButton(assembly, buttonText, command, imageName, x, y, width, height);
        }

        public static void AddText(string text)
        {
            ModRegistry.AddText(text);
        }

        public static void AddText(string text, float x, float y)
        {
            ModRegistry.AddText(text, x, y);
        }

        public static void AddImage(string imageName, float x, float y, float width, float height)
        {
            var assembly = Assembly.GetCallingAssembly();
            ModRegistry.AddImage(assembly, imageName, x, y, width, height);
        }

        // --- CLOSE BUTTONS ---

        public static void AddCloseButton(string text = "Close")
        {
            ModRegistry.AddCloseButton(text, -1, -1);
        }

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