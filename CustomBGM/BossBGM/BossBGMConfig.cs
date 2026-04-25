using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using DuckovCustomSounds.ModConfig;

namespace DuckovCustomSounds.CustomBGM.BossBGM
{
    /// <summary>
    /// BOSS BGM 配置管理
    /// 优先使用 ModConfig UI 配置（启用开关、触发距离）
    /// 高级配置从 config.json 加载（淡入淡出速度、更新频率等）
    /// </summary>
    internal static class BossBGMConfig
    {
        private const string ModName = "BossBGM";

        // ModConfig UI 配置项
        public static bool Enabled { get; private set; } = true;
        public static float TriggerDistance { get; private set; } = 40f; // 增大检测距离，解决30米太近的问题

        // config.json 高级配置项
        public static float FadeDuration { get; private set; } = 2f;
        public static float UpdateInterval { get; private set; } = 0.1f;
        public static float ManagerUpdateInterval { get; private set; } = 0.5f;

        // A: 防抖/粘滞
        public static float MinSwitchIntervalSeconds { get; private set; } = 2f;
        public static float MinDistanceDeltaToSwitch { get; private set; } = 5f;

        // B: 播放进度保留
        public static bool ResumePlaybackEnabled { get; private set; } = true;

        // C: 延迟停止策略
        public static bool DelayedStopEnabled { get; private set; } = true;
        public static float DelayedStopSeconds { get; private set; } = 1f;

        // D: BOSS 死亡淡出
        public static float BossDeathFadeOutSeconds { get; private set; } = 3f;

