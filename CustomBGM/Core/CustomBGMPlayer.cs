using System;
using System.Reflection;
using Duckov;
using FMOD.Studio;

namespace DuckovCustomSounds.CustomBGM.Core
{
    internal static class CustomBGMPlayer
    {
        public static EventInstance? PlayMusicFile(string filePath, bool loop, bool stopExistingBGM)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    return null;

                if (stopExistingBGM)
                {
                    AudioManager.StopBGM();
                }

                var bgmSource = GetBgmSource();
                if (bgmSource == null)
                {
                    BGMLogger.Warning("无法获取 bgmSource，自定义 BGM 播放失败");
                    return stopExistingBGM ? AudioManager.PlayCustomBGM(filePath, loop) : null;
                }

                string eventPath = loop ? "Music/custom_loop" : "Music/custom";
                var instance = bgmSource.PostFile(eventPath, filePath, doRelease: false);
                if (!instance.HasValue || !instance.Value.isValid())
                {
                    BGMLogger.Warning($"自定义 BGM 播放返回无效实例: {filePath}");
                }

                return instance;
            }
            catch (Exception ex)
            {
                BGMLogger.Warning($"自定义 BGM 播放异常: {ex.Message}");
                return stopExistingBGM ? AudioManager.PlayCustomBGM(filePath, loop) : null;
            }
        }

        public static bool IsEventInstanceActive(EventInstance? instance)
        {
            if (!instance.HasValue || !instance.Value.isValid())
                return false;

            var result = instance.Value.getPlaybackState(out PLAYBACK_STATE state);
            return result == FMOD.RESULT.OK &&
                   state != PLAYBACK_STATE.STOPPED &&
                   state != PLAYBACK_STATE.STOPPING;
        }

        private static AudioObject? GetBgmSource()
        {
            try
            {
                var audioManager = AudioManager.Instance;
                if (audioManager == null)
                    return null;

                var field = typeof(AudioManager).GetField("bgmSource", BindingFlags.Instance | BindingFlags.NonPublic);
                return field?.GetValue(audioManager) as AudioObject;
            }
            catch (Exception ex)
            {
                BGMLogger.Warning($"读取 bgmSource 失败: {ex.Message}");
                return null;
            }
        }
    }
}
