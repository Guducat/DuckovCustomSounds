using System;
using System.Collections.Generic;
using Duckov;
using UnityEngine;

namespace DuckovCustomSounds.CustomHitAndKillSounds
{
    internal static class HitAndKillRuntimeHooks
    {
        private static readonly Dictionary<string, float> LastPlayAt = new Dictionary<string, float>(128);
        private static bool subscribed;

        public static void Subscribe()
        {
            if (subscribed)
                return;

            Health.OnHurt += OnHealthHurt;
            Health.OnDead += OnHealthDead;
            HealthSimpleBase.OnSimpleHealthHit += OnSimpleHealthHit;
            HealthSimpleBase.OnSimpleHealthDead += OnSimpleHealthDead;
            subscribed = true;
            HitAndKillLogger.Debug("已订阅 Health/HealthSimpleBase 事件");
        }

        public static void Unsubscribe()
        {
            if (!subscribed)
                return;

            Health.OnHurt -= OnHealthHurt;
            Health.OnDead -= OnHealthDead;
            HealthSimpleBase.OnSimpleHealthHit -= OnSimpleHealthHit;
            HealthSimpleBase.OnSimpleHealthDead -= OnSimpleHealthDead;
            subscribed = false;
            LastPlayAt.Clear();
            HitAndKillLogger.Debug("已取消订阅 Health/HealthSimpleBase 事件");
        }

        public static bool ShouldThrottle(string key, int ownerId, float cooldownMs)
        {
            if (cooldownMs <= 0f)
                return false;

            var now = Time.realtimeSinceStartup;
            var mapKey = ownerId + ":" + key;
            if (LastPlayAt.TryGetValue(mapKey, out var last) && (now - last) * 1000f < cooldownMs)
                return true;

            LastPlayAt[mapKey] = now;
            return false;
        }

        private static void OnHealthHurt(Health health, DamageInfo damageInfo)
        {
            try
            {
                if (!HitAndKillConfig.Enabled || !HitAndKillConfig.PlayHurtSounds)
                    return;

                if (damageInfo.isFromBuffOrEffect || damageInfo.damageValue <= 1.01f)
                    return;

                if (health != null && health.IsMainCharacterHealth)
                {
                    PlaySemantic(HitAndKillEventClassifier.GetHurtKey(damageInfo), 0, null, "default_hurt", HitAndKillConfig.HurtCooldownMs);
                    return;
                }

                if (damageInfo.fromCharacter != null && damageInfo.fromCharacter.IsMainCharacter)
                {
                    var targetGo = health != null ? health.gameObject : null;
                    var ownerId = targetGo != null ? targetGo.GetInstanceID() : 0;
                    PlaySemantic(HitAndKillEventClassifier.GetNpcHurtKey(damageInfo), ownerId, targetGo, "default_hit", HitAndKillConfig.HurtCooldownMs);
                }
            }
            catch (Exception ex)
            {
                HitAndKillLogger.Warning($"Health.OnHurt 处理失败: {ex.Message}");
            }
        }

        private static void OnHealthDead(Health health, DamageInfo damageInfo)
        {
            try
            {
                if (!HitAndKillConfig.Enabled || !HitAndKillConfig.ReplaceMarkerSounds)
                    return;

                if (damageInfo.fromCharacter == null || !damageInfo.fromCharacter.IsMainCharacter)
                    return;

                if (damageInfo.toDamageReceiver != null && damageInfo.toDamageReceiver.IsMainCharacter)
                    return;

                PlaySemantic(HitAndKillEventClassifier.GetKillKey(damageInfo), 0, null, "killmarker", HitAndKillConfig.MarkerCooldownMs);
            }
            catch (Exception ex)
            {
                HitAndKillLogger.Warning($"Health.OnDead 处理失败: {ex.Message}");
            }
        }

        private static void OnSimpleHealthHit(HealthSimpleBase health, DamageInfo damageInfo)
        {
            try
            {
                if (!HitAndKillConfig.Enabled)
                    return;

                if (damageInfo.damageValue <= 1.01f)
                    return;

                if (damageInfo.fromCharacter == null || !damageInfo.fromCharacter.IsMainCharacter)
                    return;

                var go = health != null ? health.gameObject : null;
                var ownerId = go != null ? go.GetInstanceID() : 0;

                if (HitAndKillConfig.ReplaceMarkerSounds)
                    PlaySemantic(HitAndKillEventClassifier.GetHitKey(damageInfo), 0, null, "hitmarker", HitAndKillConfig.MarkerCooldownMs);

                if (HitAndKillConfig.PlayHurtSounds)
                    PlaySemantic(HitAndKillEventClassifier.GetNpcHurtKey(damageInfo), ownerId, go, "default_hit", HitAndKillConfig.HurtCooldownMs);
            }
            catch (Exception ex)
            {
                HitAndKillLogger.Warning($"HealthSimpleBase.OnSimpleHealthHit 处理失败: {ex.Message}");
            }
        }

        private static void OnSimpleHealthDead(HealthSimpleBase health, DamageInfo damageInfo)
        {
            try
            {
                if (!HitAndKillConfig.Enabled || !HitAndKillConfig.ReplaceMarkerSounds)
                    return;

                if (damageInfo.fromCharacter == null || !damageInfo.fromCharacter.IsMainCharacter)
                    return;

                PlaySemantic(HitAndKillEventClassifier.GetKillKey(damageInfo), 0, null, "killmarker", HitAndKillConfig.MarkerCooldownMs);
            }
            catch (Exception ex)
            {
                HitAndKillLogger.Warning($"HealthSimpleBase.OnSimpleHealthDead 处理失败: {ex.Message}");
            }
        }

        private static void PlaySemantic(string key, int ownerId, GameObject? gameObject, string fallback, float cooldownMs)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            var dir = CustomHitAndKillSounds.ModuleDirectory;
            var file = HitAndKillSoundResolver.FindSoundFile(dir, new[] { key }, new[] { fallback, "default" });
            if (file == null)
            {
                HitAndKillLogger.Debug($"未找到自定义文件: key={key}, fallback={fallback}");
                return;
            }

            if (ShouldThrottle(key, ownerId, cooldownMs))
                return;

            HitAndKillSoundPlayer.Play(file, gameObject);
            HitAndKillLogger.Debug($"播放语义音效: key={key}, file={System.IO.Path.GetFileName(file)}");
        }
    }
}
