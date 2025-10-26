using System;
using System.IO;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DuckovCustomSounds.Logging;

namespace DuckovCustomSounds
{
    /// <summary>
    /// 统一模块设置读取/写回（与 Logging.LogManager 共用同一个 settings.json）。
    /// 只关注我们新增的键：overrideExtractionBGM。
    /// </summary>
    internal static class ModSettings
    {
        private const string SettingsFileName = "settings.json";
        private static readonly ILog Log = LogManager.GetLogger("Core");

        // New: voice frequency controls (global rate limit)
        public static bool DeathVoiceEnabled { get; private set; } = true;
        public static float DeathVoiceMinInterval { get; private set; } = 0f;

        public static bool NPCGrenadeSurprisedEnabled { get; private set; } = true;
        public static float NPCGrenadeSurprisedMinInterval { get; private set; } = 0f;

        public static bool OverrideExtractionBGM { get; private set; } = false;

        public static bool LevelLoadLoggerEnabled { get; private set; } = false;


        public static bool AudioPostLoggerEnabled { get; private set; } = false;

        public static void Initialize()
        {
            try
            {
                // 与 LogManager 一致的路径：DuckovCustomSounds/settings.json（由 LogManager.Initialize 保障目录存在）
                string path = Path.Combine(ModBehaviour.ModFolderName, SettingsFileName);

                JObject root = new JObject();
                bool exists = File.Exists(path);
                if (exists)
                {
                    try
                    {
                        var text = File.ReadAllText(path);
                        if (!string.IsNullOrWhiteSpace(text))
                            root = JObject.Parse(text);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"读取 settings.json 失败，使用空白对象继续：{ex.Message}");
                        root = new JObject();
                    }
                }

                bool needsWriteBack = false;

                // 1) overrideExtractionBGM
                bool hadOverride = root.TryGetValue("overrideExtractionBGM", StringComparison.OrdinalIgnoreCase, out var overrideToken);
                bool overrideVal = overrideToken?.Type == JTokenType.Boolean ? overrideToken.Value<bool>() : false;
                if (!hadOverride)
                {
                    root["overrideExtractionBGM"] = overrideVal; // 默认 false
                    needsWriteBack = true;
                }
                OverrideExtractionBGM = overrideVal;

                // 2) deathVoiceFrequency（"always"/number seconds/"off"/true/false/"6.0f"等）
                const string DeathKey = "deathVoiceFrequency";
                bool hadDeathKey = root.TryGetValue(DeathKey, StringComparison.OrdinalIgnoreCase, out var deathToken);
                ParseRateControl(deathToken, /*defaultEnabled*/ true, /*defaultInterval*/ 0f,
                    out bool dvEnabled, out float dvInterval);
                if (!hadDeathKey)
                {
                    root[DeathKey] = "always"; // 写回默认值
                    needsWriteBack = true;
                }
                DeathVoiceEnabled = dvEnabled;
                DeathVoiceMinInterval = dvInterval;

                // 3) npcGrenadeSurprisedFrequency（同上）
                const string GrenadeKey = "npcGrenadeSurprisedFrequency";
                bool hadGrenadeKey = root.TryGetValue(GrenadeKey, StringComparison.OrdinalIgnoreCase, out var grenadeToken);
                ParseRateControl(grenadeToken, /*defaultEnabled*/ true, /*defaultInterval*/ 0f,
                    out bool gvEnabled, out float gvInterval);
                if (!hadGrenadeKey)
                {
                    root[GrenadeKey] = "always";
                    needsWriteBack = true;
                }

                // 4) enableLevelLoadLogger（仅调试用途，默认 false）
                const string LoggerKey = "enableLevelLoadLogger";
                bool hadLoggerKey = root.TryGetValue(LoggerKey, StringComparison.OrdinalIgnoreCase, out var loggerToken);
                bool loggerVal = loggerToken?.Type == JTokenType.Boolean ? loggerToken.Value<bool>() : false;
                if (!hadLoggerKey)
                {
                    root[LoggerKey] = loggerVal;
                    needsWriteBack = true;
                }
                // 5) enableAudioPostLogger（捕获所有 AudioManager.Post/AudioObject.Post 调用，默认 false）
                const string AudioLoggerKey = "enableAudioPostLogger";
                bool hadAudioLoggerKey = root.TryGetValue(AudioLoggerKey, StringComparison.OrdinalIgnoreCase, out var audioLoggerToken);
                bool audioLoggerVal = audioLoggerToken?.Type == JTokenType.Boolean ? audioLoggerToken.Value<bool>() : false;
                if (!hadAudioLoggerKey)
                {
                    root[AudioLoggerKey] = audioLoggerVal;
                    needsWriteBack = true;
                }
                AudioPostLoggerEnabled = audioLoggerVal;

                LevelLoadLoggerEnabled = loggerVal;

                NPCGrenadeSurprisedEnabled = gvEnabled;
                NPCGrenadeSurprisedMinInterval = gvInterval;

                // 统一写回：仅当文件原本不存在或新增键需要补充
                if (!exists || needsWriteBack)
                {
                    try
                    {
                        File.WriteAllText(path, root.ToString(Formatting.Indented));
                        Log.Info($"settings.json 已{(exists ? "补充" : "创建")}默认键：overrideExtractionBGM, {DeathKey}, {GrenadeKey}, {LoggerKey}, {AudioLoggerKey}");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"写回 settings.json 失败：{ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"ModSettings.Initialize 异常：{ex.Message}");
            }
        }

        // 解析频率控制：
        // - null: 使用默认
        // - bool: true=always（启用且0秒间隔），false=off
        // - number: 启用，间隔=该数值（秒，<0 则视为0）
        // - string: "always"/"off" 等；或可解析为浮点数（接受后缀 f/F/s/秒），无法解析则回退默认
        private static void ParseRateControl(JToken token, bool defaultEnabled, float defaultInterval,
            out bool enabled, out float interval)
        {
            enabled = defaultEnabled;
            interval = defaultInterval;

            try
            {
                if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
                {
                    return;
                }

                switch (token.Type)
                {
                    case JTokenType.Boolean:
                        enabled = token.Value<bool>();
                        interval = 0f;
                        return;
                    case JTokenType.Integer:
                    case JTokenType.Float:
                        enabled = true;
                        interval = Math.Max(0f, token.Value<float>());
                        return;
                    case JTokenType.String:
                        var s = (token.Value<string>() ?? string.Empty).Trim();
                        if (string.Equals(s, "always", StringComparison.OrdinalIgnoreCase))
                        {
                            enabled = true; interval = 0f; return;
                        }
                        if (string.Equals(s, "off", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(s, "disabled", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(s, "false", StringComparison.OrdinalIgnoreCase))
                        {
                            enabled = false; interval = 0f; return;
                        }
                        // 提取可解析的数值（忽略末尾常见后缀，如 f/F/s/sec/秒）
                        string cleaned = CleanFloatString(s);
                        if (float.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out var sec))
                        {
                            enabled = true;
                            interval = Math.Max(0f, sec);
                            return;
                        }
                        // 解析失败：使用默认
                        return;
                    default:
                        return; // 使用默认
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"ParseRateControl 解析异常，使用默认：{ex.Message}");
            }
        }

        private static string CleanFloatString(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            // 保留数字、点号和负号，忽略其余字符
            char[] buf = new char[s.Length];
            int j = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if ((c >= '0' && c <= '9') || c == '.' || c == '-')
                    buf[j++] = c;
            }
            return new string(buf, 0, j);
        }
    }
}

