using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace DuckovCustomSounds.CustomGrenadeSounds
{
    internal static class GrenadeSoundMap
    {
        private const string FileName = "grenade_sound_map.json";

        private static MapData _data = new MapData();
        private static string? _configPath;
        private static bool _initialized;

        internal static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                var baseDir = GrenadeSoundResolver.GetBaseDir();
                _configPath = Path.Combine(baseDir, FileName);
                EnsureFileExists();
                Load();
                GrenadeLogger.Info($"[GrenadeMap] 映射加载完成: items={_data.items.Count}, defaultWhenNoEvent={_data.defaultWhenNoEvent ?? "<none>"}");
            }
            catch (Exception ex)
            {
                GrenadeLogger.Warning($"[GrenadeMap] 初始化失败: {ex.Message}");
                _data = new MapData();
            }
        }

        internal static string ResolveForReplace(string? typeIdStr, string originalKey)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(typeIdStr) && _data.items.TryGetValue(typeIdStr, out var entry) && entry != null)
                {
                    if (!string.IsNullOrWhiteSpace(entry.soundKey)) return entry.soundKey;
                }

                if (!string.IsNullOrWhiteSpace(originalKey) && _data.aliases.TryGetValue(originalKey, out var alias))
                {
                    return string.IsNullOrWhiteSpace(alias) ? originalKey : alias;
                }
            }
            catch { }

            return originalKey;
        }

        internal static string? ResolveForInjection(string? typeIdStr)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(typeIdStr) && _data.items.TryGetValue(typeIdStr, out var entry) && entry != null)
                {
                    if (entry.forceWhenNoEvent == false) return null;
                    if (!string.IsNullOrWhiteSpace(entry.soundKey)) return entry.soundKey;
                }
            }
            catch { }

            return _data.defaultWhenNoEvent;
        }

        internal static string? ResolveFileBase(string? typeIdStr, string soundKey)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(typeIdStr) && _data.items.TryGetValue(typeIdStr, out var entry) && entry != null)
                {
                    if (!string.IsNullOrWhiteSpace(entry.fileBase)) return entry.fileBase;
                }
            }
            catch { }

            return null;
        }

        private static void EnsureFileExists()
        {
            try
            {
                var configPath = _configPath;
                if (string.IsNullOrWhiteSpace(configPath)) return;

                var dir = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                if (!File.Exists(configPath))
                {
                    var json = JsonConvert.SerializeObject(CreateDefault(), Formatting.Indented);
                    File.WriteAllText(configPath, json);
                    GrenadeLogger.Info($"[GrenadeMap] 已生成默认映射: {configPath}");
                }
            }
            catch (Exception ex)
            {
                GrenadeLogger.Warning($"[GrenadeMap] 生成默认映射失败: {ex.Message}");
            }
        }

        private static void Load()
        {
            try
            {
                var configPath = _configPath;
                if (string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
                {
                    _data = new MapData();
                    return;
                }

                var text = File.ReadAllText(configPath);
                _data = JsonConvert.DeserializeObject<MapData>(text) ?? new MapData();
                Sanitize(_data);
            }
            catch (Exception ex)
            {
                GrenadeLogger.Warning($"[GrenadeMap] 读取/解析失败，使用空配置: {ex.Message}");
                _data = new MapData();
            }
        }

        private static void Sanitize(MapData data)
        {
            if (data.items == null) data.items = new Dictionary<string, ItemEntry>(StringComparer.OrdinalIgnoreCase);
            if (data.aliases == null) data.aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        private static MapData CreateDefault()
        {
            return new MapData
            {
                defaultWhenNoEvent = null,
                items = new Dictionary<string, ItemEntry>(StringComparer.OrdinalIgnoreCase)
                {
                    // ["1234"] = new ItemEntry { soundKey = "explode_grenade", fileBase = "frag", forceWhenNoEvent = true },
                },
                aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            };
        }

        private sealed class MapData
        {
            public string? defaultWhenNoEvent { get; set; }
            public Dictionary<string, ItemEntry> items { get; set; } = new Dictionary<string, ItemEntry>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, string> aliases { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class ItemEntry
        {
            public string? soundKey { get; set; }
            public string? fileBase { get; set; }
            public bool? forceWhenNoEvent { get; set; }
        }
    }
}
