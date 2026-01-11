using ZUI.Services;
using ZUI.UI;
using HarmonyLib;
using ProjectM.UI;

namespace ZUI.Patches;

public class EscapeMenuPatch
{
    [HarmonyPatch(typeof(EscapeMenuView), "OnDestroy")]
    [HarmonyPrefix]
    private static void EscapeMenuViewOnDestroyPrefix()
    {
        if (!Plugin.UIManager.IsInitialized) return;

        // User has left the server. Reset all ui as the next server might be a different one
        Plugin.UIManager.Reset();
        MessageService.Destroy();
        Plugin.Reset();
    }
}
