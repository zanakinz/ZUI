using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using ZUI.Utils;
using ZUI.API;
using Il2CppInterop.Runtime.Attributes;

namespace ZUI.Services
{
    /// <summary>
    /// Handles downloading images from URLs and registering them as Sprites.
    /// Explicitly prevents GIF "stacking" by creating clean textures from decoded frames.
    /// </summary>
    public class ImageDownloader : MonoBehaviour
    {
        private static ImageDownloader _instance;
        private readonly List<DownloadTask> _activeTasks = new List<DownloadTask>();

        // REQUIRED for IL2CPP / V Rising Modding
        public ImageDownloader(IntPtr ptr) : base(ptr) { }

        private struct DownloadTask
        {
            public string Name;
            public string Url;
            public UnityWebRequest Request;
        }

        public static void Download(string name, string url)
        {
            if (string.IsNullOrEmpty(url)) return;

            if (_instance == null)
            {
                var obj = new GameObject("ZUI_ImageDownloader");
                GameObject.DontDestroyOnLoad(obj);
                _instance = obj.AddComponent<ImageDownloader>();
            }

            var uwr = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("User-Agent", "ZUI-Client/2.2.0 (V Rising Mod)");
            uwr.SendWebRequest();

            _instance._activeTasks.Add(new DownloadTask { Name = name, Url = url, Request = uwr });
            LogUtils.LogInfo($"[ImageDownloader] Started download for {name}");
        }

        private void Update()
        {
            if (_activeTasks.Count == 0) return;

            for (int i = _activeTasks.Count - 1; i >= 0; i--)
            {
                var task = _activeTasks[i];
                if (task.Request.isDone)
                {
                    HandleFinishedDownload(task);
                    _activeTasks.RemoveAt(i);
                }
            }
        }

        [HideFromIl2Cpp]
        private void HandleFinishedDownload(DownloadTask task)
        {
            var uwr = task.Request;

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                LogUtils.LogError($"[ImageDownloader] Failed to download {task.Name}: {uwr.error}");
                uwr.Dispose();
                return;
            }

            try
            {
                byte[] data = uwr.downloadHandler.data;
                if (data == null || data.Length < 4) return;

                // Magic Byte Check for GIF
                bool isGif = (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46);

                if (isGif)
                {
                    var frames = GifDecoder.Decode(data);
                    if (frames != null && frames.Count > 0)
                    {
                        // Register animation data
                        SpriteLoader.RegisterGif(task.Name, frames);

                        // --- FIX: Prevent Stacking Flicker ---
                        // Instead of using the raw 'data' to create a sprite (which creates the stack),
                        // we create a BRAND NEW texture and copy only the first frame pixels into it.
                        var firstFrameTex = frames[0].Texture;
                        Texture2D cleanTex = new Texture2D(firstFrameTex.width, firstFrameTex.height, TextureFormat.RGBA32, false);
                        cleanTex.SetPixels32(firstFrameTex.GetPixels32());
                        cleanTex.Apply();
                        cleanTex.name = $"{task.Name}_clean_static";

                        var cleanSprite = Sprite.Create(
                            cleanTex,
                            new Rect(0, 0, cleanTex.width, cleanTex.height),
                            new Vector2(0.5f, 0.5f)
                        );
                        cleanSprite.name = task.Name;

                        // Register the clean, non-stacked version as the primary sprite
                        SpriteLoader.RegisterSprite(task.Name, cleanSprite);
                        LogUtils.LogInfo($"[ImageDownloader] Registered Clean GIF: {task.Name}");
                    }
                }
                else
                {
                    // Standard Image (PNG/JPG)
                    Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (texture.LoadImage(data))
                    {
                        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                        sprite.name = task.Name;
                        SpriteLoader.RegisterSprite(task.Name, sprite);
                        LogUtils.LogInfo($"[ImageDownloader] Registered Image: {task.Name}");
                    }
                }

                // Notify UI to swap placeholders for the new clean sprites
                ModRegistry.NotifyChanges();
            }
            catch (Exception ex)
            {
                LogUtils.LogError($"[ImageDownloader] Error processing {task.Name}: {ex.Message}");
            }
            finally
            {
                uwr.Dispose();
            }
        }
    }
}