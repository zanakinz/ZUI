using BepInEx.Unity.IL2CPP.Utils;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using ZUI.Utils;

namespace ZUI.Services
{
    /// <summary>
    /// Handles downloading images from URLs (Web or Local Server) and registering them as Sprites.
    /// This component is automatically created and persists through scene loads.
    /// </summary>
    public class ImageDownloader : MonoBehaviour
    {
        private static ImageDownloader _instance;

        // REQUIRED for IL2CPP / V Rising Modding
        public ImageDownloader(IntPtr ptr) : base(ptr) { }

        /// <summary>
        /// Downloads an image from a URL and registers it with SpriteLoader.
        /// </summary>
        /// <param name="name">The unique name to register (e.g. "my_sword.png")</param>
        /// <param name="url">The HTTP/HTTPS URL</param>
        public static void Download(string name, string url)
        {
            if (_instance == null)
            {
                var obj = new GameObject("ZUI_ImageDownloader");
                DontDestroyOnLoad(obj);
                _instance = obj.AddComponent<ImageDownloader>();
            }
            _instance.StartCoroutine(_instance.DownloadRoutine(name, url));
        }

        private IEnumerator DownloadRoutine(string name, string url)
        {
            // Create the request manually (No 'using' block to avoid CS1674)
            UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url);

            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                LogUtils.LogError($"[ImageDownloader] Failed to download {name}: {uwr.error}");
            }
            else
            {
                try
                {
                    var texture = DownloadHandlerTexture.GetContent(uwr);
                    if (texture != null)
                    {
                        // Create sprite from the downloaded texture
                        var sprite = Sprite.Create(
                            texture,
                            new Rect(0, 0, texture.width, texture.height),
                            new Vector2(0.5f, 0.5f)
                        );
                        sprite.name = name;

                        // Register into ZUI system so Panels can find it by name
                        SpriteLoader.RegisterSprite(name, sprite);

                        LogUtils.LogInfo($"[ImageDownloader] Successfully registered: {name}");
                    }
                    else
                    {
                        LogUtils.LogError($"[ImageDownloader] Downloaded texture was null for {name}");
                    }
                }
                catch (Exception ex)
                {
                    LogUtils.LogError($"[ImageDownloader] Error processing texture for {name}: {ex.Message}");
                }
            }

            // Manually dispose to prevent memory leaks
            uwr.Dispose();
        }
    }
}