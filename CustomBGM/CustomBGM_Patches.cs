using HarmonyLib;
using Duckov;
using System;
using UnityEngine;
using System.Collections.Generic;
using DuckovCustomSounds.CustomBGM.Core; // BGMLogger
using DuckovCustomSounds.CustomBGM.HomeBGM; // HomeBGMConfig

namespace DuckovCustomSounds.CustomBGM
{
    // --- 补丁 1、2、3 已迁移到 HomeBGM 模块 (HomeBGM_Patches.cs) ---

    // --- 补丁 4：拦截 Home Stinger (stg_map_base)，播放自定义 start.mp3 ---
    [HarmonyPatch(typeof(AudioManager))]
    public static class HomeStingerPatch
    {
        [HarmonyPatch("Post", new Type[] { typeof(string) })]
        [HarmonyPrefix]
        public static bool Post_Prefix(ref FMOD.Studio.EventInstance? __result, string eventName)
        {
            try
            {
                if (!string.Equals(eventName, "Music/Stinger/stg_map_base", StringComparison.OrdinalIgnoreCase))
                    return true; // 非 Home Stinger，放行

                // 检查配置是否启用 start.mp3
                if (!HomeBGMConfig.EnableStartMusic)
                {
                    BGMLogger.Debug("start.mp3 已被配置禁用，跳过播放");
                    return true; // 放行原方法
                }

                string startPath = System.IO.Path.Combine(ModBehaviour.ModFolderName, "TitleBGM", "start.mp3");
                if (!System.IO.File.Exists(startPath))
                {
                    BGMLogger.Debug("start.mp3 不存在，放行原 Stinger");
                    return true;
                }

                // 使用 Music 总线播放自定义 start.mp3（单次播放）
                BGMLogger.Info("拦截 stg_map_base，使用 Music 总线播放 start.mp3");
                __result = Duckov.AudioManager.PlayCustomBGM(startPath, loop: false);

                // 跳过原方法
                return false;
            }
            catch (Exception ex)
            {
                BGMLogger.Warning($"HomeStingerPatch 错误: {ex.Message}");
                return true; // 出错时放行原方法
            }
        }
    }

    // --- 补丁 5：拦截 Death Stinger (stg_death)，播放自定义 death.mp3 ---
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

                string deathPath = System.IO.Path.Combine(ModBehaviour.ModFolderName, "TitleBGM", "death.mp3");
                if (!System.IO.File.Exists(deathPath))
                {
                    BGMLogger.Debug("death.mp3 不存在，放行原 Stinger");
                    return true;
                }

                // 使用新接口播放自定义 death.mp3（2D 音效，单次播放）
                BGMLogger.Info("拦截 stg_death，使用新接口播放 death.mp3");
                __result = Duckov.AudioManager.PlayCustomBGM(deathPath, loop: false);

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

