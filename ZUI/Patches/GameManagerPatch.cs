using System;
using ZUI.Utils;
using HarmonyLib;
using ProjectM;
using ProjectM.UI;

namespace ZUI.Patches;

public class GameManagerPatch
{
    /*[HarmonyPatch(typeof(GameDataManager), "OnUpdate")]
    [HarmonyPostfix]
    private static void GameDataManagerOnUpdatePostfix(GameDataManager __instance)
    {
        try
        {
            if (!__instance.GameDataInitialized) return;
            Plugin.GameDataOnInitialize(__instance.World);
        }
        catch (Exception ex)
        {
            LogUtils.LogError(ex.ToString());
        }
    }*/
}
