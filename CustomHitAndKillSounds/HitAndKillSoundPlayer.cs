using System;
using Duckov;
using FMOD.Studio;
using UnityEngine;

namespace DuckovCustomSounds.CustomHitAndKillSounds
{
    internal static class HitAndKillSoundPlayer
    {
        public static EventInstance? Play(string filePath, GameObject? gameObject = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return null;

            try
            {
                var result = AudioManager.PostCustomSFX(filePath, gameObject, loop: false);
                ApplyVolume(result);
                return result;
            }
            catch (Exception ex)
            {
                HitAndKillLogger.Warning($"播放自定义音效失败: {ex.Message}");
                return null;
            }
        }

        public static void SuppressOriginal(EventInstance? instance, bool stop = true)
        {
            try
            {
                if (!instance.HasValue)
                    return;

                var ev = instance.Value;
                try
                {
                    if (!ev.isValid())
                        return;
                }
                catch
                {
                    return;
                }

                try { ev.setVolume(0f); } catch { }
                if (stop)
                {
                    try { ev.stop(STOP_MODE.IMMEDIATE); } catch { }
                }
                try { ev.release(); } catch { }
            }
            catch
            {
            }
        }

        private static void ApplyVolume(EventInstance? instance)
        {
            try
            {
                if (!instance.HasValue || !instance.Value.isValid())
                    return;

                instance.Value.setVolume(HitAndKillConfig.Volume);
            }
            catch (Exception ex)
            {
                HitAndKillLogger.Debug($"设置音量失败: {ex.Message}");
            }
        }
    }
}
