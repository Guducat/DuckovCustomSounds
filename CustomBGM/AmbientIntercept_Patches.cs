using HarmonyLib;
using System;
using Duckov; // AudioObject

namespace DuckovCustomSounds.CustomBGM
{
    /// <summary>
    /// 环境音（Amb/amb_*）拦截：在 AudioObject.Post(string,bool) 的 Harmony Prefix 中统一屏蔽。
    /// - 仅当 ModSettings.EnableAmbientIntercept=true 时生效；默认关闭以便灰度测试
    /// - 放行特例：Amb/amb_storm
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
                if (!DuckovCustomSounds.ModSettings.EnableAmbientIntercept)
                    return true; // 未开启：放行

                if (string.IsNullOrEmpty(eventName))
                    return true;

                // 仅处理环境音：Amb/amb_*
                if (!eventName.StartsWith("Amb/amb_", StringComparison.OrdinalIgnoreCase))
                    return true;

                // 特例放行：风暴环境音
                if (string.Equals(eventName, "Amb/amb_storm", StringComparison.OrdinalIgnoreCase))
                    return true;

                // 命中：阻止原方法并返回空实例
                BGMLogger.Info($"[AmbientIntercept] 拦截环境音: {eventName}");
                __result = new FMOD.Studio.EventInstance?();
                return false;
            }
            catch (Exception ex)
            {
                BGMLogger.Warn($"[AmbientIntercept] 异常: {ex.Message}");
                return true; // 出错时放行，确保稳定性
            }
        }
    }
}

