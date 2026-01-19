using ZUI.Services;
using ZUI.Utils;
using HarmonyLib;
using ProjectM.Network;
using ProjectM.UI;
using Unity.Collections;
using Unity.Entities;

namespace ZUI.Patches;

[HarmonyPatch]
internal static class ClientChatPatch
{
    [HarmonyPatch(typeof(ClientChatSystem), nameof(ClientChatSystem.ReceiveChatMessages))]
    [HarmonyPostfix]
    private static void _OnReceiveChatMessagesPostfix(NetworkId localUserNetworkId,
        ChatColorsAsset colors,
        bool showTimeStamp,
        string timeStamp,
        NetworkIdLookupMap networkIdMap)
    {
        LogUtils.LogInfo($"_OnInputEndEditPostfix");
        var e = networkIdMap._NetworkIdToEntityMap[localUserNetworkId];
    }

    [HarmonyPatch(typeof(ClientChatSystem), nameof(ClientChatSystem._OnInputEndEdit))]
    [HarmonyPostfix]
    private static void _OnInputEndEditPostfix(ClientChatSystem __instance)
    {
        LogUtils.LogInfo($"_OnInputEndEditPostfix");
    }

    [HarmonyPatch(typeof(ClientChatSystem), nameof(ClientChatSystem._OnInputSelect))]
    [HarmonyPostfix]
    private static void _OnInputSelectPostfix(string arg0)
    {
        LogUtils.LogInfo($"_OnInputSelectPostfix: {arg0}");
    }

    [HarmonyPatch(typeof(ClientChatSystem), nameof(ClientChatSystem.OnUpdate))]
    [HarmonyPrefix]
    private static void OnUpdatePrefix(ClientChatSystem __instance)
    {
        if (Plugin.IsClientNull())
            return;

        var entities = __instance._ReceiveChatMessagesQuery.ToEntityArray(Allocator.Temp);

        try
        {
            foreach (var entity in entities)
            {
                if (!entity.Has<ChatMessageServerEvent>()) continue;

                // --- ZUI PACKET INTERCEPTION START ---
                // Read the text to see if it contains a hidden ZUI command
                var chatEvent = entity.Read<ChatMessageServerEvent>();
                string messageText = chatEvent.MessageText.ToString();

                if (PacketService.TryProcessPacket(messageText))
                {
                    // If true, it was a hidden packet and successfully handled.
                    // Destroy the entity to prevent it from showing in the chat window.
                    Plugin.EntityManager.DestroyEntity(entity);
                    continue;
                }
                // --- ZUI PACKET INTERCEPTION END ---

                // Standard processing for normal messages (.fam commands, etc)
                MessageService.HandleMessage(entity);
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}