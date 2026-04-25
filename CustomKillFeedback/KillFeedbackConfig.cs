using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using DuckovCustomSounds.ModConfig;

namespace DuckovCustomSounds.CustomKillFeedback
{
    /// <summary>
    /// 自定义击杀反馈配置：文件配置 + ModConfig UI 双通道。
    /// </summary>
    internal static class KillFeedbackConfig
    {
        private const string ModName = "KillFeedback";
        private static bool _initialized;
        private static readonly Action<string> _onChangedHandler = OnOptionsChanged;

        // ModConfig 选项
        public static bool Enabled { get; private set; } = true;
        public static float ComboWindowSeconds { get; private set; } = 4.5f;
        public static float DisplayDuration { get; private set; } = 1.25f;
        public static bool Use2DSound { get; private set; } = true;
        public static float VolumeScale { get; private set; } = 1.0f;

        // UI 视觉
        public static float FontSize { get; private set; } = 42f;
        public static string TextColorHex { get; private set; } = "#FFD16A";

        // 资源定位
        public static string BaseFolder { get; private set; } = "CustomKillFeedback";
        public static string PatternKill { get; private set; } = "kill_{n}";
        public static string PatternHeadshot { get; private set; } = "headshot_{n}";
        public static string[] PreferredExtensions { get; private set; } = new[] { ".mp3", ".wav", ".ogg" };

        // 图标配置
        public static bool UseIcons { get; private set; } = true;
        public static bool ShowText { get; private set; } = true;
        public static string IconFolder { get; private set; } = Path.Combine("CustomKillFeedback", "Icons");
        public static string[] IconExtensions { get; private set; } = new[] { ".png", ".jpg", ".jpeg", ".webp" };
        public static float IconAlpha { get; private set; } = 0.85f;

        private static string ConfigPath => Path.Combine(ModBehaviour.ModFolderName, "CustomKillFeedback", "config.json");

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            try
            {
                LoadFromConfigFile();

                if (ModConfigAPI.IsAvailable())
                {
                    try
                    {
                        SetupModConfigUI();
                        ModConfigAPI.SafeAddOnOptionsChangedDelegate(_onChangedHandler);
                        LoadFromModConfig();
                        KillFeedbackLogger.Debug("[KF] 已加载 ModConfig UI");
                    }
                    catch (Exception ex)
                    {
                        KillFeedbackLogger.Debug($"[KF] ModConfig 配置异常，回退至文件设置: {ex.Message}");
                    }
                }
                else
                {
                    KillFeedbackLogger.Debug("[KF] ModConfig 未启用，沿用文件/默认配置");
                }

                KillFeedbackLogger.Info($"[KF] 配置: Enabled={Enabled}, Combo={ComboWindowSeconds:F1}s, UI={DisplayDuration:F1}s, 2D={Use2DSound}, Vol={VolumeScale:F2}, Icons={UseIcons}, Text={ShowText}, Alpha={IconAlpha:F2}");
            }
            catch (Exception ex)
            {
                KillFeedbackLogger.Error("[KF] 配置初始化失败，使用默认值", ex);
            }
        }

        private static void SetupModConfigUI()
        {
            ModConfigAPI.SafeAddBoolDropdownList(ModName, "enabled", "启用 自定义击杀反馈", Enabled);
            ModConfigAPI.SafeAddInputWithSlider(ModName, "comboWindow", "连杀窗口 (秒, 2~10)", typeof(float), ComboWindowSeconds, new Vector2(2f, 10f));
            ModConfigAPI.SafeAddInputWithSlider(ModName, "displayDuration", "UI显示时长 (秒, 0.5~3)", typeof(float), DisplayDuration, new Vector2(0.5f, 3f));
            ModConfigAPI.SafeAddBoolDropdownList(ModName, "use2DSound", "2D 音频 (关闭为3D)", Use2DSound);
            ModConfigAPI.SafeAddInputWithSlider(ModName, "volume", "音量倍率 (0.1~2.0)", typeof(float), VolumeScale, new Vector2(0.1f, 2.0f));
            ModConfigAPI.SafeAddBoolDropdownList(ModName, "useIcons", "显示 击杀图标", UseIcons);
            ModConfigAPI.SafeAddBoolDropdownList(ModName, "showText", "显示 文本标签", ShowText);
            ModConfigAPI.SafeAddInputWithSlider(ModName, "iconAlpha", "图标透明度 (0.2~1.0)", typeof(float), IconAlpha, new Vector2(0.2f, 1.0f));
        }

