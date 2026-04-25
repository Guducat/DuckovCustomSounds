using System;
using System.Collections.Generic;
using System.IO;
using DuckovCustomSounds.CustomEnemySounds.Context;

namespace DuckovCustomSounds.CustomBGM.BossBGM
{
    /// <summary>
    /// BOSS 音乐文件匹配与路径解析
    /// 根据 EnemyContext.NameKey 解析音乐文件路径
    /// </summary>
    internal static class BossMusicResolver
    {
        private static string bossBGMFolder;
        private static Dictionary<string, string> pathCache = new Dictionary<string, string>();
        private static HashSet<string> availableMusic = new HashSet<string>();

        /// <summary>
        /// 是否有可用的音乐文件（用于判断是否应该启用 BOSS BGM 功能）
        /// </summary>
        public static bool HasAnyMusic => availableMusic.Count > 0;

        /// <summary>
        /// 初始化 - 扫描音乐文件夹并建立缓存
        /// </summary>
        public static void Initialize()
        {
            try
            {
                bossBGMFolder = Path.Combine(ModBehaviour.ModFolderName, "BossBGM");

                if (!Directory.Exists(bossBGMFolder))
                {
                    Directory.CreateDirectory(bossBGMFolder);
                    BossBGMLogger.Info($"已创建 BossBGM 文件夹: {bossBGMFolder}");
                }

                // 扫描所有音乐文件
                ScanMusicFiles();

                if (availableMusic.Count == 0)
                {
                    BossBGMLogger.Warning("未找到任何 BOSS 音乐文件，BOSS BGM 功能将被禁用（请放置至少一个音乐文件到 BossBGM 文件夹）");
                }
                else
                {
                    BossBGMLogger.Info($"BossMusicResolver 初始化完成，找到 {availableMusic.Count} 个音乐文件");
                }
            }
            catch (Exception ex)
            {
                BossBGMLogger.Error("BossMusicResolver 初始化失败", ex);
            }
        }

        /// <summary>
        /// 扫描音乐文件夹中的所有音乐文件
        /// </summary>
        private static void ScanMusicFiles()
        {
            availableMusic.Clear();

            if (!Directory.Exists(bossBGMFolder))
                return;

            string[] extensions = { "*.mp3", "*.wav", "*.ogg", "*.flac" };

            foreach (string ext in extensions)
            {
                try
                {
                    string[] files = Directory.GetFiles(bossBGMFolder, ext, SearchOption.TopDirectoryOnly);
                    foreach (string file in files)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(file);
                        availableMusic.Add(fileName.ToLowerInvariant());
                        BossBGMLogger.Debug($"发现音乐文件: {fileName}");
                    }
                }
                catch (Exception ex)
                {
                    BossBGMLogger.Warning($"扫描文件失败 ({ext}): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 解析 BOSS 音乐文件路径
        /// 匹配规则：
        /// 1. 优先匹配 nameKey（去除 "Cname_" 前缀）如 "BALeader.mp3"
        /// 2. 回退到 "default_boss.mp3"
        /// 3. 都不存在返回 null
        /// </summary>
        public static string ResolveMusicPath(EnemyContext ctx)
        {
            if (ctx == null)
                return null;

            string nameKey = ctx.NameKey ?? "unknown";

            // 检查缓存
            if (pathCache.TryGetValue(nameKey, out string cached))
            {
                return cached;
            }

            // 清理 nameKey（移除 "Cname_" 前缀）
            string cleanName = CleanNameKey(nameKey);

            BossBGMLogger.Debug($"解析音乐路径: nameKey={nameKey}, cleanName={cleanName}");

            // 尝试匹配特定 BOSS 音乐
            string specificPath = FindMusicFile(cleanName);
            if (specificPath != null)
            {
                pathCache[nameKey] = specificPath;
                BossBGMLogger.Info($"匹配到 BOSS 音乐: {nameKey} -> {Path.GetFileName(specificPath)}");
                return specificPath;
            }

            // 回退到默认 BOSS 音乐
            string defaultPath = FindMusicFile("default_boss");
            if (defaultPath != null)
            {
                pathCache[nameKey] = defaultPath;
                BossBGMLogger.Info($"使用默认 BOSS 音乐: {nameKey} -> default_boss.mp3");
                return defaultPath;
            }

            // 没有找到任何音乐
            BossBGMLogger.Warning($"未找到 BOSS 音乐: {nameKey}（请提供 {cleanName}.mp3 或 default_boss.mp3）");
            pathCache[nameKey] = null; // 缓存负结果，避免重复查找
            return null;
        }

        /// <summary>
        /// 清理 nameKey（移除 "Cname_" 前缀）
        /// </summary>
        private static string CleanNameKey(string nameKey)
        {
            if (string.IsNullOrEmpty(nameKey))
                return "unknown";

            // 移除 "Cname_" 前缀
            if (nameKey.StartsWith("Cname_", StringComparison.OrdinalIgnoreCase))
            {
                return nameKey.Substring(6); // "Cname_".Length = 6
            }

            return nameKey;
        }

        /// <summary>
        /// 查找音乐文件（按优先级尝试不同扩展名）
        /// </summary>
        private static string FindMusicFile(string baseName)
        {
            string[] extensions = { ".mp3", ".wav", ".ogg", ".flac" };

            foreach (string ext in extensions)
            {
                string filePath = Path.Combine(bossBGMFolder, baseName + ext);
                if (File.Exists(filePath))
                {
                    return filePath;
                }
            }

            return null;
        }

        /// <summary>
        /// 清除路径缓存（用于热重载）
        /// </summary>
        public static void ClearCache()
        {
            pathCache.Clear();
            BossBGMLogger.Debug("路径缓存已清除");
        }

        /// <summary>
        /// 获取所有可用的 BOSS 音乐文件名（用于调试）
        /// </summary>
        public static List<string> GetAvailableMusic()
        {
            return new List<string>(availableMusic);
        }
    }
}
