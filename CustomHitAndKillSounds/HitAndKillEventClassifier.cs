using System;

namespace DuckovCustomSounds.CustomHitAndKillSounds
{
    internal static class HitAndKillEventClassifier
    {
        private const string MarkerPrefix = "SFX/Combat/Marker/";

        public static bool TryClassifyMarkerEvent(string eventName, out string key)
        {
            key = string.Empty;

            if (string.IsNullOrWhiteSpace(eventName))
                return false;

            if (!eventName.StartsWith(MarkerPrefix, StringComparison.OrdinalIgnoreCase))
                return false;

            var marker = eventName.Substring(MarkerPrefix.Length);
            switch (marker.ToLowerInvariant())
            {
                case "hitmarker":
                case "hitmarker_head":
                case "killmarker":
                case "killmarker_head":
                    key = marker.ToLowerInvariant();
                    return true;
                default:
                    return false;
            }
        }

        public static string GetHurtKey(DamageInfo damageInfo)
        {
            var crit = damageInfo.crit > 0;
            return crit ? "player_hurt_crit" : "player_hurt";
        }

        public static string GetNpcHurtKey(DamageInfo damageInfo)
        {
            var crit = damageInfo.crit > 0;
            return crit ? "npc_hurt_crit" : "npc_hurt";
        }

        public static string GetKillKey(DamageInfo damageInfo)
        {
            return damageInfo.crit > 0 ? "killmarker_head" : "killmarker";
        }

        public static string GetHitKey(DamageInfo damageInfo)
        {
            return damageInfo.crit > 0 ? "hitmarker_head" : "hitmarker";
        }
    }
}
