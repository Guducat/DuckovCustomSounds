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
        private static readonly Dictionary<string, ModuleConfig> _modules = new Dictionary<string, ModuleConfig>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, ILog> _loggers = new Dictionary<string, ILog>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _gate = new object();
        private static bool _globalEnabled = true;
        private static string _settingsPath;
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
                if (!string.IsNullOrEmpty(modRoot))
                {
                    try { Directory.CreateDirectory(modRoot); } catch { }
                    _settingsPath = Path.Combine(modRoot, "settings.json");
                }
                LoadOrCreateSettings();
                _initialized = true;
            }
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
                        ["CustomEnemySounds"] = new ModuleSection { level = "Info" },
                        ["CustomBGM"] = new ModuleSection { level = "Info" },
                        ["CustomGrenadeSounds"] = new ModuleSection { level = "Info" },
                    }
                }
            };
        }

        private static LogLevel ParseLevel(string name, LogLevel fallback)
        {
            if (Enum.TryParse<LogLevel>(name, true, out var lv)) return lv;
            return fallback;
        }

        private static void ApplySettings(SettingsRoot root)
        {
            _modules.Clear();
            _globalEnabled = root?.logging?.enabled ?? true;
            var def = ParseLevel(root?.logging?.defaultLevel ?? "Info", LogLevel.Info);

            string[] known = new[] { "Core", "CustomEnemySounds", "CustomBGM", "CustomGrenadeSounds" };
            foreach (var m in known)
            {
                _modules[m] = new ModuleConfig { Level = def, Enabled = true, Explicit = false };
            }

            if (root?.logging?.modules != null)
            {
                foreach (var kv in root.logging.modules)
                {
                    _modules[kv.Key] = new ModuleConfig
                    {
                        Level = ParseLevel(kv.Value?.level ?? "Info", def),
                        Enabled = true,
                        Explicit = true
                    };
                }
            }
        }

        public static bool GlobalEnabled => _globalEnabled;
        public static LogLevel GetModuleLevel(string module)
        {
            lock (_gate)
            {
                return _modules.TryGetValue(module, out var mc)
                    ? mc.Level
                    : (_modules.TryGetValue("Core", out var core) ? core.Level : LogLevel.Info);
            }
        }
        public static bool IsModuleEnabled(string module)
        {
            lock (_gate) { return _globalEnabled && (_modules.TryGetValue(module, out var mc) ? mc.Enabled : true); }
        }

        public static bool ShouldLog(string module, LogLevel level)
        {
            lock (_gate)
            {
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

        // 文件快速开关：存在 debug_off 或 .nolog 时，所有模块级别钳制至 Info（仍保留 Error/Info）
        public static void ApplyFileSwitches(string modRoot)
        {
            try
            {
                if (string.IsNullOrEmpty(modRoot)) return;
                var p1 = Path.Combine(modRoot, "debug_off");
                var p2 = Path.Combine(modRoot, ".nolog");
                if (File.Exists(p1) || File.Exists(p2))
                {
                    lock (_gate)
                    {
                        foreach (var mc in _modules.Values)
                        {
                            if (mc.Level > LogLevel.Info) mc.Level = LogLevel.Info;
                        }
                    }
                }
            }
            catch { }
        }

        public static ILog GetLogger(string module)
        {
            lock (_gate)
            {
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
            public string Module { get; }
            internal ModuleLogger(string module) { Module = string.IsNullOrEmpty(module) ? "Core" : module; }

            public void Error(string msg, Exception? ex = null)
            {
                if (!ShouldLog(Module, LogLevel.Error)) return;
                UnityEngine.Debug.LogError($"[{Module}] {msg}{(ex != null ? "\n" + ex : string.Empty)}");
            }
            public void Warning(string msg)
            {
                if (!ShouldLog(Module, LogLevel.Warning)) return;
                UnityEngine.Debug.LogWarning($"[{Module}] {msg}");
            }
            public void Info(string msg)
            {
                if (!ShouldLog(Module, LogLevel.Info)) return;
                UnityEngine.Debug.Log($"[{Module}] {msg}");
            }
            public void Debug(string msg)
            {
                if (!ShouldLog(Module, LogLevel.Debug)) return;
                UnityEngine.Debug.Log($"[{Module}:Debug] {msg}");
            }
            public void Verbose(string msg)
            {
                if (!ShouldLog(Module, LogLevel.Verbose)) return;
                UnityEngine.Debug.Log($"[{Module}:Verbose] {msg}");
            }
        }
    }
}

