using System;
using System.Collections.Generic;
using System.Linq;
using ZUI.Config;
using ZUI.Utils;
using Il2CppInterop.Runtime;
using ProjectM.Network;
using Unity.Entities;
using DateTime = System.DateTime;

namespace ZUI.Services
{
    internal static partial class MessageService
    {
        private static bool _famEquipSequenceActive;
        static EntityManager EntityManager => Plugin.EntityManager;

        private static readonly ComponentType[] NetworkEventComponents =
        [
            ComponentType.ReadOnly(Il2CppType.Of<FromCharacter>()),
            ComponentType.ReadOnly(Il2CppType.Of<NetworkEventType>()),
            ComponentType.ReadOnly(Il2CppType.Of<SendNetworkEventTag>()),
            ComponentType.ReadOnly(Il2CppType.Of<ChatMessageEvent>())
        ];

        private static readonly Queue<string> OutputMessages  = new();
        private static Entity _localCharacter = Entity.Null;
        private static Entity _localUser = Entity.Null;
        private static bool _isInitialized;

        private static readonly NetworkEventType NetworkEventType = new()
        {
            IsAdminEvent = false,
            EventId = NetworkEvents.EventId_ChatMessageEvent,
            IsDebugEvent = false,
        };

        private static int Timeout;

        public static void EnqueueMessage(string text)
        {
            OutputMessages.Enqueue(text);
        }

        private static string DequeueMessage()
        {
            return OutputMessages.Dequeue();
        }

        private static DateTime _lastAction = DateTime.MinValue;

        private static void SendMessage(string text)
        {
            ChatMessageEvent chatMessageEvent = new()
            {
                MessageText = text,
                MessageType = ChatMessageType.Local,
                ReceiverEntity = _localUser.Read<NetworkId>()
            };

            var networkEntity = EntityManager.CreateEntity(NetworkEventComponents);
            networkEntity.Write(new FromCharacter { Character = _localCharacter, User = _localUser });
            networkEntity.Write(NetworkEventType);
            networkEntity.Write(chatMessageEvent);
        }

        public static void ProcessAllMessages()
        {
            if(!_isInitialized) return;

            if(Timeout == 0)
                Timeout = Settings.GlobalQueryIntervalInSeconds;

            if ((DateTime.Now - _lastAction).TotalSeconds < Timeout)
                return;
            _lastAction = DateTime.Now;

            if(OutputMessages.Any())
                SendMessage(DequeueMessage());
        }

        public static void Destroy()
        {
            _localCharacter = Entity.Null;
            _localUser = Entity.Null;
            OutputMessages.Clear();
            _isInitialized = false;
        }

        public static void SetCharacter(Entity entity)
        {
            _localCharacter = entity;
            if (_localCharacter != Entity.Null && _localUser != Entity.Null)
                _isInitialized = true;
        }

        public static void SetUser(Entity entity)
        {
            _localUser = entity;
            if (_localCharacter != Entity.Null && _localUser != Entity.Null)
                _isInitialized = true;
        }

        public static void StartAutoEnableFamiliarEquipmentSequence()
        {
            _famEquipSequenceActive = true;
            EnqueueMessage(BCCOM_ENABLEEQUIP);
        }

        public static void FinishAutoEnableFamiliarEquipmentSequence()
        {
            _famEquipSequenceActive = false;
        }

    }
}
