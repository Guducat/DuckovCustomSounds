using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DuckovCustomSounds.ModConfig;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DuckovCustomSounds.Logging
{
    internal static class LoggingConfig
    {
        private static readonly ModConfigScope Scope = ModConfigScopes.Logging;

        private static readonly string[] Modules =
        {
            "Core",
            "SoundPack",
            "Enemy",
            "Footstep",
            "BGM",
            "HomeBGM",
            "SceneBGM",
            "ExtractionBGM",
            "Gun",
            "Grenade",
            "Item",
            "Melee",
        };

        private static readonly string[] LevelNames =
        {
            "错误",
            "警告",
            "信息",
            "调试",
            "详细",
        };

        private static readonly Dictionary<string, string> ModuleAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["CustomEnemySounds"] = "Enemy",
            ["CustomFootStepSounds"] = "Footstep",
            ["CustomGunSounds"] = "Gun",
            ["CustomGrenadeSounds"] = "Grenade",
            ["CustomItemSounds"] = "Item",
            ["CustomMeleeSounds"] = "Melee",
            ["CustomBGM"] = "BGM",
            ["BossBGM"] = "BGM",
            ["CustomBossBGM"] = "BGM",
            ["CustomHomeBGM"] = "HomeBGM",
            ["CustomSceneBGM"] = "SceneBGM",
            ["Extraction"] = "ExtractionBGM",
            ["CustomExtractionBGM"] = "ExtractionBGM",
        };

        private static readonly Action<string> _onChangedHandler = OnOptionsChanged;
        private static readonly ILog Log = LogManager.GetLogger("Core");
        private static bool _initialized;
        private static bool _handlingChange;

        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                if (!ModConfigAPI.IsAvailable())
                {
                    Log.Debug("ModConfig 不可用，跳过日志配置 UI 集成");
                    return;
                }

                var settings = ReadSettings();
                RegisterControls(settings);
                ModConfigAPI.SafeAddOnOptionsChangedDelegate(_onChangedHandler);

                _initialized = true;
                Log.Info("日志 ModConfig UI 初始化完成");
            }
            catch (Exception ex)
            {
                Log.Warning($"日志 ModConfig UI 初始化失败: {ex.Message}");
            }
        }

        private static void RegisterControls(SettingsSnapshot settings)
        {
            ModConfigAPI.SafeAddBoolDropdownList(Scope, "enabled", "启用日志输出", settings.Enabled);

            foreach (var module in Modules)
            {
                string moduleDisplayName = ModConfigScopes.GetLoggingModuleDisplayName(module);
                ModConfigAPI.SafeAddDropdownList(Scope, ModuleKey(module), $"{moduleDisplayName}日志等级", BuildLevelOptions(), typeof(int), (int)settings.GetLevel(module));
            }
        }

        private static SortedDictionary<string, object> BuildLevelOptions()
        {
            var options = new SortedDictionary<string, object>(StringComparer.Ordinal);
            for (var i = 0; i < LevelNames.Length; i++)
            {
                options[LevelNames[i]] = i;
            }

            return options;
        }

        private static void OnOptionsChanged(string key)
        {
            if (!IsLoggingKey(key)) return;
            if (_handlingChange) return;

            try
            {
                _handlingChange = true;
                var settings = ReadFromModConfig();
                WriteSettings(settings);
                LogManager.ReloadSettings(ModBehaviour.RootFolderName);
                Log.Info("日志配置已从 ModConfig 更新");
            }
            catch (Exception ex)
            {
                Log.Warning($"日志配置更新失败: {ex.Message}");
            }
            finally
            {
                _handlingChange = false;
            }
        }

        private static bool IsLoggingKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return true;
            if (key.Equals("enabled", StringComparison.OrdinalIgnoreCase)) return true;
            if (key.StartsWith("level_", StringComparison.OrdinalIgnoreCase)) return true;
            return ModConfigAPI.IsKeyForMod(key, Scope);
        }

        private static SettingsSnapshot ReadFromModConfig()
        {
            var fallback = ReadSettings();
            var settings = new SettingsSnapshot
            {
                Enabled = ModConfigAPI.SafeLoad(Scope, "enabled", fallback.Enabled),
                DefaultLevel = fallback.DefaultLevel,
            };

            foreach (var module in Modules)
            {
                var level = (LogLevel)ClampLevelIndex(ModConfigAPI.SafeLoad(Scope, ModuleKey(module), (int)fallback.GetLevel(module)));
                settings.Levels[module] = level;
            }

            return settings;
        }

        private static SettingsSnapshot ReadSettings()
        {
            var settings = new SettingsSnapshot();
            var path = SettingsPath;

            if (!File.Exists(path)) return settings;

            try
            {
                var text = File.ReadAllText(path, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(text)) return settings;

                var root = JObject.Parse(text);
                var logging = root["logging"] as JObject;
                if (logging == null) return settings;

                settings.Enabled = logging.Value<bool?>("enabled") ?? true;
                settings.DefaultLevel = ParseLevel(logging.Value<string>("defaultLevel"), LogLevel.Info);

                var modules = logging["modules"] as JObject;
                if (modules == null) return settings;

                foreach (var property in modules.Properties())
                {
                    var module = NormalizeModuleName(property.Name);
                    var moduleObject = property.Value as JObject;
                    var level = ParseLevel(moduleObject?.Value<string>("level"), settings.DefaultLevel);
                    settings.Levels[module] = level;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"读取日志配置失败，使用默认值: {ex.Message}");
            }

            return settings;
        }

        private static void WriteSettings(SettingsSnapshot settings)
        {
            Directory.CreateDirectory(ModBehaviour.RootFolderName);

            var root = ReadRootObject();
            var logging = new JObject
            {
                ["enabled"] = settings.Enabled,
                ["defaultLevel"] = settings.DefaultLevel.ToString(),
            };

            var modules = new JObject();
            foreach (var module in Modules)
            {
                modules[module] = new JObject
                {
                    ["level"] = settings.GetLevel(module).ToString(),
                };
            }

            logging["modules"] = modules;
            root["logging"] = logging;

            var json = JsonConvert.SerializeObject(root, Formatting.Indented);
            File.WriteAllText(SettingsPath, json, new UTF8Encoding(false));
        }

        private static JObject ReadRootObject()
        {
            var path = SettingsPath;
            if (!File.Exists(path)) return new JObject();

            try
            {
                var text = File.ReadAllText(path, Encoding.UTF8);
                return string.IsNullOrWhiteSpace(text) ? new JObject() : JObject.Parse(text);
            }
            catch
            {
                return new JObject();
            }
        }

        private static string SettingsPath => Path.Combine(ModBehaviour.RootFolderName, "settings.json");

        private static string ModuleKey(string module) => "level_" + module;

        private static string NormalizeModuleName(string module)
        {
            if (string.IsNullOrWhiteSpace(module)) return "Core";

            var trimmed = module.Trim();
            return ModuleAliases.TryGetValue(trimmed, out var canonical) ? canonical : trimmed;
        }

        private static LogLevel ParseLevel(string? value, LogLevel fallback)
        {
            value = NormalizeLevelName(value);
            return Enum.TryParse(value, true, out LogLevel level) ? level : fallback;
        }

        private static string? NormalizeLevelName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            value = value.Trim();
            return value.Equals("Warn", StringComparison.OrdinalIgnoreCase) ? "Warning" : value;
        }

        private static int ClampLevelIndex(int value)
        {
            if (value < 0) return 0;
            return value >= LevelNames.Length ? LevelNames.Length - 1 : value;
        }

        private sealed class SettingsSnapshot
        {
            public bool Enabled { get; set; } = true;
            public LogLevel DefaultLevel { get; set; } = LogLevel.Info;
            public Dictionary<string, LogLevel> Levels { get; } = new Dictionary<string, LogLevel>(StringComparer.OrdinalIgnoreCase);

            public LogLevel GetLevel(string module)
            {
                return Levels.TryGetValue(module, out var level) ? level : DefaultLevel;
            }
        }
    }
}
