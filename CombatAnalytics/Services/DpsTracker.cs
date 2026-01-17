using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CombatAnalytics.UI;
using ProjectM;

namespace CombatAnalytics.Services
{
    /// <summary>
    /// Tracks damage dealt over time and calculates DPS statistics
    /// CLIENT-SIDE ONLY - Must be called from client thread!
    /// </summary>
    public static class DpsTracker
    {
        private static readonly List<DamageRecord> _damageHistory = new List<DamageRecord>();
        private static readonly object _lock = new object();
        
        private const float HISTORY_DURATION = 300f; // Keep 5 minutes of history
        private const float UPDATE_INTERVAL = 0.5f; // Update UI twice per second
        
        private static float _lastUpdateTime = 0f;
        private static float _totalDamage = 0f;
        private static float _combatStartTime = 0f;
        private static bool _inCombat = false;
        private static float _lastDamageTime = 0f;
        private const float COMBAT_TIMEOUT = 10f; // Exit combat after 10s of no damage (was 5s)
        
        private struct DamageRecord
        {
            public float Timestamp;
            public float Amount;
        }

        /// <summary>
        /// Record a damage event - MUST be called from client thread via UICommandQueue!
        /// </summary>
        public static void RecordDamage(float amount)
        {
            if (amount <= 0) return;

            lock (_lock)
            {
                float currentTime = Time.time;
                
                UnityEngine.Debug.Log($"[CombatAnalytics] RecordDamage called: {amount:N0} at time {currentTime:F2}");
                
                // Start combat if not in combat
                if (!_inCombat)
                {
                    _inCombat = true;
                    _combatStartTime = currentTime;
                    _totalDamage = 0f;
                    Plugin.Instance.Log.LogInfo("Combat started!");
                    UnityEngine.Debug.Log($"[CombatAnalytics] Combat started!");
                }
                
                _lastDamageTime = currentTime;
                
                // Add damage record
                _damageHistory.Add(new DamageRecord
                {
                    Timestamp = currentTime,
                    Amount = amount
                });
                
                _totalDamage += amount;
                
                UnityEngine.Debug.Log($"[CombatAnalytics] Recorded damage: {amount:N0}, Total: {_totalDamage:N0}, History count: {_damageHistory.Count}");
                
                // Clean up old records
                CleanupOldRecords(currentTime);
                
                // Don't call UpdateUI() here - let the Update loop handle it
                // This prevents spam and ensures proper timing
            }
        }

        /// <summary>
        /// Update the DPS display (call this from Unity Update loop)
        /// </summary>
        public static void Update()
        {
            float currentTime = Time.time;
            
            // Check for combat timeout
            if (_inCombat && (currentTime - _lastDamageTime) > COMBAT_TIMEOUT)
            {
                _inCombat = false;
                Plugin.Instance.Log.LogInfo("Combat ended!");
            }
            
            // Update UI at specified interval
            if (currentTime - _lastUpdateTime >= UPDATE_INTERVAL)
            {
                _lastUpdateTime = currentTime;
                UpdateUI();
            }
        }

        private static void UpdateUI()
        {
            lock (_lock)
            {
                float currentTime = Time.time;
                CleanupOldRecords(currentTime);
                
                // Only log when in combat or when there's recent damage
                // No spam when idle!
                
                // Calculate statistics
                float combatDuration = _inCombat ? (currentTime - _combatStartTime) : 0f;
                float dps = combatDuration > 0 ? _totalDamage / combatDuration : 0f;
                
                // Calculate DPS for various time windows
                float dps5s = CalculateDps(5f, currentTime);
                float dps10s = CalculateDps(10f, currentTime);
                float dps30s = CalculateDps(30f, currentTime);
                
                // Get highest damage in a single hit
                float maxHit = _damageHistory.Count > 0 ? _damageHistory.Max(r => r.Amount) : 0f;
                
                // Format the display text
                string statusText = _inCombat ? "IN COMBAT" : "Out of Combat";
                string combatTime = FormatTime(combatDuration);
                
                string displayText = $"{statusText}\n\n" +
                                   $"Total Damage: {_totalDamage:N0}\n" +
                                   $"Combat Time: {combatTime}\n" +
                                   $"Average DPS: {dps:N1}\n\n" +
                                   $"DPS (5s):  {dps5s:N1}\n" +
                                   $"DPS (10s): {dps10s:N1}\n" +
                                   $"DPS (30s): {dps30s:N1}\n\n" +
                                   $"Max Hit: {maxHit:N0}\n" +
                                   $"Hits: {_damageHistory.Count}\n\n" +
                                   $"Press F9 to toggle";
                
                // Try DIRECT update first (not queued) to test
                try
                {
                    StandaloneUIManager.Instance.UpdateDpsText(displayText);
                    // Only log during active combat, not when idle
                    // This prevents console spam
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[CombatAnalytics] Direct UI update failed: {ex}");
                }
            }
        }

        private static float CalculateDps(float timeWindow, float currentTime)
        {
            float startTime = currentTime - timeWindow;
            float totalDamage = 0f;
            int count = 0;
            
            foreach (var record in _damageHistory)
            {
                if (record.Timestamp >= startTime)
                {
                    totalDamage += record.Amount;
                    count++;
                }
            }
            
            return count > 0 ? totalDamage / timeWindow : 0f;
        }

        private static void CleanupOldRecords(float currentTime)
        {
            float cutoffTime = currentTime - HISTORY_DURATION;
            _damageHistory.RemoveAll(r => r.Timestamp < cutoffTime);
        }

        private static string FormatTime(float seconds)
        {
            if (seconds < 60f)
                return $"{seconds:F1}s";
            
            int minutes = (int)(seconds / 60f);
            int secs = (int)(seconds % 60f);
            return $"{minutes}m {secs}s";
        }

        /// <summary>
        /// Reset all statistics
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _damageHistory.Clear();
                _totalDamage = 0f;
                _inCombat = false;
                _combatStartTime = 0f;
                _lastDamageTime = 0f;
                Plugin.Instance.Log.LogInfo("DPS Tracker reset!");
                
                // Update UI immediately
                UI.UICommandQueue.Enqueue(() =>
                {
                    StandaloneUIManager.Instance.UpdateDpsText("Out of Combat\n\nTotal Damage: 0\nCombat Time: 0.0s\nAverage DPS: 0.0\n\nDPS (5s):  0.0\nDPS (10s): 0.0\nDPS (30s): 0.0\n\nMax Hit: 0\nHits: 0\n\nPress F9 to toggle");
                });
            }
        }

        /// <summary>
        /// Get current statistics (for API or commands)
        /// </summary>
        public static (float totalDamage, float dps, float combatTime, bool inCombat) GetStats()
        {
            lock (_lock)
            {
                float currentTime = Time.time;
                float combatDuration = _inCombat ? (currentTime - _combatStartTime) : 0f;
                float dps = combatDuration > 0 ? _totalDamage / combatDuration : 0f;
                
                return (_totalDamage, dps, combatDuration, _inCombat);
            }
        }
    }
}