        private static void LoadFromModConfig()
        {
            Enabled = ModConfigAPI.SafeLoad(ModName, "enabled", Enabled);
            ComboWindowSeconds = Mathf.Clamp(ModConfigAPI.SafeLoad(ModName, "comboWindow", ComboWindowSeconds), 2f, 10f);
            DisplayDuration = Mathf.Clamp(ModConfigAPI.SafeLoad(ModName, "displayDuration", DisplayDuration), 0.5f, 3f);
            Use2DSound = ModConfigAPI.SafeLoad(ModName, "use2DSound", Use2DSound);
            VolumeScale = Mathf.Clamp(ModConfigAPI.SafeLoad(ModName, "volume", VolumeScale), 0.1f, 2.0f);
            UseIcons = ModConfigAPI.SafeLoad(ModName, "useIcons", UseIcons);
            ShowText = ModConfigAPI.SafeLoad(ModName, "showText", ShowText);
            IconAlpha = Mathf.Clamp(ModConfigAPI.SafeLoad(ModName, "iconAlpha", IconAlpha), 0.2f, 1.0f);
        }

        private static void OnOptionsChanged(string _)
        {
            try
            {
                LoadFromModConfig();
                KillFeedbackLogger.Debug($"[KF] 配置已更新: Enabled={Enabled}, Combo={ComboWindowSeconds:F1}s, UI={DisplayDuration:F1}s, 2D={Use2DSound}, Vol={VolumeScale:F2}, Icons={UseIcons}, Text={ShowText}, Alpha={IconAlpha:F2}");
                KillFeedbackManager.ApplyRuntimeOptions();
            }
            catch (Exception ex)
            {
                KillFeedbackLogger.Warning($"[KF] 应用 ModConfig 选项失败: {ex.Message}");
            }
        }

        private static void LoadFromConfigFile()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    SaveDefaultConfig();
                    return;
                }

                var json = File.ReadAllText(ConfigPath);
                var cfg = JsonConvert.DeserializeObject<ConfigData>(json);
                if (cfg == null) return;

                BaseFolder = string.IsNullOrWhiteSpace(cfg.BaseFolder) ? BaseFolder : cfg.BaseFolder;
                PatternKill = string.IsNullOrWhiteSpace(cfg.PatternKill) ? PatternKill : cfg.PatternKill;
                PatternHeadshot = string.IsNullOrWhiteSpace(cfg.PatternHeadshot) ? PatternHeadshot : cfg.PatternHeadshot;
                PreferredExtensions = (cfg.PreferredExtensions != null && cfg.PreferredExtensions.Length > 0) ? cfg.PreferredExtensions : PreferredExtensions;
                FontSize = cfg.FontSize > 0 ? cfg.FontSize : FontSize;
                if (!string.IsNullOrWhiteSpace(cfg.TextColorHex)) TextColorHex = cfg.TextColorHex;
                if (cfg.UseIcons.HasValue) UseIcons = cfg.UseIcons.Value;
                if (cfg.ShowText.HasValue) ShowText = cfg.ShowText.Value;
                if (!string.IsNullOrWhiteSpace(cfg.IconFolder)) IconFolder = cfg.IconFolder;
                if (cfg.IconExtensions != null && cfg.IconExtensions.Length > 0) IconExtensions = cfg.IconExtensions;
                if (cfg.IconAlpha > 0f) IconAlpha = Mathf.Clamp(cfg.IconAlpha, 0.2f, 1.0f);
            }
            catch (Exception ex)
            {
                KillFeedbackLogger.Warning($"[KF] 读取 config.json 失败: {ex.Message}");
            }
        }

        private static void SaveDefaultConfig()
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var def = new ConfigData
                {
                    BaseFolder = BaseFolder,
                    PatternKill = PatternKill,
                    PatternHeadshot = PatternHeadshot,
                    PreferredExtensions = PreferredExtensions,
                    FontSize = FontSize,
                    TextColorHex = TextColorHex,
                    UseIcons = UseIcons,
                    ShowText = ShowText,
                    IconFolder = IconFolder,
                    IconExtensions = IconExtensions,
                    IconAlpha = IconAlpha,
                };
                var json = JsonConvert.SerializeObject(def, Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
                KillFeedbackLogger.Info($"[KF] 已生成默认配置: {ConfigPath}");
            }
            catch (Exception ex)
            {
                KillFeedbackLogger.Warning($"[KF] 写入 config.json 失败: {ex.Message}");
            }
        }

        public static Color TextColor
        {
            get
            {
                try
                {
                    if (ColorUtility.TryParseHtmlString(TextColorHex, out var c)) return c;
                }
                catch
                {
                }
                return new Color(1f, 0.82f, 0.42f, 1f);
            }
        }

        [Serializable]
        private sealed class ConfigData
        {
            public string BaseFolder;
            public string PatternKill;
            public string PatternHeadshot;
            public string[] PreferredExtensions;
            public float FontSize;
            public string TextColorHex;
            public bool? UseIcons;
            public bool? ShowText;
            public string IconFolder;
            public string[] IconExtensions;
            public float IconAlpha;
        }
    }
}
