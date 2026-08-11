using System.Collections.Generic;

namespace UnityEngine
{
    internal static class Time
    {
        public static float time;
    }

    internal static class Mathf
    {
        public static float Max(float left, float right) => System.Math.Max(left, right);

        public static float Sqrt(float value) => (float)System.Math.Sqrt(value);
    }

    internal static class Object
    {
        public static void Destroy(object value)
        {
        }
    }
}

namespace DuckovCustomSounds.CustomBGM.BossBGM
{
    internal sealed class BossBGMController
    {
        public BossBGMController(string name, float distance)
        {
            Name = name;
            Distance = distance;
        }

        public object gameObject { get; } = new();

        public string Name { get; }

        public float Distance { get; set; }

        public int DistanceQueryCount { get; private set; }

        public bool PriorityActive { get; private set; }

        public bool Valid { get; set; } = true;

        public float GetDistanceToPlayer() => Distance;

        public bool TryGetDistanceSquaredToPlayer(out float distanceSquared)
        {
            DistanceQueryCount++;
            distanceSquared = Distance * Distance;
            return true;
        }

        public void ResetDistanceQueryCount() => DistanceQueryCount = 0;

        public string GetBossName() => Name;

        public void SetPriority(bool active) => PriorityActive = active;

        public bool IsValid() => Valid;
    }

    internal static class BossBGMConfig
    {
        public static bool Enabled { get; set; } = true;

        public static float TriggerDistance { get; set; } = 40f;

        public static float MinSwitchIntervalSeconds { get; set; } = 2f;

        public static float MinDistanceDeltaToSwitch { get; set; } = 5f;
    }

    internal static class BossBGMFader
    {
        public static void ForceStopAll(string reason)
        {
        }
    }

    internal static class BossBGMLogger
    {
        public static void Info(string message)
        {
        }

        public static void Debug(string message)
        {
        }
    }

    internal static class BossBGMTestEnvironment
    {
        public static void Reset()
        {
            BossBGMManager.Clear();
            BossBGMConfig.Enabled = true;
            BossBGMConfig.TriggerDistance = 40f;
            BossBGMConfig.MinSwitchIntervalSeconds = 2f;
            BossBGMConfig.MinDistanceDeltaToSwitch = 5f;
            UnityEngine.Time.time = 100f;
            SceneBGM.CustomSceneBGM.Reset();
        }
    }
}

namespace DuckovCustomSounds.CustomBGM.SceneBGM
{
    internal static class CustomSceneBGM
    {
        private static readonly List<bool> States = new();

        public static bool IsBossBGMActive { get; private set; }

        public static IReadOnlyList<bool> StateChanges => States;

        public static void SetBossBGMActive(bool active)
        {
            IsBossBGMActive = active;
            States.Add(active);
        }

        public static void Reset()
        {
            IsBossBGMActive = false;
            States.Clear();
        }
    }
}
