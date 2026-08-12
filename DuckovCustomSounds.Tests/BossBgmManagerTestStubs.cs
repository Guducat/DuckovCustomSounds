using System.Collections.Generic;

namespace UnityEngine
{
    internal static class Time
    {
        public static float time;
        public static float unscaledDeltaTime { get; set; }
    }

    internal static class Mathf
    {
        public static float Max(float left, float right) => System.Math.Max(left, right);

        public static float Sqrt(float value) => (float)System.Math.Sqrt(value);

        public static float Clamp01(float value) => System.Math.Clamp(value, 0f, 1f);
    }

    internal class Object
    {
        public static void Destroy(object value)
        {
        }

        public static void DontDestroyOnLoad(object value)
        {
        }
    }

    internal sealed class GameObject : Object
    {
        public GameObject(string name)
        {
        }

        public T AddComponent<T>() where T : new() => new T();
    }

    internal class MonoBehaviour : Object
    {
    }
}

namespace FMOD
{
    internal enum RESULT
    {
        OK
    }
}

namespace FMOD.Studio
{
    internal enum STOP_MODE
    {
        IMMEDIATE,
        ALLOWFADEOUT
    }

    internal sealed class EventInstanceState
    {
        public bool Valid { get; set; } = true;

        public bool Stopped { get; set; }
        public bool Released { get; set; }
        public float Volume { get; set; } = 1f;
    }

    internal readonly struct EventInstance
    {
        private readonly EventInstanceState? state;

        public EventInstance(EventInstanceState state)
        {
            this.state = state;
        }

        public bool isValid() => state?.Valid == true;

        public FMOD.RESULT getVolume(out float volume, out float finalVolume)
        {
            volume = state?.Volume ?? 0f;
            finalVolume = volume;
            return FMOD.RESULT.OK;
        }

        public FMOD.RESULT setVolume(float volume)
        {
            if (state != null)
                state.Volume = volume;
            return FMOD.RESULT.OK;
        }

        public FMOD.RESULT stop(STOP_MODE mode)
        {
            if (state != null)
                state.Stopped = true;
            return FMOD.RESULT.OK;
        }

        public FMOD.RESULT release()
        {
            if (state != null)
            {
                state.Released = true;
                state.Valid = false;
            }
            return FMOD.RESULT.OK;
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

        public bool isActiveAndEnabled { get; set; } = true;

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
