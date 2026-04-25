using Duckov.ItemUsage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Duckov;
using DuckovCustomSounds.CustomEnemySounds.Context;
using DuckovCustomSounds.Logging;
using UnityEngine;

namespace DuckovCustomSounds.CustomEnemySounds.Filters
{
    internal static class EnemyVoiceFilter
    {
        private const float CombatDistanceThreshold = 20f;
        private const float VisionDistanceThreshold = 15f;
        private const float VisionHeightOffset = 1.6f;
        private const float VisionAngleThreshold = 60f;
        private const float HybridSwitchDistance = 50f;
        private const float MovementSpeedThreshold = 2f;
        private const float DecisionCacheLifetime = 0.3f;

        private static readonly Dictionary<int, FilterCacheEntry> DecisionCache = new Dictionary<int, FilterCacheEntry>();
        private static readonly Dictionary<int, bool> MerchantCache = new Dictionary<int, bool>();
        private static readonly object CacheSync = new object();

        private sealed class FilterCacheEntry
        {
            public bool Result;
            public string SoundKey;
            public float ExpireTime;
        }

        internal sealed class VoiceEventContext
        {
            public Teams? SoundSourceTeam { get; set; }
            public Vector3? SoundPosition { get; set; }
            public float? SoundRadius { get; set; }
            public string SoundType { get; set; }
        }

        public static VoiceEventContext CreateContext(EnemyContext ctx)
        {
            if (ctx == null) return null;
            return new VoiceEventContext
            {
                SoundPosition = ctx.GameObject != null ? (Vector3?)ctx.GameObject.transform.position : null,
                SoundSourceTeam = ParseTeam(ctx.GetTeamNormalized())
            };
        }

        public static bool ShouldAllow(CharacterMainControl speaker, string soundKey, VoiceEventContext context)
        {
            if (speaker == null)
                return false;

            var instanceId = speaker.GetInstanceID();
            var now = Time.realtimeSinceStartup;
            if (TryGetCachedDecision(instanceId, soundKey, now, out var cachedDecision))
            {
                return cachedDecision;
            }

            var mode = ModSettings.EnemyVoiceMode;
            if (!ModSettings.EnableNPCtoNPCCombatVoices)
            {
                mode = EnemyVoiceTriggerMode.PlayerOnly;
            }

            bool decision;
            switch (mode)
            {
                case EnemyVoiceTriggerMode.Original:
                    decision = true;
                    break;
                case EnemyVoiceTriggerMode.PlayerOnly:
                    decision = IsPlayerRelatedEvent(speaker, soundKey, context);
                    break;
                case EnemyVoiceTriggerMode.Hybrid:
                    decision = ShouldProcessInHybridMode(speaker, soundKey, context);
                    break;
                default:
                    decision = true;
                    break;
            }

            StoreDecision(instanceId, soundKey, decision, now);
            return decision;
        }

        private static Teams? ParseTeam(string team)
        {
            if (string.IsNullOrEmpty(team)) return null;
            switch (team.ToLowerInvariant())
            {
                case "player":
                    return Teams.player;
                case "scav":
                    return Teams.scav;
                case "pmc":
                case "usec":
                    return Teams.all;
                default:
                    return Teams.all;
            }
        }

        private static bool ShouldProcessInHybridMode(CharacterMainControl enemy, string soundKey, VoiceEventContext context)
        {
            var player = GetPlayerCharacter();
            if (player == null)
                return true;

            float distanceToPlayer;
            try
            {
                distanceToPlayer = Vector3.Distance(enemy.transform.position, player.transform.position);
            }
            catch
            {
                distanceToPlayer = float.MaxValue;
            }

            if (distanceToPlayer <= HybridSwitchDistance)
            {
                return IsPlayerRelatedEvent(enemy, soundKey, context);
            }

            return true;
        }

        private static bool TryGetCachedDecision(int instanceId, string soundKey, float now, out bool decision)
        {
            var key = soundKey ?? string.Empty;
            lock (CacheSync)
            {
                if (DecisionCache.TryGetValue(instanceId, out var entry))
                {
                    if (now <= entry.ExpireTime &&
                        string.Equals(entry.SoundKey, key, StringComparison.OrdinalIgnoreCase))
                    {
                        decision = entry.Result;
                        return true;
                    }

                    if (now - entry.ExpireTime > DecisionCacheLifetime)
                    {
                        DecisionCache.Remove(instanceId);
                    }
                }
            }

            decision = false;
            return false;
        }

        private static void StoreDecision(int instanceId, string soundKey, bool result, float now)
        {
            var entry = new FilterCacheEntry
            {
                Result = result,
                SoundKey = soundKey ?? string.Empty,
                ExpireTime = now + DecisionCacheLifetime
            };

            lock (CacheSync)
            {
                DecisionCache[instanceId] = entry;
            }
        }

