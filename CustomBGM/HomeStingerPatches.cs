using HarmonyLib;
using Duckov;
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using FMOD;
using FMODUnity;

namespace DuckovCustomSounds.CustomBGM
{
    // Postfix 方案：让原始 Studio 事件先创建并占位（保持时序与路由），
    // 我们将其静音，同时用 Core 播放自定义 start.mp3，路由到 Music 总线。
    [HarmonyPatch(typeof(AudioManager))]
    public static class HomeStingerPatches
    {
        [HarmonyPatch("Post", new Type[] { typeof(string) })]
        [HarmonyPostfix]
        public static void Postfix(ref FMOD.Studio.EventInstance? __result, string eventName)
        {
            try
            {
                BGMLogger.Debug($"[HomeStinger] AudioManager.Post Postfix ENTER: event='{eventName}'");
                if (!string.Equals(eventName, "Music/Stinger/stg_map_base", StringComparison.OrdinalIgnoreCase))
                    return;

                string startPath = Path.Combine(ModBehaviour.ModFolderName, "TitleBGM", "start.mp3");
                if (!File.Exists(startPath))
                {
                    BGMLogger.Debug("[HomeStinger] start.mp3 不存在，放行原事件");
                    return;
                }

                try { if (!RuntimeManager.IsInitialized) { BGMLogger.Info("[HomeStinger] FMOD 未初始化，放行原事件"); return; } } catch { }

                // 将原 Studio 事件静音，但不停止，让其自然结束以维持 IsStingerPlaying/时序
                try
                {
                    if (__result.HasValue)
                    {
                        var ev = __result.Value;
                        try { if (ev.isValid()) ev.setVolume(0f); } catch { }
                    }
                }
                catch { }

                // 获取 Music Bus 的 ChannelGroup（此时已由原事件确保总线存在）
                ChannelGroup group = ResolveMusicGroupSafe();
                if (!group.hasHandle())
                {
                    BGMLogger.Warn("[HomeStinger] 无法获取 Music/SFX/Master 任何 ChannelGroup，放弃自定义 stinger");
                    return;
                }

                // 播放自定义 2D 非循环 stinger
                var mode = MODE.CREATESTREAM | MODE._2D | MODE.LOOP_OFF;
                var r1 = RuntimeManager.CoreSystem.createSound(startPath, mode, out Sound sound);
                if (r1 != RESULT.OK || !sound.hasHandle())
                {
                    BGMLogger.Warn($"[HomeStinger] createSound 失败: {r1}");
                    return;
                }

                var r2 = RuntimeManager.CoreSystem.playSound(sound, group, true, out Channel channel);
                if (r2 != RESULT.OK || !channel.hasHandle())
                {
                    BGMLogger.Warn($"[HomeStinger] playSound 失败: {r2}");
                    try { if (sound.hasHandle()) sound.release(); } catch { }
                    return;
                }

                try { channel.setPaused(false); } catch { }
                BGMLogger.Info("[HomeStinger] stg_map_base → 自定义 start.mp3（路由：Music 优先，回退 SFX→Master；时序由原事件维持）");

                // 对齐原事件时长：原事件结束或超时后，停止并释放自定义音频
                try
                {
                    if (ModBehaviour.Instance != null)
                    {
                        ModBehaviour.Instance.StartCoroutine(AlignWithOriginalThenCleanup(__result, sound, channel));
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                BGMLogger.Warn($"[HomeStinger] Postfix 异常: {ex.Message}");
            }
        }

        private static IEnumerator AlignWithOriginalThenCleanup(FMOD.Studio.EventInstance? original, Sound sound, Channel channel)
        {
            // 兜底最长 30s，避免资源泄漏或长音频“顶住”
            float deadline = Time.realtimeSinceStartup + 30f;
            try
            {
                while (Time.realtimeSinceStartup < deadline)
                {
                    bool origPlaying = false;
                    try
                    {
                        if (original.HasValue && original.Value.isValid())
                        {
                            if (original.Value.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE st) == RESULT.OK)
                            {
                                origPlaying = (st == FMOD.Studio.PLAYBACK_STATE.PLAYING || st == FMOD.Studio.PLAYBACK_STATE.STARTING);
                            }
                        }
                    }
                    catch { }

                    bool customPlaying = false;
                    try { if (channel.hasHandle()) channel.isPlaying(out customPlaying); } catch { }

                    if (!origPlaying || !customPlaying)
                        break;

                    yield return new WaitForSeconds(0.05f);
                }
            }
            finally
            {
                try { if (channel.hasHandle()) channel.stop(); } catch { }
                try { if (sound.hasHandle()) sound.release(); } catch { }
            }
        }

        private static ChannelGroup ResolveMusicGroupSafe()
        {
            try
            {
                var musicBus = RuntimeManager.GetBus("bus:/Master/Music");
                if (musicBus.getChannelGroup(out var cg) == RESULT.OK && cg.hasHandle())
                    return cg;
            }
            catch { }

            try
            {
                var sfxBus = RuntimeManager.GetBus("bus:/Master/SFX");
                if (sfxBus.getChannelGroup(out var cg2) == RESULT.OK && cg2.hasHandle())
                    return cg2;
            }
            catch { }

            try
            {
                if (RuntimeManager.CoreSystem.getMasterChannelGroup(out var master) == RESULT.OK && master.hasHandle())
                    return master;
            }
            catch { }

            return default;
        }

        // 补丁 2：直接拦截 AudioObject.Post(string,bool)（从日志看 stg_map_base 实际走这里）
        [HarmonyPatch(typeof(AudioObject))]
        internal static class HomeStinger_AudioObject_Post_Patch
        {
            [HarmonyPatch("Post", new Type[] { typeof(string), typeof(bool) })]
            [HarmonyPostfix]
            private static void Postfix(AudioObject __instance, string eventName, bool doRelease, FMOD.Studio.EventInstance? __result)
            {
                try
                {
                    string goName = null;
                    try { goName = __instance?.gameObject?.name; } catch { }
                    BGMLogger.Debug($"[HomeStinger] AudioObject.Post Postfix ENTER: event='{eventName}', go={goName}, doRelease={doRelease}");

                    if (!string.Equals(eventName, "Music/Stinger/stg_map_base", StringComparison.OrdinalIgnoreCase))
                        return;

                    string startPath = Path.Combine(ModBehaviour.ModFolderName, "TitleBGM", "start.mp3");
                    if (!File.Exists(startPath))
                    {
                        BGMLogger.Debug("[HomeStinger] start.mp3 不存在，放行原事件");
                        return;
                    }

                    try { if (!RuntimeManager.IsInitialized) { BGMLogger.Info("[HomeStinger] FMOD 未初始化，放行原事件"); return; } } catch { }

                    // 静音原 Studio 事件（保留生命周期）
                    try
                    {
                        if (__result.HasValue)
                        {
                            var ev = __result.Value;
                            try { if (ev.isValid()) ev.setVolume(0f); } catch { }
                        }
                    }
                    catch { }

                    // 获取路由组：Music 优先，退 SFX，再退 Master
                    ChannelGroup group = ResolveMusicGroupSafe();
                    if (!group.hasHandle())
                    {
                        BGMLogger.Warn("[HomeStinger] 无法获取任何 ChannelGroup，放弃自定义 stinger");
                        return;
                    }

                    // 播放 2D 非循环自定义 stinger
                    var mode = MODE.CREATESTREAM | MODE._2D | MODE.LOOP_OFF;
                    var r1 = RuntimeManager.CoreSystem.createSound(startPath, mode, out Sound sound);
                    if (r1 != RESULT.OK || !sound.hasHandle())
                    {
                        BGMLogger.Warn($"[HomeStinger] createSound 失败: {r1}");
                        return;
                    }

                    var r2 = RuntimeManager.CoreSystem.playSound(sound, group, true, out Channel channel);
                    if (r2 != RESULT.OK || !channel.hasHandle())
                    {
                        BGMLogger.Warn($"[HomeStinger] playSound 失败: {r2}");
                        try { if (sound.hasHandle()) sound.release(); } catch { }
                        return;
                    }

                    try { channel.setPaused(false); } catch { }
                    BGMLogger.Info("[HomeStinger] (AudioObject) stg_map_base → 自定义 start.mp3（路由：Music 优先，回退 SFX→Master；时序由原事件维持）");

                    // 与原事件播放状态对齐后清理
                    try
                    {
                        if (ModBehaviour.Instance != null)
                        {
                            ModBehaviour.Instance.StartCoroutine(AlignWithOriginalThenCleanup_AudioObject(__result, sound, channel));
                        }
                    }
                    catch { }
                }
                catch (Exception ex)
                {
                    BGMLogger.Warn($"[HomeStinger] AudioObject.Post Postfix 异常: {ex.Message}");
                }
            }

            private static IEnumerator AlignWithOriginalThenCleanup_AudioObject(FMOD.Studio.EventInstance? original, Sound sound, Channel channel)
            {
                float deadline = Time.realtimeSinceStartup + 30f;
                try
                {
                    while (Time.realtimeSinceStartup < deadline)
                    {
                        bool origPlaying = false;
                        try
                        {
                            if (original.HasValue && original.Value.isValid())
                            {
                                if (original.Value.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE st) == RESULT.OK)
                                {
                                    origPlaying = (st == FMOD.Studio.PLAYBACK_STATE.PLAYING || st == FMOD.Studio.PLAYBACK_STATE.STARTING);
                                }
                            }
                        }
                        catch { }

                        bool customPlaying = false;
                        try { if (channel.hasHandle()) channel.isPlaying(out customPlaying); } catch { }

                        if (!origPlaying || !customPlaying)
                            break;

                        yield return new WaitForSeconds(0.05f);
                    }
                }
                finally
                {
                    try { if (channel.hasHandle()) channel.stop(); } catch { }
                    try { if (sound.hasHandle()) sound.release(); } catch { }
                }
            }
        }

    }
}

