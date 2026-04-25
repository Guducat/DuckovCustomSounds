using System;
using Duckov;
using FMOD;
using FMOD.Studio;

namespace DuckovCustomSounds
{
    /// <summary>
    /// Utilities for copying 3D attenuation settings from native FMOD events to custom programmer sounds.
    /// </summary>
    internal static class AudioDistanceHelper
    {
        internal static bool TryExtractFromEventInstance(EventInstance instance, out float min, out float max)
        {
            min = 0f;
            max = 0f;
            try
            {
                if (!instance.isValid()) return false;
                if (instance.getDescription(out var desc) != RESULT.OK) return false;
                if (desc.getMinMaxDistance(out min, out max) != RESULT.OK) return false;
                return min >= 0f && max >= min;
            }
            catch
            {
                min = 0f;
                max = 0f;
                return false;
            }
        }

        internal static bool TryExtractFromEventName(string eventPath, out float min, out float max)
        {
            min = 0f;
            max = 0f;
            if (string.IsNullOrWhiteSpace(eventPath)) return false;

            try
            {
                if (!AudioManager.TryCreateEventInstance(eventPath, out var ev)) return false;
                try
                {
                    return TryExtractFromEventInstance(ev, out min, out max);
                }
                finally
                {
                    try { ev.stop(STOP_MODE.IMMEDIATE); } catch { }
                    try { ev.release(); } catch { }
                }
            }
            catch
            {
                min = 0f;
                max = 0f;
                return false;
            }
        }

        internal static void ApplyToEventInstance(EventInstance instance, float min, float max)
        {
            if (!instance.isValid()) return;
            if (min < 0f || max <= 0f || max < min) return;
            try
            {
                // Studio API: 通过实例属性设置 3D 最小/最大距离
                try { instance.setProperty(EVENT_PROPERTY.MINIMUM_DISTANCE, min); } catch { }
                try { instance.setProperty(EVENT_PROPERTY.MAXIMUM_DISTANCE, max); } catch { }
            }
            catch
            {
                // ignore – failing to apply distances should not break playback
            }
        }
    }
}
