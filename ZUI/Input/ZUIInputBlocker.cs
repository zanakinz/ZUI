using BepInEx.Unity.IL2CPP.Utils;
using ProjectM;
using System;
using System.Collections;
using Unity.Entities;
using UnityEngine;

namespace ZUI.InputBlocking
{
    /// <summary>
    /// Input blocker that briefly blocks game inputs during UI clicks.
    /// Provides a momentary "shield" to prevent clicks from passing through to the game.
    /// </summary>
    public static class ZUIInputBlocker
    {
        private static bool _shouldBlock = false;
        private static bool _isInitialized = false;
        private static Coroutine _unblockCoroutine;

        /// <summary>
        /// Gets whether game inputs should currently be blocked.
        /// </summary>
        public static bool ShouldBlock => _shouldBlock;

        /// <summary>
        /// Initialize the blocker - ensures we start in unblocked state.
        /// Call this once during plugin initialization.
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;

            _shouldBlock = false;
            _isInitialized = true;
            UnityEngine.Debug.Log("[ZUI] InputBlocker initialized - starting UNBLOCKED");
        }

        /// <summary>
        /// Momentarily blocks input for a brief period (default 0.1 seconds).
        /// Call this when the user clicks on ZUI UI elements.
        /// </summary>
        public static void BlockMomentarily(float duration = 0.1f)
        {
            if (!_isInitialized)
            {
                UnityEngine.Debug.LogWarning("[ZUI] BlockMomentarily called before Initialize!");
                Initialize();
            }

            _shouldBlock = true;
            UnityEngine.Debug.Log($"[ZUI] Momentary input block START ({duration}s)");

            // Cancel any existing unblock coroutine
            if (_unblockCoroutine != null)
            {
                Plugin.CoreUpdateBehavior.StopCoroutine(_unblockCoroutine);
            }

            // Start new unblock timer
            _unblockCoroutine = Plugin.CoreUpdateBehavior.StartCoroutine(UnblockAfterDelay(duration));
        }

        /// <summary>
        /// Immediately restores game inputs.
        /// </summary>
        public static void UnblockImmediately()
        {
            if (_unblockCoroutine != null)
            {
                Plugin.CoreUpdateBehavior.StopCoroutine(_unblockCoroutine);
                _unblockCoroutine = null;
            }

            if (_shouldBlock)
            {
                _shouldBlock = false;
                UnityEngine.Debug.Log("[ZUI] Input block CLEARED immediately");
            }
        }

        private static IEnumerator UnblockAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            _shouldBlock = false;
            _unblockCoroutine = null;
            UnityEngine.Debug.Log("[ZUI] Momentary input block END");
        }

        /// <summary>
        /// Legacy method - kept for compatibility but not recommended.
        /// Use BlockMomentarily() instead for click handling.
        /// </summary>
        public static void SetBlocking(bool block)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            if (_shouldBlock != block)
            {
                _shouldBlock = block;
                UnityEngine.Debug.Log($"[ZUI] Game input blocking: {(block ? "ENABLED" : "disabled")}");
            }
        }
    }
}