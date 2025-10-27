using System;
using UnityEngine;
using DuckovCustomSounds.ModConfig;

namespace DuckovCustomSounds.CustomBGM
{
    /// <summary>
    /// 自定义 BGM 的图形化配置与运行时选项（优先使用 ModConfig；无 ModConfig 时回退到 settings.json）。
    /// </summary>
    public static class BGMConfig
    {
        public const string ModName = "CustomBGM";

        // 运行时可读设置
        public static bool RandomEnabled { get; private set; } = false;
        public static bool RandomizePrevious { get; private set; } = false; // 默认仅 Next 随机
        public static bool AvoidImmediateRepeat { get; private set; } = true; // 默认避免连播同一首

        private static readonly Action<string> _onChangedHandler = OnOptionsChanged;
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            // 1) 设置默认值：从 settings.json 读取（若不存在则使用默认）
            LoadFromSettingsFallback();

            // 2) 若 ModConfig 可用，注册 UI 与变更回调
            if (ModConfigAPI.IsAvailable())
            {
                try
                {
                    ModConfigAPI.SafeAddOnOptionsChangedDelegate(_onChangedHandler);
                    ModConfigAPI.SafeAddBoolDropdownList(ModName, "randomEnabled", "随机播放(Next)", RandomEnabled);
                    ModConfigAPI.SafeAddBoolDropdownList(ModName, "randomizePrevious", "上一曲也随机(可选)", RandomizePrevious);
                    ModConfigAPI.SafeAddBoolDropdownList(ModName, "avoidImmediateRepeat", "避免连续重复同一曲目", AvoidImmediateRepeat);

                    // 初始从 ModConfig 拉取一次，覆盖 fallback 值
                    LoadFromModConfig();
                }
                catch { }
            }
        }

        private static void OnOptionsChanged(string key)
        {
            // ModConfig 可能会传入空 key 或任意 key；这里统一重新加载
            LoadFromModConfig();
        }

        private static void LoadFromModConfig()
        {
            try
            {
                RandomEnabled = ModConfigAPI.SafeLoad<bool>(ModName, "randomEnabled", RandomEnabled);
                RandomizePrevious = ModConfigAPI.SafeLoad<bool>(ModName, "randomizePrevious", RandomizePrevious);
                AvoidImmediateRepeat = ModConfigAPI.SafeLoad<bool>(ModName, "avoidImmediateRepeat", AvoidImmediateRepeat);
            }
            catch { }
        }

        private static void LoadFromSettingsFallback()
        {
            try
            {
                // 来自 settings.json 的回退值
                RandomEnabled = DuckovCustomSounds.ModSettings.HomeBgmRandomEnabled;
                RandomizePrevious = DuckovCustomSounds.ModSettings.HomeBgmRandomizePrevious;
                AvoidImmediateRepeat = DuckovCustomSounds.ModSettings.HomeBgmRandomNoRepeat;
            }
            catch { }
        }
    }
}

