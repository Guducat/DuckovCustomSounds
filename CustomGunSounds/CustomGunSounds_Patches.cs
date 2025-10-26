using HarmonyLib;
using Duckov; // AudioManager
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FMOD;
using FMODUnity;

namespace DuckovCustomSounds.CustomGunSounds
{
    // 主要补丁：拦截 枪械射击 SFX 并替换为自定义 3D 音效
    [HarmonyPatch(typeof(AudioManager))]
    public static class AudioManager_Post_GunShootReplace
    {
        private const string ShootPrefix = "SFX/Combat/Gun/Shoot/";

        [HarmonyPatch("Post", new Type[] { typeof(string), typeof(GameObject) })]
        [HarmonyPostfix]
        public static void Postfix(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
        {
            try
            {
                // Postfix 覆盖：静音原 Studio 事件并以 Core 播放自定义音频（保持射击命名兼容）
                GunSfxUtil.PostfixOverrideShoot_Safe(ref __result, eventName, gameObject);
            }
            catch (Exception ex)
            {
                GunLogger.Warn($"[GunShoot] Postfix 异常: {ex.Message}");
            }
        }

        private static System.Collections.IEnumerator Cleanup(Sound sound, Channel channel, float maxSec)
        {
            float end = Time.realtimeSinceStartup + Mathf.Max(1f, maxSec);
            try
            {
                while (Time.realtimeSinceStartup < end)
                {
                    bool playing = false;
                    try { if (channel.hasHandle()) channel.isPlaying(out playing); } catch { }
                    if (!playing) break;
                    yield return new WaitForSeconds(0.05f);
                }
            }
            finally
            {
                try { if (channel.hasHandle()) channel.stop(); } catch { }
                try { if (sound.hasHandle()) sound.release(); } catch { }
            }
        }
    }

    // 启用：拦截 枪械换弹 SFX，并按细粒度(_start/_end)替换
    [HarmonyPatch(typeof(AudioManager))]
    public static class AudioManager_Post_GunReloadReplace
    {
        private const string ReloadPrefix = "SFX/Combat/Gun/Reload/";

        [HarmonyPatch("Post", new Type[] { typeof(string), typeof(GameObject) })]
        [HarmonyPostfix]
        public static void Postfix(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
        {
            try
            {
                // Postfix 覆盖：静音原 Studio 事件并以 Core 播放自定义音频（保持细粒度 _start/_end 命名与回退链）
                GunSfxUtil.PostfixOverrideReload_Safe(ref __result, eventName, gameObject);
            }
            catch (Exception ex)
            {
                GunLogger.Warn($"[GunReload] Postfix 异常: {ex.Message}");
            }
        }
    }
}

