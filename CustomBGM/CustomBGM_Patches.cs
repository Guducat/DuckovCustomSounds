using HarmonyLib;
using Duckov; // 引用 AudioManager
using System;
using UnityEngine; // 引用 Type
using System.Diagnostics; // 采集调用堆栈
using System.Text; // 构建堆栈字符串
using System.IO; // 访问 start.mp3
using FMOD; // Core API（RESULT、MODE、Channel 等）
using System.Collections; // 协程监控自定义 Stinger 结束



namespace DuckovCustomSounds.CustomBGM
{
    // --- 补丁 1：劫持主菜单 BGM ---
    [HarmonyPatch(typeof(AudioManager))]
    public static class AudioManagerPatch
    {
        [HarmonyPatch("PlayBGM")]
        [HarmonyPostfix]
        public static void PlayBGM_Postfix(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            if (name == "mus_title")
            {
                BGMLogger.Info("拦截到 mus_title（Postfix），先让原版启动以初始化 Music Bus，然后切换到自定义主菜单 BGM...");
                try { Duckov.AudioManager.StopBGM(); } catch { }
                CustomBGM.PlayTitleBGM();
            }
        }
    }

    // --- 补丁 2：与“唱片机”整合 ---
    // 目标：
    // - 保留原方法的 UI/索引/保存 等副作用（让唱片机“识别”当前歌曲）
    // - 阻止原方法触发的 FMOD 事件播放（避免“异常播放一次”）
    // - 在 Postfix 中按最终索引播放自定义曲目（只播放一次、走我们自己的路由）
    [HarmonyPatch(typeof(BaseBGMSelector))]
    public static class BaseBGMSelectorPatch
    {
        // 1) Set(int index, bool showInfo, bool play)
        // Prefix：如果有自定义歌曲，强制 play=false，让原方法不触发 AudioManager.PlayBGM
        [HarmonyPatch("Set", new Type[] { typeof(int), typeof(bool), typeof(bool) })]
        [HarmonyPrefix]
        public static void Set_Prefix(int index, bool showInfo, ref bool play, ref bool __state)
        {
            __state = play; // 记录原始 play 值，供 Postfix 判断是否应当播放
            if (CustomBGM.HasHomeSongs)
            {
                if (!play)
                {
                    // 加载阶段（原始 play=false），为避免与标题音乐冲突，先停掉当前 BGM（通常是标题曲）
                    CustomBGM.StopCurrentBGM(false);
                    BGMLogger.Info("Load阶段停止标题音乐，避免冲突");
                }
                // 重要：不再强制 play=false，允许原版 AudioManager.PlayBGM 执行，以确保 Studio/Music 总线初始化
                BGMLogger.Debug($"拦截到 BaseBGMSelector.Set()：index={index}, play={play}, showInfo={showInfo} → 允许原曲初始化后再切换自定义");
            }
        }

