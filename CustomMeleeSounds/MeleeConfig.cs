using System;
using UnityEngine;
using DuckovCustomSounds.ModConfig;

namespace DuckovCustomSounds.CustomMeleeSounds
{
    /// <summary>
    /// Melee 配置管理
    /// 优先使用 ModConfig UI 配置（支持热重载）
    /// </summary>
    internal static class MeleeConfig
    {
        private const string ModName = "Melee";

        // ModConfig UI 配置项
        public static bool Enabled { get; private set; } = true;
        public static float Volume { get; private set; } = 1.0f;

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
                // 如果 ModConfig 可用，注册 UI 与变更回调
                if (ModConfigAPI.IsAvailable())
                {
                    try
                    {
                        SetupModConfigUI();
                        ModConfigAPI.SafeAddOnOptionsChangedDelegate(_onChangedHandler);
                        // 初始从 ModConfig 拉取一次，覆盖默认值
                        LoadFromModConfig();
                        MeleeLogger.Debug("已集成 ModConfig UI");
                    }
                    catch (Exception ex)
                    {
                        // ModConfig 集成失败（意外情况），静默回退到默认配置
                        MeleeLogger.Debug($"ModConfig 集成异常（已回退到默认配置）: {ex.Message}");
                    }
                }
                else
                {
                    // ModConfig 不可用（正常兼容场景），静默使用默认配置
                    MeleeLogger.Debug("ModConfig 不可用，使用默认配置");
                }

                MeleeLogger.Info($"配置加载完成: Enabled={Enabled}, Volume={Volume:F2}");
            }
            catch (Exception ex)
            {
                MeleeLogger.Error("配置加载失败，使用默认配置", ex);
            }
        }

        /// <summary>
        /// 设置 ModConfig UI
        /// </summary>
        private static void SetupModConfigUI()
        {
            ModConfigAPI.SafeAddBoolDropdownList(ModName, "enabled", "启用自定义近战音效", Enabled);
            ModConfigAPI.SafeAddInputWithSlider(ModName, "volume", "音量 (0~2)", typeof(float), Volume, new Vector2(0f, 2f));
        }

        /// <summary>
        /// 从 ModConfig 加载配置
        /// </summary>
        private static void LoadFromModConfig()
        {
            Enabled = ModConfigAPI.SafeLoad(ModName, "enabled", Enabled);
            Volume = ModConfigAPI.SafeLoad(ModName, "volume", Volume);
            Volume = Mathf.Clamp(Volume, 0f, 2f); // 钳制范围
        }

        /// <summary>
        /// ModConfig 变更回调（热重载）
        /// </summary>
        private static void OnOptionsChanged(string key)
        {
            var oldEnabled = Enabled;
            var oldVolume = Volume;

            LoadFromModConfig();

            // 如果启用状态切换，记录日志
            if (oldEnabled != Enabled)
            {
                MeleeLogger.Info($"配置已热重载: Enabled {oldEnabled} → {Enabled}");
            }

            // 如果音量变更，记录日志
            if (Math.Abs(oldVolume - Volume) > 0.01f)
            {
                MeleeLogger.Debug($"配置已热重载: Volume {oldVolume:F2} → {Volume:F2}");
            }
        }
    }
}
