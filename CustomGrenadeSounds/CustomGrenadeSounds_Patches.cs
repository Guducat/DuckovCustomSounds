using HarmonyLib;
using System;
using UnityEngine;
using Duckov;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using FMOD;

namespace DuckovCustomSounds.CustomGrenadeSounds
{
    /// <summary>
    /// 手雷音效替换 - 使用新接口简化版本
    /// </summary>
    [HarmonyPatch(typeof(AudioManager))]
    public static class AudioManager_Post_GrenadeReplace
    {
        private const string GrenadePrefix = "SFX/Combat/Explosive/";
        private static readonly string[] Exts = new[] { ".mp3", ".wav", ".ogg", ".oga" };

        private static IEnumerable<string> ExpandCandidates(string dir, params string[] namesNoExt)
        {
            foreach (var name in namesNoExt)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                foreach (var ext in Exts)
                {
                    yield return Path.Combine(dir, name + ext);
                }
            }
        }

        [HarmonyPatch("Post", new Type[] { typeof(string), typeof(GameObject) })]
        [HarmonyPostfix]
        public static void Postfix(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
        {
            try
            {
                // 检查配置是否启用
                if (!GrenadeConfig.Enabled) return;

                if (string.IsNullOrEmpty(eventName)) return;
                if (!eventName.StartsWith(GrenadePrefix, StringComparison.OrdinalIgnoreCase)) return;

                string soundKey = eventName.Substring(GrenadePrefix.Length);
                if (string.IsNullOrWhiteSpace(soundKey)) return;

                string dir = Path.Combine(ModBehaviour.ModFolderName, "CustomGrenadeSounds");

                // 构建候选文件列表
                var attempts = new List<string>();
                attempts.AddRange(ExpandCandidates(dir, soundKey));

                string filePath = attempts.FirstOrDefault(File.Exists);
                if (filePath == null)
                {
                    var fallbacks = ExpandCandidates(dir, "default");
                    filePath = fallbacks.FirstOrDefault(File.Exists);
                    if (filePath == null)
                    {
                        GrenadeLogger.Debug($"[Grenade] soundKey={soundKey}, 未找到自定义文件");
                        return;
                    }
                }

                GrenadeLogger.Debug($"[Grenade] soundKey={soundKey}, 使用: {Path.GetFileName(filePath)}");

                // 使用新接口播放自定义 SFX
                try
                {
                    var originalInstance = __result;
                    float minDistance = 0f;
                    float maxDistance = 0f;
                    bool hasDistance = false;
                    if (originalInstance.HasValue && originalInstance.Value.isValid())
                    {
                        hasDistance = AudioDistanceHelper.TryExtractFromEventInstance(originalInstance.Value, out minDistance, out maxDistance);
                        try { originalInstance.Value.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); } catch { }
                        try { originalInstance.Value.release(); } catch { }
                    }
                    if (!hasDistance)
                    {
                        hasDistance = AudioDistanceHelper.TryExtractFromEventName(eventName, out minDistance, out maxDistance);
                    }

                    __result = AudioManager.PostCustomSFX(filePath, gameObject, loop: false);
                    if (__result.HasValue && __result.Value.isValid())
                    {
                        // 应用距离衰减
                        if (hasDistance)
                        {
                            AudioDistanceHelper.ApplyToEventInstance(__result.Value, minDistance, maxDistance);
                        }

                        // 应用音量配置
                        try
                        {
                            __result.Value.setVolume(GrenadeConfig.Volume);
                            GrenadeLogger.Debug($"[Grenade] 已应用音量: {GrenadeConfig.Volume:F2}");
                        }
                        catch (Exception volEx)
                        {
                            GrenadeLogger.Warning($"[Grenade] 设置音量失败: {volEx.Message}");
                        }
                    }
                    GrenadeLogger.Info($"[Grenade] 使用SFX接口播放: {Path.GetFileName(filePath)}");

                }
                catch (Exception ex)
                {
                    GrenadeLogger.Warning($"[Grenade] 新接口播放失败: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                GrenadeLogger.Warning($"[Grenade] Postfix 覆盖异常: {ex.Message}");
            }
        }
    }
}

