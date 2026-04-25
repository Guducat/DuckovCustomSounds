using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Duckov;
using Duckov.Utilities;

namespace DuckovCustomSounds.CustomEnemySounds.Context
{
    /// <summary>
    /// Preloads and indexes character random presets to avoid repeated reflection.
    /// </summary>
    internal static class PresetCache
    {
        private static readonly Dictionary<string, CharacterRandomPreset> ByNameKey =
            new Dictionary<string, CharacterRandomPreset>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<Teams, List<CharacterRandomPreset>> ByTeam =
            new Dictionary<Teams, List<CharacterRandomPreset>>();

        private static readonly Dictionary<string, List<CharacterRandomPreset>> ByIcon =
            new Dictionary<string, List<CharacterRandomPreset>>(StringComparer.OrdinalIgnoreCase);

        private static readonly List<CharacterRandomPreset> EmptyList = new List<CharacterRandomPreset>();

        private static CharacterRandomPreset[] _presets = Array.Empty<CharacterRandomPreset>();

        public static bool IsLoaded { get; private set; }
        public static int PresetCount => _presets.Length;

        public static void EnsureLoaded()
        {
            if (!IsLoaded) Load();
        }

        public static void Load()
        {
            try
            {
                var sw = Stopwatch.StartNew();
                ClearInternal();

                var data = GameplayDataSettings.CharacterRandomPresetData;
                var list = data?.presets;
                if (list == null)
                {
                    CESLogger.Info("[PresetCache] CharacterRandomPresetData.presets is null; skip warm-up");
                    return;
                }

                var presets = list.Where(p => p != null).Distinct().ToArray();
                _presets = presets;

                foreach (var preset in presets)
                {
                    var nameKey = (preset.nameKey ?? string.Empty).Trim();
                    if (!string.IsNullOrEmpty(nameKey))
                    {
                        ByNameKey[nameKey] = preset;
                    }

                    if (!ByTeam.TryGetValue(preset.team, out var teamList))
                    {
                        teamList = new List<CharacterRandomPreset>();
                        ByTeam[preset.team] = teamList;
                    }
                    teamList.Add(preset);

                    var iconKey = GetIconKey(preset);
                    if (!string.IsNullOrEmpty(iconKey))
                    {
                        if (!ByIcon.TryGetValue(iconKey, out var iconList))
                        {
                            iconList = new List<CharacterRandomPreset>();
                            ByIcon[iconKey] = iconList;
                        }
                        iconList.Add(preset);
                    }
                }

                sw.Stop();
                CESLogger.Info($"[PresetCache] Loaded {presets.Length} presets in {sw.ElapsedMilliseconds} ms");
                IsLoaded = true;
            }
            catch (Exception ex)
            {
                CESLogger.Error("[PresetCache] Load failed", ex);
                ClearInternal();
            }
        }

        public static void Reload()
        {
            Load();
        }

        public static CharacterRandomPreset Resolve(CharacterMainControl cmc)
        {
            EnsureLoaded();
            if (cmc == null) return null;

            try
            {
                if (cmc.characterPreset != null) return cmc.characterPreset;
            }
            catch
            {
            }

            // Fallback via name key if exposed.
            var nameKey = ReflectionCache.GetValue(cmc, "nameKey") as string;
            if (string.IsNullOrEmpty(nameKey))
            {
                var presetObj = ReflectionCache.GetValue(cmc, "characterPreset");
                if (presetObj is CharacterRandomPreset reflected)
                {
                    return reflected;
                }
                nameKey = presetObj as string;
            }

            if (string.IsNullOrEmpty(nameKey)) return null;
            return GetByNameKey(nameKey);
        }

        public static CharacterRandomPreset GetByNameKey(string nameKey)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(nameKey)) return null;
            ByNameKey.TryGetValue(nameKey, out var preset);
            return preset;
        }

        public static IReadOnlyList<CharacterRandomPreset> GetByTeam(Teams team)
        {
            EnsureLoaded();
            return ByTeam.TryGetValue(team, out var list) ? list : EmptyList;
        }

        public static IReadOnlyList<CharacterRandomPreset> GetByIcon(string iconKey)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(iconKey)) return EmptyList;
            iconKey = iconKey.ToLowerInvariant();
            return ByIcon.TryGetValue(iconKey, out var list) ? list : EmptyList;
        }

        public static CharacterRandomPreset[] GetAll()
        {
            EnsureLoaded();
            return _presets;
        }

        public static void Clear()
        {
            ClearInternal();
        }

        private static void ClearInternal()
        {
            ByNameKey.Clear();
            ByTeam.Clear();
            ByIcon.Clear();
            _presets = Array.Empty<CharacterRandomPreset>();
            IsLoaded = false;
        }

        private static string GetIconKey(CharacterRandomPreset preset)
        {
            if (preset == null) return string.Empty;
            var iconObj = ReflectionCache.GetValue(preset, "characterIconType");
            return iconObj?.ToString().Trim().ToLowerInvariant() ?? string.Empty;
        }
    }
}
