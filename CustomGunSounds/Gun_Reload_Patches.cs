using HarmonyLib;
using Duckov; // AudioManager
using System;
using UnityEngine;

namespace DuckovCustomSounds.CustomGunSounds
{
    /// <summary>
    /// 枪械换弹音效补丁
    /// </summary>
    [HarmonyPatch(typeof(AudioManager))]
    public static class AudioManager_Post_GunReloadReplace
    {
        [HarmonyPatch("Post", new Type[] { typeof(string), typeof(GameObject) })]
        [HarmonyPostfix]
        public static void Postfix(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
        {
            try
            {
                // 检查配置是否启用
                if (!GunConfig.Enabled) return;

                // Postfix 覆盖：静音原 Studio 事件并以 Core 播放自定义音频
                GunUtil.PostfixOverrideReload_Safe(ref __result, eventName, gameObject);
            }
            catch (Exception ex)
            {
                GunLogger.Warning($"[GunReload] Postfix 异常: {ex.Message}");
            }
        }
    }
}
