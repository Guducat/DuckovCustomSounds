using System;
using UnityEngine;
using Duckov; // Game types: AudioManager, CharacterMainControl

namespace DuckovCustomSounds.CustomEnemySounds
{
    /// <summary>
    /// 用于语音路由的敌人身份不可变快照。
    /// 在AICharacterController.Init时构建，在语音类型更改时更新。
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

        private EnemyContext() { }

        public static EnemyContext FromCharacter(CharacterMainControl cmc,
            AudioManager.VoiceType voiceType,
            AudioManager.FootStepMaterialType foot)
        {
            var ctx = new EnemyContext();
            ctx.GameObject = cmc != null ? cmc.gameObject : null;
            ctx.InstanceId = ctx.GameObject != null ? ctx.GameObject.GetInstanceID() : 0;
            ctx.VoiceType = voiceType;
            ctx.FootStepMaterialType = foot;

            object preset = TryGetFieldOrProp(cmc, "characterPreset");

            ctx.Team = GetStringSafe(TryGetFieldOrProp(preset, "team"))
                ?? GetStringSafe(TryGetFieldOrProp(cmc, "team"));
            ctx.IconType = GetStringSafe(TryGetFieldOrProp(preset, "characterIconType"))
                ?? GetStringSafe(TryGetFieldOrProp(cmc, "characterIconType"));
            ctx.NameKey = GetStringSafe(TryGetFieldOrProp(preset, "nameKey"));
            ctx.Health = GetFloatSafe(TryGetFieldOrProp(preset, "health"));
            ctx.HasSkill = GetBoolSafe(TryGetFieldOrProp(preset, "hasSkill"));
            ctx.EnemyType = (preset != null ? preset.GetType().Name : null)
                ?? (ctx.NameKey ?? "unknown");

            return ctx;
        }

        /// <summary>
        /// 从图标类型或生命值推断的标准化等级。
        /// </summary>
        public string GetRank()
        {
            var icon = (IconType ?? string.Empty).ToLowerInvariant();
            if (icon.Contains("boss")) return "boss";
            if (icon.Contains("elite") || icon.Contains("elete")) return "elite";
            if (Health >= 1000f) return "boss";
            if (Health >= 500f) return "elite";
            return "normal";
        }

        /// <summary>
        /// 在路径模板中使用的标准化团队关键词（例如：scav/pmc/unknown）。
        /// </summary>
        public string GetTeamNormalized()
        {
            var t = (Team ?? string.Empty).ToLowerInvariant();
            if (t.Contains("scav")) return "scav";
            if (t.Contains("pmc")) return "pmc";
            return string.IsNullOrEmpty(t) ? "unknown" : t;
        }

        public override string ToString()
        {
            return $"EnemyContext(go={InstanceId}, type={EnemyType}, team={Team}, icon={IconType}, nameKey={NameKey}, health={Health}, vt={VoiceType})";
        }

        private static object TryGetFieldOrProp(object target, string name)
        {
            if (target == null || string.IsNullOrEmpty(name)) return null;
            var t = target.GetType();
            try
            {
                var pi = t.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (pi != null) return pi.GetValue(target);
            }
            catch { }
            try
            {
                var fi = t.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fi != null) return fi.GetValue(target);
            }
            catch { }
            return null;
        }

        private static string GetStringSafe(object o) => o?.ToString();
        private static float GetFloatSafe(object o)
        {
            if (o == null) return 0f;
            if (o is float f) return f;
            if (o is double d) return (float)d;
            if (o is int i) return i;
            if (float.TryParse(o.ToString(), out var parsed)) return parsed;
            return 0f;
        }
        private static bool GetBoolSafe(object o)
        {
            if (o == null) return false;
            if (o is bool b) return b;
            if (bool.TryParse(o.ToString(), out var parsed)) return parsed;
            return false;
        }
    }
}

