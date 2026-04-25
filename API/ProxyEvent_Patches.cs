using System;
using System.IO;
using Duckov;
using HarmonyLib;
using UnityEngine;

namespace DuckovCustomSounds.API
{
    /// <summary>
    /// 兼容历史 DCS:/ 假事件。新代码应优先使用 Duckov.AudioManager.PostCustomSFX。
    /// </summary>
    [HarmonyPatch(typeof(AudioManager))]
    public static class DcsProxyEventPatch
    {
        private const string Prefix = "DCS:/";

        [HarmonyPatch(nameof(AudioManager.Post), new Type[] { typeof(string), typeof(GameObject) })]
        [HarmonyPrefix]
        public static bool Prefix_Post(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
        {
            if (string.IsNullOrEmpty(eventName) || !eventName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            __result = new FMOD.Studio.EventInstance?();
            if (!TryResolveFilePath(eventName, out var fullPath))
            {
                return false;
            }

            try
            {
                AudioManager.PostCustomSFX(fullPath, gameObject, loop: false);
            }
            catch
            {
            }

            return false;
        }

        internal static bool TryResolveFilePath(string eventName, out string fullPath)
        {
            fullPath = string.Empty;
            if (string.IsNullOrEmpty(eventName) || !eventName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var tail = eventName.Substring(Prefix.Length).Trim();
            if (string.IsNullOrEmpty(tail))
            {
                return false;
            }

            fullPath = Path.IsPathRooted(tail)
                ? tail
                : Path.Combine(ModBehaviour.ModFolderName, tail.Replace('/', Path.DirectorySeparatorChar));
            return true;
        }
    }
}
