using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace ZUI.Utils
{
    /// <summary>
    /// Utility class for loading sprites from plugin directories.
    /// Loads images from: BepInEx/plugins/{PluginName}/Sprites/
    /// </summary>
    public static class SpriteLoader
    {
        private static readonly Dictionary<string, Sprite> _cachedSprites = new();
        private static readonly Dictionary<Assembly, string> _pluginPaths = new();

        /// <summary>
        /// Loads a sprite from the calling plugin's Sprites directory.
        /// Images should be placed in: BepInEx/plugins/{YourPlugin.dll}/Sprites/{filename}
        /// Supported formats: PNG, JPG
        /// </summary>
        /// <param name="filename">The filename of the image (e.g., "icon.png")</param>
        /// <param name="pixelsPerUnit">Pixels per unit for the sprite (default: 100)</param>
        /// <returns>The loaded Sprite, or null if loading failed</returns>
        public static Sprite LoadSprite(string filename, float pixelsPerUnit = 100f)
        {
            try
            {
                // Get the calling assembly (the mod that called this method)
                var callingAssembly = Assembly.GetCallingAssembly();
                return LoadSpriteFromAssembly(callingAssembly, filename, pixelsPerUnit);
            }
            catch (Exception ex)
            {
                LogUtils.LogError($"[SpriteLoader] Failed to load sprite '{filename}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Loads a sprite from a specific plugin's Sprites directory.
        /// </summary>
        /// <param name="pluginAssembly">The assembly of the plugin</param>
        /// <param name="filename">The filename of the image</param>
        /// <param name="pixelsPerUnit">Pixels per unit for the sprite</param>
        /// <returns>The loaded Sprite, or null if loading failed</returns>
        public static Sprite LoadSpriteFromAssembly(Assembly pluginAssembly, string filename, float pixelsPerUnit = 100f)
        {
            if (pluginAssembly == null)
            {
                LogUtils.LogError("[SpriteLoader] Plugin assembly is null");
                return null;
            }

            if (string.IsNullOrWhiteSpace(filename))
            {
                LogUtils.LogError("[SpriteLoader] Filename is null or empty");
                return null;
            }

            try
            {
                // Check cache first
                var cacheKey = $"{pluginAssembly.FullName}|{filename}";
                if (_cachedSprites.TryGetValue(cacheKey, out var cachedSprite))
                {
                    return cachedSprite;
                }

                // Get plugin directory
                var pluginPath = GetPluginDirectory(pluginAssembly);
                if (string.IsNullOrEmpty(pluginPath))
                {
                    LogUtils.LogError($"[SpriteLoader] Could not determine plugin directory for assembly: {pluginAssembly.GetName().Name}");
                    return null;
                }

                // Construct sprites directory path
                var spritesDir = Path.Combine(pluginPath, "Sprites");
                if (!Directory.Exists(spritesDir))
                {
                    LogUtils.LogWarning($"[SpriteLoader] Sprites directory does not exist: {spritesDir}");
                    return null;
                }

                // Construct full file path
                var filePath = Path.Combine(spritesDir, filename);
                if (!File.Exists(filePath))
                {
                    LogUtils.LogWarning($"[SpriteLoader] Sprite file not found: {filePath}");
                    return null;
                }

                // Load texture from file
                var texture = LoadTexture(filePath);
                if (texture == null)
                {
                    LogUtils.LogError($"[SpriteLoader] Failed to load texture from: {filePath}");
                    return null;
                }

                // Create sprite from texture
                var sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    pixelsPerUnit
                );

                if (sprite != null)
                {
                    sprite.name = Path.GetFileNameWithoutExtension(filename);
                    _cachedSprites[cacheKey] = sprite;
                    LogUtils.LogInfo($"[SpriteLoader] Successfully loaded sprite: {filename} from {pluginAssembly.GetName().Name}");
                }

                return sprite;
            }
            catch (Exception ex)
            {
                LogUtils.LogError($"[SpriteLoader] Exception loading sprite '{filename}': {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Clears the sprite cache. Useful for reloading sprites during development.
        /// </summary>
        public static void ClearCache()
        {
            _cachedSprites.Clear();
            _pluginPaths.Clear();
            LogUtils.LogInfo("[SpriteLoader] Sprite cache cleared");
        }

        private static string GetPluginDirectory(Assembly assembly)
        {
            if (_pluginPaths.TryGetValue(assembly, out var cachedPath))
            {
                return cachedPath;
            }

            try
            {
                // Get the location of the assembly (the .dll file)
                var assemblyLocation = assembly.Location;
                if (string.IsNullOrEmpty(assemblyLocation))
                {
                    LogUtils.LogError($"[SpriteLoader] Assembly location is empty for: {assembly.GetName().Name}");
                    return null;
                }

                // Get the directory containing the .dll
                var pluginDir = Path.GetDirectoryName(assemblyLocation);
                _pluginPaths[assembly] = pluginDir;
                return pluginDir;
            }
            catch (Exception ex)
            {
                LogUtils.LogError($"[SpriteLoader] Failed to get plugin directory: {ex.Message}");
                return null;
            }
        }

        private static Texture2D LoadTexture(string filePath)
        {
            try
            {
                // Read file bytes
                var fileData = File.ReadAllBytes(filePath);
                if (fileData == null || fileData.Length == 0)
                {
                    LogUtils.LogError($"[SpriteLoader] File is empty: {filePath}");
                    return null;
                }

                // Create texture and load image data
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;

                if (!texture.LoadImage(fileData))
                {
                    LogUtils.LogError($"[SpriteLoader] Failed to load image data from: {filePath}");
                    UnityEngine.Object.Destroy(texture);
                    return null;
                }

                return texture;
            }
            catch (Exception ex)
            {
                LogUtils.LogError($"[SpriteLoader] Exception loading texture: {ex.Message}");
                return null;
            }
        }
    }
}
