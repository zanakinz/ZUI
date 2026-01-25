using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using BepInEx;
using ZUI.Utils;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;

namespace ZUI.Services
{
    public class AudioLoader : MonoBehaviour
    {
        private static AudioLoader _instance;
        private static bool _isRegistered = false;

        // Track active downloads
        private readonly List<AudioTask> _activeTasks = new List<AudioTask>();
        // Track names of sounds currently downloading so we can queue play requests
        private static readonly HashSet<string> _downloadingNames = new HashSet<string>();
        // Queue: SoundName -> Volume. If a sound finishes downloading and is in here, it plays.
        private static readonly Dictionary<string, float> _playbackQueue = new Dictionary<string, float>();

        private static string _cachePath;
        private static Dictionary<string, string> _soundFileRegistry = new Dictionary<string, string>();

        [DllImport("winmm.dll")]
        private static extern long mciSendString(string strCommand, StringBuilder strReturn, int iReturnLength, IntPtr hwndCallback);

        public AudioLoader(IntPtr ptr) : base(ptr) { }

        private struct AudioTask
        {
            public string Name;
            public string Url;
            public UnityWebRequest Request;
        }

        public static void Initialize()
        {
            if (_cachePath != null) return;
            _cachePath = Path.Combine(Paths.ConfigPath, "ZUI_Audio_Cache");
            if (!Directory.Exists(_cachePath)) Directory.CreateDirectory(_cachePath);
        }

        public static void Download(string name, string url)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(name)) return;

            if (!_isRegistered)
            {
                ClassInjector.RegisterTypeInIl2Cpp<AudioLoader>();
                _isRegistered = true;
                Initialize();
            }

            if (_instance == null)
            {
                var obj = new GameObject("ZUI_AudioLoader");
                GameObject.DontDestroyOnLoad(obj);
                _instance = obj.AddComponent(Il2CppType.Of<AudioLoader>()).Cast<AudioLoader>();
            }

            // 1. Local File Check
            if (url.StartsWith("file://"))
            {
                string localPath = new Uri(url).LocalPath;
                if (File.Exists(localPath))
                {
                    _soundFileRegistry[name] = localPath;
                    CheckQueue(name); // Play if it was waiting
                    return;
                }
            }

            // 2. Disk Cache Check
            // We try to preserve extension from URL if possible for better MCI compatibility
            string extension = Path.GetExtension(url);
            if (string.IsNullOrEmpty(extension) || extension.Length > 5) extension = ".wav";

            string filename = GetSafeFilename(name, extension);
            string diskPath = Path.Combine(_cachePath, filename);

            if (File.Exists(diskPath))
            {
                _soundFileRegistry[name] = diskPath;
                LogUtils.LogInfo($"[AudioLoader] Found cached: {name}");
                CheckQueue(name);
                return;
            }

            // 3. Download
            if (_downloadingNames.Contains(name)) return; // Already downloading

            var uwr = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SendWebRequest();

            _downloadingNames.Add(name);
            _instance._activeTasks.Add(new AudioTask { Name = name, Url = url, Request = uwr });
            LogUtils.LogInfo($"[AudioLoader] Downloading: {name} from {url}");
        }

        public static void Play(string name, float volume = 1.0f)
        {
            // Case A: File is ready
            if (_soundFileRegistry.TryGetValue(name, out string path) && File.Exists(path))
            {
                PlayNative(path, volume);
                return;
            }

            // Case B: File is downloading
            if (_downloadingNames.Contains(name))
            {
                LogUtils.LogInfo($"[AudioLoader] '{name}' is downloading. Queued for playback.");
                _playbackQueue[name] = volume;
                return;
            }

            // Case C: Maybe it's in the cache folder but not registered yet (Manual check)
            // Try matching with common extensions
            string[] extensions = { ".wav", ".mp3", ".ogg" };
            foreach (var ext in extensions)
            {
                string potentialPath = Path.Combine(_cachePath, GetSafeFilename(name, ext));
                if (File.Exists(potentialPath))
                {
                    _soundFileRegistry[name] = potentialPath;
                    PlayNative(potentialPath, volume);
                    return;
                }
            }

            LogUtils.LogWarning($"[AudioLoader] Cannot play '{name}'. Not found locally or downloading.");
        }

        private static void CheckQueue(string name)
        {
            if (_playbackQueue.TryGetValue(name, out float volume))
            {
                _playbackQueue.Remove(name);
                Play(name, volume);
            }
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
        private void HandleFinishedDownload(AudioTask task)
        {
            var uwr = task.Request;
            _downloadingNames.Remove(task.Name);

            try
            {
                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    LogUtils.LogError($"[AudioLoader] Download failed {task.Name}: {uwr.error}");
                    return;
                }

                byte[] data = uwr.downloadHandler.data;
                if (data == null || data.Length == 0) return;

                // Determine extension from URL to help Windows identify format
                string extension = Path.GetExtension(task.Url);
                if (string.IsNullOrEmpty(extension) || extension.Length > 5) extension = ".wav";

                string filename = GetSafeFilename(task.Name, extension);
                string diskPath = Path.Combine(_cachePath, filename);

                File.WriteAllBytes(diskPath, data);

                _soundFileRegistry[task.Name] = diskPath;
                LogUtils.LogInfo($"[AudioLoader] Download Complete: {task.Name}");

                // Play if queued
                CheckQueue(task.Name);
            }
            catch (Exception ex)
            {
                LogUtils.LogError($"[AudioLoader] Write Error {task.Name}: {ex.Message}");
            }
            finally
            {
                uwr.Dispose();
            }
        }

        private static void PlayNative(string filePath, float volume)
        {
            try
            {
                string alias = "zui_" + Guid.NewGuid().ToString("N");

                // Quote the path to handle spaces
                string openCmd = $"open \"{filePath}\" type mpegvideo alias {alias}";
                mciSendString(openCmd, null, 0, IntPtr.Zero);

                int volInt = Mathf.Clamp((int)(volume * 1000), 0, 1000);
                mciSendString($"set {alias} audio volume {volInt}", null, 0, IntPtr.Zero);

                mciSendString($"play {alias} notify", null, 0, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                LogUtils.LogError($"[AudioLoader] Play Failed: {ex.Message}");
            }
        }

        private static string GetSafeFilename(string name, string extension)
        {
            string s = name;
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                s = s.Replace(c, '_');
            }
            // Ensure extension matches what we expect
            if (!s.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                s += extension;

            return s;
        }
    }
}