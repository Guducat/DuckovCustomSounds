using System;
using UnityEngine;
using DuckovCustomSounds.ModConfig;

namespace DuckovCustomSounds.CustomBGM.HomeBGM
{
    /// <summary>
    /// HomeBGM 配置管理
    /// 优先使用 ModConfig UI 配置（支持热重载）
    /// 回退到 settings.json
    /// </summary>
    internal static class HomeBGMConfig
    {
        private const string ModName = "HomeBGM";

        // ModConfig UI 配置项
        public static bool Enabled { get; private set; } = true;
        public static bool EnableStartMusic { get; private set; } = true; // start.mp3 播放开关
        public static bool RandomEnabled { get; private set; } = false;
        public static bool RandomizePrevious { get; private set; } = false;
        public static bool AvoidImmediateRepeat { get; private set; } = true;
        public static bool AutoPlayNext { get; private set; } = true;
        public static float Volume { get; private set; } = 1.0f; // 音量控制 (0.0-1.0)
        public static bool UseSFXBus { get; private set; } = false; // 使用 SFX 总线播放（实验性）

        private static readonly Action<string> _onChangedHandler = OnOptionsChanged;
        private static bool _initialized;

        /// <summary>
        /// 初始化配置
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                // 1. 从 settings.json 加载默认配置
                LoadFromSettingsFallback();

                // 2. 如果 ModConfig 可用，注册 UI 与变更回调
                if (ModConfigAPI.IsAvailable())
                {
                    try
                    {
                        SetupModConfigUI();
                        ModConfigAPI.SafeAddOnOptionsChangedDelegate(_onChangedHandler);
                        // 初始从 ModConfig 拉取一次，覆盖默认值
                        LoadFromModConfig();
                        HomeBGMLogger.Debug("已集成 ModConfig UI");
                    }
                    catch (Exception ex)
                    {
                        // ModConfig 集成失败（意外情况），静默回退到默认配置
                        HomeBGMLogger.Debug($"ModConfig 集成异常（已回退到默认配置）: {ex.Message}");
                    }
                }
                else
                {
                    // ModConfig 不可用（正常兼容场景），静默使用默认配置
                    HomeBGMLogger.Debug("ModConfig 不可用，使用默认配置");
                }

                HomeBGMLogger.Info($"配置加载完成: Enabled={Enabled}, Random={RandomEnabled}, AutoNext={AutoPlayNext}");
            }
            catch (Exception ex)
            {
                HomeBGMLogger.Error("配置加载失败，使用默认配置", ex);
            }
        }

        /// <summary>
        /// 设置 ModConfig UI
        /// </summary>
        private static void SetupModConfigUI()
        {
            ModConfigAPI.SafeAddBoolDropdownList(ModName, "enabled", "启用主页BGM", Enabled);
            ModConfigAPI.SafeAddBoolDropdownList(ModName, "enableStartMusic", "启用进入基地音效 (start.mp3)", EnableStartMusic);
            ModConfigAPI.SafeAddInputWithSlider(ModName, "volume", "音乐音量 (%)", typeof(int), (int)(Volume * 100), new UnityEngine.Vector2(0, 100));
            ModConfigAPI.SafeAddBoolDropdownList(ModName, "useSFXBus", "使用 SFX 总线播放（实验性，可能改善立体声效果，但受 SFX 音量控制影响）", UseSFXBus);
            ModConfigAPI.SafeAddBoolDropdownList(ModName, "randomEnabled", "随机播放(Next)", RandomEnabled);
            ModConfigAPI.SafeAddBoolDropdownList(ModName, "randomizePrevious", "上一曲也随机", RandomizePrevious);
            ModConfigAPI.SafeAddBoolDropdownList(ModName, "avoidImmediateRepeat", "避免连续重复同一曲目", AvoidImmediateRepeat);
            ModConfigAPI.SafeAddBoolDropdownList(ModName, "autoPlayNext", "自动播放下一曲", AutoPlayNext);
        }

        /// <summary>
        /// 从 ModConfig 加载配置
        /// </summary>
        private static void LoadFromModConfig()
        {
            Enabled = ModConfigAPI.SafeLoad(ModName, "enabled", Enabled);
            EnableStartMusic = ModConfigAPI.SafeLoad(ModName, "enableStartMusic", EnableStartMusic);

            // 加载音量配置（0-100 整数，转换为 0.0-1.0 浮点数）
            int volumePercent = ModConfigAPI.SafeLoad(ModName, "volume", (int)(Volume * 100));
            Volume = Mathf.Clamp01(volumePercent / 100f);

            UseSFXBus = ModConfigAPI.SafeLoad(ModName, "useSFXBus", UseSFXBus);
            RandomEnabled = ModConfigAPI.SafeLoad(ModName, "randomEnabled", RandomEnabled);
            RandomizePrevious = ModConfigAPI.SafeLoad(ModName, "randomizePrevious", RandomizePrevious);
            AvoidImmediateRepeat = ModConfigAPI.SafeLoad(ModName, "avoidImmediateRepeat", AvoidImmediateRepeat);
            AutoPlayNext = ModConfigAPI.SafeLoad(ModName, "autoPlayNext", AutoPlayNext);
        }

        /// <summary>
        /// ModConfig 变更回调（热重载）
        /// </summary>
        private static void OnOptionsChanged(string key)
        {
            var oldEnabled = Enabled;
            var oldRandom = RandomEnabled;
            var oldAutoNext = AutoPlayNext;
            var oldVolume = Volume;
            var oldUseSFXBus = UseSFXBus;

            LoadFromModConfig();

            // 如果音量变化，应用到当前播放的 BGM
            if (Math.Abs(oldVolume - Volume) > 0.01f)
            {
                HomeBGMLogger.Info($"音量已更新: {oldVolume:F2} -> {Volume:F2}");
                HomeBGMManager.ApplyVolumeToCurrentBGM();
            }

            // 如果 SFX 总线设置变化，重新播放当前 BGM
            if (oldUseSFXBus != UseSFXBus)
            {
                HomeBGMLogger.Info($"SFX 总线设置已更新: {oldUseSFXBus} -> {UseSFXBus}");
                HomeBGMManager.ReplayCurrentBGM();
            }

            // 如果启用状态切换，记录日志
            if (oldEnabled != Enabled)
            {
                HomeBGMLogger.Info($"配置已热重载: Enabled {oldEnabled} → {Enabled}");
            }

            // 如果随机或自动切歌设置变更，记录日志
            if (oldRandom != RandomEnabled || oldAutoNext != AutoPlayNext)
            {
                HomeBGMLogger.Debug($"配置已热重载: Random={RandomEnabled}, AutoNext={AutoPlayNext}");
            }
        }

        /// <summary>
        /// 从 settings.json 加载默认配置
        /// </summary>
        private static void LoadFromSettingsFallback()
        {
            try
            {
                // 从 ModSettings 读取旧配置
                RandomEnabled = DuckovCustomSounds.ModSettings.HomeBgmRandomEnabled;
                RandomizePrevious = DuckovCustomSounds.ModSettings.HomeBgmRandomizePrevious;
                AvoidImmediateRepeat = DuckovCustomSounds.ModSettings.HomeBgmRandomNoRepeat;
                AutoPlayNext = DuckovCustomSounds.ModSettings.HomeBgmAutoPlayNext;
                Enabled = true; // 默认启用

                HomeBGMLogger.Debug("从 settings.json 加载配置");
            }
            catch (Exception ex)
            {
                HomeBGMLogger.Warning($"settings.json 回退加载失败: {ex.Message}");
            }
        }
    }
}
