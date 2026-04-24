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

        // --- 补丁：拦截撤离成功 Stinger (stg_map_zero) ---
        // 根据配置模式决定行为：
        // - Disabled: 放行原版逻辑
        // - CountdownMode: 屏蔽Stinger（倒计时音效会持续）
        // - SuccessStingerMode: 替换为自定义音效
        [HarmonyPatch(typeof(AudioManager))]
        internal static class AudioManager_PlayStringer_Patch
        {
            [HarmonyPatch("PlayStringer", new Type[] { typeof(string) })]
            [HarmonyPrefix]
            public static bool Prefix(string key)
            {
                try
                {
                    // 仅拦截撤离成功 Stinger
                    if (!ExtractionSounds.IsExtractionStingerKey(key))
                    {
                        ExtractionBGMLogger.Debug($"放行非撤离Stinger事件: {key}");
                        return true; // 非撤离Stinger，放行
                    }

                    ExtractionBGMLogger.Debug($"检测到撤离Stinger事件: {key}");
                    
                    // 调用 ExtractionSounds 处理
                    bool handled = ExtractionSounds.OnSuccessStingerRequested(key);
                    return !handled; // true=放行原版，false=拦截原版
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
