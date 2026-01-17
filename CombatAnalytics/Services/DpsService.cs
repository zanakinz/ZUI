using VampireCommandFramework;
using CombatAnalytics.UI;

namespace CombatAnalytics.Services
{
    public static class DpsService
    {
        public static void Initialize()
        {
            try
            {
                Plugin.Instance.Log.LogInfo("DpsService.Initialize() called");
                StandaloneUIManager.Instance.Initialize();
                Plugin.Instance.Log.LogInfo("DpsService.Initialize() completed");
            }
            catch (System.Exception ex)
            {
                Plugin.Instance.Log.LogError($"DpsService.Initialize() failed: {ex}");
            }
        }

        public static void Reset()
        {
            try
            {
                Plugin.Instance.Log.LogInfo("DpsService.Reset() called");
                DpsTracker.Reset();
            }
            catch (System.Exception ex)
            {
                Plugin.Instance.Log.LogError($"DpsService.Reset() failed: {ex}");
            }
        }

        public static void TogglePanel()
        {
            UnityEngine.Debug.Log("[CombatAnalytics] TogglePanel called - QUEUEING for main thread");
            try
            {
                // Queue the UI operation to run on Unity's main thread
                UI.UICommandQueue.Enqueue(() =>
                {
                    UnityEngine.Debug.Log("[CombatAnalytics] Executing TogglePanel on main thread");
                    StandaloneUIManager.Instance.ToggleDpsPanel();
                });
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[CombatAnalytics] TogglePanel failed: {ex}");
            }
        }

        public static void UpdateDps(string text)
        {
            try
            {
                StandaloneUIManager.Instance.UpdateDpsText(text);
            }
            catch (System.Exception ex)
            {
                Plugin.Instance.Log.LogError($"DpsService.UpdateDps() failed: {ex}");
            }
        }
    }

    public class DpsCommands
    {
        [Command("dps", description: "Show DPS meter info and controls", adminOnly: false)]
        public void DpsInfoCommand(ICommandContext ctx)
        {
            ctx.Reply("=== CombatAnalytics DPS Meter ===");
            ctx.Reply("Hotkeys (CLIENT-SIDE):");
            ctx.Reply("  F9  - Toggle DPS panel");
            ctx.Reply("  F10 - Test 1000 damage");
            ctx.Reply("  F11 - Test 50x100 damage");
            ctx.Reply("");
            ctx.Reply("Commands:");
            ctx.Reply("  .dpsreset - Reset statistics");
            ctx.Reply("  .dpsstats - Show current stats");
            ctx.Reply("");
            ctx.Reply("Note: Test commands don't work because");
            ctx.Reply("commands run on SERVER but UI is CLIENT.");
            ctx.Reply("Use F10/F11 hotkeys instead!");
        }

        [Command("dpsreset", description: "Reset DPS statistics", adminOnly: false)]
        public void DpsResetCommand(ICommandContext ctx)
        {
            ctx.Reply("Resetting DPS statistics...");
            DpsService.Reset();
            ctx.Reply("DPS statistics reset!");
        }

        [Command("dpsstats", description: "Show current DPS statistics", adminOnly: false)]
        public void DpsStatsCommand(ICommandContext ctx)
        {
            var stats = DpsTracker.GetStats();
            ctx.Reply($"=== DPS Statistics ===");
            ctx.Reply($"Status: {(stats.inCombat ? "IN COMBAT" : "Out of Combat")}");
            ctx.Reply($"Total Damage: {stats.totalDamage:N0}");
            ctx.Reply($"Combat Time: {stats.combatTime:F1}s");
            ctx.Reply($"Average DPS: {stats.dps:N1}");
        }

        [Command("dpstest", description: "Simulate damage for testing (amount)", adminOnly: false)]
        public void DpsTestCommand(ICommandContext ctx, float amount = 100f)
        {
            ctx.Reply($"Queueing {amount:N0} damage to client...");
            
            // Queue the damage recording to happen on the CLIENT thread
            // Commands run on SERVER, but DpsTracker must run on CLIENT
            UI.UICommandQueue.Enqueue(() =>
            {
                UnityEngine.Debug.Log($"[CombatAnalytics] Client executing damage recording: {amount:N0}");
                DpsTracker.RecordDamage(amount);
            });
            
            ctx.Reply("Damage queued! Check panel in 0.5s");
        }

        [Command("dpsspam", description: "Simulate rapid damage (count, amount per hit)", adminOnly: false)]
        public void DpsSpamCommand(ICommandContext ctx, int count = 10, float amount = 50f)
        {
            var total = count * amount;
            ctx.Reply($"Queueing {count} hits of {amount:N0} damage to client...");
            
            // Queue the damage recording to happen on the CLIENT thread
            UI.UICommandQueue.Enqueue(() =>
            {
                UnityEngine.Debug.Log($"[CombatAnalytics] Client executing {count} damage hits");
                for (int i = 0; i < count; i++)
                {
                    DpsTracker.RecordDamage(amount);
                }
                UnityEngine.Debug.Log($"[CombatAnalytics] Completed recording {count} hits, total: {total:N0}");
            });
            
            ctx.Reply($"Total: {total:N0} damage queued!");
        }

        [Command("dpsdiag", description: "Comprehensive diagnostic test", adminOnly: false)]
        public void DpsDiagCommand(ICommandContext ctx)
        {
            ctx.Reply("=== DPS Meter Diagnostic ===");
            
            // Step 1: Check if panel exists
            var mgr = StandaloneUIManager.Instance;
            ctx.Reply($"1. Manager exists: {mgr != null}");
            
            if (mgr == null)
            {
                ctx.Reply("ERROR: Manager is null!");
                return;
            }
            
            // Step 2: Initialize if needed
            ctx.Reply("2. Ensuring initialized...");
            mgr.Initialize();
            ctx.Reply("   Initialized.");
            
            // Step 3: Toggle panel to make sure it's visible
            ctx.Reply("3. Making panel visible...");
            UI.UICommandQueue.Enqueue(() =>
            {
                mgr.ToggleDpsPanel();
                UnityEngine.Debug.Log("[CombatAnalytics] Panel toggled from diagnostic");
            });
            ctx.Reply("   Panel toggle queued.");
            
            // Step 4: Simulate some damage
            ctx.Reply("4. Simulating damage...");
            UI.UICommandQueue.Enqueue(() =>
            {
                UnityEngine.Debug.Log("[CombatAnalytics] Recording diagnostic damage on CLIENT");
                DpsTracker.RecordDamage(100);
                DpsTracker.RecordDamage(200);
                DpsTracker.RecordDamage(150);
                UnityEngine.Debug.Log("[CombatAnalytics] 3 damage hits recorded");
            });
            ctx.Reply("   3 hits queued (100, 200, 150)");
            
            ctx.Reply("");
            ctx.Reply("=== Diagnostic Complete ===");
            ctx.Reply("Watch the panel - it should update within 1s");
        }
    }
}
