using System;
using System.Collections.Generic;
using System.IO;
using DuckovCustomSounds.CustomBGM.Core;

namespace DuckovCustomSounds.CustomBGM.SceneBGM
{
    /// <summary>
    /// 场景音乐文件匹配与路径解析
    /// 根据场景名称解析进入音乐（Enter）和循环音乐（Loop）的文件路径
    /// </summary>
    internal static class SceneMusicResolver
    {
        private static string sceneBGMFolder = string.Empty;
        private static string enterMusicFolder = string.Empty;
        private static string loopMusicFolder = string.Empty;

        private static Dictionary<string, string?> enterPathCache = new Dictionary<string, string?>();
        private static Dictionary<string, string?> loopPathCache = new Dictionary<string, string?>();

        private static HashSet<string> availableEnterMusic = new HashSet<string>();
        private static HashSet<string> availableLoopMusic = new HashSet<string>();

        /// <summary>
        /// 是否有可用的进入音乐文件
        /// </summary>
        public static bool HasAnyEnterMusic => availableEnterMusic.Count > 0;

        /// <summary>
        /// 是否有可用的循环音乐文件
        /// </summary>
        public static bool HasAnyLoopMusic => availableLoopMusic.Count > 0;

        /// <summary>
        /// 初始化 - 扫描音乐文件夹并建立缓存
        /// </summary>
        public static void Initialize()
        {
            try
            {
                sceneBGMFolder = Path.Combine(ModBehaviour.ModFolderName, "SceneBGM");
                enterMusicFolder = Path.Combine(sceneBGMFolder, "Enter");
                loopMusicFolder = Path.Combine(sceneBGMFolder, "Loop");

                // 创建文件夹（如果不存在）
                EnsureDirectoriesExist();

                // 记录当前使用的声音包与目录（便于排查资源来源）
                SceneBGMLogger.Info($"使用声音包路径: {ModBehaviour.ModFolderName}");
                SceneBGMLogger.Info($"SceneBGM 目录: {sceneBGMFolder} | Enter: {enterMusicFolder} | Loop: {loopMusicFolder}");

                // 扫描所有音乐文件
                ScanMusicFiles();

                int totalFiles = availableEnterMusic.Count + availableLoopMusic.Count;
                if (totalFiles == 0)
                {
                    SceneBGMLogger.Warning("未找到任何场景音乐文件，场景 BGM 功能将被禁用（请放置音乐文件到 SceneBGM/Enter 或 SceneBGM/Loop 文件夹）");
                }
                else
                {
                    SceneBGMLogger.Info($"SceneMusicResolver 初始化完成，找到 {availableEnterMusic.Count} 个进入音乐，{availableLoopMusic.Count} 个循环音乐");
                }
            }
            catch (Exception ex)
            {
                SceneBGMLogger.Error("SceneMusicResolver 初始化失败", ex);
            }
        }

        /// <summary>
        /// 确保目录存在
        /// </summary>
        private static void EnsureDirectoriesExist()
        {
            if (!Directory.Exists(sceneBGMFolder))
            {
                Directory.CreateDirectory(sceneBGMFolder);
                SceneBGMLogger.Info($"已创建 SceneBGM 文件夹: {sceneBGMFolder}");
            }

            if (!Directory.Exists(enterMusicFolder))
            {
                Directory.CreateDirectory(enterMusicFolder);
                SceneBGMLogger.Info($"已创建 Enter 文件夹: {enterMusicFolder}");
            }

            if (!Directory.Exists(loopMusicFolder))
            {
                Directory.CreateDirectory(loopMusicFolder);
                SceneBGMLogger.Info($"已创建 Loop 文件夹: {loopMusicFolder}");
            }
        }

        /// <summary>
        /// 扫描音乐文件夹中的所有音乐文件
        /// </summary>
        private static void ScanMusicFiles()
        {
            availableEnterMusic.Clear();
            availableLoopMusic.Clear();

            // 扫描进入音乐
            ScanFolder(enterMusicFolder, availableEnterMusic, "Enter");

            // 扫描循环音乐
            ScanFolder(loopMusicFolder, availableLoopMusic, "Loop");
        }

