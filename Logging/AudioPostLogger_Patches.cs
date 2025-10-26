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
            TryLog("AudioManager.Post(str)", eventName, null, null);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(AudioManager.Post), new[] { typeof(string), typeof(GameObject) })]
        private static void Post_GameObject_Postfix(string eventName, GameObject gameObject, EventInstance? __result)
        {
            TryLog("AudioManager.Post(str,GameObject)", eventName, gameObject ? gameObject.name : null, null);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(AudioManager.Post), new[] { typeof(string), typeof(Vector3) })]
        private static void Post_Vector3_Postfix(string eventName, Vector3 position, EventInstance? __result)
        {
            TryLog("AudioManager.Post(str,Vector3)", eventName, null, position);
        }

        private static void TryLog(string source, string eventName, string goName, Vector3? pos)
        {
            try
            {
                if (!ModSettings.AudioPostLoggerEnabled) return;
                if (string.IsNullOrEmpty(eventName)) eventName = "<null>";
                string where = goName != null ? $"go={goName}" : (pos.HasValue ? $"pos={pos.Value}" : "no-context");
                Debug.Log($"[AudioPostLogger] {source}: event='{eventName}', {where}");
            }
            catch { /* swallow */ }
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
                string goName = __instance != null && __instance.gameObject != null ? __instance.gameObject.name : null;
                Debug.Log($"[AudioPostLogger] AudioObject.Post: event='{eventName}', go={goName}");
            }
            catch { /* swallow */ }
        }
    }
}

