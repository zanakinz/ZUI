using System;
using HarmonyLib;
using ProjectM;
using Unity.Entities;
using Unity.Collections;
using CombatAnalytics.Services;

namespace CombatAnalytics.Patches
{
    /// <summary>
    /// Tracks when player deals damage during combat encounters
    /// Uses InCombat buff to determine when to track damage
    /// CLIENT-SIDE: Only tracks damage during player's combat
    /// </summary>
    [HarmonyPatch(typeof(ClientBootstrapSystem), nameof(ClientBootstrapSystem.OnUpdate))]
    public static class DamageTrackingPatch
    {
        private static Entity _lastTargetEntity = Entity.Null;
        private static float _lastTargetHealth = 0f;
        private static float _lastCheckTime = 0f;
        private const float CHECK_INTERVAL = 0.1f; // Check 10 times per second
        private static float _lastDamageRecordTime = 0f;
        private const float MIN_DAMAGE_INTERVAL = 0.15f; // Minimum 150ms between damage records (anti-DoT)
        
        // Track combat state based on player's InCombat buff
        private static bool _playerWasInCombat = false;

        [HarmonyPostfix]
        public static void Postfix(ClientBootstrapSystem __instance)
        {
            try
            {
                // Only check periodically to reduce overhead
                float currentTime = UnityEngine.Time.time;
                if (currentTime - _lastCheckTime < CHECK_INTERVAL)
                    return;
                
                _lastCheckTime = currentTime;

                var entityManager = __instance.EntityManager;

                // Find the local player character
                var playerQuery = entityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<PlayerCharacter>()
                );

                if (playerQuery.IsEmpty)
                {
                    playerQuery.Dispose();
                    return;
                }

                var playerEntities = playerQuery.ToEntityArray(Allocator.Temp);
                Entity playerEntity = Entity.Null;

                // Find the actual local player (the one we control)
                foreach (var entity in playerEntities)
                {
                    if (entityManager.HasComponent<EntityInput>(entity))
                    {
                        playerEntity = entity;
                        break;
                    }
                }

                playerEntities.Dispose();
                playerQuery.Dispose();

                if (playerEntity == Entity.Null)
                    return;

                // Check if player has EntityInput (means they're targeting something)
                if (!entityManager.HasComponent<EntityInput>(playerEntity))
                    return;

                var entityInput = entityManager.GetComponentData<EntityInput>(playerEntity);
                var targetEntity = entityInput.HoveredEntity;
                
                // Check if player has InCombat buff (set when attacking OR being attacked)
                bool playerInCombat = entityManager.HasComponent<InCombatBuff>(playerEntity);
                
                // Track combat state changes
                if (playerInCombat && !_playerWasInCombat)
                {
                    UnityEngine.Debug.Log("[CombatAnalytics] Player entered combat");
                }
                else if (!playerInCombat && _playerWasInCombat)
                {
                    UnityEngine.Debug.Log("[CombatAnalytics] Player left combat");
                    _lastTargetEntity = Entity.Null;
                    _lastTargetHealth = 0f;
                }
                _playerWasInCombat = playerInCombat;

                // IMPORTANT: Only track during player's combat encounter
                // Track ALL damage to enemies while player has InCombat buff
                // This captures combo attacks, spells + attacks, multi-target, etc.
                // Filters:
                // 1. Player must be in combat (has InCombatBuff)
                // 2. Target exists
                // 3. Target is NOT the player entity
                // 4. Target does NOT have PlayerCharacter component
                // 5. Target has Health
                // 6. Target is an actual enemy (UnitLevel or Aggroable)
                if (playerInCombat &&
                    targetEntity != Entity.Null && 
                    targetEntity != playerEntity &&
                    entityManager.Exists(targetEntity) &&
                    !entityManager.HasComponent<PlayerCharacter>(targetEntity) &&
                    entityManager.HasComponent<Health>(targetEntity))
                {
                    // Check if this is actually an enemy/mob
                    bool isEnemy = entityManager.HasComponent<UnitLevel>(targetEntity) ||
                                   entityManager.HasComponent<Aggroable>(targetEntity);

                    if (!isEnemy)
                    {
                        // Not an enemy, skip
                        _lastTargetEntity = Entity.Null;
                        _lastTargetHealth = 0f;
                        return;
                    }

                    var targetHealth = entityManager.GetComponentData<Health>(targetEntity);
                    float currentHealth = targetHealth.Value;
                    float maxHealth = targetHealth.MaxHealth._Value;

                    // If this is the same target we were tracking
                    if (targetEntity == _lastTargetEntity)
                    {
                        // Check if health decreased
                        if (currentHealth < _lastTargetHealth)
                        {
                            float damage = _lastTargetHealth - currentHealth;
                            float timeSinceLastRecord = currentTime - _lastDamageRecordTime;
                            
                            // First damage ever recorded
                            if (_lastDamageRecordTime == 0f)
                            {
                                timeSinceLastRecord = float.MaxValue;
                            }
                            
                            // Filter out DoT/bleed ticks via rate limiting
                            bool shouldRecord = damage > 0.5f && 
                                               damage <= maxHealth && 
                                               damage < 5000f &&
                                               timeSinceLastRecord >= MIN_DAMAGE_INTERVAL;
                            
                            if (shouldRecord)
                            {
                                UnityEngine.Debug.Log($"[CombatAnalytics] ? Recorded {damage:N1} damage");
                                DpsTracker.RecordDamage(damage);
                                _lastDamageRecordTime = currentTime;
                            }
                            else if (damage > 0.5f)
                            {
                                string reason = "";
                                if (timeSinceLastRecord < MIN_DAMAGE_INTERVAL) 
                                    reason = $"rate limited ({timeSinceLastRecord:F3}s)";
                                else if (damage > maxHealth) 
                                    reason = "exceeds max HP";
                                else if (damage >= 5000f) 
                                    reason = "too high";
                                    
                                UnityEngine.Debug.Log($"[CombatAnalytics] ? Ignored {damage:N1} - {reason}");
                            }
                        }
                    }
                    else
                    {
                        // New target
                        if (_lastTargetEntity == Entity.Null && currentHealth < maxHealth)
                        {
                            UnityEngine.Debug.Log($"[CombatAnalytics] Tracking enemy (HP: {currentHealth:N0}/{maxHealth:N0})");
                        }
                    }

                    // Update tracking
                    _lastTargetEntity = targetEntity;
                    _lastTargetHealth = currentHealth;
                }
                else
                {
                    // Target lost/died
                    if (_lastTargetEntity != Entity.Null && _lastTargetHealth > 0f)
                    {
                        // Record killing blow
                        if (_lastTargetHealth > 0.5f && _lastTargetHealth < 500f)
                        {
                            if (_lastTargetHealth > 5f)
                            {
                                UnityEngine.Debug.Log($"[CombatAnalytics] Enemy died - final {_lastTargetHealth:N1}");
                            }
                            DpsTracker.RecordDamage(_lastTargetHealth);
                        }
                    }
                    
                    // Reset tracking
                    _lastTargetEntity = Entity.Null;
                    _lastTargetHealth = 0f;
                }
            }
            catch (Exception ex)
            {
                // Fail silently to not spam logs
                if (UnityEngine.Time.frameCount % 600 == 0)
                {
                    Plugin.Instance.Log.LogWarning($"DamageTrackingPatch error: {ex.Message}");
                }
            }
        }
    }
}
