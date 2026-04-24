using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using DuckovCustomSounds.ModConfig;

namespace DuckovCustomSounds.CustomBGM.SceneBGM
{
    /// <summary>
    /// 场景 BGM 配置管理
    /// 优先使用 ModConfig UI 配置（启用开关、音量调节）
    /// 高级配置从 config.json 加载（淡入淡出速度、延迟等）
    /// </summary>
    internal static class SceneBGMConfig
    {
        private static readonly ModConfigScope Scope = ModConfigScopes.SceneBGM;

        // 总开关
        public static bool Enabled { get; private set; } = true;

        // 进入场景 BGM 配置 (ModConfig UI)
        public static bool EnterBGMEnabled { get; private set; } = true;
        public static float EnterBGMVolume { get; private set; } = 0.8f;

        // 场景循环 BGM 配置 (ModConfig UI)
        public static bool LoopBGMEnabled { get; private set; } = true;
        public static float LoopBGMVolume { get; private set; } = 0.6f;
        public static bool OverrideDefaultBGM { get; private set; } = true;

        // config.json 高级配置项
        public static float EnterFadeDuration { get; private set; } = 1.5f;
        public static float LoopFadeDuration { get; private set; } = 2.0f;
        public static float SceneLoadDelay { get; private set; } = 2.0f;
        public static float CrossfadeDuration { get; private set; } = 1.0f;

        private static string ConfigPath => Path.Combine(ModBehaviour.ModFolderName, "SceneBGM", "config.json");
        private static readonly Action<string> _onChangedHandler = OnOptionsChanged;
        private static bool _initialized;

        // 配置变更事件（用于热配置）
        public static event Action? OnEnterBGMConfigChanged;
        public static event Action? OnLoopBGMConfigChanged;

        /// <summary>
        /// 加载配置
        /// </summary>
        public static void Load()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                // 1. 从 config.json 加载高级配置
                LoadFromConfigFile();

                // 2. 如果 ModConfig 可用，注册 UI 与变更回调
                if (ModConfigAPI.IsAvailable())
                {
                    try
                    {
                        SetupModConfigUI();
                        ModConfigAPI.SafeAddOnOptionsChangedDelegate(_onChangedHandler);
                        // 初始从 ModConfig 拉取一次，覆盖默认值
                        LoadFromModConfig();
                        SceneBGMLogger.Debug("已集成 ModConfig UI");
                    }
                    catch (Exception ex)
                    {
                        // ModConfig 集成失败（意外情况），静默回退到默认配置
                        SceneBGMLogger.Debug($"ModConfig 集成异常（已回退到默认配置）: {ex.Message}");
                    }
                }
                else
                {
                    // ModConfig 不可用（正常兼容场景），静默使用默认配置
                    SceneBGMLogger.Debug("ModConfig 不可用，使用默认配置");
                }

