using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using DuckovCustomSounds.CustomBGM.Core;
using DuckovCustomSounds.Logging;

namespace DuckovCustomSounds.SoundPack
{
    /// <summary>
    /// 声音包管理器 - 负责扫描、加载和管理声音包
    /// </summary>
    public static class SoundPackManager
    {
        private static readonly ILog Log = LogManager.GetLogger("SoundPack");

        // 根目录常量（永远不变）
        private const string RootFolderName = "DuckovCustomSounds";

        // Default 包的特殊 ID
        private const string DefaultPackId = "";
        private const string DefaultPackDisplayName = "Default";

        // 当前激活的声音包
        private static string _currentPackId = DefaultPackId;
        private static Dictionary<string, SoundPackInfo> _availablePacks = new Dictionary<string, SoundPackInfo>();
        private static bool _initialized = false;

        /// <summary>
        /// 获取当前声音包的完整路径（供 ModBehaviour.ModFolderName 使用）
        /// </summary>
        public static string CurrentPackPath
        {
            get
            {
                if (string.IsNullOrEmpty(_currentPackId) || _currentPackId == DefaultPackId)
                {
                    // Default 包 = 根目录
                    return RootFolderName;
                }
                else
                {
                    // 其他声音包 = DuckovCustomSounds\[PackName]
                    return Path.Combine(RootFolderName, _currentPackId);
                }
            }
        }

        /// <summary>
        /// 获取根目录路径（用于 settings.json 等全局配置）
        /// </summary>
        public static string RootPath => RootFolderName;

        /// <summary>
        /// 获取当前声音包 ID
        /// </summary>
        public static string CurrentPackId => _currentPackId;

        /// <summary>
        /// 获取所有可用的声音包列表
        /// </summary>
        public static IReadOnlyList<SoundPackInfo> AvailablePacks => _availablePacks.Values.ToList();

        /// <summary>
        /// 初始化声音包系统（在 ModBehaviour.OnEnable 最开始调用）
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
            {
                Log.Info("声音包系统已初始化，跳过");
                return;
            }

            try
            {
                Log.Info("初始化声音包系统...");

                // 1. 扫描所有可用的声音包
                ScanAvailablePacks();

                // 2. 从 settings.json 读取用户选择的声音包
                LoadCurrentPackFromSettings();

                // 3. 验证声音包有效性
                if (!ValidateCurrentPack())
                {
                    Log.Warning($"声音包 '{_currentPackId}' 无效，已回退到 Default");
                    _currentPackId = DefaultPackId;
                }

                _initialized = true;
                Log.Info($"声音包系统初始化完成，当前声音包: {GetCurrentPackDisplayName()}");
                Log.Info($"可用声音包数量: {_availablePacks.Count}");
            }
            catch (Exception ex)
            {
                Log.Error("声音包系统初始化失败", ex);
                // 失败时使用 Default 包
                _currentPackId = DefaultPackId;
                _initialized = true;
            }
        }

