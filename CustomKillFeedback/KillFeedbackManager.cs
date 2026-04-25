using System;
using UnityEngine;

namespace DuckovCustomSounds.CustomKillFeedback
{
    /// <summary>
    /// 自定义击杀反馈管理：串联事件监听、连杀统计、音效与 UI。
    /// </summary>
    internal static class KillFeedbackManager
    {
        private static bool _initialized;
        private static bool _eventHooked;
        private static KillUIController _ui;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            try
            {
                KillFeedbackLogger.Info("[KF] 初始化...");
                KillFeedbackConfig.Initialize();
                KillIconLibrary.Initialize();

                KillStreakTracker.Initialize();
                KillStreakTracker.OnStreak += OnStreak;

                _ui = KillUIController.EnsureInstance();

                Subscribe();
                ApplyRuntimeOptions();

                KillFeedbackLogger.Info("[KF] 初始化完成");
            }
            catch (Exception ex)
            {
                KillFeedbackLogger.Error("[KF] 初始化失败", ex);
            }
        }

        public static void Unload()
        {
            try
            {
                Unsubscribe();
                KillStreakTracker.OnStreak -= OnStreak;
                KillStreakTracker.Deinitialize();
                KillUIController.DestroyInstance();
            }
            catch (Exception ex)
            {
                KillFeedbackLogger.Warning($"[KF] 卸载异常: {ex.Message}");
            }
            finally
            {
                _initialized = false;
                _eventHooked = false;
                _ui = null;
            }
        }

        public static void ApplyRuntimeOptions()
        {
            try
            {
                KillIconLibrary.Reload();
                EnsureUI()?.ApplyConfig();
            }
            catch (Exception ex)
            {
                KillFeedbackLogger.Warning($"[KF] 应用运行时配置失败: {ex.Message}");
            }
        }

        private static void Subscribe()
        {
            if (_eventHooked) return;
            try
            {
                Health.OnDead += OnHealthDead;
                _eventHooked = true;
                KillFeedbackLogger.Info("[KF] 已监听 Health.OnDead");
            }
            catch (Exception ex)
            {
                KillFeedbackLogger.Error("[KF] 注册 Health.OnDead 失败", ex);
            }
        }

        private static void Unsubscribe()
        {
            if (!_eventHooked) return;
            try { Health.OnDead -= OnHealthDead; } catch { }
            _eventHooked = false;
        }

        private static void OnHealthDead(Health health, DamageInfo dmg)
        {
            try
            {
                if (!KillFeedbackConfig.Enabled) return;
                if (dmg.fromCharacter == null || !dmg.fromCharacter.IsMainCharacter) return;
                if (dmg.toDamageReceiver != null && dmg.toDamageReceiver.IsMainCharacter) return;

                bool isHeadshot = dmg.crit > 0;
                bool isExplosion = dmg.isExplosion;
                bool isMelee = false;
                try { isMelee = dmg.fromCharacter.GetMeleeWeapon() != null; } catch { }

                bool isGoldenHeadshot = false;
                if (isHeadshot && health != null)
                {
                    try
                    {
                        float maxHealth = health.MaxHealth;
                        if (maxHealth > 0f && dmg.finalDamage >= maxHealth * 0.9f)
                            isGoldenHeadshot = true;
                    }
                    catch { }
                }

                int streak = KillStreakTracker.RegisterKill(isHeadshot);
                var context = new KillEventContext(streak, isHeadshot, isGoldenHeadshot, isExplosion, isMelee);

                KillSoundPlayer.PlayKill(context);
                EnsureUI()?.ShowKill(context);

                if (KillFeedbackLogger.IsDebugEnabled)
                {
                    KillFeedbackLogger.Debug($"[KF] Kill: streak={streak}, head={isHeadshot}, melee={isMelee}, explosion={isExplosion}, gold={isGoldenHeadshot}");
                }
            }
            catch (Exception ex)
            {
                KillFeedbackLogger.Error("[KF] OnHealthDead 处理异常", ex);
            }
        }

        private static KillUIController EnsureUI()
        {
            if (_ui != null) return _ui;
            try
            {
                _ui = KillUIController.EnsureInstance();
            }
            catch (Exception ex)
            {
                KillFeedbackLogger.Warning($"[KF] 创建 UI 异常: {ex.Message}");
            }
            return _ui;
        }

        private static void OnStreak(int streak, bool isHeadshot)
        {
            // 预留扩展：例如数值阈值触发额外事件
        }
    }
}
