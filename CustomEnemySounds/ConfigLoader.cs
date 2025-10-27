using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DuckovCustomSounds.CustomEnemySounds
{
    internal sealed class VoiceRuleConfig
    {
        public string? Team { get; set; } = null;            // 例如：scav, pmc
        public string? IconType { get; set; } = null;        // 例如：elite/boss/merchant/pet/none
        public float? MinHealth { get; set; }
        public float? MaxHealth { get; set; }
        public string? NameKeyContains { get; set; } = null; // 在nameKey中进行部分匹配
        public string? ForceVoiceType { get; set; } = null;  // 覆盖语音类型，例如：Duck/Robot
        public string? FilePattern { get; set; } = null;     // 覆盖文件模式，令牌：{enemyType},{team},{rank},{voiceType},{soundKey},{ext}
        public string[]? SoundKeys { get; set; } = null;     // 如果提供，仅匹配这些soundKeys
    }

        internal sealed class SimpleRuleConfig
        {
            // 新增：可选 Team 条件（例：player/pmc/scav）。当 NameKey 为空且 Team 命中时用于 SimpleRules 匹配。
            public string? Team { get; set; } = null;
            public string? NameKey { get; set; } = null;     // 敌人唯一标识（如 Cname_Scav）
            public string? IconType { get; set; } = null;    // 可选：限定图标类型（空=匹配所有）
            public string? FilePattern { get; set; } = null; // 目录或路径前缀，例如 "CustomEnemySounds/Scav"
        }


    internal sealed class DebugConfig
    {
        public bool Enabled { get; set; } = true;
        public string Level { get; set; } = "Info"; // 错误，信息，调试，详细
        public bool ValidateFileExists { get; set; } = true; // 为true时，在路由前检查磁盘
    }

    internal sealed class FallbackConfig
    {
        public bool UseOriginalWhenMissing { get; set; } = true; // 当找不到文件或出错时
        public string[] PreferredExtensions { get; set; } = new[] { ".mp3", ".wav" };
    }

    internal sealed class VoiceConfig
    {
        public List<VoiceRuleConfig> Rules { get; set; } = new List<VoiceRuleConfig>();
        public DebugConfig Debug { get; set; } = new DebugConfig();
        public FallbackConfig Fallback { get; set; } = new FallbackConfig();
        public string DefaultPattern { get; set; } = "CustomEnemySounds/{team}/{rank}_{voiceType}_{soundKey}{ext}";

        // 新增：简化规则模式
        public bool UseSimpleRules { get; set; } = false;
        public List<SimpleRuleConfig> SimpleRules { get; set; } = new List<SimpleRuleConfig>();

        // 播放行为配置：优先级打断机制（默认启用）
        public bool PriorityInterruptEnabled { get; set; } = true;

        // 变体索引绑定：同一敌人实例的所有语音共享同一变体索引（默认关闭，保持随机）
        public bool BindVariantIndexPerEnemy { get; set; } = false;


        // Footstep-specific: minimum cooldown seconds; voice module ignores when 0
        public float MinCooldownSeconds { get; set; } = 0f;

    }

    internal static class ConfigLoader
    {
        private const string ConfigFileName = "voice_rules.json";
        public static string ConfigFullPath => Path.Combine(ModBehaviour.ModFolderName, "CustomEnemySounds", ConfigFileName);

        public static VoiceConfig Load()
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigFullPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                if (!File.Exists(ConfigFullPath))
                {
                    // 写入一个“默认简化规则版本”，帮助用户快速开始（与 settings.json 的自动生成风格一致）
                    var defaultConfig = CreateDefault();
                    var jsonText = CreateDefaultJsonText();
                    File.WriteAllText(ConfigFullPath, jsonText, Encoding.UTF8);
                    return defaultConfig;
                }

                var text = File.ReadAllText(ConfigFullPath, Encoding.UTF8);
                var config = JsonConvert.DeserializeObject<VoiceConfig>(text) ?? CreateDefault();

                // 清理旧版令牌如{enemyType}
                SanitizeConfig(config);

                // 配置日志记录器
                var level = LogLevel.Info;
                if (Enum.TryParse(config.Debug.Level, true, out LogLevel parsed)) level = parsed;
                CESLogger.Configure(config.Debug.Enabled, level);
                CESLogger.ApplyFileSwitches(ModBehaviour.ModFolderName);

                return config;
            }
            catch (Exception ex)
            {
                CESLogger.Error("加载 voice_rules.json 失败，已使用内置默认配置。", ex);
                return CreateDefault();
            }
        }

        private static VoiceConfig CreateDefault()
        {
            var cfg = new VoiceConfig
            {
                UseSimpleRules = true,
                SimpleRules = new List<SimpleRuleConfig>
                {
                    new SimpleRuleConfig{ NameKey = "Cname_Scav", IconType = string.Empty, FilePattern = "CustomEnemySounds/Scav" },
                    new SimpleRuleConfig{ NameKey = "Cname_Usec", IconType = string.Empty, FilePattern = "CustomEnemySounds/Usec" },
                }
            };
            // 确保默认值已清理
            SanitizeConfig(cfg);
            return cfg;
        }

        //      JSON  ( _comment )    Json.NET 
        private static string CreateDefaultJsonText()
        {
            var sample = new
            {
                _comment = "\u81ea\u5b9a\u4e49\u654c\u4eba\u8bed\u97f3\u89c4\u5219 - \u7b80\u5316\u7248\u793a\u4f8b\u3002\u5b57\u6bb5\u8bf4\u660e\u89c1\u5404\u6bb5 _comment\u3002",
                Debug = new
                {
                    _comment = "\u65e5\u5fd7\u914d\u7f6e\uff1aEnabled=\u5f00\u5173\u3001Level=Error/Warning/Info/Debug/Verbose\u3001ValidateFileExists=\u8def\u7531\u524d\u662f\u5426\u68c0\u67e5\u6587\u4ef6\u5b58\u5728",
                    Enabled = true,
                    Level = "Info",
                    ValidateFileExists = true
                },
                Fallback = new
                {
                    _comment = "\u56de\u9000\u7b56\u7565\uff1a\u627e\u4e0d\u5230\u6216\u51fa\u9519\u65f6\u662f\u5426\u4f7f\u7528\u539f\u59cb\u58f0\u97f3\uff1b\u53ef\u9009\u6269\u5c55\u540d\u4f18\u5148\u7ea7",
                    UseOriginalWhenMissing = true,
                    PreferredExtensions = new[] { ".mp3", ".wav" }
                },
                DefaultPattern = "CustomEnemySounds/{team}/{rank}_{voiceType}_{soundKey}{ext}",
                UseSimpleRules = true,
                SimpleRules = new object[]
                {
                    new { _comment = "\u793a\u4f8b\uff1a\u4e3a\u67d0\u4e2a\u654c\u4eba NameKey \u6307\u5b9a\u6839\u76ee\u5f55\uff0c\u6587\u4ef6\u547d\u540d\u9075\u5faa {icon}_{voiceType}_{soundKey}{ext}", NameKey = "Cname_Scav", IconType = "", FilePattern = "CustomEnemySounds/Scav" },
                    new { NameKey = "Cname_Usec", IconType = "", FilePattern = "CustomEnemySounds/Usec" }
                },
                PriorityInterruptEnabled = true,
                BindVariantIndexPerEnemy = false,
                Rules = new object[] { }
            };
            return JsonConvert.SerializeObject(sample, Formatting.Indented);
        }


        private static void SanitizeConfig(VoiceConfig cfg)
        {
            if (cfg == null) return;
            cfg.DefaultPattern = SanitizePattern(cfg.DefaultPattern);
            if (cfg.Rules != null)
            {
                foreach (var r in cfg.Rules)
                {
                    if (r == null) continue;
                    r.FilePattern = SanitizePattern(r.FilePattern);
                }
            }
            if (cfg.SimpleRules != null)
            {
                foreach (var s in cfg.SimpleRules)
                {
                    if (s == null) continue;
                    s.FilePattern = SanitizePattern(s.FilePattern);
                }
            }
        }

        private static string SanitizePattern(string p)
        {
            if (string.IsNullOrEmpty(p)) return p;
            var t = p.Replace("{enemyType}/", string.Empty)
                     .Replace("/{enemyType}", string.Empty)
                     .Replace("{enemyType}", string.Empty);
            // 折叠双分隔符
            t = t.Replace("\\\\", "\\").Replace("//", "/");
            if (!ReferenceEquals(t, p))
            {
                CESLogger.Info($"[CES:Config] 已移除 legacy token {"{enemyType}"} 并清理模板: {t}");
            }
            return t;
        }
    }
}

