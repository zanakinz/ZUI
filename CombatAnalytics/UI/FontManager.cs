using System;
using UnityEngine;
using TMPro;
using Il2CppInterop.Runtime;

namespace CombatAnalytics.UI
{
    /// <summary>
    /// Manages the default font asset for TextMeshPro components
    /// </summary>
    public static class FontManager
    {
        private static TMP_FontAsset _defaultFont;
        private static Material _defaultFontMaterial;
        private static bool _initialized = false;

        public static TMP_FontAsset DefaultFont
        {
            get
            {
                if (!_initialized)
                {
                    InitializeFont();
                }
                return _defaultFont;
            }
        }

        public static Material DefaultFontMaterial
        {
            get
            {
                if (!_initialized)
                {
                    InitializeFont();
                }
                return _defaultFontMaterial;
            }
        }

        private static void InitializeFont()
        {
            if (_initialized) return;

            try
            {
                Plugin.Instance.Log.LogInfo("Initializing font asset...");

                // Try to find any existing TextMeshProUGUI in the scene to get its font
                var existingTexts = UnityEngine.Object.FindObjectsOfType(Il2CppType.Of<TextMeshProUGUI>());
                
                foreach (var obj in existingTexts)
                {
                    var tmp = obj.TryCast<TextMeshProUGUI>();
                    if (tmp != null && tmp.font != null)
                    {
                        _defaultFont = tmp.font;
                        _defaultFontMaterial = tmp.fontMaterial;
                        _initialized = true;
                        Plugin.Instance.Log.LogInfo($"Font asset initialized from existing UI: {_defaultFont.name}");
                        return;
                    }
                }

                // If we couldn't find a font from existing UI, try to load the default TMP font
                Plugin.Instance.Log.LogWarning("Could not find font from existing UI elements. Trying to load default font...");
                _defaultFont = TMP_Settings.defaultFontAsset;
                if (_defaultFont != null)
                {
                    _defaultFontMaterial = _defaultFont.material;
                    _initialized = true;
                    Plugin.Instance.Log.LogInfo("Default TMP font asset loaded");
                }
                else
                {
                    Plugin.Instance.Log.LogError("Failed to initialize font asset!");
                }
            }
            catch (Exception ex)
            {
                Plugin.Instance.Log.LogError($"Error initializing font: {ex}");
            }
        }

        /// <summary>
        /// Force re-initialization of the font (useful if called before UI is ready)
        /// </summary>
        public static void Reset()
        {
            _initialized = false;
            _defaultFont = null;
            _defaultFontMaterial = null;
        }
    }
}
