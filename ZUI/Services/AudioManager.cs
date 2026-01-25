using System;
using UnityEngine;
using ZUI.Utils;

namespace ZUI.Services
{
    public static class AudioManager
    {
        // Unity AudioSources are no longer used because we are using Native Windows Audio.

        public static void Initialize()
        {
            AudioLoader.Initialize();
        }

        public static void RegisterClip(string name, AudioClip clip)
        {
            // Deprecated path. 
            // If code tries to register a clip, we assume AudioLoader already has the file path cached.
            // We do nothing here.
        }

        public static void Play(string name, float volume = 1.0f)
        {
            // Redirect to Native Player
            AudioLoader.Play(name, volume);
        }

        public static void LoadLocalClip(System.Reflection.Assembly assembly, string filename)
        {
            // Redirect to AudioLoader Download logic (which handles local files)
            // We construct a file:// url based on standard BepInEx paths

            string path = "";

            // 1. Try Plugin Dir
            if (assembly != null && !string.IsNullOrEmpty(assembly.Location))
            {
                string dir = System.IO.Path.GetDirectoryName(assembly.Location);
                string attempt = System.IO.Path.Combine(dir, "Audio", filename);
                if (System.IO.File.Exists(attempt)) path = attempt;
            }

            // 2. Try Global Audio Dir
            if (string.IsNullOrEmpty(path))
            {
                string global = System.IO.Path.Combine(BepInEx.Paths.PluginPath, "Audio", filename);
                if (System.IO.File.Exists(global)) path = global;
            }

            if (!string.IsNullOrEmpty(path))
            {
                string url = "file://" + path;
                AudioLoader.Download(filename, url);
            }
            else
            {
                LogUtils.LogWarning($"[AudioManager] Could not find local file: {filename}");
            }
        }
    }
}