using System;
using System.Collections.Generic;
using System.IO;
using Duckov;
using UnityEngine;

namespace DuckovCustomSounds.CustomEnemySounds
{
    internal static class PathBuilder
    {
        /// <summary>
        /// 通过填充令牌构建候选相对路径（相对于ModBehaviour.ModFolderName）。
        /// 令牌：{enemyType}, {team}, {rank}, {voiceType}, {soundKey}, {ext}
        /// </summary>
        public static IEnumerable<string> BuildCandidates(
            string pattern,
            EnemyContext ctx,
            string soundKey,
            AudioManager.VoiceType voiceType,
            IEnumerable<string> preferredExts,
            bool includePatternWithoutSoundKey)
        {
            if (string.IsNullOrEmpty(pattern)) yield break;
            var team = ctx.GetTeamNormalized();
            var rank = ctx.GetRank();
            var enemyType = Safe(ctx.EnemyType);
            var vt = voiceType.ToString();
            var sk = Safe(soundKey);

            // 包含soundKey的主要模式
            foreach (var ext in preferredExts)
            {
                var pathStr = pattern
                    .Replace("{enemyType}", enemyType)
                    .Replace("{team}", team)
                    .Replace("{rank}", rank)
                    .Replace("{voiceType}", vt)
                    .Replace("{soundKey}", sk)
                    .Replace("{ext}", ext);
                var full1 = EnsureRooted(pathStr);
                CESLogger.Debug($"[CES:Path] cand: {full1}");
                yield return full1;
            }

            // 不包含soundKey的可选模式（回退）
            if (includePatternWithoutSoundKey)
            {
                foreach (var ext in preferredExts)
                {
                    var pathStr = pattern
                        .Replace("_{soundKey}", "")
                        .Replace("{soundKey}_", "")
                        .Replace("{soundKey}", "")
                        .Replace("{enemyType}", enemyType)
                        .Replace("{team}", team)
                        .Replace("{rank}", rank)
                        .Replace("{voiceType}", vt)
                        .Replace("{ext}", ext);
                    var full2 = EnsureRooted(pathStr);
                    CESLogger.Debug($"[CES:Path] fallback-cand: {full2}");
                    yield return full2;
                }
            }
        }

        /// <summary>
        /// 与 BuildCandidates 一致，但允许以原样字符串覆盖 {voiceType}（即无需受枚举限制）。
        /// </summary>
        public static IEnumerable<string> BuildCandidatesWithVoiceTypeString(
            string pattern,
            EnemyContext ctx,
            string soundKey,
            string voiceTypeString,
            IEnumerable<string> preferredExts,
            bool includePatternWithoutSoundKey)
        {
            if (string.IsNullOrEmpty(pattern)) yield break;
            var team = ctx.GetTeamNormalized();
            var rank = ctx.GetRank();
            var enemyType = Safe(ctx.EnemyType);
            var vt = voiceTypeString ?? string.Empty; // 按原样使用
            var sk = Safe(soundKey);

            foreach (var ext in preferredExts)
            {
                var pathStr = pattern
                    .Replace("{enemyType}", enemyType)
                    .Replace("{team}", team)
                    .Replace("{rank}", rank)
                    .Replace("{voiceType}", vt)
                    .Replace("{soundKey}", sk)
                    .Replace("{ext}", ext);
                var full1 = EnsureRooted(pathStr);
                CESLogger.Debug($"[CES:Path] cand: {full1}");
                yield return full1;
            }

            if (includePatternWithoutSoundKey)
            {
                foreach (var ext in preferredExts)
                {
                    var pathStr = pattern
                        .Replace("_{soundKey}", "")
                        .Replace("{soundKey}_", "")
                        .Replace("{soundKey}", "")
                        .Replace("{enemyType}", enemyType)
                        .Replace("{team}", team)
                        .Replace("{rank}", rank)
                        .Replace("{voiceType}", vt)
                        .Replace("{ext}", ext);
                    var full2 = EnsureRooted(pathStr);
                    CESLogger.Debug($"[CES:Path] fallback-cand: {full2}");
                    yield return full2;
                }
            }
        }