        private static string ConfigPath => Path.Combine(ModBehaviour.ModFolderName, "BossBGM", "config.json");
        private static readonly Action<string> _onChangedHandler = OnOptionsChanged;
        private static bool _initialized;

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
                        BossBGMLogger.Debug("已集成 ModConfig UI");
                    }
                    catch (Exception ex)
                    {
                        // ModConfig 集成失败（意外情况），静默回退到默认配置
                        BossBGMLogger.Debug($"ModConfig 集成异常（已回退到默认配置）: {ex.Message}");
                    }
                }
                else
                {
                    // ModConfig 不可用（正常兼容场景），静默使用默认配置
                    BossBGMLogger.Debug("ModConfig 不可用，使用默认配置");
                }

                BossBGMLogger.Info($"配置加载完成: Enabled={Enabled}, Distance={TriggerDistance}m, Fade={FadeDuration}s, Update={UpdateInterval}s, ManagerUpdate={ManagerUpdateInterval}s, MinSwitch={MinSwitchIntervalSeconds}s, MinDelta={MinDistanceDeltaToSwitch}m, Resume={ResumePlaybackEnabled}, DelayedStop={DelayedStopEnabled}({DelayedStopSeconds}s), DeathFade={BossDeathFadeOutSeconds}s");
            }
            catch (Exception ex)
            {
                BossBGMLogger.Error("配置加载失败，使用默认配置", ex);
            }
        }

        /// <summary>
        /// 设置 ModConfig UI
        /// </summary>
        private static void SetupModConfigUI()
        {
            ModConfigAPI.SafeAddBoolDropdownList(ModName, "enabled", "启用 BOSS BGM", Enabled);
            ModConfigAPI.SafeAddInputWithSlider(ModName, "triggerDistance", "触发距离 (米, 10~200)", typeof(float), TriggerDistance, new Vector2(10f, 200f));
        }

        /// <summary>
        /// 从 ModConfig 加载配置
        /// </summary>
        private static void LoadFromModConfig()
        {
            Enabled = ModConfigAPI.SafeLoad(ModName, "enabled", Enabled);
            TriggerDistance = ModConfigAPI.SafeLoad(ModName, "triggerDistance", TriggerDistance);
            TriggerDistance = Mathf.Clamp(TriggerDistance, 10f, 200f); // 钳制范围
        }

        /// <summary>
        /// ModConfig 变更回调
        /// </summary>
        private static void OnOptionsChanged(string key)
        {
            LoadFromModConfig();
            BossBGMLogger.Debug($"配置已更新: Enabled={Enabled}, Distance={TriggerDistance}m");
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
                    BossBGMLogger.Info("未找到 config.json，使用默认高级配置");
                    SaveDefaultConfig();
                    return;
                }

                string json = File.ReadAllText(ConfigPath);
                var config = JsonConvert.DeserializeObject<ConfigData>(json);

                if (config != null)
                {
                    // 只从 config.json 加载高级配置项
                    FadeDuration = config.FadeDuration;
                    UpdateInterval = config.UpdateInterval;
                    ManagerUpdateInterval = config.ManagerUpdateInterval;

                    // A: 防抖/粘滞
                    MinSwitchIntervalSeconds = config.MinSwitchIntervalSeconds;
                    MinDistanceDeltaToSwitch = config.MinDistanceDeltaToSwitch;

                    // B: 播放进度保留
                    ResumePlaybackEnabled = config.ResumePlaybackEnabled;

                    // C: 延迟停止策略
                    DelayedStopEnabled = config.DelayedStopEnabled;
                    DelayedStopSeconds = config.DelayedStopSeconds;

                    // D: BOSS 死亡淡出
                    BossDeathFadeOutSeconds = config.BossDeathFadeOutSeconds;

                    BossBGMLogger.Debug($"高级配置加载: Fade={FadeDuration}s, UpdateInterval={UpdateInterval}s, ManagerUpdate={ManagerUpdateInterval}s, MinSwitch={MinSwitchIntervalSeconds}s, MinDelta={MinDistanceDeltaToSwitch}m, Resume={ResumePlaybackEnabled}, DelayedStop={DelayedStopEnabled}, Delay={DelayedStopSeconds}s, DeathFade={BossDeathFadeOutSeconds}s");
                }
            }
            catch (Exception ex)
            {
                BossBGMLogger.Warning($"config.json 加载失败: {ex.Message}");
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
                    FadeDuration = 2f,
                    UpdateInterval = 0.1f,
                    ManagerUpdateInterval = 0.5f,
                    MinSwitchIntervalSeconds = 2f,
                    MinDistanceDeltaToSwitch = 5f,
                    ResumePlaybackEnabled = true,
                    DelayedStopEnabled = true,
                    DelayedStopSeconds = 1f,
                    BossDeathFadeOutSeconds = 3f
                };

                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                string directory = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(ConfigPath, json);
                BossBGMLogger.Info($"已生成默认配置文件: {ConfigPath}");
            }
            catch (Exception ex)
            {
                BossBGMLogger.Error("保存默认配置失败", ex);
            }
        }

        /// <summary>
        /// 配置数据结构（仅高级配置）
        /// </summary>
        [Serializable]
        private class ConfigData
        {
            [JsonProperty("fadeDuration")]
            public float FadeDuration = 2f;

            [JsonProperty("updateInterval")]
            public float UpdateInterval = 0.1f;

            [JsonProperty("managerUpdateInterval")]
            public float ManagerUpdateInterval = 0.5f;

            // A: 防抖/粘滞
            [JsonProperty("minSwitchIntervalSeconds")]
            public float MinSwitchIntervalSeconds = 2f;

            [JsonProperty("minDistanceDeltaToSwitch")]
            public float MinDistanceDeltaToSwitch = 5f;

            // B: 播放进度保留
            [JsonProperty("resumePlaybackEnabled")]
            public bool ResumePlaybackEnabled = true;

            // C: 延迟停止策略
            [JsonProperty("delayedStopEnabled")]
            public bool DelayedStopEnabled = true;

            [JsonProperty("delayedStopSeconds")]
            public float DelayedStopSeconds = 1f;

            // D: BOSS 死亡淡出
            [JsonProperty("bossDeathFadeOutSeconds")]
            public float BossDeathFadeOutSeconds = 3f;
        }
    }
}
