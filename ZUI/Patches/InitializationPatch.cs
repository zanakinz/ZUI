using System;
using ZUI.Services;
using ZUI.UI;
using ZUI.Utils;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.UI;
using Unity.Collections;
using Unity.Entities;

namespace ZUI.Patches
{
    [HarmonyPatch]
    public static class InitializationPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterHUDEntry), nameof(CharacterHUDEntry.Awake))]
        private static void AwakePostfix()
        {
            try
            {
                if (Plugin.UIManager.IsInitialized) return;
                LogUtils.LogInfo("Creating UI...");
                Plugin.UIOnInitialize();
            }
            catch (Exception ex)
            {
                LogUtils.LogError(ex.ToString());
            }
        }
        
        [HarmonyPatch(typeof(CommonClientDataSystem), nameof(CommonClientDataSystem.OnUpdate))]
        [HarmonyPostfix]
        static void OnUpdatePostfix(CommonClientDataSystem __instance)
        {
            if (!Plugin.UIManager.IsInitialized) return;
            Plugin.GameDataOnInitialize(__instance.World);

            var entities = __instance.__query_1840110770_0.ToEntityArray(Allocator.Temp);

            try
            {
                foreach (Entity entity in entities)
                {
                    if (entity.Has<LocalUser>()) MessageService.SetUser(entity);
                    break;
                }
            }
            finally
            {
                entities.Dispose();
            }

            entities = __instance.__query_1840110770_1.ToEntityArray(Allocator.Temp);

            try
            {
                foreach (Entity entity in entities)
                {
                    if (entity.Has<LocalCharacter>())
                    {
                        MessageService.SetCharacter(entity);
                        Plugin.LocalCharacter = entity;
                    }

                    break;
                }
            }
            finally
            {
                entities.Dispose();
            }
        }
    }
}