using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DuckovCustomSounds.CustomEnemySounds; // reuse VoiceConfig and types

namespace DuckovCustomSounds.CustomFootStepSounds
{
    internal static class FootstepConfigLoader
    {
        private const string ConfigFileName = "footstep_voice_rule.json";
        public static string ConfigFullPath => Path.Combine(ModBehaviour.ModFolderName, "CustomFootStepSounds", ConfigFileName);

        public static VoiceConfig Load()
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigFullPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                if (!File.Exists(ConfigFullPath))
                {
                    var defaultCfg = CreateDefault();
                    var jsonText = CreateDefaultJsonText();
                    File.WriteAllText(ConfigFullPath, jsonText, Encoding.UTF8);
                    ApplyLogger(defaultCfg);
                    return defaultCfg;
                }

                var text = File.ReadAllText(ConfigFullPath, Encoding.UTF8);
                var cfg = JsonConvert.DeserializeObject<VoiceConfig>(text) ?? CreateDefault();
                SanitizeConfig(cfg);
                ApplyLogger(cfg);
                return cfg;
            }
            catch (Exception ex)
            {
                FootstepLogger.Error("加载 footstep_voice_rule.json 失败，已使用内置默认配置。", ex);
                var d = CreateDefault();
                ApplyLogger(d);
                return d;
            }
        }

        private static void ApplyLogger(VoiceConfig cfg)
        {
            var ceLevel = DuckovCustomSounds.CustomEnemySounds.LogLevel.Info;
            if (Enum.TryParse(cfg.Debug.Level, true, out DuckovCustomSounds.CustomEnemySounds.LogLevel parsed)) ceLevel = parsed;
            // 1) Footstep 模块自身日志级别
            FootstepLogger.Configure(cfg.Debug.Enabled, (LogLevel)(int)ceLevel);
            // 2) 为了显示 [CES:Path] / [CES:Rule] 的调试细节（PathBuilder/VoiceRuleEngine 使用 CESLogger），同步提升 CustomEnemySounds 日志级别
            CESLogger.Configure(cfg.Debug.Enabled, ceLevel);
            // 3) 文件快速开关
            FootstepLogger.ApplyFileSwitches(ModBehaviour.ModFolderName);
        }

        private static VoiceConfig CreateDefault()
        {
            var cfg = new VoiceConfig
            {
                UseSimpleRules = true,
                SimpleRules = new List<SimpleRuleConfig>
                {
                    new SimpleRuleConfig{ Team = "player", IconType = string.Empty, FilePattern = "CustomFootStepSounds/Player" },
                    new SimpleRuleConfig{ NameKey = "Cname_Scav", IconType = string.Empty, FilePattern = "CustomFootStepSounds/Scav" },
                    new SimpleRuleConfig{ NameKey = "Cname_Usec", IconType = string.Empty, FilePattern = "CustomFootStepSounds/Usec" },
                },
                DefaultPattern = "CustomFootStepSounds/{team}/{rank}_{voiceType}_{soundKey}{ext}",
                PriorityInterruptEnabled = false,
                BindVariantIndexPerEnemy = false,
            };
            SanitizeConfig(cfg);
            return cfg;
        }

        private static string CreateDefaultJsonText()
        {
            var sample = new
            {
                _comment = "自定义脚步声规则 - 简化版示例。与语音规则结构一致；soundKey 建议为 footstep_walk_light, footstep_walk_heavy, footstep_run_light, footstep_run_heavy, dash 等。",
                Debug = new
                {
                    _comment = "日志配置：Enabled=开关、Level=Error/Warning/Info/Debug/Verbose、ValidateFileExists=路由前是否检查文件存在",
                    Enabled = true,
                    Level = "Debug",
                    ValidateFileExists = true
                },
                Fallback = new
                {
                    _comment = "回退策略：找不到或出错时是否使用原始声音；可选扩展名优先级",
                    UseOriginalWhenMissing = true,
                    PreferredExtensions = new[] { ".mp3", ".wav" }
                },
                DefaultPattern = "CustomFootStepSounds/{team}/{rank}_{voiceType}_{soundKey}{ext}",
                MinCooldownSeconds = 0.3,
                UseSimpleRules = true,
                SimpleRules = new object[]
                {
                    new { _comment = "玩家角色（nameKey为空）按 team 匹配", Team = "player", IconType = "", FilePattern = "CustomFootStepSounds/Player" },
                    new { _comment = "示例：为某个敌人 NameKey 指定根目录，文件命名遵循 {icon}_{voiceType}_{soundKey}{ext}", NameKey = "Cname_Scav", IconType = "", FilePattern = "CustomFootStepSounds/Scav" },
                    new { NameKey = "Cname_Usec", IconType = "", FilePattern = "CustomFootStepSounds/Usec" }
                },
                PriorityInterruptEnabled = false,
                BindVariantIndexPerEnemy = false,
                Rules = new object[] { }
            };
            return JsonConvert.SerializeObject(sample, Formatting.Indented);
        }

        private static void SanitizeConfig(VoiceConfig cfg)
        {
            if (cfg == null) return;

                // Clamp/Default MinCooldownSeconds for footstep module
                try
                {
                    if (cfg.MinCooldownSeconds <= 0f || float.IsNaN(cfg.MinCooldownSeconds) || float.IsInfinity(cfg.MinCooldownSeconds))
                        cfg.MinCooldownSeconds = 0.3f;
                    else if (cfg.MinCooldownSeconds < 0.05f)
                        cfg.MinCooldownSeconds = 0.05f;
                    else if (cfg.MinCooldownSeconds > 2f)
                        cfg.MinCooldownSeconds = 2f;
                }
                catch { cfg.MinCooldownSeconds = 0.3f; }

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
            t = t.Replace("\\\\", "\\").Replace("//", "/");
            if (!ReferenceEquals(t, p))
            {
                FootstepLogger.Info($"[CFS:Config] 已移除 legacy token {"{enemyType}"} 并清理模板: {t}");
            }
            return t;
        }
    }
}