        /// <summary>
        /// 扫描所有可用的声音包
        /// </summary>
        private static void ScanAvailablePacks()
        {
            _availablePacks.Clear();

            try
            {
                // 1. 检查根目录是否存在
                if (!Directory.Exists(RootFolderName))
                {
                    Log.Warning($"根目录不存在: {RootFolderName}");
                    return;
                }

                // 2. 检查根目录是否有音频文件（向后兼容：作为 Default 包）
                if (IsDefaultPackAvailable())
                {
                    var defaultPack = new SoundPackInfo
                    {
                        Id = DefaultPackId,
                        Name = DefaultPackDisplayName,
                        Author = "Unknown",
                        Version = "1.0.0",
                        Description = "默认声音包（向后兼容）"
                    };
                    _availablePacks[DefaultPackId] = defaultPack;
                    Log.Info("检测到 Default 声音包（根目录）");
                }

                // 3. 扫描子文件夹中的声音包（必须有 pack.json）
                var subDirs = Directory.GetDirectories(RootFolderName);
                foreach (var dir in subDirs)
                {
                    string packJsonPath = Path.Combine(dir, "pack.json");
                    if (File.Exists(packJsonPath))
                    {
                        try
                        {
                            var packInfo = LoadPackInfo(dir, packJsonPath);
                            if (packInfo != null && packInfo.IsValid())
                            {
                                _availablePacks[packInfo.Id] = packInfo;
                                Log.Info($"检测到声音包: {packInfo.GetDisplayText()}");
                            }
                            else
                            {
                                Log.Warning($"声音包元数据无效: {packJsonPath}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Warning($"加载声音包元数据失败: {packJsonPath} - {ex.Message}");
                        }
                    }
                }

                Log.Info($"声音包扫描完成，共找到 {_availablePacks.Count} 个声音包");
            }
            catch (Exception ex)
            {
                Log.Error("扫描声音包失败", ex);
            }
        }

        /// <summary>
        /// 检查根目录是否有音频文件（判断是否有 Default 包）
        /// </summary>
        private static bool IsDefaultPackAvailable()
        {
            try
            {
                // 检查根目录下是否有典型的音频文件夹
                string[] typicalFolders = { "HomeBGM", "BossBGM", "SceneBGM", "TitleBGM" };
                foreach (var folder in typicalFolders)
                {
                    string folderPath = Path.Combine(RootFolderName, folder);
                    if (Directory.Exists(folderPath))
                    {
                        if (AudioFileExtensions.GetMusicFiles(folderPath).Any())
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 从 pack.json 加载声音包信息
        /// </summary>
        private static SoundPackInfo? LoadPackInfo(string packFolder, string packJsonPath)
        {
            try
            {
                string json = File.ReadAllText(packJsonPath);
                var packInfo = JsonConvert.DeserializeObject<SoundPackInfo>(json);
                if (packInfo != null)
                {
                    // 设置 ID 为文件夹名称
                    packInfo.Id = Path.GetFileName(packFolder);
                    return packInfo;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"解析 pack.json 失败: {packJsonPath} - {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 从 settings.json 读取用户选择的声音包
        /// </summary>
        private static void LoadCurrentPackFromSettings()
        {
            try
            {
                // 这个方法会在 ModSettings.Initialize() 之前调用，所以我们直接读取 settings.json
                string settingsPath = Path.Combine(RootFolderName, "settings.json");
                if (File.Exists(settingsPath))
                {
                    string json = File.ReadAllText(settingsPath);
                    var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    if (settings != null && settings.TryGetValue("currentSoundPack", out var packId))
                    {
                        _currentPackId = packId?.ToString() ?? DefaultPackId;
                        Log.Info($"从 settings.json 读取声音包: {_currentPackId}");
                    }
                    else
                    {
                        _currentPackId = DefaultPackId;
                        Log.Info("settings.json 中未找到 currentSoundPack，使用 Default");
                    }
                }
                else
                {
                    _currentPackId = DefaultPackId;
                    Log.Info("settings.json 不存在，使用 Default");
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"读取 settings.json 失败: {ex.Message}，使用 Default");
                _currentPackId = DefaultPackId;
            }
        }

        /// <summary>
        /// 验证当前声音包是否有效
        /// </summary>
        public static bool ValidateCurrentPack()
        {
            // Default 包永远有效（向后兼容）
            if (string.IsNullOrEmpty(_currentPackId) || _currentPackId == DefaultPackId)
            {
                return true;
            }

            // 检查声音包是否在可用列表中
            if (!_availablePacks.ContainsKey(_currentPackId))
            {
                Log.Warning($"声音包 '{_currentPackId}' 不在可用列表中");
                return false;
            }

            // 检查声音包文件夹是否存在
            string packPath = Path.Combine(RootFolderName, _currentPackId);
            if (!Directory.Exists(packPath))
            {
                Log.Warning($"声音包文件夹不存在: {packPath}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 获取当前声音包的显示名称
        /// </summary>
        public static string GetCurrentPackDisplayName()
        {
            if (string.IsNullOrEmpty(_currentPackId) || _currentPackId == DefaultPackId)
            {
                return DefaultPackDisplayName;
            }

            if (_availablePacks.TryGetValue(_currentPackId, out var packInfo))
            {
                return packInfo.Name;
            }

            return _currentPackId;
        }

        /// <summary>
        /// 获取声音包信息（用于 ModConfig UI 显示）
        /// </summary>
        public static SoundPackInfo GetPackInfo(string packId)
        {
            if (string.IsNullOrEmpty(packId))
                packId = DefaultPackId;

            _availablePacks.TryGetValue(packId, out var info);
            return info;
        }

        /// <summary>
        /// 获取所有声音包的 ID 列表（用于 ModConfig UI 下拉列表）
        /// </summary>
        public static List<string> GetAvailablePackIds()
        {
            return _availablePacks.Keys.OrderBy(id =>
            {
                // Default 排在第一位
                if (id == DefaultPackId) return "0";
                return "1" + id;
            }).ToList();
        }

        /// <summary>
        /// 获取所有声音包的显示名称列表（用于 ModConfig UI）
        /// </summary>
        public static List<string> GetAvailablePackNames()
        {
            var ids = GetAvailablePackIds();
            return ids.Select(id =>
            {
                if (id == DefaultPackId) return DefaultPackDisplayName;
                if (_availablePacks.TryGetValue(id, out var info))
                    return info.Name;
                return id;
            }).ToList();
        }

        /// <summary>
        /// 设置当前声音包（保存到 settings.json，重启后生效）
        /// </summary>
        public static void SetCurrentPack(string packId)
        {
            try
            {
                if (packId == null)
                    packId = DefaultPackId;

                Log.Info($"切换声音包: {_currentPackId} → {packId}");

                // 注意：这里只保存到 settings.json，不立即切换
                // 需要重启游戏后才会生效
                string settingsPath = Path.Combine(RootFolderName, "settings.json");

                Newtonsoft.Json.Linq.JObject root = new Newtonsoft.Json.Linq.JObject();
                if (File.Exists(settingsPath))
                {
                    try
                    {
                        string json = File.ReadAllText(settingsPath);
                        root = Newtonsoft.Json.Linq.JObject.Parse(json);
                    }
                    catch
                    {
                        // 解析失败，使用空对象
                    }
                }

                root["currentSoundPack"] = packId;
                File.WriteAllText(settingsPath, root.ToString(Formatting.Indented));

                Log.Info($"声音包设置已保存: {packId}（需重启游戏生效）");
            }
            catch (Exception ex)
            {
                Log.Error("保存声音包设置失败", ex);
            }
        }
    }
}