        // Postfix：在原方法完成 UI/索引后，按最终索引播放我们的自定义音乐
        [HarmonyPatch("Set", new Type[] { typeof(int), typeof(bool), typeof(bool) })]
        [HarmonyPostfix]
        public static void Set_Postfix(BaseBGMSelector __instance, int index, bool showInfo, bool play, bool __state)
        {
            if (!CustomBGM.HasHomeSongs) return; // 无自定义时让原逻辑自然工作
            if (!__state) return; // 原始 play=false（如加载阶段）则不播放
            try
            {
                int count = (__instance.entries != null) ? __instance.entries.Length : 0;
                if (count <= 0) return;
                int safe = index;
                if (safe < 0 || safe >= count) safe = Mathf.Clamp(safe, 0, count - 1);
                // 让原版 PlayBGM 先运行以完成 Studio 初始化，然后立即停止原曲并切到自定义
                try { Duckov.AudioManager.StopBGM(); } catch { }
                CustomBGM.PlayHomeBGM(safe);

                // 显示自定义歌曲信息（通过反射访问私有字段/属性，避免访问权限问题）
                if (showInfo && CustomBGM.TryGetCurrentMusicInfo(out var name, out var author))
                {
                    try
                    {
                        // 1) 尝试获取 proxy 并调用 Pop(string, float)
                        var proxyField = AccessTools.Field(__instance.GetType(), "proxy");
                        var proxy = proxyField?.GetValue(__instance);
                        if (proxy != null)
                        {
                            // 统一使用“自定义列表的实际索引”作为显示索引，避免与原 entries 索引错位
                            int homeCount = CustomBGM.GetHomeCount();
                            int displayIndex = (CustomBGM.TryGetCurrentHomeIndex(out var curIdx))
                                ? (curIdx + 1)
                                : (homeCount > 0 ? (((safe % homeCount) + homeCount) % homeCount) + 1 : (safe + 1));

                            string display = null;
                            var fmtProp = AccessTools.Property(__instance.GetType(), "BGMInfoFormat");
                            object fmt = fmtProp?.GetValue(__instance, null) ?? AccessTools.Field(__instance.GetType(), "BGMInfoFormat")?.GetValue(__instance);
                            if (fmt is string s)
                            {
                                display = s;
                                try
                                {
                                    display = display.Replace("{name}", name ?? "")
                                                     .Replace("{author}", author ?? "")
                                                     .Replace("{index}", displayIndex.ToString());
                                }
                                catch { /* ignore */ }
                            }

                            if (string.IsNullOrEmpty(display))
                                display = $"{name} - {author}  #{displayIndex}";

                            var pop = proxy.GetType().GetMethod("Pop", new System.Type[] { typeof(string), typeof(float) })
                                      ?? proxy.GetType().GetMethod("Pop", new System.Type[] { typeof(string), typeof(double) });
                            if (pop != null)
                            {
                                pop.Invoke(proxy, new object[] { display, 200f });
                            }
                            else
                            {
                                BGMLogger.Info("proxy.Pop(string,float) 未找到，已跳过显示信息。");
                            }
                        }
                        else
                        {
                            BGMLogger.Info("无法通过反射获取 BaseBGMSelector.proxy，已跳过显示信息。");
                        }
                    }
                    catch (Exception uiEx)
                    {
                        BGMLogger.Warn($"ShowInfo 反射失败: {uiEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                BGMLogger.Warn($"Set_Postfix 错误: {ex.Message}");
            }
        }

        // 2) SetNext/SetPrevious 保持放行，让它们内部最终调用 Set()
        [HarmonyPatch("SetNext")]
        [HarmonyPrefix]
        public static bool SetNext_Prefix_PassThrough() => true;

        [HarmonyPatch("SetPrevious")]
        [HarmonyPrefix]
        public static bool SetPrevious_Prefix_PassThrough() => true;
    }

        // --- 补丁 3：确保任何触发 AudioManager.StopBGM() 的场景（如场景切换/死亡等）
        // 都会同步停止自定义 BGM，避免叠加。
        [HarmonyPatch(typeof(AudioManager))]
        public static class AudioManagerStopBGMPatch
        {
            [HarmonyPatch("StopBGM")]
            [HarmonyPostfix]
            public static void StopBGM_Postfix()
            {
                try
                {
                    CustomBGM.StopCurrentBGM(false);
                    BGMLogger.Debug("StopBGM 钩子：已停止自定义 BGM 渠道");
                }
                catch { }
            }
        }


        // --- 监控补丁：记录所有 AudioManager.PlayStringer(string key) 调用 ---
        [HarmonyPatch(typeof(AudioManager))]
        public static class AudioManager_PlayStringer_Monitor
        {
            private static float s_ModStartTime = -1f;
            private static float s_AfterInitStartTime = -1f;

            [HarmonyPatch("PlayStringer", new Type[] { typeof(string) })]
            [HarmonyPostfix]
            public static void PlayStringer_Monitor_Postfix(string key)
            {
                try
                {
                    float now = Time.realtimeSinceStartup;
                    if (s_ModStartTime < 0f) s_ModStartTime = now;

                    bool afterInit = false;
                    try { afterInit = LevelManager.AfterInit; } catch { /* ignore */ }
                    if (afterInit && s_AfterInitStartTime < 0f) s_AfterInitStartTime = now;
                    string dtAfterInitStr = (s_AfterInitStartTime >= 0f) ? (now - s_AfterInitStartTime).ToString("F3") : "N/A";

                    bool isStPlaying = false;
                    try { isStPlaying = AudioManager.IsStingerPlaying; } catch { /* ignore */ }

                    string origin = "<stack-unavailable>";
                    try
                    {
                        var st = new System.Diagnostics.StackTrace(2, false);
                        int n = Math.Min(8, st.FrameCount);
                        var sb = new StringBuilder();
                        for (int i = 0; i < n; i++)
                        {
                            var f = st.GetFrame(i);
                            var m = f?.GetMethod();
                            if (m == null) continue;
                            var cls = m.DeclaringType != null ? m.DeclaringType.FullName : "<unknown>";
                            sb.Append($"{i}:{cls}.{m.Name} | ");
                        }
                        origin = sb.ToString();
                    }
                    catch { /* ignore */ }

                    BGMLogger.Info($"[Monitor] PlayStringer key='{key}', t={now:F3}s, AfterInit={afterInit}, dtAfterInit={dtAfterInitStr}s, IsStingerPlaying={isStPlaying}, origin={origin}");
                }
                catch (Exception e)
                {
                    BGMLogger.Warn($"[Monitor] PlayStringer logger error: {e.Message}");
                }
            }
        }

        // --- 仅拦截 stg_map_base，阻止原始 Stinger，改为播放自定义 start.mp3，并用 Getter 补丁模拟“仍在播放”以保留后续 BGM 的时序 ---
        [HarmonyPatch(typeof(AudioManager))]
        public static class AudioManager_PlayStringer_Intercept
        {
            private static bool s_CustomStingerActive;
            private static Sound s_StartSound;
            private static Channel s_StartChannel;

            [HarmonyPatch("PlayStringer", new Type[] { typeof(string) })]
            [HarmonyPrefix]
            public static bool Prefix(string key)
            {
                try
                {
                    if (!string.Equals(key, "stg_map_base", StringComparison.OrdinalIgnoreCase))
                        return true; // 仅拦截过渡期 Stinger

                    // 定位自定义文件
                    string startPath = Path.Combine(ModBehaviour.ModFolderName, "TitleBGM", "start.mp3");
                    if (!File.Exists(startPath))
                    {
                        BGMLogger.Info("[Intercept] 拦截到 stg_map_base Stinger，但 start.mp3 不存在，放行原 Stinger");
                        return true;
                    }

                    // 如果 FMOD 尚未初始化，走原始，保证安全
                    try { if (!FMODUnity.RuntimeManager.IsInitialized) { BGMLogger.Info("[Intercept] FMOD 未初始化，放行原 Stinger"); return true; } } catch { }

                    // 若 Music 总线静音/0音量，则放行原 Stinger，避免在 0 音量时仍有音乐
                    try {
                        var bus = FMODUnity.RuntimeManager.GetBus("bus:/Master/Music");
                        bool mute=false; float vol=1f;
                        try { bus.getMute(out mute); } catch { }
                        try { bus.getVolume(out vol); } catch { }
                        if (mute || vol <= 0.0001f) {
                            BGMLogger.Info("[Intercept] Music 总线为0/静音，放行原 Stinger");
                            return true;
                        }
                    } catch { }

                    // 创建 Sound（流式，单次播放，2D）
                    var mode = MODE.CREATESTREAM | MODE._2D | MODE.LOOP_OFF;
                    var res = FMODUnity.RuntimeManager.CoreSystem.createSound(startPath, mode, out s_StartSound);
                    if (res != RESULT.OK || !s_StartSound.hasHandle())
                    {
                        BGMLogger.Warn($"[Intercept] start.mp3 createSound 失败: {res}，放行原 Stinger");
                        return true;
                    }

                    // 尽量走 SFX，总线未就绪则回退到 Master
                    var group = ResolveStingerGroupSafe();
                    var playRes = FMODUnity.RuntimeManager.CoreSystem.playSound(s_StartSound, group, false, out s_StartChannel);
                    if (playRes == RESULT.OK && s_StartChannel.hasHandle())
                    {
                        s_CustomStingerActive = true;
                        BGMLogger.Info("[Intercept] 拦截到 stg_map_base Stinger，播放自定义 start.mp3（Music→SFX→Master 路由，保持与原版一致）");

                        // 启动监控协程：播放结束后清理并释放“占位”状态，用于还原 BaseBGMSelector 的时序
                        try
                        {
                            if (ModBehaviour.Instance != null)
                                ModBehaviour.Instance.StartCoroutine(WaitAndCleanupCustomStinger());
                            else
                                BGMLogger.Warn("[Intercept] 无 ModBehaviour.Instance，无法监控自定义 Stinger 结束（时序可能提前放行 BGM）。");
                        }
                        catch { }

                        return false; // 阻止原始 Stinger
                    }
                    else
                    {
                        BGMLogger.Warn($"[Intercept] start.mp3 playSound 失败: {playRes}，放行原 Stinger");
                        try { if (s_StartSound.hasHandle()) s_StartSound.release(); } catch { }
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    BGMLogger.Warn($"[Intercept] PlayStringer Prefix 异常: {ex.Message}");
                    return true; // 安全回退
                }
            }

            // 模拟 IsStingerPlaying = true，在我们自定义 start.mp3 播放期间，保留原本“等待 Stinger 结束再进 BGM”的节奏
            [HarmonyPatch("get_IsStingerPlaying")]
            [HarmonyPostfix]
            public static void IsStingerPlaying_Postfix(ref bool __result)
            {
                try
                {
                    if (s_CustomStingerActive)
                        __result = true;
                }
                catch { }
            }

            private static IEnumerator WaitAndCleanupCustomStinger()
            {
                float deadline = Time.realtimeSinceStartup + 12f; // 限制最长 12s，防止异常占位
                try
                {
                    while (Time.realtimeSinceStartup < deadline)
                    {
                        bool playing = false;

                        try { if (s_StartChannel.hasHandle()) s_StartChannel.isPlaying(out playing); } catch { }
                        if (!playing) break;
                        yield return new WaitForSeconds(0.05f);
                    }
                }
                finally
                {
                    s_CustomStingerActive = false;
                    try { if (s_StartChannel.hasHandle()) s_StartChannel.stop(); } catch { }
                    try { if (s_StartSound.hasHandle()) s_StartSound.release(); } catch { }
                }
            }

            public static ChannelGroup ResolveStingerGroupSafe()
            {
                try
                {
                    // 1) 优先尝试 Music bus（与原版 Stinger 一致）
                    try
                    {
                        var musicBus = FMODUnity.RuntimeManager.GetBus("bus:/Master/Music");
                        if (musicBus.getChannelGroup(out var cg) == RESULT.OK && cg.hasHandle())
                            return cg;
                    }
                    catch { }

                    // 2) 回退 SFX bus（Music 不可用时）
                    try
                    {
                        var sfxBus = FMODUnity.RuntimeManager.GetBus("bus:/Master/SFX");
                        if (sfxBus.getChannelGroup(out var cg2) == RESULT.OK && cg2.hasHandle())
                            return cg2;
                    }
                    catch { }

                    // 3) 最后回退 Master
                    if (FMODUnity.RuntimeManager.CoreSystem.getMasterChannelGroup(out var master) == RESULT.OK && master.hasHandle())
                        return master;
                }
                catch { }
                return default;
            }
        }


        // --- 拦截 AudioManager.Post(string) 的 stg_death，替换为自定义 death.mp3 ---
        [HarmonyPatch(typeof(AudioManager))]
        public static class AudioManager_Post_DeathStinger_Intercept
        {
            private static Sound s_DeathSound;
            private static Channel s_DeathChannel;

            [HarmonyPatch("Post", new Type[] { typeof(string) })]
            [HarmonyPrefix]
            public static bool Prefix(ref FMOD.Studio.EventInstance? __result, string eventName)
            {
                try
                {
                    if (!string.Equals(eventName, "Music/Stinger/stg_death", StringComparison.OrdinalIgnoreCase))
                        return true; // 非死亡 Stinger：放行

                    string deathPath = Path.Combine(ModBehaviour.ModFolderName, "TitleBGM", "death.mp3");
                    if (!File.Exists(deathPath))
                    {
                        BGMLogger.Info("[Intercept] 拦截到 stg_death，但 death.mp3 不存在，放行原 Stinger");
                        return true;
                    }

                    try { if (!FMODUnity.RuntimeManager.IsInitialized) { BGMLogger.Info("[Intercept] FMOD 未初始化，放行原 Stinger"); return true; } } catch { }

                    var mode = MODE.CREATESTREAM | MODE._2D | MODE.LOOP_OFF;
                    var res = FMODUnity.RuntimeManager.CoreSystem.createSound(deathPath, mode, out s_DeathSound);
                    if (res != RESULT.OK || !s_DeathSound.hasHandle())
                    {
                        BGMLogger.Warn($"[Intercept] death.mp3 createSound 失败: {res}，放行原 Stinger");
                        return true;
                    }

                    var group = AudioManager_PlayStringer_Intercept.ResolveStingerGroupSafe();
                    var playRes = FMODUnity.RuntimeManager.CoreSystem.playSound(s_DeathSound, group, false, out s_DeathChannel);
                    if (playRes == RESULT.OK && s_DeathChannel.hasHandle())
                    {
                        BGMLogger.Info("[Intercept] 拦截 stg_death，播放自定义 death.mp3（Music→SFX→Master 路由，保持与原版一致）");
                        try
                        {
                            if (ModBehaviour.Instance != null)
                                ModBehaviour.Instance.StartCoroutine(WaitAndCleanupDeath());
                        }
                        catch { }

                        __result = new FMOD.Studio.EventInstance?();
                        return false; // 阻止原始 stg_death
                    }
                    else
                    {
                        BGMLogger.Warn($"[Intercept] death.mp3 playSound 失败: {playRes}，放行原 Stinger");
                        try { if (s_DeathSound.hasHandle()) s_DeathSound.release(); } catch { }
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    BGMLogger.Warn($"[Intercept] Post(string) stg_death Prefix 异常: {ex.Message}");
                    return true; // 安全回退
                }
            }

            private static IEnumerator WaitAndCleanupDeath()
            {
                float deadline = Time.realtimeSinceStartup + 12f;
                try
                {
                    while (Time.realtimeSinceStartup < deadline)
                    {
                        bool playing = false;
                        try { if (s_DeathChannel.hasHandle()) s_DeathChannel.isPlaying(out playing); } catch { }
                        if (!playing) break;
                        yield return new WaitForSeconds(0.05f);
                    }
                }
                finally
                {
                    try { if (s_DeathChannel.hasHandle()) s_DeathChannel.stop(); } catch { }
                    try { if (s_DeathSound.hasHandle()) s_DeathSound.release(); } catch { }
                }
            }
        }

}