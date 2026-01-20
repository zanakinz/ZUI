using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using BepInEx;

namespace ZUI.Utils
{
    public static class SpriteLoader
    {
        private static readonly Dictionary<string, Sprite> _cachedSprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> _manualSprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, List<GifFrame>> _manualGifs = new Dictionary<string, List<GifFrame>>();
        private static readonly Dictionary<Assembly, string> _pluginPaths = new Dictionary<Assembly, string>();

        public static void RegisterSprite(string name, Sprite sprite)
        {
            if (string.IsNullOrEmpty(name) || sprite == null) return;
            _manualSprites[name] = sprite;
        }

        public static void RegisterGif(string name, List<GifFrame> frames)
        {
            if (string.IsNullOrEmpty(name) || frames == null) return;
            _manualGifs[name] = frames;
        }

        public static List<GifFrame> GetGif(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return _manualGifs.TryGetValue(name, out var frames) ? frames : null;
        }

        public static Sprite LoadSprite(string filename, float pixelsPerUnit = 100f, Vector4? border = null)
        {
            return LoadSpriteFromAssembly(Assembly.GetCallingAssembly(), filename, pixelsPerUnit, border);
        }

        public static Sprite LoadSpriteFromAssembly(Assembly pluginAssembly, string filename, float pixelsPerUnit = 100f, Vector4? border = null)
        {
            if (string.IsNullOrWhiteSpace(filename)) return null;

            if (_manualSprites.TryGetValue(filename, out var manualSprite)) return manualSprite;

            string assemblyName = pluginAssembly != null ? pluginAssembly.FullName : "Global";
            string cacheKey = $"{assemblyName}|{filename}";
            if (_cachedSprites.TryGetValue(cacheKey, out var cachedSprite)) return cachedSprite;

            string imagePath = FindImageFile(pluginAssembly, filename);
            if (string.IsNullOrEmpty(imagePath)) return null;

            try
            {
                byte[] data = File.ReadAllBytes(imagePath);

                // Magic byte check for GIF (G I F)
                bool isGif = data.Length > 3 && data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46;

                if (isGif)
                {
                    var frames = GifDecoder.Decode(data);
                    if (frames != null && frames.Count > 0)
                    {
                        RegisterGif(filename, frames);

                        // --- THE FIX: PREVENT STACKED TEXTURE CREATION ---
                        // Instead of passing the raw GIF bytes to Unity, we use the decoded texture 
                        // from the first frame to create our static sprite.
                        var firstFrameTex = frames[0].Texture;
                        var sprite = Sprite.Create(
                            firstFrameTex,
                            new Rect(0, 0, firstFrameTex.width, firstFrameTex.height),
                            new Vector2(0.5f, 0.5f),
                            pixelsPerUnit
                        );

                        _cachedSprites[cacheKey] = sprite;
                        return sprite;
                    }
                    return null;
                }

                // Standard Image (PNG/JPG)
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (texture.LoadImage(data))
                {
                    var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect, border ?? Vector4.zero);
                    _cachedSprites[cacheKey] = sprite;
                    return sprite;
                }
            }
            catch (Exception ex)
            {
                LogUtils.LogError($"[SpriteLoader] Error: {ex.Message}");
            }
            return null;
        }

        private static string FindImageFile(Assembly assembly, string filename)
        {
            if (assembly != null)
            {
                string pluginDir = GetPluginDirectory(assembly);
                if (!string.IsNullOrEmpty(pluginDir))
                {
                    string localPath = Path.Combine(pluginDir, "Sprites", filename);
                    if (File.Exists(localPath)) return localPath;
                    string flatPath = Path.Combine(pluginDir, filename);
                    if (File.Exists(flatPath)) return flatPath;
                }
            }
            string globalPath = Path.Combine(Paths.PluginPath, "Sprites", filename);
            return File.Exists(globalPath) ? globalPath : null;
        }

        private static string GetPluginDirectory(Assembly assembly)
        {
            if (assembly == null) return null;
            if (_pluginPaths.TryGetValue(assembly, out var cachedPath)) return cachedPath;
            try { var path = Path.GetDirectoryName(assembly.Location); _pluginPaths[assembly] = path; return path; } catch { return null; }
        }
    }
}