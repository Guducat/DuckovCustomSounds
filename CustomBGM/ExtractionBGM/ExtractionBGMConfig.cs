using System;
using UnityEngine;
using DuckovCustomSounds.ModConfig;

namespace DuckovCustomSounds.CustomBGM.ExtractionBGM
{
    /// <summary>
    /// 撤离音乐模式
    /// </summary>
    public enum ExtractionBGMMode
    {
        /// <summary>
        /// 禁用：不修改任何撤离音效
        /// </summary>
        Disabled = 0,

        /// <summary>
        /// 倒计时音效模式：播放倒计时音效并屏蔽成功Stinger
        /// </summary>
        CountdownMode = 1,

        /// <summary>
        /// 成功音效替换模式：仅替换成功Stinger
        /// </summary>
        SuccessStingerMode = 2
    }

    /// <summary>
    /// ExtractionBGM 配置管理
    /// 优先使用 ModConfig UI 配置（支持热重载）
    /// 回退到 settings.json（兼容旧配置 overrideExtractionBGM）
    /// </summary>
    internal static class ExtractionBGMConfig
    {
        private static readonly ModConfigScope Scope = ModConfigScopes.ExtractionBGM;

        // ModConfig UI 配置项
        public static ExtractionBGMMode Mode { get; private set; } = ExtractionBGMMode.Disabled;
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
                // 1. 从 settings.json 加载默认配置（兼容旧配置）
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
                        ExtractionBGMLogger.Debug("已集成 ModConfig UI");
                    }
                    catch (Exception ex)
                    {
                        // ModConfig 集成失败（意外情况），静默回退到默认配置
                        ExtractionBGMLogger.Debug($"ModConfig 集成异常（已回退到默认配置）: {ex.Message}");
                    }
                }
                else
                {
                    // ModConfig 不可用（正常兼容场景），静默使用默认配置
                    ExtractionBGMLogger.Debug("ModConfig 不可用，使用默认配置");
                }

                ExtractionBGMLogger.Info($"配置加载完成: Mode={Mode}, Volume={Volume:F2}");
            }
            catch (Exception ex)
            {
                ExtractionBGMLogger.Error("配置加载失败，使用默认配置", ex);
            }
        }

        /// <summary>
        /// 设置 ModConfig UI
        /// </summary>
        private static void SetupModConfigUI()
        {
            // 使用枚举下拉选择
            var modeOptions = new System.Collections.Generic.SortedDictionary<string, object>(StringComparer.Ordinal)
            {
                { "禁用: 不修改任何撤离音效", (int)ExtractionBGMMode.Disabled },
                { "倒计时音效模式: 播放倒计时音效并屏蔽成功Stinger", (int)ExtractionBGMMode.CountdownMode },
                { "成功音效替换模式: 仅替换成功Stinger", (int)ExtractionBGMMode.SuccessStingerMode }
            };

            ModConfigAPI.SafeAddDropdownList(Scope, "mode", "撤离音乐模式", modeOptions, typeof(int), (int)Mode);
            ModConfigAPI.SafeAddInputWithSlider(Scope, "volume", "撤离音效音量 (0~100%)", typeof(float), Volume * 100f, new Vector2(0f, 100f));
        }

        /// <summary>
        /// 从 ModConfig 加载配置
        /// </summary>
        private static void LoadFromModConfig()
        {
            int modeInt = ModConfigAPI.SafeLoad(Scope, "mode", (int)Mode);
            if (Enum.IsDefined(typeof(ExtractionBGMMode), modeInt))
            {
                Mode = (ExtractionBGMMode)modeInt;
            }

            float volumePercent = ModConfigAPI.SafeLoad(Scope, "volume", Volume * 100f);
            Volume = Mathf.Clamp01(volumePercent / 100f);
        }

        /// <summary>
        /// ModConfig 变更回调（热重载）
        /// </summary>
        private static void OnOptionsChanged(string key)
        {
            if (!ModConfigAPI.IsKeyForMod(key, Scope))
                return;

            var oldMode = Mode;
            var oldVolume = Volume;
            LoadFromModConfig();

            // 如果模式切换，停止当前播放的音效
            if (oldMode != Mode)
            {
                ExtractionBGMLogger.Info($"配置已热重载: {oldMode} → {Mode}, Volume={Volume:F2}");
                try
                {
                    // 停止所有撤离音效，避免模式切换时的冲突
                    ExtractionSounds.StopAllExtractionSounds();
                }
                catch (Exception ex)
                {
                    ExtractionBGMLogger.Warning($"停止撤离音效失败: {ex.Message}");
                }
            }

            if (Math.Abs(oldVolume - Volume) > 0.01f)
            {
                ExtractionBGMLogger.Info($"音量已更新: {oldVolume:F2} -> {Volume:F2}");
                ExtractionSounds.ApplyVolumeToCurrentSounds();
            }
        }

        /// <summary>
        /// 从 settings.json 加载默认配置（兼容旧配置）
        /// </summary>
        private static void LoadFromSettingsFallback()
        {
            try
            {
                // 兼容旧配置：overrideExtractionBGM
                // true → CountdownMode
                // false → Disabled
                if (DuckovCustomSounds.ModSettings.OverrideExtractionBGM)
                {
                    Mode = ExtractionBGMMode.CountdownMode;
                    ExtractionBGMLogger.Debug("从 settings.json 迁移配置: overrideExtractionBGM=true → CountdownMode");
                }
                else
                {
                    Mode = ExtractionBGMMode.Disabled;
                }
            }
            catch (Exception ex)
            {
                ExtractionBGMLogger.Warning($"settings.json 回退加载失败: {ex.Message}");
            }
        }
    }
}
