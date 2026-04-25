using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Duckov; // AudioManager

namespace DuckovCustomSounds.CustomKillFeedback
{
    /// <summary>
    /// 击杀/连杀音效：根据击杀上下文选择最优匹配的自定义音频文件。
    /// </summary>
    internal static class KillSoundPlayer
    {
        public static void PlayKill(KillEventContext context)
        {
            try
            {
                string baseDir = Path.Combine(ModBehaviour.ModFolderName, KillFeedbackConfig.BaseFolder);
                if (!Directory.Exists(baseDir))
                {
                    if (KillFeedbackLogger.IsDebugEnabled) KillFeedbackLogger.Debug($"[KF] 未找到目录: {baseDir}");
                    return;
                }

                string[] exts = KillFeedbackConfig.PreferredExtensions ?? new[] { ".mp3", ".wav" };
                string patHeadshot = KillFeedbackConfig.PatternHeadshot ?? "headshot_{n}";
                string patKill = KillFeedbackConfig.PatternKill ?? "kill_{n}";

                string ReplaceNumber(string pattern) =>
                    string.IsNullOrEmpty(pattern) ? string.Empty : pattern.Replace("{n}", context.Streak.ToString());

                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var candidates = new List<string>();

                void AddName(string name)
                {
                    if (string.IsNullOrWhiteSpace(name)) return;
                    name = name.Trim();
                    if (seen.Add(name))
                        candidates.Add(name);
                }

                if (context.IsExplosion)
                {
                    AddName($"grenade_{context.Streak}");
                    AddName("grenade_kill");
                    AddName("grenade");
                }

                if (context.IsMelee)
                {
                    AddName($"melee_{context.Streak}");
                    AddName("melee_kill");
                    AddName("melee");
                }

                if (context.IsHeadshot)
                {
                    if (context.IsGoldenHeadshot)
                    {
                        AddName($"headshot_gold_{context.Streak}");
                        AddName("headshot_gold");
                    }

                    AddName(ReplaceNumber(patHeadshot));
                    AddName("headshot");
                }

                AddName(ReplaceNumber(patKill));
                AddName($"kill{Mathf.Clamp(context.Streak, 1, 8)}");
                AddName("kill");

                var attempts = candidates
                    .SelectMany(n => exts.Select(ext => Path.Combine(baseDir, n + ext)))
                    .ToList();

                string filePath = attempts.FirstOrDefault(File.Exists);
                if (filePath == null)
                {
                    if (KillFeedbackLogger.IsDebugEnabled)
                    {
                        string chain = string.Join(" -> ", attempts.Select(p => $"[{Path.GetFileName(p)}]"));
                        KillFeedbackLogger.Debug($"[KF] 未找到自定义击杀音效: {chain}");
                    }
                    return;
                }

                GameObject owner = null; // 2D 播放
                if (!KillFeedbackConfig.Use2DSound)
                {
                    try { owner = Camera.main != null ? Camera.main.gameObject : null; } catch { owner = null; }
                }

                var ei = AudioManager.PostCustomSFX(filePath, owner, loop: false);
                if (ei.HasValue && ei.Value.isValid())
                {
                    try { ei.Value.setVolume(Mathf.Clamp01(KillFeedbackConfig.VolumeScale)); } catch { }
                    KillFeedbackLogger.Info($"[KF] 播放: {Path.GetFileName(filePath)} (streak={context.Streak}, head={context.IsHeadshot}, melee={context.IsMelee}, explosion={context.IsExplosion})");
                }
                else
                {
                    KillFeedbackLogger.Warning("[KF] PostCustomSFX 返回无效 EventInstance");
                }
            }
            catch (Exception ex)
            {
                KillFeedbackLogger.Warning($"[KF] 播放击杀音效失败: {ex.Message}");
            }
        }
    }
}
