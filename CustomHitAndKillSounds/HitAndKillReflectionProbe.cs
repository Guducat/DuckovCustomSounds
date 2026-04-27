using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DuckovCustomSounds.CustomHitAndKillSounds
{
    internal static class HitAndKillReflectionProbe
    {
        private static readonly string[] CandidateFieldNames =
        {
            "critSfx",
            "nonCritSfx",
            "hitSfx",
            "hurtSfx",
            "deathSfx",
            "sfx",
            "audioEvent",
            "soundKey"
        };

        public static void LogAudioFields(Component? component, string source)
        {
            if (!HitAndKillConfig.ReflectionDiagnostics || component == null)
                return;

            try
            {
                var values = new List<string>();
                var type = component.GetType();
                foreach (var name in CandidateFieldNames)
                {
                    var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (field == null || field.FieldType != typeof(string))
                        continue;

                    var value = field.GetValue(component) as string;
                    if (!string.IsNullOrWhiteSpace(value))
                        values.Add(name + "=" + value);
                }

                if (values.Count > 0)
                    HitAndKillLogger.Debug($"[Reflection:{source}] {type.FullName}: {string.Join(", ", values)}");
            }
            catch (Exception ex)
            {
                HitAndKillLogger.Verbose($"反射诊断失败: {ex.Message}");
            }
        }

        public static bool LooksRelevantAudioEvent(string? eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
                return false;

            return eventName.IndexOf("hit", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   eventName.IndexOf("hurt", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   eventName.IndexOf("kill", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   eventName.IndexOf("crit", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   eventName.IndexOf("marker", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
