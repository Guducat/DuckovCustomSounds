using System;
using UnityEngine;
using Duckov; // Game types: AudioManager, CharacterMainControl

namespace DuckovCustomSounds.CustomEnemySounds.Context
{
    /// <summary>
    /// Snapshot of enemy metadata consumed by the rule engine.
    /// Created during AICharacterController.Init and kept immutable afterwards.
    /// </summary>
    internal sealed class EnemyContext
    {
        public int InstanceId { get; private set; }
        public GameObject GameObject { get; private set; }
        public string Team { get; private set; }
        public string IconType { get; private set; }
        public string EnemyType { get; private set; }
        public string NameKey { get; private set; }
        public float Health { get; private set; }
        public bool HasSkill { get; private set; }

        public AudioManager.VoiceType VoiceType { get; internal set; }
        public AudioManager.FootStepMaterialType FootStepMaterialType { get; private set; }

        private string _rank;
        private string _teamNormalized;

        private EnemyContext() { }

        public static EnemyContext FromCharacter(CharacterMainControl cmc,
            AudioManager.VoiceType voiceType,
            AudioManager.FootStepMaterialType foot)
        {
            var ctx = new EnemyContext
            {
                GameObject = cmc != null ? cmc.gameObject : null,
                InstanceId = cmc != null && cmc.gameObject != null ? cmc.gameObject.GetInstanceID() : 0,
                VoiceType = voiceType,
                FootStepMaterialType = foot
            };

            var preset = PresetCache.Resolve(cmc);
            PopulateFromPreset(ctx, preset);

            if (cmc != null)
            {
                if (string.IsNullOrEmpty(ctx.Team))
                {
                    try { ctx.Team = GetStringSafe(cmc.Team); }
                    catch { }
                }

                if (string.IsNullOrEmpty(ctx.IconType))
                {
                    ctx.IconType = GetStringSafe(ReflectionCache.GetValue(cmc, "characterIconType"));
                }

                if (string.IsNullOrEmpty(ctx.NameKey))
                {
                    ctx.NameKey = GetStringSafe(ReflectionCache.GetValue(cmc, "nameKey"));
                }

                if (ctx.Health <= 0f)
                {
                    ctx.Health = GetFloatSafe(ReflectionCache.GetValue(cmc, "health"));
                }

                if (!ctx.HasSkill)
                {
                    ctx.HasSkill = GetBoolSafe(ReflectionCache.GetValue(cmc, "hasSkill"));
                }
            }

            if (string.IsNullOrEmpty(ctx.EnemyType))
            {
                ctx.EnemyType = ctx.NameKey ?? (ctx.GameObject != null ? ctx.GameObject.name : "unknown");
            }

            ctx._teamNormalized = ComputeTeamNormalized(ctx.Team);
            ctx._rank = ComputeRank(ctx.IconType, ctx.Health);

            return ctx;
        }

        /// <summary>
        /// Rank estimation reused by rule engine.
        /// </summary>
        public string GetRank() => _rank;

        /// <summary>
        /// Normalized team token (scav/pmc/player/unknown).
        /// </summary>
        public string GetTeamNormalized() => _teamNormalized;

        public override string ToString()
        {
            return $"EnemyContext(go={InstanceId}, type={EnemyType}, team={Team}, icon={IconType}, nameKey={NameKey}, health={Health}, vt={VoiceType})";
        }

        private static void PopulateFromPreset(EnemyContext ctx, CharacterRandomPreset preset)
        {
            if (ctx == null || preset == null) return;

            if (string.IsNullOrEmpty(ctx.Team)) ctx.Team = GetStringSafe(preset.team);
            if (string.IsNullOrEmpty(ctx.IconType)) ctx.IconType = GetIconType(preset);
            if (string.IsNullOrEmpty(ctx.NameKey)) ctx.NameKey = GetStringSafe(preset.nameKey);
            if (ctx.Health <= 0f) ctx.Health = preset.health;
            if (!ctx.HasSkill) ctx.HasSkill = preset.hasSkill;
            if (string.IsNullOrEmpty(ctx.EnemyType)) ctx.EnemyType = preset.GetType().Name;
        }

        private static string ComputeRank(string iconType, float health)
        {
            var icon = (iconType ?? string.Empty).ToLowerInvariant();
            if (icon.Contains("boss")) return "boss";
            if (icon.Contains("elite") || icon.Contains("elete")) return "elite";
            if (health >= 1000f) return "boss";
            if (health >= 500f) return "elite";
            return "normal";
        }

        private static string ComputeTeamNormalized(string team)
        {
            var t = (team ?? string.Empty).ToLowerInvariant();
            if (t.Contains("scav")) return "scav";
            if (t.Contains("pmc")) return "pmc";
            if (t.Contains("player")) return "player";
            return string.IsNullOrEmpty(t) ? "unknown" : t;
        }

        private static string GetIconType(CharacterRandomPreset preset)
        {
            var value = ReflectionCache.GetValue(preset, "characterIconType");
            return GetStringSafe(value);
        }

        private static string GetStringSafe(object value) => value?.ToString();

        private static float GetFloatSafe(object value)
        {
            if (value == null) return 0f;
            if (value is float f) return f;
            if (value is double d) return (float)d;
            if (value is int i) return i;
            return float.TryParse(value.ToString(), out var parsed) ? parsed : 0f;
        }

        private static bool GetBoolSafe(object value)
        {
            if (value == null) return false;
            if (value is bool b) return b;
            return bool.TryParse(value.ToString(), out var parsed) && parsed;
        }
    }
}
