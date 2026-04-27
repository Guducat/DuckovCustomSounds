using HarmonyLib;
using System;
using Duckov; // AudioObject
using DuckovCustomSounds.CustomBGM.Core; // BGMLogger

namespace DuckovCustomSounds.CustomBGM.AmbientIntercept
{
    /// <summary>
    /// 环境音（Amb/amb_*）拦截：在 AudioObject.Post(string,bool) 的 Harmony Prefix 中统一屏蔽。
    /// - 仅当 AmbientInterceptConfig.Enabled=true 时生效；默认关闭以便灰度测试
    /// - 风暴阶段提示音由 AmbientInterceptConfig.InterceptStormStingers 单独控制
    /// - 环境音放行特例：Amb/amb_storm
    /// - 出错安全：异常时放行原方法
    /// </summary>
    [HarmonyPatch(typeof(AudioObject))]
    internal static class AmbientIntercept_Patches
    {
        [HarmonyPatch("Post", new Type[] { typeof(string), typeof(bool) })]
        [HarmonyPrefix]
        private static bool Post_Prefix(AudioObject __instance, ref FMOD.Studio.EventInstance? __result, string eventName, bool doRelease)
        {
            try
            {
                if (string.IsNullOrEmpty(eventName))
                    return true;

                if (IsAmbientEvent(eventName))
                {
                    if (!AmbientInterceptConfig.Enabled || IsAllowedAmbientEvent(eventName))
                        return true;

                    return BlockEvent(ref __result, eventName);
                }

                if (IsStormStinger(eventName))
                {
                    if (!AmbientInterceptConfig.InterceptStormStingers)
                        return true;

                    return BlockEvent(ref __result, eventName);
                }

                return true;
            }
            catch (Exception ex)
            {
                BGMLogger.Warning($"[AmbientIntercept] 异常: {ex.Message}");
                return true; // 出错时放行，确保稳定性
            }
        }

        private static bool IsAmbientEvent(string eventName)
        {
            return eventName.StartsWith("Amb/amb_", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAllowedAmbientEvent(string eventName)
        {
            return string.Equals(eventName, "Amb/amb_storm", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsStormStinger(string eventName)
        {
            return string.Equals(eventName, "Music/Stinger/stg_storm_1", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(eventName, "Music/Stinger/stg_storm_2", StringComparison.OrdinalIgnoreCase);
        }

        private static bool BlockEvent(ref FMOD.Studio.EventInstance? result, string eventName)
        {
            BGMLogger.Info($"[AmbientIntercept] 拦截音频事件: {eventName}");
            result = new FMOD.Studio.EventInstance?();
            return false;
        }
    }
}
