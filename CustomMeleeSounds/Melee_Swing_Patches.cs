using HarmonyLib;
using Duckov;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DuckovCustomSounds.CustomMeleeSounds
{
    /// <summary>
    /// 近战挥舞音效替换
    /// </summary>
    [HarmonyPatch(typeof(AudioManager))]
    public static class AudioManager_Post_MeleeSwingReplace
    {
        private const string MeleeSwingPrefix = "SFX/Combat/Melee/swing_";

        [HarmonyPatch("Post", new Type[] { typeof(string), typeof(GameObject) })]
        [HarmonyPostfix]
        public static void Postfix(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
        {
            try
            {
                // 检查配置是否启用
                if (!MeleeConfig.Enabled) return;

                if (string.IsNullOrEmpty(eventName)) return;
                if (!eventName.StartsWith(MeleeSwingPrefix, StringComparison.OrdinalIgnoreCase)) return;

                string soundKey = eventName.Substring(MeleeSwingPrefix.Length);
                if (string.IsNullOrWhiteSpace(soundKey)) return;

                string dir = Path.Combine(ModBehaviour.ModFolderName, "CustomMeleeSounds");

                // 捕获近战组件
                var (melee, typeIdStr) = MeleeUtil.GetMeleeAndTypeId(gameObject);

                // 构建候选文件列表（swing 文件）
                var attempts = new List<string>();
                if (!string.IsNullOrWhiteSpace(typeIdStr))
                {
                    attempts.AddRange(MeleeUtil.ExpandCandidates(dir, typeIdStr + "_swing"));
                    attempts.AddRange(MeleeUtil.ExpandCandidates(dir, soundKey));
                }
                else
                {
                    MeleeLogger.Debug($"[MeleeSwing] 未捕获到 ItemAgent_MeleeWeapon，回退到 soundKey 查找模式");
                    attempts.AddRange(MeleeUtil.ExpandCandidates(dir, soundKey));
                }

                var fallbacks = MeleeUtil.ExpandCandidates(dir, "default_swing").ToList();
                string filePath = MeleeUtil.FindSoundFile(dir, attempts, fallbacks);

                if (filePath == null)
                {
                    string chain = string.Join(" → ", attempts.Select(p => $"[{Path.GetFileName(p)}]").ToArray());
                    MeleeLogger.Debug($"[MeleeSwing] 查找: {chain}, 结果: 未找到");
                    return;
                }

                // 命中基准文件后，尝试差分变体
                try
                {
                    string picked = MeleeUtil.TryPickVariant(filePath);
                    if (!string.Equals(picked, filePath, StringComparison.OrdinalIgnoreCase) && MeleeLogger.IsDebugEnabled)
                    {
                        MeleeLogger.Debug($"[MeleeSwing] 发现变体，替换 {Path.GetFileName(filePath)} → {Path.GetFileName(picked)}");
                    }
                    filePath = picked;
                }
                catch { }

                string chainStr = string.Join(" → ", attempts.Select(p => $"[{Path.GetFileName(p)}]").ToArray());
                MeleeLogger.Debug($"[MeleeSwing] TypeID={typeIdStr ?? "N/A"}, soundKey={soundKey}, 查找: {chainStr}, 结果: {Path.GetFileName(filePath)}");

                // 使用新接口播放自定义音效
                try
                {
                    __result = AudioManager.PostCustomSFX(filePath, gameObject, loop: false);

                    // 应用音量配置
                    if (__result.HasValue && __result.Value.isValid())
                    {
                        try
                        {
                            __result.Value.setVolume(MeleeConfig.Volume);
                            MeleeLogger.Debug($"[MeleeSwing] 已应用音量: {MeleeConfig.Volume:F2}");
                        }
                        catch (Exception volEx)
                        {
                            MeleeLogger.Warning($"[MeleeSwing] 设置音量失败: {volEx.Message}");
                        }
                    }

                    MeleeLogger.Info($"[MeleeSwing] 使用新接口播放: {Path.GetFileName(filePath)}");
                }
                catch (Exception ex)
                {
                    MeleeLogger.Warning($"[MeleeSwing] 新接口播放失败: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                MeleeLogger.Warning($"[MeleeSwing] Postfix 覆盖异常: {ex.Message}");
            }
        }
    }
}
