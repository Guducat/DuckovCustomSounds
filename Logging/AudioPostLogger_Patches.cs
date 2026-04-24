using HarmonyLib;
using UnityEngine;
using FMOD.Studio;
using Duckov; // AudioManager, AudioObject

namespace DuckovCustomSounds.Logging
{
    // Log all AudioManager.Post overloads
    [HarmonyPatch(typeof(AudioManager))]
    internal static class AudioPostLogger_Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(AudioManager.Post), new[] { typeof(string) })]
        private static void Post_String_Postfix(string eventName, EventInstance? __result)
        {
            TryLog("AudioManager.Post(str)", eventName, goName: null, pos: null);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(AudioManager.Post), new[] { typeof(string), typeof(GameObject) })]
        private static void Post_GameObject_Postfix(string eventName, GameObject gameObject, EventInstance? __result)
        {
            string? goName = gameObject != null ? gameObject.name : null;
            TryLog("AudioManager.Post(str,GameObject)", eventName, goName, pos: null);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(AudioManager.Post), new[] { typeof(string), typeof(Vector3) })]
        private static void Post_Vector3_Postfix(string eventName, Vector3 position, EventInstance? __result)
        {
            TryLog("AudioManager.Post(str,Vector3)", eventName, goName: null, position);
        }

        private static void TryLog(string source, string? eventName, string? goName, Vector3? pos)
        {
            try
            {
                if (!ModSettings.AudioPostLoggerEnabled) return;
                if (string.IsNullOrEmpty(eventName)) eventName = "<null>";
                string where = goName != null ? $"go={goName}" : (pos.HasValue ? $"pos={pos.Value}" : "no-context");
                LogManager.GetLogger(ResolveModule(eventName))
                    .ForScope("AudioPostLogger")
                    .Verbose($"[Trace] {source}: event='{eventName}', {where}");
            }
            catch { /* swallow */ }
        }

        internal static string ResolveModule(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName)) return "Core";
            if (eventName.StartsWith("Char/Footstep/", System.StringComparison.OrdinalIgnoreCase)) return "Footstep";
            if (eventName.StartsWith("Char/Voice/", System.StringComparison.OrdinalIgnoreCase)) return "Enemy";
            if (eventName.StartsWith("SFX/Combat/Gun/", System.StringComparison.OrdinalIgnoreCase)) return "Gun";
            if (eventName.StartsWith("SFX/Combat/Explosive/", System.StringComparison.OrdinalIgnoreCase)) return "Grenade";
            if (eventName.StartsWith("SFX/Combat/Melee/", System.StringComparison.OrdinalIgnoreCase)) return "Melee";
            if (eventName.StartsWith("SFX/Item/", System.StringComparison.OrdinalIgnoreCase)) return "Item";
            if (eventName.StartsWith("Music/", System.StringComparison.OrdinalIgnoreCase)) return "BGM";
            return "Core";
        }
    }

    // Also log lower-level AudioObject.Post so that stinger/ambient sources are captured too
    [HarmonyPatch(typeof(AudioObject))]
    internal static class AudioObjectPostLogger_Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(AudioObject.Post), new[] { typeof(string), typeof(bool) })]
        private static void Post_AudioObject_Postfix(AudioObject __instance, string eventName, bool doRelease, EventInstance? __result)
        {
            try
            {
                if (!ModSettings.AudioPostLoggerEnabled) return;
                string? goName = __instance != null && __instance.gameObject != null ? __instance.gameObject.name : null;
                eventName ??= "<null>";
                LogManager.GetLogger(AudioPostLogger_Patches.ResolveModule(eventName))
                    .ForScope("AudioPostLogger")
                    .Verbose($"[Trace] AudioObject.Post: event='{eventName}', go={goName}");
            }
            catch { /* swallow */ }
        }
    }
}
