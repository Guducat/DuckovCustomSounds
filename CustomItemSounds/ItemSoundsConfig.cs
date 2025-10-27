using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Duckov.Modding; // ModManager
using DuckovCustomSounds.ModConfig; // ModConfigAPI

namespace DuckovCustomSounds.CustomItemSounds
{
    /// <summary>
    /// CustomItemSounds 的图形化配置桥接与运行时设置
    /// </summary>
    public static class ItemSoundsConfig
    {
        public const string ModName = "CustomItemSounds";

        // 当前生效的设置（运行时读取）
        public static bool Enabled { get; private set; } = true;
        public static float Volume { get; private set; } = 1.0f; // 0~2
        public static bool ReplaceOriginal { get; private set; } = true; // 默认保持原逻辑：静音原事件
        public static string RootDir { get; private set; } = string.Empty; // 为空=使用默认路径
        public static bool EnableFood { get; private set; } = true;
        public static bool EnableMeds { get; private set; } = true;
        public static bool EnableSyringe { get; private set; } = true;

        private static bool _initialized = false;
        private static Action<string> _onChangedHandler = OnOptionsChanged;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            // 先尝试加载一次（即使没有 ModConfig 也能有默认行为）
            LoadFromModConfig();

            // 订阅 Mod 激活事件，以在 ModConfig 加载后注册 UI
            try { ModManager.OnModActivated += OnModActivated; } catch { }

            if (ModConfigAPI.IsAvailable())
            {
                SetupModConfigUI();
                LoadFromModConfig();
            }
        }

        public static void Deinitialize()
        {
            try { ModManager.OnModActivated -= OnModActivated; } catch { }
            try { ModConfigAPI.SafeRemoveOnOptionsChangedDelegate(_onChangedHandler); } catch { }
            _initialized = false;
        }

        private static void OnModActivated(ModInfo info, Duckov.Modding.ModBehaviour behaviour)
        {
            try
            {
                if (info.name == ModConfigAPI.ModConfigName)
                {
                    SetupModConfigUI();
                    LoadFromModConfig();
                }
            }
            catch { }
        }

        private static void SetupModConfigUI()
        {
            if (!ModConfigAPI.IsAvailable()) return;

            // 注册一次变更回调
            ModConfigAPI.SafeAddOnOptionsChangedDelegate(_onChangedHandler);

            // 简单中文描述（如需国际化可接入 LocalizationManager）
            ModConfigAPI.SafeAddBoolDropdownList(ModName, "enabled", "启用 CustomItemSounds", Enabled);
            ModConfigAPI.SafeAddInputWithSlider(ModName, "volume", "音量倍率 (0~2)", typeof(float), Volume, new Vector2(0f, 2f));
            // ModConfigAPI.SafeAddBoolDropdownList(ModName, "replaceOriginal", "替换原游戏音效(静音原事件)", ReplaceOriginal);
            // ModConfigAPI.SafeAddInputWithSlider(ModName, "rootDir", "音效根目录(留空=默认)", typeof(string), string.IsNullOrEmpty(RootDir) ? "" : RootDir, null);

            // 分类开关
            ModConfigAPI.SafeAddBoolDropdownList(ModName, "enable_food", "启用 食物/饮料 声音", EnableFood);
            ModConfigAPI.SafeAddBoolDropdownList(ModName, "enable_meds", "启用 药品 声音", EnableMeds);
            ModConfigAPI.SafeAddBoolDropdownList(ModName, "enable_syringe", "启用 注射器 声音", EnableSyringe);
        }

        private static void OnOptionsChanged(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (!key.StartsWith(ModName + "_", StringComparison.OrdinalIgnoreCase)) return;
            LoadFromModConfig();
        }

        private static void LoadFromModConfig()
        {
            // 当 ModConfig 不可用时，SafeLoad 会返回默认值，不抛异常
            Enabled = ModConfigAPI.SafeLoad<bool>(ModName, "enabled", Enabled);
            Volume = Mathf.Clamp(ModConfigAPI.SafeLoad<float>(ModName, "volume", Volume), 0f, 2f);
            ReplaceOriginal = ModConfigAPI.SafeLoad<bool>(ModName, "replaceOriginal", ReplaceOriginal);
            RootDir = ModConfigAPI.SafeLoad<string>(ModName, "rootDir", RootDir ?? string.Empty) ?? string.Empty;

            EnableFood = ModConfigAPI.SafeLoad<bool>(ModName, "enable_food", EnableFood);
            EnableMeds = ModConfigAPI.SafeLoad<bool>(ModName, "enable_meds", EnableMeds);
            EnableSyringe = ModConfigAPI.SafeLoad<bool>(ModName, "enable_syringe", EnableSyringe);
        }

        public static string GetBaseDir()
        {
            if (!string.IsNullOrWhiteSpace(RootDir))
            {
                // 兼容相对路径（相对于游戏工作目录）
                try { return Path.GetFullPath(RootDir); } catch { return RootDir; }
            }
            return Path.Combine(ModBehaviour.ModFolderName, "CustomItemSounds");
        }

        public static bool IsCategoryEnabled(string category)
        {
            if (string.IsNullOrWhiteSpace(category)) return true;
            switch (category.ToLowerInvariant())
            {
                case "food": return EnableFood;
                case "meds": return EnableMeds;
                case "syringe": return EnableSyringe;
                default: return true;
            }
        }
    }
}

