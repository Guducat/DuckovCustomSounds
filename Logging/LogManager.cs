using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace DuckovCustomSounds.Logging
{
    // 统一日志级别（数值越大越详细）
    public enum LogLevel
    {
        Error = 0,
        Warning = 1,
        Info = 2,
        Debug = 3,
        Verbose = 4,
    }

    // 统一日志接口（供 Core 等新代码直接使用）
    public interface ILog
    {
        string Module { get; }
        ILog ForScope(params string[] scopes);
        void Error(string msg, Exception? ex = null);
        void Warning(string msg);
        void Info(string msg);
        void Debug(string msg);
        void Verbose(string msg);
    }

    internal sealed class ModuleConfig
    {
        public LogLevel Level;
        public bool Enabled = true;
        public bool Explicit; // true: 来自 settings.json 的模块项；false: 默认/兼容回退
    }

    public static class LogManager
    {
        private const string ModName = "DuckovCustomSounds";
        private static readonly Dictionary<string, ModuleConfig> _modules = new Dictionary<string, ModuleConfig>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, ILog> _loggers = new Dictionary<string, ILog>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, ModuleConfig> _voiceRuleFallbacks = new Dictionary<string, ModuleConfig>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> _moduleAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["CustomEnemySounds"] = "Enemy",
            ["CustomFootStepSounds"] = "Footstep",
            ["CustomGunSounds"] = "Gun",
            ["CustomGrenadeSounds"] = "Grenade",
            ["CustomItemSounds"] = "Item",
            ["CustomMeleeSounds"] = "Melee",
            ["CustomHitAndKillSounds"] = "HitAndKill",
            ["CustomBGM"] = "BGM",
            ["BossBGM"] = "BGM",
            ["CustomBossBGM"] = "BGM",
            ["CustomHomeBGM"] = "HomeBGM",
            ["CustomSceneBGM"] = "SceneBGM",
            ["Extraction"] = "ExtractionBGM",
            ["CustomExtractionBGM"] = "ExtractionBGM",
        };
        private static readonly Dictionary<string, string> _scopeModuleAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["CES"] = "Enemy",
            ["CFS"] = "Footstep",
            ["CustomSounds"] = "Core",
            ["DuckovCustomSounds"] = "Core",
        };
        private static readonly string[] _knownModules = new[]
        {
            "Core",
            "SoundPack",
            "Enemy",
            "BGM",
            "HomeBGM",
            "SceneBGM",
            "ExtractionBGM",
            "Gun",
            "Grenade",
            "Footstep",
            "Item",
            "Melee",
            "HitAndKill",
        };
        private static readonly object _gate = new object();
        private static bool _globalEnabled = true;
        private static string _settingsPath = string.Empty;
        private static bool _initialized;

        // JSON 结构定义
        private sealed class SettingsRoot
        {
            public LoggingSection logging { get; set; } = new LoggingSection();
        }
        private sealed class LoggingSection
        {
            public bool enabled { get; set; } = true;
            public string defaultLevel { get; set; } = "Info";
            public Dictionary<string, ModuleSection> modules { get; set; } = new Dictionary<string, ModuleSection>(StringComparer.OrdinalIgnoreCase);
        }
        private sealed class ModuleSection
        {
            public string level { get; set; } = "Info";
        }

        public static void Initialize(string modRoot)
        {
            lock (_gate)
            {
                if (_initialized && !string.IsNullOrEmpty(_settingsPath)) return;
                ConfigureSettingsPath(modRoot);
                LoadOrCreateSettings();
                ApplyFileSwitchesLocked(modRoot);
                _initialized = true;
            }
        }

        public static bool ReloadSettings(string modRoot)
        {
            lock (_gate)
            {
                ConfigureSettingsPath(modRoot);
                LoadOrCreateSettings();
                ApplyFileSwitchesLocked(modRoot);
                _initialized = true;
                return true;
            }
        }

        private static void ConfigureSettingsPath(string modRoot)
        {
            if (string.IsNullOrEmpty(modRoot)) return;

            try { Directory.CreateDirectory(modRoot); } catch { }
            _settingsPath = Path.Combine(modRoot, "settings.json");
        }

        private static void LoadOrCreateSettings()
        {
            SettingsRoot root;
            if (string.IsNullOrEmpty(_settingsPath))
            {
                root = CreateDefaultSettings();
            }
            else if (!File.Exists(_settingsPath))
            {
                root = CreateDefaultSettings();
                try
                {
                    var json = JsonConvert.SerializeObject(root, Formatting.Indented);
                    File.WriteAllText(_settingsPath, json);
                }
                catch { }
            }
            else
            {
                try
                {
                    var text = File.ReadAllText(_settingsPath);
                    root = JsonConvert.DeserializeObject<SettingsRoot>(text) ?? CreateDefaultSettings();
                }
                catch
                {
                    root = CreateDefaultSettings();
                }
            }
            ApplySettings(root);
        }

        private static SettingsRoot CreateDefaultSettings()
        {
            return new SettingsRoot
            {
                logging = new LoggingSection
                {
                    enabled = true,
                    defaultLevel = "Info",
                    modules = new Dictionary<string, ModuleSection>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Core"] = new ModuleSection { level = "Info" },
                        ["SoundPack"] = new ModuleSection { level = "Info" },
                        ["Enemy"] = new ModuleSection { level = "Info" },
                        ["BGM"] = new ModuleSection { level = "Info" },
                        ["HomeBGM"] = new ModuleSection { level = "Info" },
                        ["SceneBGM"] = new ModuleSection { level = "Info" },
                        ["ExtractionBGM"] = new ModuleSection { level = "Info" },
                        ["Gun"] = new ModuleSection { level = "Info" },
                        ["Grenade"] = new ModuleSection { level = "Info" },
                        ["Footstep"] = new ModuleSection { level = "Info" },
                        ["Item"] = new ModuleSection { level = "Info" },
                        ["Melee"] = new ModuleSection { level = "Info" },
                        ["HitAndKill"] = new ModuleSection { level = "Info" },
                    }
                }
            };
        }

        private static LogLevel ParseLevel(string name, LogLevel fallback)
        {
            name = NormalizeLevelName(name);
            if (Enum.TryParse<LogLevel>(name, true, out var lv)) return lv;
            return fallback;
        }

        private static string NormalizeLevelName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            value = value.Trim();
            return value.Equals("Warn", StringComparison.OrdinalIgnoreCase) ? "Warning" : value;
        }

        private static string NormalizeModuleName(string module)
        {
            if (string.IsNullOrWhiteSpace(module)) return "Core";

            var trimmed = module.Trim();
            return _moduleAliases.TryGetValue(trimmed, out var canonical) ? canonical : trimmed;
        }

        private static void ApplySettings(SettingsRoot root)
        {
            _modules.Clear();
            _globalEnabled = root?.logging?.enabled ?? true;
            var def = ParseLevel(root?.logging?.defaultLevel ?? "Info", LogLevel.Info);

            foreach (var m in _knownModules)
            {
                _modules[m] = new ModuleConfig { Level = def, Enabled = true, Explicit = false };
            }

            if (root?.logging?.modules != null)
            {
                foreach (var kv in root.logging.modules)
                {
                    var module = NormalizeModuleName(kv.Key);
                    _modules[module] = new ModuleConfig
                    {
                        Level = ParseLevel(kv.Value?.level ?? "Info", def),
                        Enabled = true,
                        Explicit = true
                    };
                }
            }

            ApplyVoiceRulesFallbacksLocked();
        }

        public static bool GlobalEnabled => _globalEnabled;
        public static LogLevel GetModuleLevel(string module)
        {
            lock (_gate)
            {
                module = NormalizeModuleName(module);
                return _modules.TryGetValue(module, out var mc)
                    ? mc.Level
                    : (_modules.TryGetValue("Core", out var core) ? core.Level : LogLevel.Info);
            }
        }
        public static bool IsModuleEnabled(string module)
        {
            lock (_gate)
            {
                module = NormalizeModuleName(module);
                return _globalEnabled && (_modules.TryGetValue(module, out var mc) ? mc.Enabled : true);
            }
        }

        public static bool ShouldLog(string module, LogLevel level)
        {
            lock (_gate)
            {
                module = NormalizeModuleName(module);
                if (!_globalEnabled) return false;
                if (!_modules.TryGetValue(module, out var mc))
                {
                    mc = new ModuleConfig { Level = LogLevel.Info, Enabled = true, Explicit = false };
                    _modules[module] = mc;
                }
                if (!mc.Enabled) return false;
                return mc.Level >= level; // 级别上限：Info(2) 允许 Error/Warning/Info；禁止 Debug/Verbose
            }
        }

        // 来自 voice_rules.json 的兼容回退（仅当 settings.json 未对该模块显式指定时生效）
        public static void ApplyVoiceRulesFallback(string module, bool enabled, LogLevel level)
        {
            lock (_gate)
            {
                module = NormalizeModuleName(module);
                _voiceRuleFallbacks[module] = new ModuleConfig { Level = level, Enabled = enabled, Explicit = false };
                if (!_modules.TryGetValue(module, out var mc))
                {
                    _modules[module] = new ModuleConfig { Level = level, Enabled = enabled, Explicit = false };
                    return;
                }
                if (!mc.Explicit)
                {
                    mc.Level = level;
                    mc.Enabled = enabled;
                }
            }
        }

        private static void ApplyVoiceRulesFallbacksLocked()
        {
            foreach (var kv in _voiceRuleFallbacks)
            {
                if (!_modules.TryGetValue(kv.Key, out var mc) || !mc.Explicit)
                {
                    _modules[kv.Key] = new ModuleConfig
                    {
                        Level = kv.Value.Level,
                        Enabled = kv.Value.Enabled,
                        Explicit = false
                    };
                }
            }
        }

        // 文件快速开关：存在 debug_off 或 .nolog 时，所有模块级别钳制至 Info（仍保留 Error/Info）
        public static void ApplyFileSwitches(string modRoot)
        {
            try
            {
                lock (_gate)
                {
                    ApplyFileSwitchesLocked(modRoot);
                }
            }
            catch { }
        }

        private static void ApplyFileSwitchesLocked(string modRoot)
        {
            if (string.IsNullOrEmpty(modRoot)) return;

            var p1 = Path.Combine(modRoot, "debug_off");
            var p2 = Path.Combine(modRoot, ".nolog");
            if (!File.Exists(p1) && !File.Exists(p2)) return;

            foreach (var mc in _modules.Values)
            {
                if (mc.Level > LogLevel.Info) mc.Level = LogLevel.Info;
            }
        }

        public static ILog GetLogger(string module)
        {
            lock (_gate)
            {
                module = NormalizeModuleName(module);
                if (_loggers.TryGetValue(module, out var l)) return l;
                var nl = new ModuleLogger(module);
                _loggers[module] = nl;
                if (!_modules.ContainsKey(module))
                {
                    _modules[module] = new ModuleConfig
                    {
                        Level = _modules.TryGetValue("Core", out var core) ? core.Level : LogLevel.Info,
                        Enabled = true,
                        Explicit = false
                    };
                }
                return nl;
            }
        }

        private sealed class ModuleLogger : ILog
        {
            private readonly string[] _scopes;

            public string Module { get; }

            internal ModuleLogger(string module)
                : this(module, Array.Empty<string>())
            {
            }

            private ModuleLogger(string module, string[] scopes)
            {
                Module = string.IsNullOrEmpty(module) ? "Core" : module;
                _scopes = scopes ?? Array.Empty<string>();
            }

            public ILog ForScope(params string[] scopes)
            {
                return new ModuleLogger(Module, MergeScopes(Module, _scopes, scopes));
            }

            public void Error(string msg, Exception? ex = null)
            {
                if (!ShouldLog(Module, LogLevel.Error)) return;
                UnityEngine.Debug.LogError(Format(LogLevel.Error, msg, ex));
            }
            public void Warning(string msg)
            {
                if (!ShouldLog(Module, LogLevel.Warning)) return;
                UnityEngine.Debug.LogWarning(Format(LogLevel.Warning, msg));
            }
            public void Info(string msg)
            {
                if (!ShouldLog(Module, LogLevel.Info)) return;
                UnityEngine.Debug.Log(Format(LogLevel.Info, msg));
            }
            public void Debug(string msg)
            {
                if (!ShouldLog(Module, LogLevel.Debug)) return;
                UnityEngine.Debug.Log(Format(LogLevel.Debug, msg));
            }
            public void Verbose(string msg)
            {
                if (!ShouldLog(Module, LogLevel.Verbose)) return;
                UnityEngine.Debug.Log(Format(LogLevel.Verbose, msg));
            }

            private string Format(LogLevel level, string msg, Exception? ex = null)
            {
                var scopes = new List<string>(_scopes);
                var message = ExtractLeadingScopes(msg, Module, scopes);
                var formatted = BuildPrefix(level, scopes);
                if (!string.IsNullOrEmpty(message))
                {
                    formatted += " " + message;
                }

                return ex != null ? formatted + "\n" + ex : formatted;
            }

            private string BuildPrefix(LogLevel level, IEnumerable<string> scopes)
            {
                var prefix = new System.Text.StringBuilder();
                AppendSegment(prefix, ModName);
                AppendSegment(prefix, Module);
                foreach (var scope in scopes)
                {
                    AppendSegment(prefix, scope);
                }
                AppendSegment(prefix, level.ToString());
                return prefix.ToString();
            }

            private static void AppendSegment(System.Text.StringBuilder prefix, string? value)
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                prefix.Append('[').Append(value.Trim()).Append(']');
            }

            private static string ExtractLeadingScopes(string? message, string module, List<string> scopes)
            {
                var text = message ?? string.Empty;

                while (true)
                {
                    text = text.TrimStart();
                    if (!text.StartsWith("[", StringComparison.Ordinal)) return text;

                    var end = text.IndexOf(']');
                    if (end <= 1) return text;

                    var tag = text.Substring(1, end - 1);
                    AddScopeTag(module, scopes, tag);
                    text = text.Substring(end + 1);
                }
            }

            private static string[] MergeScopes(string module, string[] current, string[]? next)
            {
                var merged = new List<string>(current ?? Array.Empty<string>());
                if (next == null) return merged.ToArray();

                foreach (var scope in next)
                {
                    AddScopeTag(module, merged, scope);
                }

                return merged.ToArray();
            }

            private static void AddScopeTag(string module, List<string> scopes, string? tag)
            {
                if (string.IsNullOrWhiteSpace(tag)) return;

                foreach (var raw in tag.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    AddScopeSegment(module, scopes, raw);
                }
            }

            private static void AddScopeSegment(string module, List<string> scopes, string? raw)
            {
                var scope = NormalizeScopeSegment(raw);
                if (string.IsNullOrEmpty(scope)) return;
                if (IsModuleScope(module, scope)) return;
                if (Enum.TryParse(scope, true, out LogLevel _)) return;

                foreach (var existing in scopes)
                {
                    if (string.Equals(existing, scope, StringComparison.OrdinalIgnoreCase)) return;
                }

                scopes.Add(scope);
            }

            private static string NormalizeScopeSegment(string? raw)
            {
                if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
                return raw.Trim().Trim('[', ']').Trim();
            }

            private static bool IsModuleScope(string module, string scope)
            {
                if (string.IsNullOrWhiteSpace(scope)) return true;
                if (string.Equals(scope, module, StringComparison.OrdinalIgnoreCase)) return true;

                return _scopeModuleAliases.TryGetValue(scope, out var canonical)
                    && string.Equals(canonical, module, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
