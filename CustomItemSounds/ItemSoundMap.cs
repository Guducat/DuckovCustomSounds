using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace DuckovCustomSounds.CustomItemSounds
{
    /// <summary>
    /// 物品使用音效映射（从 JSON 读取）。
    /// - 允许按 TypeID 指定 soundKey（以及可选的 actionKey/finishKey）
    /// - 可为“无原版事件”的物品提供默认 soundKey（defaultWhenNoEvent）
    /// - 保守：解析失败或未命中时不做更改
    /// </summary>
    internal static class ItemSoundMap
    {
        private const string FileName = "item_sound_map.json";

        private static MapData _data = new MapData();
        private static string? _configPath;
        private static bool _initialized;

        internal static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                var baseDir = ItemConfig.GetBaseDir();
                if (string.IsNullOrWhiteSpace(baseDir)) baseDir = ModBehaviour.ModFolderName;
                _configPath = Path.Combine(baseDir, FileName);

                EnsureFileExists();
                Load();
                ItemLogger.Info($"[ItemMap] 映射加载完成: items={_data.items.Count}, defaultWhenNoEvent={_data.defaultWhenNoEvent ?? "<none>"}");
            }
            catch (Exception ex)
            {
                ItemLogger.Warning($"[ItemMap] 初始化失败: {ex.Message}");
                _data = new MapData();
            }
        }

        private static void EnsureFileExists()
        {
            try
            {
                var configPath = _configPath;
                if (string.IsNullOrWhiteSpace(configPath)) return;

                var dir = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                if (!File.Exists(configPath))
                {
                    var def = CreateDefault();
                    var json = JsonConvert.SerializeObject(def, Formatting.Indented);
                    File.WriteAllText(configPath, json);
                    ItemLogger.Info($"[ItemMap] 已生成默认映射: {configPath}");
                }
            }
            catch (Exception ex)
            {
                ItemLogger.Warning($"[ItemMap] 生成默认映射失败: {ex.Message}");
            }
        }

        private static void Load()
        {
            try
            {
                var configPath = _configPath;
                if (string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath)) { _data = new MapData(); return; }
                var text = File.ReadAllText(configPath);
                _data = JsonConvert.DeserializeObject<MapData>(text) ?? new MapData();
                Sanitize(_data);
            }
            catch (Exception ex)
            {
                ItemLogger.Warning($"[ItemMap] 读取/解析失败，使用空配置: {ex.Message}");
                _data = new MapData();
            }
        }

        private static void Sanitize(MapData d)
        {
            if (d.items == null) d.items = new Dictionary<string, ItemEntry>(StringComparer.OrdinalIgnoreCase);
            if (d.aliases == null) d.aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        private static MapData CreateDefault()
        {
            return new MapData
            {
                defaultWhenNoEvent = null, // 比如设为 "food" 可为所有无事件的物品播放食物音效
                items = new Dictionary<string, ItemEntry>(StringComparer.OrdinalIgnoreCase)
                {
                    // 示例：
                    // ["403"] = new ItemEntry { soundKey = "bandage" },
                    // ["84"] = new ItemEntry { soundKey = "food" },
                },
                // 以 bandage 作为主分类；如遇到原版 use_meds，将自动映射到 bandage
                aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["meds"] = "bandage",
                }
            };
        }

        /// <summary>
        /// 用于“拦截并替换”路径：如果此 TypeID 配置了自定义 soundKey/actionKey/finishKey，则返回替换后的键；
        /// 否则按别名表替换（如 meds->bandage）；若仍无替换，则返回原键。
        /// </summary>
        internal static string ResolveForReplace(string? typeIdStr, string originalKey, CustomItemSounds_Patches.ItemUsePhase? phase)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(typeIdStr) && _data.items.TryGetValue(typeIdStr, out var entry) && entry != null)
                {
                    if (phase == CustomItemSounds_Patches.ItemUsePhase.Action && !string.IsNullOrWhiteSpace(entry.actionKey)) return entry.actionKey;
                    if (phase == CustomItemSounds_Patches.ItemUsePhase.Finish && !string.IsNullOrWhiteSpace(entry.finishKey)) return entry.finishKey;
                    if (!string.IsNullOrWhiteSpace(entry.soundKey)) return entry.soundKey;
                }

                if (!string.IsNullOrWhiteSpace(originalKey) && _data.aliases.TryGetValue(originalKey, out var alias))
                {
                    return alias ?? originalKey;
                }
            }
            catch { }
            return originalKey;
        }

        /// <summary>
        /// 用于“无原版事件时主动添加”路径：优先取 TypeID 的 actionKey/finishKey/soundKey；
        /// 否则回退 defaultWhenNoEvent；都没有则返回 null。
        /// forceWhenNoEvent=false 则返回 null（不注入）。
        /// </summary>
        internal static string? ResolveForInjection(string? typeIdStr, CustomItemSounds_Patches.ItemUsePhase phase)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(typeIdStr) && _data.items.TryGetValue(typeIdStr, out var entry) &&
                    entry != null)
                {
                    if (entry.forceWhenNoEvent == false) return null;
                    if (phase == CustomItemSounds_Patches.ItemUsePhase.Action &&
                        !string.IsNullOrWhiteSpace(entry.actionKey)) return entry.actionKey;
                    if (phase == CustomItemSounds_Patches.ItemUsePhase.Finish &&
                        !string.IsNullOrWhiteSpace(entry.finishKey)) return entry.finishKey;
                    if (!string.IsNullOrWhiteSpace(entry.soundKey)) return entry.soundKey;
                }
            }
            catch
            {
            }

            return _data.defaultWhenNoEvent; // 可能为 null
            }


        /// <summary>
        /// 可选文件基名（fileBase）解析：允许多个 TypeID 共享同一个实际文件名（如 64.mp3）。
        /// 若未配置，返回 null，调用方应回退使用 soundKey。
        /// </summary>
        internal static string? ResolveFileBase(string? typeIdStr, string soundKey, CustomItemSounds_Patches.ItemUsePhase? phase)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(typeIdStr) && _data.items.TryGetValue(typeIdStr, out var entry) && entry != null)
                {
                    if (phase == CustomItemSounds_Patches.ItemUsePhase.Action && !string.IsNullOrWhiteSpace(entry.actionFileBase)) return entry.actionFileBase;
                    if (phase == CustomItemSounds_Patches.ItemUsePhase.Finish && !string.IsNullOrWhiteSpace(entry.finishFileBase)) return entry.finishFileBase;
                    if (!string.IsNullOrWhiteSpace(entry.fileBase)) return entry.fileBase;
                }
            }
            catch { }
            return null;
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
            public string? actionKey { get; set; }
            public string? finishKey { get; set; }

            // 允许文件基名覆盖（多 TypeID 共用同一 <fileBase>.*）
            public string? fileBase { get; set; }
            public string? actionFileBase { get; set; }
            public string? finishFileBase { get; set; }

            public bool? forceWhenNoEvent { get; set; } // 缺省=true
        }
    }
}
