using Duckov;
using HarmonyLib;
using System;

namespace DuckovCustomSounds.CustomBGM.ExtractionBGM
{
    /// <summary>
    /// Harmony hooks: 仅负责与游戏方法对接并转发到 ExtractionSounds
    /// 包括倒计时生命周期管理和撤离成功Stinger拦截
    /// </summary>
    [HarmonyPatch]
    internal static class ExtractionSounds_Patches
    {
        // LevelManager.NotifyEvacuated 是游戏确认撤离成功的统一入口。
        [HarmonyPatch(typeof(LevelManager))]
        internal static class LevelManager_NotifyEvacuated_Patch
        {
            [HarmonyPatch("NotifyEvacuated")]
            [HarmonyPostfix]
            public static void Postfix()
            {
                try { ExtractionSounds.OnEvacuationCompleted(); } catch { }
            }
        }

        // CountDownArea.BeginCountDown -> 通知开始
        [HarmonyPatch(typeof(CountDownArea))]
        internal static class CountDownArea_Begin_Patch
        {
            [HarmonyPatch("BeginCountDown")]
            [HarmonyPostfix]
            public static void Postfix(CountDownArea __instance)
            {
                try { ExtractionSounds.OnCountDownStarted(__instance); } catch { }
            }
        }

        // CountDownArea.UpdateCountDown -> 每帧检查剩余时间
        [HarmonyPatch(typeof(CountDownArea))]
        internal static class CountDownArea_Update_Patch
        {
            [HarmonyPatch("UpdateCountDown")]
            [HarmonyPostfix]
            public static void Postfix(CountDownArea __instance)
            {
                try { ExtractionSounds.OnTick(__instance, __instance.RemainingTime); } catch { }
            }
        }

        // CountDownArea.AbortCountDown -> 中止撤离时立即停止
        [HarmonyPatch(typeof(CountDownArea))]
        internal static class CountDownArea_Abort_Patch
        {
            [HarmonyPatch("AbortCountDown")]
            [HarmonyPostfix]
            public static void Postfix(CountDownArea __instance)
            {
                try { ExtractionSounds.OnCountDownStopped(__instance); } catch { }
            }
        }

        // CountDownArea.OnCountdownSucceed -> 成功时保留直至自然结束（不做停止）
        [HarmonyPatch(typeof(CountDownArea))]
        internal static class CountDownArea_Succeed_Patch
        {
            [HarmonyPatch("OnCountdownSucceed")]
            [HarmonyPostfix]
            public static void Postfix(CountDownArea __instance)
            {
                try { ExtractionSounds.OnCountDownSucceeded(__instance); } catch { }
            }
        }

        // AudioManager.StopBGM -> 场景切换/死亡等时机通用停止保护
        [HarmonyPatch(typeof(AudioManager))]
        internal static class AudioManager_StopBGM_Patch
        {
            [HarmonyPatch("StopBGM")]
            [HarmonyPostfix]
            public static void Postfix()
            {
                try { ExtractionSounds.StopOnSceneChangeIfNeeded(); } catch { }
            }
        }

        // 替换成功后的短期转场保护：仅抑制非基地的地图 Stinger。
        [HarmonyPatch(typeof(AudioManager))]
        internal static class AudioManager_PlayStringer_Patch
        {
            [HarmonyPatch("PlayStringer", new Type[] { typeof(string) })]
            [HarmonyPrefix]
            public static bool Prefix(string key)
            {
                try
                {
                    // 替换音乐已经由 NotifyEvacuated 播放；这里只抑制撤离转场随后发出的原版地图 Stinger。
                    if (!ExtractionSounds.ShouldSuppressEvacuationStinger(key))
                    {
                        ExtractionBGMLogger.Debug($"放行非撤离Stinger事件: {key}");
                        return true;
                    }

                    ExtractionBGMLogger.Debug($"撤离转场已使用自定义音乐，抑制原版地图 Stinger: {key}");
                    return false;
                }
                catch (Exception ex)
                {
                    ExtractionBGMLogger.Warning($"PlayStringer_Prefix 异常：{ex.Message}");
                    return true; // 异常时放行原版
                }
            }
        }
    }
}
