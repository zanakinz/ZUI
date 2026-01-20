/*using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using ProjectM.Network;
using ZUI.Utils;
using ZUI.API;

namespace ZUI.Services
{
    /// <summary>
    /// Server-Side only service.
    /// Monitors the game world for new Player Connections and fires events for other mods to use.
    /// </summary>
    internal static class ServerUserTracker
    {
        private static EntityQuery _userQuery;
        private static readonly HashSet<int> _connectedUserIndexes = new();
        private static World _serverWorld;

        public static void Initialize(World world)
        {
            _serverWorld = world;

            // Query for all entities with a User component
            _userQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<User>());

            // Hook into ZUI's update loop
            // Assuming CoreUpdateBehavior runs on server (it should if Plugin.Load sets it up)
            ZUI.Behaviors.CoreUpdateBehavior.Actions.Add(OnUpdate);
        }

        private static void OnUpdate()
        {
            if (_serverWorld == null) return;

            var userEntities = _userQuery.ToEntityArray(Allocator.Temp);

            foreach (var entity in userEntities)
            {
                var user = entity.Read<User>();
                int userIndex = user.Index;

                // Check if user is fully connected
                // 1. IsConnected must be true
                // 2. CharacterName must not be empty (means they finished character creation/loading)
                bool isReady = user.IsConnected && !user.CharacterName.IsEmpty;

                if (isReady)
                {
                    // New Connection Detected
                    if (!_connectedUserIndexes.Contains(userIndex))
                    {
                        _connectedUserIndexes.Add(userIndex);

                        LogUtils.LogInfo($"[ServerUserTracker] Player Connected: {user.CharacterName} (Index: {userIndex})");

                        // Fire API Event so external mods (like ExampleMod) can send their UI packets
                        ModRegistry.InvokeOnPlayerLogin(entity);
                    }
                }
                else
                {
                    // Handle Disconnect
                    if (_connectedUserIndexes.Contains(userIndex))
                    {
                        _connectedUserIndexes.Remove(userIndex);
                        // Optional: ModRegistry.InvokeOnPlayerLogout(entity);
                    }
                }
            }

            userEntities.Dispose();
        }
    }
}*/