        private static bool IsPlayerRelatedEvent(CharacterMainControl enemy, string soundKey, VoiceEventContext context)
        {
            try
            {
                if (enemy.Team == Teams.player)
                    return false;

                if (IsBlackMarketMerchant(enemy))
                    return false;

                var player = GetPlayerCharacter();
                if (player == null)
                    return false;

                if (context?.SoundSourceTeam == Teams.player)
                    return true;

                if (IsInCombatWithPlayer(enemy, player))
                    return true;

                if (CanEnemySeePlayer(enemy, player))
                    return true;

                if (string.Equals(soundKey, "surprise", StringComparison.OrdinalIgnoreCase))
                {
                    if (context?.SoundSourceTeam != Teams.player)
                    {
                        float distance = 0f;
                        try
                        {
                            distance = Vector3.Distance(enemy.transform.position, player.transform.position);
                        }
                        catch
                        {
                        }

                        if (distance > 30f)
                        {
                            CESLogger.Debug($"[EnemyVoiceFilter] Suppress distant surprise voice: {enemy.name}");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CESLogger.Debug($"[EnemyVoiceFilter] Player-related evaluation failed: {ex.Message}");
                return false;
            }

            return false;
        }

        private static bool IsBlackMarketMerchant(CharacterMainControl character)
        {
            if (character == null)
                return false;

            int instanceId = character.GetInstanceID();
            lock (CacheSync)
            {
                if (MerchantCache.TryGetValue(instanceId, out var cached))
                    return cached;
            }

            bool isMerchant = false;
            try
            {
                string name = character.name ?? string.Empty;
                if (name.IndexOf("merchant", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("trader", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("dealer", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    isMerchant = true;
                    return CacheMerchant(instanceId, true);
                }

                if (character.Team == Teams.all && HasSpecialMerchantChild(character.transform))
                    return CacheMerchant(instanceId, true);

                var monoBehaviours = character.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var mb in monoBehaviours)
                {
                    if (mb == null) continue;
                    var typeName = mb.GetType().Name;
                    if (typeName.IndexOf("Merchant", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        typeName.IndexOf("Shop", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return CacheMerchant(instanceId, true);
                    }
                }

                var modelRoot = character.transform.Find("ModelRoot");
                if (modelRoot != null)
                {
                    foreach (Transform child in modelRoot)
                    {
                        var childName = child?.name;
                        if (string.IsNullOrEmpty(childName)) continue;
                        if (childName.IndexOf("merchant", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            childName.IndexOf("trader", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            childName.IndexOf("shop", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            childName.IndexOf("bugboss", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            childName.IndexOf("patro", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return CacheMerchant(instanceId, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CESLogger.Debug($"[EnemyVoiceFilter] Merchant detection error: {ex.Message}");
            }

            return CacheMerchant(instanceId, isMerchant);
        }

        private static bool HasSpecialMerchantChild(Transform parent)
        {
            if (parent == null) return false;
            foreach (Transform child in parent)
            {
                var childName = child?.name;
                if (!string.IsNullOrEmpty(childName) && childName.StartsWith("SpecialAttachment_Merchant_", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool CacheMerchant(int instanceId, bool value)
        {
            lock (CacheSync)
            {
                MerchantCache[instanceId] = value;
            }
            return value;
        }

        private static CharacterMainControl GetPlayerCharacter()
        {
            try
            {
                return LevelManager.Instance?.MainCharacter;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsInCombatWithPlayer(CharacterMainControl enemy, CharacterMainControl player)
        {
            try
            {
                if (player != null)
                {
                    var distance = Vector3.Distance(enemy.transform.position, player.transform.position);
                    if (distance <= CombatDistanceThreshold)
                    {
                        return true;
                    }
                }

                var weapon = GetCurrentWeapon(enemy);
                if (weapon != null)
                {
                    var method = weapon.GetType().GetMethod("IsFiring", BindingFlags.Public | BindingFlags.Instance);
                    if (method != null)
                    {
                        var isFiring = method.Invoke(weapon, null) as bool?;
                        if (isFiring.GetValueOrDefault())
                            return true;
                    }
                }

                var body = enemy.GetComponent<Rigidbody>();
                if (body != null && body.velocity.magnitude > MovementSpeedThreshold)
                    return true;
            }
            catch (Exception ex)
            {
                CESLogger.Debug($"[EnemyVoiceFilter] Combat detection error: {ex.Message}");
            }

            return false;
        }

        private static ItemAgent_Gun GetCurrentWeapon(CharacterMainControl enemy)
        {
            try
            {
                var guns = enemy.GetComponentsInChildren<ItemAgent_Gun>(true);
                return guns.FirstOrDefault(g => g != null && g.Holder == enemy);
            }
            catch
            {
                return null;
            }
        }

        private static bool CanEnemySeePlayer(CharacterMainControl enemy, CharacterMainControl player)
        {
            if (enemy == null || player == null) return false;

            try
            {
                var enemyPos = enemy.transform.position + Vector3.up * VisionHeightOffset;
                var playerPos = player.transform.position + Vector3.up * VisionHeightOffset;
                var direction = playerPos - enemyPos;
                var distance = direction.magnitude;

                if (distance > VisionDistanceThreshold)
                    return false;

                var angle = Vector3.Angle(enemy.transform.forward, direction.normalized);
                if (angle > VisionAngleThreshold)
                    return false;

                if (Physics.Raycast(enemyPos, direction.normalized, out var hit, distance))
                {
                    return hit.transform == player.transform || hit.transform.IsChildOf(player.transform);
                }

                return true;
            }
            catch (Exception ex)
            {
                CESLogger.Debug($"[EnemyVoiceFilter] Vision check error: {ex.Message}");
                return false;
            }
        }

        public static void ClearCaches()
        {
            lock (CacheSync)
            {
                DecisionCache.Clear();
                MerchantCache.Clear();
            }
        }
    }
}
