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
        [HarmonyPrefix]
        public static bool Prefix(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
        {
            try
            {
                // 复用公共逻辑（保持射击行为与命名兼容）
                if (GunSfxUtil.TryOverrideShoot(ref __result, eventName, gameObject))
                    return false; // 已替换并播放
                return true;      // 放行原事件
            }
            catch (Exception ex)
            {
                GunLogger.Warn($"[GunShoot] Prefix 异常，放行原始事件: {ex.Message}");
                return true;
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
        [HarmonyPrefix]
        public static bool Prefix(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
        {
            try
            {
                if (GunSfxUtil.TryOverrideReload(ref __result, eventName, gameObject))
                    return false; // 已替换并播放
                return true;      // 放行原事件
            }
            catch (Exception ex)
            {
                GunLogger.Warn($"[GunReload] Prefix 异常，放行原始事件: {ex.Message}");
                return true;
            }
        }
    }

    internal static class GunChamberPatch_Template
    {
        // [HarmonyPatch(typeof(AudioManager))]
        // [HarmonyPatch("Post", new Type[] { typeof(string), typeof(GameObject) })]
        // [HarmonyPrefix]
        public static bool DebugOnly_LogChamber(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
        {
            if (string.IsNullOrEmpty(eventName)) return true;
            if (eventName.Contains("Gun") && eventName.Contains("chamber"))
            {
                GunLogger.Debug($"[GunChamber:Debug] 捕获可能的上膛事件: {eventName}");
            }
            return true; // 放行
        }
    }
}

