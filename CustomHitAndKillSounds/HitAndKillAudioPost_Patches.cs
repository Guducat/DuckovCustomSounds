using System;
using System.IO;
using Duckov;
using FMOD.Studio;
using HarmonyLib;
using UnityEngine;

namespace DuckovCustomSounds.CustomHitAndKillSounds
{
    [HarmonyPatch(typeof(AudioManager))]
    internal static class HitAndKillAudioManagerPostPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(AudioManager.Post), new[] { typeof(string) })]
        private static void PostStringPostfix(string eventName, ref EventInstance? __result)
        {
            try
            {
                if (!HitAndKillConfig.Enabled || !HitAndKillConfig.ReplaceMarkerSounds)
                    return;

                if (!HitAndKillEventClassifier.TryClassifyMarkerEvent(eventName, out var key))
                    return;

                var file = HitAndKillSoundResolver.FindSoundFile(CustomHitAndKillSounds.ModuleDirectory, new[] { key }, new[] { "default_marker", "default" });
                if (file == null)
                {
                    HitAndKillLogger.Debug($"Marker 未找到自定义文件: event={eventName}, key={key}");
                    return;
                }

                if (HitAndKillRuntimeHooks.ShouldThrottle(key, 0, HitAndKillConfig.MarkerCooldownMs))
                {
                    HitAndKillSoundPlayer.SuppressOriginal(__result);
                    return;
                }

                HitAndKillSoundPlayer.SuppressOriginal(__result);
                __result = HitAndKillSoundPlayer.Play(file, null);
                HitAndKillLogger.Debug($"Marker 替换: {key} -> {Path.GetFileName(file)}");
            }
            catch (Exception ex)
            {
                HitAndKillLogger.Warning($"AudioManager.Post(string) 处理失败: {ex.Message}");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(AudioManager.Post), new[] { typeof(string), typeof(GameObject) })]
        private static void PostGameObjectPostfix(string eventName, GameObject gameObject, EventInstance? __result)
        {
            try
            {
                if (!HitAndKillConfig.ReflectionDiagnostics)
                    return;

                if (!HitAndKillReflectionProbe.LooksRelevantAudioEvent(eventName))
                    return;

                var components = gameObject != null ? gameObject.GetComponents<Component>() : Array.Empty<Component>();
                foreach (var component in components)
                    HitAndKillReflectionProbe.LogAudioFields(component, "AudioManager.Post");
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(AudioObject))]
    internal static class HitAndKillAudioObjectPostPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(AudioObject.Post), new[] { typeof(string), typeof(bool) })]
        private static void PostPostfix(AudioObject __instance, string eventName, bool doRelease, EventInstance? __result)
        {
            try
            {
                if (!HitAndKillConfig.ReflectionDiagnostics)
                    return;

                if (!HitAndKillReflectionProbe.LooksRelevantAudioEvent(eventName))
                    return;

                HitAndKillLogger.Debug($"[AudioObject.Post] event={eventName}, go={(__instance != null ? __instance.gameObject.name : "null")}");
                var components = __instance != null ? __instance.GetComponents<Component>() : Array.Empty<Component>();
                foreach (var component in components)
                    HitAndKillReflectionProbe.LogAudioFields(component, "AudioObject.Post");
            }
            catch
            {
            }
        }
    }
}
