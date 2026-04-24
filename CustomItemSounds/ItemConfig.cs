using System;
using System.IO;
using UnityEngine;
using Duckov.Modding; // ModManager
using DuckovCustomSounds.ModConfig; // ModConfigAPI

namespace DuckovCustomSounds.CustomItemSounds
{
    /// <summary>
    /// Item 配置管理
    /// 优先使用 ModConfig UI 配置（支持热重载）
    /// </summary>
    public static class ItemConfig
    {
        private static readonly ModConfigScope Scope = ModConfigScopes.Item;

        // 当前生效的设置（运行时读取）
        public static bool Enabled { get; private set; } = true;
        public static float Volume { get; private set; } = 1.0f; // 0~2
        public static bool ReplaceOriginal { get; private set; } = true; // 默认保持原逻辑：静音原事件
        public static string RootDir { get; private set; } = string.Empty; // 为空=使用默认路径
        public static float MinActionAudibleSeconds { get; private set; } = 0.35f; // 防止Action被过早截断的宽裕期（秒）

        public static bool EnableFood { get; private set; } = true;
        public static bool EnableBandage { get; private set; } = true; // 主分类：bandage（兼容旧称 meds）
        public static bool EnableSyringe { get; private set; } = true;

        private static bool _initialized = false;
        private static Action<string> _onChangedHandler = OnOptionsChanged;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                // 先尝试加载一次（即使没有 ModConfig 也能有默认行为）
                LoadFromModConfig();

                // 订阅 Mod 激活事件，以在 ModConfig 加载后注册 UI
                try { ModManager.OnModActivated += OnModActivated; } catch { }

                if (ModConfigAPI.IsAvailable())
                {
                    SetupModConfigUI();
                    LoadFromModConfig();
                    ItemLogger.Debug("已集成 ModConfig UI");
                }
                else
                {
                    ItemLogger.Debug("ModConfig 不可用，使用默认配置");
                }

                ItemLogger.Info($"配置加载完成: Enabled={Enabled}, Volume={Volume:F2}");
            }
            catch (Exception ex)
            {
                ItemLogger.Error("配置加载失败，使用默认配置", ex);
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
            ModConfigAPI.SafeAddBoolDropdownList(Scope, "enabled", "启用自定义物品音效", Enabled);
            ModConfigAPI.SafeAddInputWithSlider(Scope, "volume", "音量倍率(0~2)", typeof(float), Volume, new Vector2(0f, 2f));
            ModConfigAPI.SafeAddInputWithSlider(Scope, "min_action_audible_sec", "Action最短可听时间(秒)", typeof(float), MinActionAudibleSeconds, new Vector2(0f, 3f));

            // 分类开关
            ModConfigAPI.SafeAddBoolDropdownList(Scope, "enable_food", "启用 食物/饮料 声音", EnableFood);
            ModConfigAPI.SafeAddBoolDropdownList(Scope, "enable_bandage", "启用 绷带/药品 声音", EnableBandage);
            ModConfigAPI.SafeAddBoolDropdownList(Scope, "enable_syringe", "启用 注射器 声音", EnableSyringe);
        }

        private static void OnOptionsChanged(string key)
        {
            if (!ModConfigAPI.IsKeyForMod(key, Scope))
                return;

            var oldEnabled = Enabled;
            LoadFromModConfig();

            if (oldEnabled != Enabled)
            {
                ItemLogger.Info($"配置已热重载: Enabled {oldEnabled} → {Enabled}");
            }
            else
            {
                ItemLogger.Debug("配置已热重载");
            }
        }

        private static void LoadFromModConfig()
        {
            // 当 ModConfig 不可用时，SafeLoad 会返回默认值，不抛异常
            Enabled = ModConfigAPI.SafeLoad<bool>(Scope, "enabled", Enabled);
            Volume = Mathf.Clamp(ModConfigAPI.SafeLoad<float>(Scope, "volume", Volume), 0f, 2f);
            ReplaceOriginal = ModConfigAPI.SafeLoad<bool>(Scope, "replaceOriginal", ReplaceOriginal);
            RootDir = ModConfigAPI.SafeLoad<string>(Scope, "rootDir", RootDir ?? string.Empty) ?? string.Empty;


            MinActionAudibleSeconds = Mathf.Clamp(ModConfigAPI.SafeLoad<float>(Scope, "min_action_audible_sec", MinActionAudibleSeconds), 0f, 3f);

            EnableFood = ModConfigAPI.SafeLoad<bool>(Scope, "enable_food", EnableFood);
            EnableBandage = ModConfigAPI.SafeLoad<bool>(Scope, "enable_bandage", ModConfigAPI.SafeLoad<bool>(Scope, "enable_meds", EnableBandage));
            EnableSyringe = ModConfigAPI.SafeLoad<bool>(Scope, "enable_syringe", EnableSyringe);
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
                case "bandage": return EnableBandage; // 主分类：bandage
                case "meds": return EnableBandage;    // 兼容旧称谓：meds 视为 bandage
                case "syringe": return EnableSyringe;
                default: return true;
            }
        }
    }
}
