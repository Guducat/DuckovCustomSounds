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
    /// 近战攻击音效替换
    /// </summary>
    [HarmonyPatch(typeof(AudioManager))]
    public static class AudioManager_Post_MeleeAttackReplace
    {
        private const string MeleeAttackPrefix = "SFX/Combat/Melee/attack_";

        [HarmonyPatch("Post", new Type[] { typeof(string), typeof(GameObject) })]
        [HarmonyPostfix]
        public static void Postfix(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
        {
            try
            {
                // 检查配置是否启用
                if (!MeleeConfig.Enabled) return;

                if (string.IsNullOrEmpty(eventName)) return;
                if (!eventName.StartsWith(MeleeAttackPrefix, StringComparison.OrdinalIgnoreCase)) return;

                string soundKey = eventName.Substring(MeleeAttackPrefix.Length);
                if (string.IsNullOrWhiteSpace(soundKey)) return;

                string dir = Path.Combine(ModBehaviour.ModFolderName, "CustomMeleeSounds");

                // 捕获近战组件
                var (melee, typeIdStr) = MeleeUtil.GetMeleeAndTypeId(gameObject);

                // 构建候选文件列表
                var attempts = new List<string>();
                if (!string.IsNullOrWhiteSpace(typeIdStr))
                {
                    attempts.AddRange(MeleeUtil.ExpandCandidates(dir, typeIdStr));
                    attempts.AddRange(MeleeUtil.ExpandCandidates(dir, soundKey));
                }
                else
                {
                    MeleeLogger.Debug($"[MeleeAttack] 未捕获到 ItemAgent_MeleeWeapon，回退到 soundKey 查找模式");
                    attempts.AddRange(MeleeUtil.ExpandCandidates(dir, soundKey));
                }

                var fallbacks = MeleeUtil.ExpandCandidates(dir, "default").ToList();
                string filePath = MeleeUtil.FindSoundFile(dir, attempts, fallbacks);

                if (filePath == null)
                {
                    string chain = string.Join(" → ", attempts.Select(p => $"[{Path.GetFileName(p)}]").ToArray());
                    MeleeLogger.Debug($"[MeleeAttack] 查找: {chain}, 结果: 未找到");
                    return;
                }

                // 命中基准文件后，尝试差分变体
                try
                {
                    string picked = MeleeUtil.TryPickVariant(filePath);
                    if (!string.Equals(picked, filePath, StringComparison.OrdinalIgnoreCase) && MeleeLogger.IsDebugEnabled)
                    {
                        MeleeLogger.Debug($"[MeleeAttack] 发现变体，替换 {Path.GetFileName(filePath)} → {Path.GetFileName(picked)}");
                    }
                    filePath = picked;
                }
                catch { }

                // 性能优化：仅当Debug日志启用时才构建字符串
                if (MeleeLogger.IsDebugEnabled)
                {
                    string chainStr = string.Join(" → ", attempts.Select(p => $"[{Path.GetFileName(p)}]").ToArray());
                    MeleeLogger.Debug($"[MeleeAttack] TypeID={typeIdStr ?? "N/A"}, soundKey={soundKey}, 查找: {chainStr}, 结果: {Path.GetFileName(filePath)}");
                }

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
                            MeleeLogger.Debug($"[MeleeAttack] 已应用音量: {MeleeConfig.Volume:F2}");
                        }
                        catch (Exception volEx)
                        {
                            MeleeLogger.Warning($"[MeleeAttack] 设置音量失败: {volEx.Message}");
                        }
                    }

                    MeleeLogger.Info($"[MeleeAttack] 使用新接口播放: {Path.GetFileName(filePath)}");
                }
                catch (Exception ex)
                {
                    MeleeLogger.Warning($"[MeleeAttack] 新接口播放失败: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                MeleeLogger.Warning($"[MeleeAttack] Postfix 覆盖异常: {ex.Message}");
            }
        }
    }
}
