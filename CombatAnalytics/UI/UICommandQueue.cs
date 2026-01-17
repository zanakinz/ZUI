using System;
using System.Collections.Generic;
using UnityEngine;

namespace CombatAnalytics.UI
{
    /// <summary>
    /// Queues UI operations to be executed on the Unity main thread.
    /// VCF commands can't create UI directly, so we queue them here.
    /// </summary>
    public class UICommandQueue : MonoBehaviour
    {
        private static UICommandQueue _instance;
        private static readonly Queue<Action> _commandQueue = new Queue<Action>();
        private static readonly object _lock = new object();

        public UICommandQueue(IntPtr ptr) : base(ptr) { }

        public static UICommandQueue Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("CombatAnalytics_UICommandQueue");
                    DontDestroyOnLoad(go);
                    go.SetActive(true); // Ensure active
                    _instance = go.AddComponent<UICommandQueue>();
                    _instance.enabled = true; // Ensure component is enabled
                    UnityEngine.Debug.Log("[CombatAnalytics] UICommandQueue created and enabled");
                }
                return _instance;
            }
        }

        private void Awake()
        {
            UnityEngine.Debug.Log("[CombatAnalytics] UICommandQueue.Awake() called");
        }

        private void Start()
        {
            UnityEngine.Debug.Log("[CombatAnalytics] UICommandQueue.Start() called");
        }

        public static void Enqueue(Action command)
        {
            try
            {
                UnityEngine.Debug.Log($"[CombatAnalytics] Enqueue called. Instance exists: {_instance != null}");
                
                // Ensure instance exists (this will create it if needed)
                var instance = Instance;
                UnityEngine.Debug.Log($"[CombatAnalytics] Instance obtained: {instance != null}");
                
                lock (_lock)
                {
                    _commandQueue.Enqueue(command);
                    UnityEngine.Debug.Log($"[CombatAnalytics] Command queued. Queue size: {_commandQueue.Count}");
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[CombatAnalytics] Enqueue failed: {ex}");
                UnityEngine.Debug.LogError($"[CombatAnalytics] Stack: {ex.StackTrace}");
            }
        }

        private void Update()
        {
            // Update DPS tracker
            try
            {
                Services.DpsTracker.Update();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[CombatAnalytics] DpsTracker.Update failed: {ex}");
            }

            // Check for F9 to toggle DPS panel
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F9))
            {
                UnityEngine.Debug.Log("[CombatAnalytics] ===== F9 PRESSED =====");
                try
                {
                    StandaloneUIManager.Instance.ToggleDpsPanel();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[CombatAnalytics] F9 toggle failed: {ex}");
                    UnityEngine.Debug.LogError($"[CombatAnalytics] Stack: {ex.StackTrace}");
                }
            }

            // F10: Test single damage hit (CLIENT-SIDE ONLY)
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F10))
            {
                UnityEngine.Debug.Log("[CombatAnalytics] ===== F10 PRESSED - Test Damage =====");
                try
                {
                    Services.DpsTracker.RecordDamage(1000);
                    UnityEngine.Debug.Log("[CombatAnalytics] F10: Recorded 1000 test damage");
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[CombatAnalytics] F10 test damage failed: {ex}");
                }
            }

            // F11: Test damage spam (CLIENT-SIDE ONLY)
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F11))
            {
                UnityEngine.Debug.Log("[CombatAnalytics] ===== F11 PRESSED - Spam Damage =====");
                try
                {
                    for (int i = 0; i < 50; i++)
                    {
                        Services.DpsTracker.RecordDamage(100);
                    }
                    UnityEngine.Debug.Log("[CombatAnalytics] F11: Recorded 50x100 test damage (5000 total)");
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[CombatAnalytics] F11 spam damage failed: {ex}");
                }
            }

            // Process queued commands
            lock (_lock)
            {
                if (_commandQueue.Count > 0)
                {
                    UnityEngine.Debug.Log($"[CombatAnalytics] Processing {_commandQueue.Count} queued commands");
                }
                
                while (_commandQueue.Count > 0)
                {
                    try
                    {
                        var command = _commandQueue.Dequeue();
                        UnityEngine.Debug.Log($"[CombatAnalytics] Executing queued command...");
                        command.Invoke();
                        UnityEngine.Debug.Log($"[CombatAnalytics] Command executed successfully");
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"[CombatAnalytics] Queued command failed: {ex}");
                    }
                }
            }
        }
    }
}