                SceneBGMLogger.Info($"配置加载完成: Enabled={Enabled}, EnterBGM={EnterBGMEnabled}, LoopBGM={LoopBGMEnabled}");
            }
            catch (Exception ex)
            {
                SceneBGMLogger.Error("配置加载失败，使用默认配置", ex);
            }
        }

        /// <summary>
        /// 设置 ModConfig UI
        /// </summary>
        private static void SetupModConfigUI()
        {
            // 总开关
            ModConfigAPI.SafeAddBoolDropdownList(Scope, "enabled", "启用场景音乐系统", Enabled);

            // 进入场景 BGM 分组
            ModConfigAPI.SafeAddBoolDropdownList(Scope, "enterBGM_enabled", "[进入BGM] 启用进入场景 BGM", EnterBGMEnabled);
            ModConfigAPI.SafeAddInputWithSlider(Scope, "enterBGM_volume", "[进入BGM] 进入音乐音量 (0~100%)", typeof(float), EnterBGMVolume * 100f, new Vector2(0f, 100f));

            // 场景循环 BGM 分组
            ModConfigAPI.SafeAddBoolDropdownList(Scope, "loopBGM_enabled", "[循环BGM] 启用场景循环 BGM", LoopBGMEnabled);
            ModConfigAPI.SafeAddInputWithSlider(Scope, "loopBGM_volume", "[循环BGM] 循环音乐音量 (0~100%)", typeof(float), LoopBGMVolume * 100f, new Vector2(0f, 100f));
            ModConfigAPI.SafeAddBoolDropdownList(Scope, "loopBGM_overrideDefault", "[循环BGM] 覆盖默认场景音乐", OverrideDefaultBGM);
        }

        /// <summary>
        /// 从 ModConfig 加载配置
        /// </summary>
        private static void LoadFromModConfig()
        {
            bool prevEnterEnabled = EnterBGMEnabled;
            bool prevLoopEnabled = LoopBGMEnabled;
            float prevEnterVolume = EnterBGMVolume;
            float prevLoopVolume = LoopBGMVolume;

            Enabled = ModConfigAPI.SafeLoad(Scope, "enabled", Enabled);

            EnterBGMEnabled = ModConfigAPI.SafeLoad(Scope, "enterBGM_enabled", EnterBGMEnabled);
            float enterVolumePercent = ModConfigAPI.SafeLoad(Scope, "enterBGM_volume", EnterBGMVolume * 100f);
            EnterBGMVolume = Mathf.Clamp01(enterVolumePercent / 100f);

            LoopBGMEnabled = ModConfigAPI.SafeLoad(Scope, "loopBGM_enabled", LoopBGMEnabled);
            float loopVolumePercent = ModConfigAPI.SafeLoad(Scope, "loopBGM_volume", LoopBGMVolume * 100f);
            LoopBGMVolume = Mathf.Clamp01(loopVolumePercent / 100f);
            OverrideDefaultBGM = ModConfigAPI.SafeLoad(Scope, "loopBGM_overrideDefault", OverrideDefaultBGM);

            // 触发配置变更事件（用于热配置）
            if (prevEnterEnabled != EnterBGMEnabled || !Mathf.Approximately(prevEnterVolume, EnterBGMVolume))
            {
                OnEnterBGMConfigChanged?.Invoke();
            }
            if (prevLoopEnabled != LoopBGMEnabled || !Mathf.Approximately(prevLoopVolume, LoopBGMVolume))
            {
                OnLoopBGMConfigChanged?.Invoke();
            }
        }

        /// <summary>
        /// ModConfig 变更回调
        /// </summary>
        private static void OnOptionsChanged(string key)
        {
            if (!ModConfigAPI.IsKeyForMod(key, Scope))
                return;

            LoadFromModConfig();
            SceneBGMLogger.Debug($"配置已更新: Enabled={Enabled}, EnterBGM={EnterBGMEnabled}, LoopBGM={LoopBGMEnabled}");
        }

        /// <summary>
        /// 从 config.json 加载高级配置
        /// </summary>
        private static void LoadFromConfigFile()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    SceneBGMLogger.Info("未找到 config.json，使用默认高级配置");
                    SaveDefaultConfig();
                    return;
                }

                string json = File.ReadAllText(ConfigPath);
                var config = JsonConvert.DeserializeObject<ConfigData>(json);

                if (config != null)
                {
                    // 只从 config.json 加载高级配置项
                    EnterFadeDuration = config.EnterFadeDuration;
                    LoopFadeDuration = config.LoopFadeDuration;
                    SceneLoadDelay = config.SceneLoadDelay;
                    CrossfadeDuration = config.CrossfadeDuration;

                    SceneBGMLogger.Debug($"高级配置加载: EnterFade={EnterFadeDuration}s, LoopFade={LoopFadeDuration}s, Delay={SceneLoadDelay}s");
                }
            }
            catch (Exception ex)
            {
                SceneBGMLogger.Warning($"config.json 加载失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存默认配置文件（仅包含高级配置）
        /// </summary>
        private static void SaveDefaultConfig()
        {
            try
            {
                var config = new ConfigData
                {
                    EnterFadeDuration = 1.5f,
                    LoopFadeDuration = 2.0f,
                    SceneLoadDelay = 2.0f,
                    CrossfadeDuration = 1.0f
                };

                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                string directory = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(ConfigPath, json);
                SceneBGMLogger.Info($"已生成默认配置文件: {ConfigPath}");
            }
            catch (Exception ex)
            {
                SceneBGMLogger.Error("保存默认配置失败", ex);
            }
        }

        /// <summary>
        /// 配置数据结构（仅高级配置）
        /// </summary>
        [Serializable]
        private class ConfigData
        {
            [JsonProperty("enterFadeDuration")]
            public float EnterFadeDuration = 1.5f;

            [JsonProperty("loopFadeDuration")]
            public float LoopFadeDuration = 2.0f;

            [JsonProperty("sceneLoadDelay")]
            public float SceneLoadDelay = 2.0f;

            [JsonProperty("crossfadeDuration")]
            public float CrossfadeDuration = 1.0f;
        }
    }
}
