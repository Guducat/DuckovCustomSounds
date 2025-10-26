using System;
using System.IO;
using HarmonyLib;
using Duckov; // AudioManager
using UnityEngine;

namespace DuckovCustomSounds.API
{
    /// <summary>
    /// 
    /// </summary>
    [HarmonyPatch(typeof(AudioManager))]
    public static class DcsProxyEventPatch
    {
        private const string Prefix = "DCS:/";

        [HarmonyPatch("Post", new Type[] { typeof(string), typeof(GameObject) })]
        [HarmonyPrefix]
        public static bool Prefix_Post(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
        {
            try
            {
                if (string.IsNullOrEmpty(eventName) || !eventName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                    return true; // 非 DCS 代理事件 -> 放行

                // 解析路径：支持绝对路径；相对路径相对于本 Mod 目录
                var tail = eventName.Substring(Prefix.Length).Trim();
                if (string.IsNullOrEmpty(tail))
                {
                    __result = new FMOD.Studio.EventInstance?();
                    return false; // 吞掉无效事件，避免 FMOD 报错
                }

                var full = tail;
                if (!Path.IsPathRooted(full))
                {
                    full = Path.Combine(DuckovCustomSounds.ModBehaviour.ModFolderName, tail.Replace('/', Path.DirectorySeparatorChar));
                }

                var req = new PlaybackRequest
                {
                    Source = gameObject,
                    FileFullPath = full,
                    SoundKey = "external",
                    MinDistance = 1.5f,
                    MaxDistance = 25f,
                    FollowTransform = true,
                };

                if (CustomModController.Play3D(req, out var _))
                {
                    __result = new FMOD.Studio.EventInstance?();
                    return false; // 拦截并消费
                }

                // 播放失败也消费，避免无效 FMOD 事件名引起异常
                __result = new FMOD.Studio.EventInstance?();
                return false;
            }
            catch
            {
                // 发生异常则放行，避免影响原有逻辑
                return true;
            }
        }
    }
}