        /// <summary>
        /// 扫描指定文件夹
        /// </summary>
        private static void ScanFolder(string folder, HashSet<string> collection, string label)
        {
            if (!Directory.Exists(folder))
                return;

            foreach (string file in AudioFileExtensions.GetMusicFiles(folder))
            {
                try
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    collection.Add(fileName.ToLowerInvariant());
                    SceneBGMLogger.Debug($"发现{label}音乐文件: {fileName}");
                }
                catch (Exception ex)
                {
                    SceneBGMLogger.Warning($"扫描{label}文件失败 ({file}): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 解析场景进入音乐文件路径
        /// 匹配规则：
        /// 1. 精确匹配场景名称（如 "zero_enter.mp3"）
        /// 2. 场景类型匹配（如 "lab_enter.mp3"）
        /// 3. 回退到 "default_enter.mp3"
        /// 4. 都不存在返回 null
        /// </summary>
        public static string? ResolveEnterMusicPath(string? sceneId, string? displayName)
        {
            return ResolveMusicPath(sceneId, displayName, enterMusicFolder, enterPathCache, "enter", "进入");
        }

        /// <summary>
        /// 解析场景循环音乐文件路径
        /// 匹配规则：
        /// 1. 精确匹配场景名称（如 "zero_loop.mp3"）
        /// 2. 场景类型匹配（如 "lab_loop.mp3"）
        /// 3. 回退到 "default_loop.mp3"
        /// 4. 都不存在返回 null
        /// </summary>
        public static string? ResolveLoopMusicPath(string? sceneId, string? displayName)
        {
            return ResolveMusicPath(sceneId, displayName, loopMusicFolder, loopPathCache, "loop", "循环");
        }

        /// <summary>
        /// 解析音乐路径（通用方法）
        /// </summary>
        private static string? ResolveMusicPath(string? sceneId, string? displayName, string folder, Dictionary<string, string?> cache, string suffix, string label)
        {
            string cacheKey = sceneId ?? displayName ?? "unknown";

            // 检查缓存
            if (cache.TryGetValue(cacheKey, out string? cached))
            {
                return cached;
            }

            // 清理场景名称
            string cleanName = CleanSceneName(displayName ?? sceneId ?? "unknown");
            string cleanSceneId = CleanSceneName(sceneId);

            SceneBGMLogger.Debug($"解析{label}音乐路径: sceneId={sceneId}, displayName={displayName}, cleanName={cleanName}");

            var candidates = BuildMusicCandidates(cleanName, cleanSceneId, sceneId ?? string.Empty, suffix);
            string? sceneType = GetFirstSceneType(cleanName, cleanSceneId);

            foreach (var candidate in candidates)
            {
                // Enter 对加载场景不使用默认，避免初始黑屏误播。
                if (candidate.IsDefault &&
                    string.Equals(suffix, "enter", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(sceneType, "loading", StringComparison.OrdinalIgnoreCase))
                {
                    SceneBGMLogger.Info($"跳过默认场景{label}音乐（加载界面不播 enter）: {cleanName}");
                    cache[cacheKey] = null;
                    return null;
                }

                string? path = FindMusicFile(folder, candidate.Name);
                if (path != null)
                {
                    cache[cacheKey] = path;
                    if (candidate.IsDefault)
                    {
                        SceneBGMLogger.Info($"使用默认场景{label}音乐: {cleanName} -> {candidate.Name} ({path})");
                    }
                    else
                    {
                        SceneBGMLogger.Info($"匹配到场景{label}音乐（{candidate.MatchKind}）: {candidate.SourceName} -> {Path.GetFileName(path)} ({path})");
                    }

                    return path;
                }
            }

            // 没有找到任何音乐
            string missingMusicNames = BuildMissingMusicNames(candidates);
            SceneBGMLogger.Debug($"未找到场景{label}音乐: {cleanName}({missingMusicNames})");
            cache[cacheKey] = null; // 缓存负结果，避免重复查找
            return null;
        }

        private static List<MusicCandidate> BuildMusicCandidates(string cleanName, string cleanSceneId, string sceneId, string suffix)
        {
            var candidates = new List<MusicCandidate>();
            AddMusicCandidate(candidates, $"{cleanName}_{suffix}", "精确", cleanName);
            AddMusicCandidate(candidates, cleanName, "无后缀精确", cleanName);
            AddMusicCandidate(candidates, $"{cleanSceneId}_{suffix}", "sceneId", sceneId);
            AddMusicCandidate(candidates, cleanSceneId, "sceneId无后缀", sceneId);

            foreach (string alias in BuildSceneIdAliases(cleanSceneId))
            {
                AddMusicCandidate(candidates, $"{alias}_{suffix}", "sceneId兼容", sceneId);
                AddMusicCandidate(candidates, alias, "sceneId兼容无后缀", sceneId);
            }

            foreach (string type in GetSceneTypes(cleanName, cleanSceneId))
            {
                AddMusicCandidate(candidates, $"{type}_{suffix}", "类型", cleanName);
            }

            AddMusicCandidate(candidates, $"default_{suffix}", "默认", cleanName, isDefault: true);
            return candidates;
        }

        private static string BuildMissingMusicNames(List<MusicCandidate> candidates)
        {
            var names = new List<string>();
            foreach (var candidate in candidates)
            {
                names.Add(candidate.Name);
            }

            return string.Join(", ", names);
        }

        private static void AddMusicCandidate(List<MusicCandidate> candidates, string name, string matchKind, string sourceName, bool isDefault = false)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            foreach (var existing in candidates)
            {
                if (string.Equals(existing.Name, name, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            candidates.Add(new MusicCandidate(name, matchKind, string.IsNullOrWhiteSpace(sourceName) ? name : sourceName, isDefault));
        }

        private static IEnumerable<string> BuildSceneIdAliases(string cleanSceneId)
        {
            if (!TrySplitLevelSceneId(cleanSceneId, out string mapName, out string variant))
                yield break;

            if (IsNumericVariant(variant))
            {
                yield return $"level_{mapName}_main";
            }

            yield return $"level_{mapName}";

            foreach (string mapAlias in GetMapAliases(mapName))
            {
                if (IsNumericVariant(variant) || string.Equals(variant, "main", StringComparison.OrdinalIgnoreCase))
                {
                    yield return $"level_{mapAlias}_main";
                }

                yield return $"level_{mapAlias}";
            }
        }

        private static bool TrySplitLevelSceneId(string cleanSceneId, out string mapName, out string variant)
        {
            mapName = string.Empty;
            variant = string.Empty;

            if (string.IsNullOrWhiteSpace(cleanSceneId))
                return false;

            string[] parts = cleanSceneId.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3 || !string.Equals(parts[0], "level", StringComparison.OrdinalIgnoreCase))
                return false;

            mapName = string.Join("_", parts, 1, parts.Length - 2);
            variant = parts[parts.Length - 1];
            return !string.IsNullOrWhiteSpace(mapName) && !string.IsNullOrWhiteSpace(variant);
        }

        private static bool IsNumericVariant(string variant)
        {
            if (string.IsNullOrWhiteSpace(variant))
                return false;

            foreach (char c in variant)
            {
                if (!char.IsDigit(c))
                    return false;
            }

            return true;
        }

        private static IEnumerable<string> GetMapAliases(string mapName)
        {
            if (string.Equals(mapName, "hiddenwarehouse", StringComparison.OrdinalIgnoreCase))
            {
                yield return "warehouse";
            }
        }

        private static string? GetFirstSceneType(string cleanName, string cleanSceneId)
        {
            foreach (string type in GetSceneTypes(cleanName, cleanSceneId))
            {
                return type;
            }

            return null;
        }

        private static List<string> GetSceneTypes(string cleanName, string cleanSceneId)
        {
            var types = new List<string>();
            AddSceneType(types, GetSceneType(cleanName));
            AddSceneType(types, GetSceneType(cleanSceneId));
            return types;
        }

        private static void AddSceneType(List<string> types, string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return;

            foreach (string existing in types)
            {
                if (string.Equals(existing, type, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            types.Add(type);
        }

        private readonly struct MusicCandidate
        {
            public MusicCandidate(string name, string matchKind, string sourceName, bool isDefault)
            {
                Name = name;
                MatchKind = matchKind;
                SourceName = sourceName;
                IsDefault = isDefault;
            }

            public string Name { get; }
            public string MatchKind { get; }
            public string SourceName { get; }
            public bool IsDefault { get; }
        }

        /// <summary>
        /// 清理场景名称（转换为文件名友好格式）
        /// </summary>
        private static string CleanSceneName(string? sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return "unknown";

            // 移除特殊字符，转换为小写
            string cleaned = sceneName
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace("（", "")
                .Replace("）", "")
                .Replace("(", "")
                .Replace(")", "")
                .ToLowerInvariant();

            return cleaned;
        }

        /// <summary>
        /// 获取场景类型（用于类型匹配）
        /// </summary>
        private static string? GetSceneType(string cleanName)
        {
            // 场景类型映射（可以根据实际游戏场景扩展）
            Dictionary<string, List<string>> typeMapping = new Dictionary<string, List<string>>
            {
                // 通用加载界面（Loading Screen）
                { "loading", new List<string> { "loading", "loadingscreen", "loading_screen", "loadingscreen_getout", "加载", "讀條", "读条" } },
                { "lab", new List<string> { "lab", "实验室", "研究所" } },
                { "factory", new List<string> { "factory", "工厂", "工业区" } },
                { "farm", new List<string> { "farm", "fram", "农场", "农场镇" } },
                { "zero", new List<string> { "zero", "groundzero", "零号区", "0号区" } },
                { "warehouse", new List<string> { "warehouse", "hiddenwarehouse", "仓库", "仓库区" } },
                { "expedition", new List<string> { "expedition", "探险", "任务" } },
                { "outskirts", new List<string> { "outskirts", "郊区", "边缘" } }
            };

            foreach (var kvp in typeMapping)
            {
                foreach (var keyword in kvp.Value)
                {
                    if (cleanName.Contains(keyword))
                    {
                        return kvp.Key;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 查找音乐文件（按优先级尝试不同扩展名）
        /// </summary>
        private static string? FindMusicFile(string folder, string baseName)
        {
            return AudioFileExtensions.FindMusicFile(folder, baseName);
        }

        /// <summary>
        /// 清除路径缓存（用于热重载）
        /// </summary>
        public static void ClearCache()
        {
            enterPathCache.Clear();
            loopPathCache.Clear();
            AudioFileExtensions.ClearCache(enterMusicFolder);
            AudioFileExtensions.ClearCache(loopMusicFolder);
            SceneBGMLogger.Debug("路径缓存已清除");
        }

        /// <summary>
        /// 获取所有可用的进入音乐文件名（用于调试）
        /// </summary>
        public static List<string> GetAvailableEnterMusic()
        {
            return new List<string>(availableEnterMusic);
        }

        /// <summary>
        /// 获取所有可用的循环音乐文件名（用于调试）
        /// </summary>
        public static List<string> GetAvailableLoopMusic()
        {
            return new List<string>(availableLoopMusic);
        }
    }
}
