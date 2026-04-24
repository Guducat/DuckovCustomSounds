using HarmonyLib;
using Duckov;
using System;
using UnityEngine;
using System.Collections.Generic;
using DuckovCustomSounds.CustomBGM.Core; // BGMLogger
using DuckovCustomSounds.CustomBGM.HomeBGM; // HomeBGMConfig

namespace DuckovCustomSounds.CustomBGM
{
    internal static class BGMVolumeApplier
    {
        public static void ApplyHomeBgmVolume(FMOD.Studio.EventInstance? instance)
        {
            try
            {
                if (instance.HasValue && instance.Value.isValid())
                {
                    instance.Value.setVolume(HomeBGMConfig.Volume);
                }
            }
            catch { }
        }
    }

    // --- 补丁 1、2、3 已迁移到 HomeBGM 模块 (HomeBGM_Patches.cs) ---

    // --- 补丁 4：拦截 Home Stinger (stg_map_base)，播放自定义 start.* ---
    [HarmonyPatch(typeof(AudioManager))]
    public static class HomeStingerPatch
    {
        private const string HomeStingerEventName = "Music/Stinger/stg_map_base";
        private const string HomeStingerKey = "stg_map_base";

        [HarmonyPatch("Post", new Type[] { typeof(string) })]
        [HarmonyPrefix]
        public static bool Post_Prefix(ref FMOD.Studio.EventInstance? __result, string eventName)
        {
            try
            {
                return !TryPlayHomeStinger(eventName, isKey: false, ref __result);
            }
            catch (Exception ex)
            {
                BGMLogger.Warning($"HomeStingerPatch 错误: {ex.Message}");
                return true; // 出错时放行原方法
            }
        }

        [HarmonyPatch("PlayStringer", new Type[] { typeof(string) })]
        [HarmonyPrefix]
        public static bool PlayStringer_Prefix(string key)
        {
            try
            {
                FMOD.Studio.EventInstance? result = null;
                return !TryPlayHomeStinger(key, isKey: true, ref result);
            }
            catch (Exception ex)
            {
                BGMLogger.Warning($"HomeStingerPatch PlayStringer 错误: {ex.Message}");
                return true;
            }
        }

        private static bool TryPlayHomeStinger(string eventNameOrKey, bool isKey, ref FMOD.Studio.EventInstance? result)
        {
            if (isKey)
            {
                if (!string.Equals(eventNameOrKey, HomeStingerKey, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            else if (!string.Equals(eventNameOrKey, HomeStingerEventName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!HomeBGMConfig.EnableStartMusic)
            {
                BGMLogger.Debug("start.* 已被配置禁用，跳过播放");
                return false;
            }

            var titleDir = System.IO.Path.Combine(ModBehaviour.ModFolderName, "TitleBGM");
            var startPath = AudioFileExtensions.FindMusicFile(titleDir, "start");
            if (string.IsNullOrEmpty(startPath))
            {
                BGMLogger.Debug("start.* 不存在，放行原 Stinger");
                return false;
            }

            BGMLogger.Info($"拦截 stg_map_base，使用 Music 总线播放 {System.IO.Path.GetFileName(startPath)}");
            result = CustomBGMPlayer.PlayMusicFile(startPath, loop: false, stopExistingBGM: true);
            BGMVolumeApplier.ApplyHomeBgmVolume(result);
            HomeBGMManager.BeginStartStingerProtection(result);
            return true;
        }
    }

    // --- 补丁 5：拦截 Death Stinger (stg_death)，播放自定义 death.* ---
    [HarmonyPatch(typeof(AudioManager))]
    public static class DeathStingerPatch
    {
        [HarmonyPatch("Post", new Type[] { typeof(string) })]
        [HarmonyPrefix]
        public static bool Post_Prefix(ref FMOD.Studio.EventInstance? __result, string eventName)
        {
            try
            {
                if (!string.Equals(eventName, "Music/Stinger/stg_death", StringComparison.OrdinalIgnoreCase))
                    return true; // 非 Death Stinger，放行

                var titleDir = System.IO.Path.Combine(ModBehaviour.ModFolderName, "TitleBGM");
                var deathPath = AudioFileExtensions.FindMusicFile(titleDir, "death");
                if (string.IsNullOrEmpty(deathPath))
                {
                    BGMLogger.Debug("death.* 不存在，放行原 Stinger");
                    return true;
                }

                // 使用新接口播放自定义 death.*（2D 音效，单次播放）
                BGMLogger.Info($"拦截 stg_death，使用新接口播放 {System.IO.Path.GetFileName(deathPath)}");
                __result = Duckov.AudioManager.PlayCustomBGM(deathPath, loop: false);
                BGMVolumeApplier.ApplyHomeBgmVolume(__result);

                // 跳过原方法
                return false;
            }
            catch (Exception ex)
            {
                BGMLogger.Warning($"DeathStingerPatch 错误: {ex.Message}");
                return true; // 出错时放行原方法
            }
        }
    }

    // --- 补丁 6 已迁移到 ExtractionBGM 模块 (ExtractionSounds_Patches.cs) ---
}