        public static bool TryResolveExisting(IEnumerable<string> candidates, bool validateExists, int ownerId, out string chosen, out List<string> tried)
        {
            tried = new List<string>();
            foreach (var c in candidates)
            {
                var normalized = Normalize(c);
                tried.Add(normalized);
                if (!validateExists)
                {
                    CESLogger.Debug($"[CES:Path] skip-exists-check: {normalized}");
                    chosen = normalized; return true;
                }
                try
                {
                    bool exists = File.Exists(normalized);
                    CESLogger.Debug($"[CES:Path] exists={exists}: {normalized}");
                    if (exists)
                    {
                        // 基础文件存在，尝试查找随机变体
                        var final = ChooseRandomVariant(normalized, ownerId, out var _);
                        CESLogger.Debug($"[CES:Path] 选择文件: {final}");
                        chosen = final;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    CESLogger.Debug($"[CES:Path] exists-check-ex: {normalized} => {ex.Message}");
                }
            }
            chosen = null; return false;
        }

        /// <summary>
        /// 若存在同名变体（base_1.ext, base_2.ext, ...，编号需连续），随机在 [0, N) 中选择其一
        /// 0 表示基础文件本身；若出现异常或无变体，返回基础文件
        /// </summary>
        internal static string ChooseRandomVariant(string baseFullPath, int ownerId, out int availableCount)
        {
            availableCount = 1;
            try
            {
                var dir = Path.GetDirectoryName(baseFullPath);
                var name = Path.GetFileNameWithoutExtension(baseFullPath);
                var ext = Path.GetExtension(baseFullPath);
                int count = 1; // 包括基础文件
                for (int i = 1; ; i++)
                {
                    var cand = Path.Combine(dir, $"{name}_{i}{ext}");
                    if (File.Exists(cand)) count++;
                    else break; // 要求编号连续，遇到缺失即停止
                }
                availableCount = count;
                if (count <= 1)
                {
                    return baseFullPath;
                }

                // 是否启用绑定：启用则使用绑定索引；否则随机
                bool bindEnabled = CustomEnemySounds.Config?.BindVariantIndexPerEnemy ?? false;
                if (bindEnabled)
                {
                    var boundIndex = VariantIndexBinder.GetOrAllocate(ownerId, count);
                    int selectedIndex = boundIndex;
                    if (boundIndex >= count)
                    {
                        int fallbackIndex = Mathf.Clamp(boundIndex, 0, count - 1);
                        CESLogger.Debug($"[CES:Variant] 绑定索引 {boundIndex} 超出范围（共 {count} 个变体），回退到索引 {fallbackIndex}");
                        selectedIndex = fallbackIndex;
                    }
                    var selectedBound = selectedIndex == 0 ? baseFullPath : Path.Combine(dir, $"{name}_{selectedIndex}{ext}");
                    CESLogger.Debug($"[CES:Variant] 使用绑定索引 {selectedIndex} 选择文件 -> {selectedBound}");
                    return selectedBound;
                }
                else
                {
                    int index = UnityEngine.Random.Range(0, count);
                    CESLogger.Debug($"[CES:Path] 找到 {count} 个语音变体（包括基础文件），随机选择索引 {index}");
                    var selected = index == 0 ? baseFullPath : Path.Combine(dir, $"{name}_{index}{ext}");
                    CESLogger.Debug($"[CES:Path] 最终选择文件 -> {selected}");
                    return selected;
                }
            }
            catch (Exception ex)
            {
                CESLogger.Debug($"[CES:Path] 变体检测异常：{ex.Message}");
                return baseFullPath;
            }
        }

        private static string EnsureRooted(string relativeOrAbsolute)
        {
            if (string.IsNullOrEmpty(relativeOrAbsolute)) return relativeOrAbsolute;
            if (Path.IsPathRooted(relativeOrAbsolute)) return Normalize(relativeOrAbsolute);
            try
            {
                var combined = Path.Combine(ModBehaviour.ModFolderName, relativeOrAbsolute);
                var full = Path.GetFullPath(combined);
                return Normalize(full);
            }
            catch { return Normalize(Path.Combine(ModBehaviour.ModFolderName, relativeOrAbsolute)); }
        }

        private static string Normalize(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            return path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        }

        private static string Safe(string s)
        {
            if (string.IsNullOrEmpty(s)) return "unknown";
            return s.Replace(' ', '_').Replace(':', '_');
        }
    }
}

