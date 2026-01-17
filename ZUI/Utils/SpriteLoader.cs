using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using BepInEx; // Required for Paths.PluginPath

namespace ZUI.Utils
{
    /// <summary>
    /// Utility class for loading sprites from plugin directories.
    /// Loads images from: 
    /// 1. BepInEx/plugins/{YourPlugin}/Sprites/{filename}
    /// 2. BepInEx/plugins/Sprites/{filename} (Fallback)
    /// </summary>
    public static class SpriteLoader
    {
        private static readonly Dictionary<string, Sprite> _cachedSprites = new();
        private static readonly Dictionary<Assembly, string> _pluginPaths = new();

        /// <summary>
        /// Loads a sprite from the calling plugin's directory or the global Sprites folder.
        /// </summary>
        /// <param name="filename">The filename of the image (e.g., "icon.png")</param>
        /// <param name="pixelsPerUnit">Pixels per unit for the sprite (default: 100)</param>
        /// <param name="border">The 9-slice border (Left, Bottom, Right, Top). Default is zero.</param>
        /// <returns>The loaded Sprite, or null if loading failed</returns>
        public static Sprite LoadSprite(string filename, float pixelsPerUnit = 100f, Vector4? border = null)
        {
            try
            {
                // Get the calling assembly (the mod that called this method)
                var callingAssembly = Assembly.GetCallingAssembly();
                return LoadSpriteFromAssembly(callingAssembly, filename, pixelsPerUnit, border);
            }
            catch (Exception ex)
            {
                LogUtils.LogError($"[SpriteLoader] Failed to load sprite '{filename}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Loads a sprite from a specific assembly context.
        /// </summary>
        public static Sprite LoadSpriteFromAssembly(Assembly pluginAssembly, string filename, float pixelsPerUnit = 100f, Vector4? border = null)
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
                // Generate a unique cache key that includes the border dimensions
                // This ensures if we load "panel.png" with borders and then without, they don't conflict
                string borderSuffix = border.HasValue ? $"_{border.Value}" : "_0";
                string cacheKey = $"{pluginAssembly.FullName}|{filename}{borderSuffix}";

                // Check Cache
                if (_cachedSprites.TryGetValue(cacheKey, out var cachedSprite))
                {
                    // Verify the Unity Object hasn't been destroyed by the engine
                    if (cachedSprite != null && cachedSprite.texture != null)
                    {
                        return cachedSprite;
                    }
                    // If destroyed, remove from cache and reload
                    _cachedSprites.Remove(cacheKey);
                }

                // --- LOCATE THE FILE ---
                string imagePath = FindImageFile(pluginAssembly, filename);

                if (string.IsNullOrEmpty(imagePath))
                {
                    // Debug log to help you track what went wrong, but usually we just return null so the UI can fallback
                    // LogUtils.LogWarning($"[SpriteLoader] Could not find image '{filename}' in plugin folder or global Sprites folder.");
                    return null;
                }

                // --- LOAD TEXTURE ---
                Texture2D texture = LoadTexture(imagePath);
                if (texture == null)
                {
                    LogUtils.LogError($"[SpriteLoader] Failed to decode texture from: {imagePath}");
                    return null;
                }

                // --- CREATE SPRITE ---
                // Apply the 9-slice border if provided
                Vector4 spriteBorder = border ?? Vector4.zero;

                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), // Pivot Center
                    pixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect,
                    spriteBorder
                );

                if (sprite != null)
                {
                    sprite.name = Path.GetFileNameWithoutExtension(filename);
                    _cachedSprites[cacheKey] = sprite;
                    // LogUtils.LogInfo($"[SpriteLoader] Loaded: {filename} (Border: {spriteBorder})");
                }

                return sprite;
            }
            catch (Exception ex)
            {
                LogUtils.LogError($"[SpriteLoader] Critical exception loading '{filename}': {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Locates the image file by checking multiple valid directories.
        /// </summary>
        private static string FindImageFile(Assembly assembly, string filename)
        {
            // Strategy 1: Check BepInEx/plugins/{MyPlugin}/Sprites/{filename}
            // This is the "Best Practice" location
            string pluginDir = GetPluginDirectory(assembly);
            if (!string.IsNullOrEmpty(pluginDir))
            {
                string localSpritesPath = Path.Combine(pluginDir, "Sprites", filename);
                if (File.Exists(localSpritesPath))
                {
                    return localSpritesPath;
                }
            }

            // Strategy 2: Check BepInEx/plugins/Sprites/{filename}
            // This is the "Global/Shared" location (where your screenshot shows the files are)
            try
            {
                string globalSpritesPath = Path.Combine(Paths.PluginPath, "Sprites", filename);
                if (File.Exists(globalSpritesPath))
                {
                    return globalSpritesPath;
                }
            }
            catch (Exception)
            {
                // Paths.PluginPath might fail if BepInEx isn't fully initialized or weird context
            }

            // Strategy 3: Check relative to the DLL if it's not in a standard structure
            if (!string.IsNullOrEmpty(pluginDir))
            {
                string flatPath = Path.Combine(pluginDir, filename);
                if (File.Exists(flatPath)) return flatPath;
            }

            return null;
        }

        /// <summary>
        /// Clears the sprite cache. Useful for reloading sprites during development without restarting.
        /// </summary>
        public static void ClearCache()
        {
            int count = 0;
            foreach (var sprite in _cachedSprites.Values)
            {
                if (sprite != null)
                {
                    if (sprite.texture != null)
                    {
                        UnityEngine.Object.Destroy(sprite.texture);
                    }
                    UnityEngine.Object.Destroy(sprite);
                    count++;
                }
            }
            _cachedSprites.Clear();
            _pluginPaths.Clear();
            LogUtils.LogInfo($"[SpriteLoader] Cleared {count} sprites from cache.");
        }

        private static string GetPluginDirectory(Assembly assembly)
        {
            // Cache the assembly path lookup to avoid expensive Reflection/IO calls every time
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
                    return null;
                }

                // Get the directory containing the .dll
                var pluginDir = Path.GetDirectoryName(assemblyLocation);
                _pluginPaths[assembly] = pluginDir;
                return pluginDir;
            }
            catch (Exception ex)
            {
                LogUtils.LogError($"[SpriteLoader] Failed to resolve directory for assembly {assembly.FullName}: {ex.Message}");
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
                // Size (2,2) is a placeholder; LoadImage replaces it with the actual file dimensions
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                // Important: linear vs sRGB. UI usually wants sRGB, but let's stick to default.
                // Bilinear filtering makes resizing look smoother. Point makes it pixelated.
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp; // Don't tile

                if (!texture.LoadImage(fileData))
                {
                    LogUtils.LogError($"[SpriteLoader] Texture.LoadImage failed for: {filePath}");
                    UnityEngine.Object.Destroy(texture);
                    return null;
                }

                return texture;
            }
            catch (Exception ex)
            {
                LogUtils.LogError($"[SpriteLoader] Exception loading texture I/O: {ex.Message}");
                return null;
            }
        }
    }
}