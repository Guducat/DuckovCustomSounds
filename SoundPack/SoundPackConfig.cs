using System;
using System.Linq;
using DuckovCustomSounds.ModConfig;
using DuckovCustomSounds.Logging;
using System.Collections.Generic;

namespace DuckovCustomSounds.SoundPack
{
    /// <summary>
    /// 声音包配置 - ModConfig UI 集成
    /// </summary>
    public static class SoundPackConfig
    {
        private const string ModName = "DuckovCustomSounds";
        private static readonly ILog Log = LogManager.GetLogger("SoundPack");

        private static readonly Action<string> _onChangedHandler = OnOptionsChanged;
        private static bool _initialized;

        /// <summary>
        /// 初始化 ModConfig UI（在 ModBehaviour.OnEnable 中调用）
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
            {
                Log.Debug("声音包配置已初始化，跳过");
                return;
            }

            try
            {
                // 检查 ModConfig 是否可用
                if (!ModConfigAPI.IsAvailable())
                {
                    Log.Info("ModConfig 不可用，跳过 UI 集成");
                    return;
                }

                Log.Info("初始化声音包 ModConfig UI...");

                // 注册配置变更回调
                ModConfigAPI.SafeAddOnOptionsChangedDelegate(_onChangedHandler);

                // 添加声音包选择下拉列表
                RegisterSoundPackDropdown();

                _initialized = true;
                Log.Info("声音包 ModConfig UI 初始化完成");
            }
            catch (Exception ex)
            {
                Log.Error("初始化声音包 ModConfig UI 失败", ex);
            }
        }

        /// <summary>
        /// 注册声音包选择下拉列表
        /// </summary>
        private static void RegisterSoundPackDropdown()
        {
            try
            {
                // 获取所有可用的声音包 ID
                var packIds = SoundPackManager.GetAvailablePackIds();
                var packNames = SoundPackManager.GetAvailablePackNames();

                if (packIds.Count == 0)
                {
                    Log.Warning("没有可用的声音包，跳过 UI 注册");
                    return;
                }

                // 找到当前声音包在列表中的索引
                string currentPackId = SoundPackManager.CurrentPackId;
                int currentIndex = packIds.IndexOf(currentPackId);
                if (currentIndex < 0)
                {
                    // 未找到，默认选择第一个（Default）
                    currentIndex = 0;
                }

                // 构建下拉列表选项（SortedDictionary<string, object>）
                // 键 = 显示名称，值 = 索引
                var soundPackOptions = new System.Collections.Generic.SortedDictionary<string, object>(StringComparer.Ordinal);
                for (int i = 0; i < packNames.Count; i++)
                {
                    soundPackOptions[packNames[i]] = i;
                }

                // 注册下拉列表
                // description: 更换声音包（需重启游戏）
                // options: SortedDictionary<显示名称, 索引>
                // valueType: typeof(int)
                // defaultValue: 当前索引
                ModConfigAPI.SafeAddDropdownList(
                    modName: ModName,
                    key: "soundPack",
                    description: "更换声音包（需重启游戏）",
                    options: soundPackOptions,
                    valueType: typeof(int),
                    defaultValue: currentIndex
                );

                Log.Info($"已注册声音包下拉列表，共 {packNames.Count} 个选项，当前选择: {packNames[currentIndex]}");
            }
            catch (Exception ex)
            {
                Log.Error("注册声音包下拉列表失败", ex);
            }
        }

        /// <summary>
        /// ModConfig 配置变更回调
        /// </summary>
        private static void OnOptionsChanged(string key)
        {
            try
            {
                // 检查是否是声音包选项变更
                if (key != "soundPack" && !string.IsNullOrEmpty(key))
                {
                    return; // 不是声音包选项，忽略
                }

                Log.Debug($"配置变更回调: key={key}");

                // 从 ModConfig 读取用户选择的声音包索引
                var packIds = SoundPackManager.GetAvailablePackIds();
                var packNames = SoundPackManager.GetAvailablePackNames();

                if (packIds.Count == 0)
                {
                    Log.Warning("没有可用的声音包");
                    return;
                }

                int selectedIndex = ModConfigAPI.SafeLoad<int>(ModName, "soundPack", 0);

                // 验证索引有效性
                if (selectedIndex < 0 || selectedIndex >= packIds.Count)
                {
                    Log.Warning($"无效的声音包索引: {selectedIndex}，重置为 0");
                    selectedIndex = 0;
                }

                string selectedPackId = packIds[selectedIndex];
                string selectedPackName = packNames[selectedIndex];

                // 检查是否真的变更了
                string currentPackId = SoundPackManager.CurrentPackId;
                if (selectedPackId == currentPackId)
                {
                    Log.Debug($"声音包未变更: {selectedPackName}");
                    return;
                }

                // 保存到 settings.json（重启后生效）
                SoundPackManager.SetCurrentPack(selectedPackId);

                Log.Info($"用户选择了新的声音包: {selectedPackName}（将在重启游戏后生效）");
            }
            catch (Exception ex)
            {
                Log.Error("处理配置变更失败", ex);
            }
        }

        /// <summary>
        /// 获取当前声音包显示信息（用于调试）
        /// </summary>
        public static string GetCurrentPackInfo()
        {
            try
            {
                string packId = SoundPackManager.CurrentPackId;
                var packInfo = SoundPackManager.GetPackInfo(packId);
                if (packInfo != null)
                {
                    return packInfo.GetDisplayText();
                }
                return "Default";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
