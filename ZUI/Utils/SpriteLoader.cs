using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using BepInEx;

namespace ZUI.Utils
{
    /// <summary>
    /// Utility class for loading sprites from plugin directories or manual registration (URLs).
    /// Loads images from: 
    /// 1. Manual Registry (Runtime/Downloaded)
    /// 2. BepInEx/plugins/{YourPlugin}/Sprites/{filename}
    /// 3. BepInEx/plugins/Sprites/{filename} (Fallback)
    /// </summary>
    public static class SpriteLoader
    {
        // Cache for file-loaded sprites
        private static readonly Dictionary<string, Sprite> _cachedSprites = new();

        // Cache for sprites registered manually (via URL or Base64/Runtime)
        private static readonly Dictionary<string, Sprite> _manualSprites = new();

        private static readonly Dictionary<Assembly, string> _pluginPaths = new();

        /// <summary>
        /// Registers a sprite manually into the system. 
        /// Used for images downloaded via URL or generated at runtime.
        /// </summary>
        public static void RegisterSprite(string name, Sprite sprite)
        {
            if (string.IsNullOrEmpty(name) || sprite == null) return;

            if (_manualSprites.ContainsKey(name))
            {
                // Clean up old sprite if overwriting to prevent leaks
                var old = _manualSprites[name];
                if (old != null) UnityEngine.Object.Destroy(old);
                _manualSprites[name] = sprite;
            }
            else
            {
                _manualSprites.Add(name, sprite);
            }
        }

        /// <summary>
        /// Loads a sprite from the calling plugin's directory or the global Sprites folder.
        /// </summary>
        public static Sprite LoadSprite(string filename, float pixelsPerUnit = 100f, Vector4? border = null)
        {
            try
            {
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
            if (string.IsNullOrWhiteSpace(filename)) return null;

            // 1. CHECK MANUAL CACHE FIRST (URL/Runtime images)
            // This allows downloaded images to override file-based lookups or serve as the primary source
            if (_manualSprites.TryGetValue(filename, out var manualSprite))
            {
                // Ensure the object hasn't been destroyed
                if (manualSprite != null) return manualSprite;
                _manualSprites.Remove(filename);
            }

            // 2. Generate cache key for file-based sprites
            // Handle null assembly gracefully for key generation (Server Packet context)
            string assemblyName = pluginAssembly != null ? pluginAssembly.FullName : "Global";
            string borderSuffix = border.HasValue ? $"_{border.Value}" : "_0";
            string cacheKey = $"{assemblyName}|{filename}{borderSuffix}";

            // 3. Check File Cache
            if (_cachedSprites.TryGetValue(cacheKey, out var cachedSprite))
            {
                if (cachedSprite != null && cachedSprite.texture != null)
                {
                    return cachedSprite;
                }
                _cachedSprites.Remove(cacheKey);
            }

            // 4. Locate File on Disk
            string imagePath = FindImageFile(pluginAssembly, filename);

            if (string.IsNullOrEmpty(imagePath))
            {
                return null;
            }

            try
            {
                var texture = LoadTexture(imagePath);
                if (texture == null)
                {
                    LogUtils.LogError($"[SpriteLoader] Failed to decode texture from: {imagePath}");
                    return null;
                }

                // Apply the 9-slice border if provided
                Vector4 spriteBorder = border ?? Vector4.zero;

                var sprite = Sprite.Create(
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
                }

                return sprite;
            }
            catch (Exception ex)
            {
                LogUtils.LogError($"[SpriteLoader] Error creating sprite from '{imagePath}': {ex.Message}");
                return null;
            }
        }

        private static string FindImageFile(Assembly assembly, string filename)
        {
            // Strategy 1: Check BepInEx/plugins/{PluginName}/Sprites/{filename}
            // Only if assembly is provided
            if (assembly != null)
            {
                string pluginDir = GetPluginDirectory(assembly);
                if (!string.IsNullOrEmpty(pluginDir))
                {
                    string localPath = Path.Combine(pluginDir, "Sprites", filename);
                    if (File.Exists(localPath)) return localPath;
                }
            }

            // Strategy 2: Check BepInEx/plugins/Sprites/{filename} (Fallback / Global)
            try
            {
                string globalPath = Path.Combine(Paths.PluginPath, "Sprites", filename);
                if (File.Exists(globalPath)) return globalPath;
            }
            catch { }

            // Strategy 3: Check relative to the DLL (Flat structure)
            if (assembly != null)
            {
                string pluginDir = GetPluginDirectory(assembly);
                if (!string.IsNullOrEmpty(pluginDir))
                {
                    string flatPath = Path.Combine(pluginDir, filename);
                    if (File.Exists(flatPath)) return flatPath;
                }
            }

            return null;
        }

        public static void ClearCache()
        {
            // Clear Manual Cache
            foreach (var s in _manualSprites.Values)
            {
                if (s != null) UnityEngine.Object.Destroy(s);
            }
            _manualSprites.Clear();

            // Clear File Cache
            foreach (var s in _cachedSprites.Values)
            {
                if (s != null)
                {
                    if (s.texture != null) UnityEngine.Object.Destroy(s.texture);
                    UnityEngine.Object.Destroy(s);
                }
            }
            _cachedSprites.Clear();
            _pluginPaths.Clear();

            LogUtils.LogInfo($"[SpriteLoader] Cleared all sprite caches.");
        }

        private static string GetPluginDirectory(Assembly assembly)
        {
            if (assembly == null) return null;

            if (_pluginPaths.TryGetValue(assembly, out var cachedPath)) return cachedPath;
            try
            {
                if (string.IsNullOrEmpty(assembly.Location)) return null;

                var path = Path.GetDirectoryName(assembly.Location);
                _pluginPaths[assembly] = path;
                return path;
            }
            catch { return null; }
        }

        private static Texture2D LoadTexture(string filePath)
        {
            try
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;

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