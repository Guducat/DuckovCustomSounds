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

            // 1. 尝试精确匹配场景名称（带后缀）
            string exactName = $"{cleanName}_{suffix}";
            string? exactPath = FindMusicFile(folder, exactName);
            if (exactPath != null)
            {
                cache[cacheKey] = exactPath;
                SceneBGMLogger.Info($"匹配到场景{label}音乐（精确）: {cleanName} -> {Path.GetFileName(exactPath)} ({exactPath})");
                return exactPath;
            }

            // 1.2 补充规则：精确匹配（无后缀），用于兼容如 "loadingscreen_getout.mp3"
            string exactNoSuffixName = cleanName;
            string? exactNoSuffixPath = FindMusicFile(folder, exactNoSuffixName);
            if (exactNoSuffixPath != null)
            {
                cache[cacheKey] = exactNoSuffixPath;
                SceneBGMLogger.Info($"匹配到场景{label}音乐（无后缀精确）: {cleanName} -> {Path.GetFileName(exactNoSuffixPath)} ({exactNoSuffixPath})");
                return exactNoSuffixPath;
            }

            // 1.3 补充规则：使用 sceneId 精确匹配（带后缀），用于支持如 "level_farm_main_enter.mp3"
            string sceneIdExactName = $"{cleanSceneId}_{suffix}";
            string? sceneIdExactPath = FindMusicFile(folder, sceneIdExactName);
            if (sceneIdExactPath != null)
            {
                cache[cacheKey] = sceneIdExactPath;
                SceneBGMLogger.Info($"匹配到场景{label}音乐（sceneId）: {sceneId} -> {Path.GetFileName(sceneIdExactPath)} ({sceneIdExactPath})");
                return sceneIdExactPath;
            }

            // 1.4 补充规则：使用 sceneId 精确匹配（无后缀）
            string? sceneIdNoSuffixPath = FindMusicFile(folder, cleanSceneId);
            if (sceneIdNoSuffixPath != null)
            {
                cache[cacheKey] = sceneIdNoSuffixPath;
                SceneBGMLogger.Info($"匹配到场景{label}音乐（sceneId无后缀）: {sceneId} -> {Path.GetFileName(sceneIdNoSuffixPath)} ({sceneIdNoSuffixPath})");
                return sceneIdNoSuffixPath;
            }

            // 2. 尝试场景类型匹配
            string? sceneType = GetSceneType(cleanName);
            if (!string.IsNullOrEmpty(sceneType))
            {
                string typeName = $"{sceneType}_{suffix}";
                string? typePath = FindMusicFile(folder, typeName);
                if (typePath != null)
                {
                    cache[cacheKey] = typePath;
                    SceneBGMLogger.Info($"匹配到场景{label}音乐（类型）: {cleanName} -> {Path.GetFileName(typePath)} ({typePath})");
                    return typePath;
                }
            }

            // 3. 回退到默认音乐（但 Enter 对加载场景不使用默认，避免初始黑屏误播）
            if (string.Equals(suffix, "enter", StringComparison.OrdinalIgnoreCase))
            {
                string? typeForDefault = sceneType ?? GetSceneType(cleanName);
                if (string.Equals(typeForDefault, "loading", StringComparison.OrdinalIgnoreCase))
                {
                    SceneBGMLogger.Info($"跳过默认场景{label}音乐（加载界面不播 enter）: {cleanName}");
                    cache[cacheKey] = null;
                    return null;
                }
            }
            string defaultName = $"default_{suffix}";
            string? defaultPath = FindMusicFile(folder, defaultName);
            if (defaultPath != null)
            {
                cache[cacheKey] = defaultPath;
                SceneBGMLogger.Info($"使用默认场景{label}音乐: {cleanName} -> {defaultName} ({defaultPath})");
                return defaultPath;
            }

            // 没有找到任何音乐
            string missingMusicNames = BuildMissingMusicNames(cleanName, cleanSceneId, sceneId ?? string.Empty, sceneType, suffix);
            SceneBGMLogger.Debug($"未找到场景{label}音乐: {cleanName}({missingMusicNames})");
            cache[cacheKey] = null; // 缓存负结果，避免重复查找
            return null;
        }

        private static string BuildMissingMusicNames(string cleanName, string cleanSceneId, string sceneId, string? sceneType, string suffix)
        {
            var names = new List<string>();
            AddMusicName(names, $"{cleanName}_{suffix}");
            AddMusicName(names, cleanName);
            AddMusicName(names, $"{cleanSceneId}_{suffix}");
            AddMusicName(names, cleanSceneId);
            AddMusicName(names, sceneId);
            if (!string.IsNullOrEmpty(sceneType))
            {
                AddMusicName(names, $"{sceneType}_{suffix}");
            }
            AddMusicName(names, $"default_{suffix}");
            return string.Join(", ", names);
        }

        private static void AddMusicName(List<string> names, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            foreach (string existing in names)
            {
                if (string.Equals(existing, name, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            names.Add(name);
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
                { "zero", new List<string> { "zero", "零号区", "0号区" } },
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
