using System;

namespace CombatAnalytics.API
{
    /// <summary>
    /// Public API for external mods (like ZUI) to directly invoke CombatAnalytics functions
    /// without going through chat/VCF commands.
    /// </summary>
    public static class CombatAnalyticsAPI
    {
        public static void ToggleDpsPanel()
        {
            try
            {
                if (Plugin.Instance?.Log != null)
                    Plugin.Instance.Log.LogInfo("[API] ToggleDpsPanel called directly");
                Services.DpsService.TogglePanel();
            }
            catch (Exception ex)
            {
                if (Plugin.Instance?.Log != null)
                    Plugin.Instance.Log.LogError($"[API] ToggleDpsPanel failed: {ex}");
                UnityEngine.Debug.LogError($"[CombatAnalytics] ToggleDpsPanel failed: {ex}");
            }
        }

        public static void ResetDps()
        {
            try
            {
                Plugin.Instance.Log.LogInfo("[API] ResetDps called directly");
                Services.DpsService.Reset();
            }
            catch (Exception ex)
            {
                Plugin.Instance.Log.LogError($"[API] ResetDps failed: {ex}");
            }
        }

        public static string GetDebugInfo()
        {
            try
            {
                return UI.StandaloneUIManager.Instance.GetDebugInfo();
            }
            catch (Exception ex)
            {
                Plugin.Instance.Log.LogError($"[API] GetDebugInfo failed: {ex}");
                return $"Error: {ex.Message}";
            }
        }
    }
}
