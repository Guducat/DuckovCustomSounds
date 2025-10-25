using System;
using HarmonyLib;
using Duckov; // Game's AudioManager/CountDownArea lives in Duckov assembly

namespace DuckovCustomSounds
{
    /// <summary>
    /// Harmony hooks: 仅负责与游戏方法对接并转发到 ExtractionSounds
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
    }
}

