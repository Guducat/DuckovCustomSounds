using System;
using System.Collections.Generic;

namespace DuckovCustomSounds.CustomBGM.ExtractionBGM
{
    internal static class ExtractionCoveragePolicy
    {
        private static readonly HashSet<string> SupportedSourceScenes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Level_Farm_Main",
                "Level_GroundZero_Main",
                "Prologue_Main",
                "Level_HiddenWarehouse_Main",
                "Level_Guide_Main",
                "Level_JLab_Main",
                "Level_DemoChallenge_Main",
                "Level_StormZone_Main",
                "Level_ChallengeSnow_Main",
                "Level_SnowMilitaryBase_Main",
                "Level_SnowMilitaryBase_ColdStorage_Main",
                "Level_SurivalChallenge_Main"
            };

        public static bool IsSupportedSourceScene(string? sceneName)
        {
            return !string.IsNullOrEmpty(sceneName) && SupportedSourceScenes.Contains(sceneName);
        }

        public static bool ShouldSuppressMapStinger(
            string? key,
            float completedAt,
            float now,
            float windowSeconds)
        {
            if (completedAt < 0f || now < completedAt || now - completedAt > windowSeconds)
                return false;

            if (string.IsNullOrEmpty(key) ||
                !key.StartsWith("stg_map_", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return !string.Equals(key, "stg_map_base", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsDuplicateCompletion(
            string? sceneName,
            string? lastHandledSceneName,
            float lastHandledAt,
            float now,
            float windowSeconds)
        {
            if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(lastHandledSceneName))
                return false;

            if (lastHandledAt < 0f || now < lastHandledAt || now - lastHandledAt > windowSeconds)
                return false;

            return string.Equals(sceneName, lastHandledSceneName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
