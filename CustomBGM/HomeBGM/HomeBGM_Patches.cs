using HarmonyLib;
using Duckov;
using System;
using UnityEngine;
using System.Collections.Generic;
using Duckov.Scenes;


namespace DuckovCustomSounds.CustomBGM.HomeBGM
{
    // --- 补丁 1：劫持主菜单 BGM ---
    [HarmonyPatch(typeof(AudioManager))]
    public static class AudioManagerPatch
    {
        [HarmonyPatch("PlayBGM")]
        [HarmonyPrefix]
        public static bool PlayBGM_Prefix(string name, ref FMOD.Studio.EventInstance? __result)
        {
            try
            {
                if (!HomeBGMConfig.Enabled) return true; // 模块未启用，放行原方法

                if (string.IsNullOrWhiteSpace(name)) return true;

                if (string.Equals(name, "mus_title", StringComparison.OrdinalIgnoreCase))
                {
                    if (HomeBGMManager.IsTitleStartSequenceActive())
                    {
                        HomeBGMLogger.Debug("[MenuStart] 序列进行中：屏蔽 mus_title（避免与 startFX 冲突）");
                        __result = null;
                        return false;
                    }

                    // 检查是否有自定义title BGM
                    string titlePath = HomeBGMManager.GetTitleBGMPath();
                    if (string.IsNullOrEmpty(titlePath))
                    {
                        HomeBGMLogger.Debug("未找到自定义title BGM文件，放行原版");
                        return true; // 没有自定义BGM，放行原版
                    }

                    HomeBGMLogger.Info($"拦截到 mus_title，阻止原版播放并使用自定义主菜单 BGM: {titlePath}");

                    try
                    {
                        HomeBGMManager.PlayTitleBGMWithStartFX();

                        // 设置返回值为空（因为我们自己处理了）
                        __result = null;

                        HomeBGMLogger.Info("自定义title BGM序列已启动");
                        return false; // 跳过原方法
                    }
                    catch (Exception ex)
                    {
                        HomeBGMLogger.Error($"播放自定义title BGM失败", ex);
                        // 注意：这里不能放行原版，因为我们已经StopBGM了
                        // 返回一个空的EventInstance避免崩溃
                        __result = null;
                        return false;
                    }
                }

                return true; // 其他BGM，放行
            }
            catch (Exception ex)
            {
                HomeBGMLogger.Error("PlayBGM_Prefix 严重错误", ex);
                return true; // 出错时放行
            }
        }
    }

        // 补丁：在场景切换/退出等触发 AudioManager.StopBGM() 时，同步停止自定义 HomeBGM 并禁用自动切歌
        [HarmonyPatch(typeof(AudioManager))]
        internal static class AudioManager_StopBGM_HomeBGMGuard
        {
            [HarmonyPatch("StopBGM")]
            [HarmonyPostfix]
            private static void Postfix()
            {
                try { HomeBGMManager.StopCurrentBGM(false); } catch { }
            }
        }


    // --- 补丁 2：在 BaseBGMSelector 初始化时扩展 entries，添加自定义 BGM ---
    [HarmonyPatch(typeof(BaseBGMSelector))]
    public static class BaseBGMSelectorPatch
    {
        // 防重入标志：防止 Set 方法被递归调用
        private static bool _isProcessingSet = false;
        private static float _lastStartStingerDelayLogTime = -10f;

            // 手动切歌标记：用于确保手动切歌时强制显示信息气泡
            [ThreadStatic]
            internal static bool _manualInvokeFlag = false;


        // 在 Awake 时扩展 entries 列表，添加自定义 BGM 的 Entry（包含 filePath）
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void Awake_Postfix(BaseBGMSelector __instance)
        {
            if (!HomeBGMConfig.Enabled) return; // 模块未启用

            try
            {
                if (!HomeBGMManager.HasHomeSongs) return;

                // 获取当前 entries（新版本是 List<Entry>）
                var entriesField = AccessTools.Field(typeof(BaseBGMSelector), "entries");
                if (entriesField == null)
                {
                    HomeBGMLogger.Warning("无法找到 BaseBGMSelector.entries 字段");
                    return;
                }

                var entries = entriesField.GetValue(__instance) as List<BaseBGMSelector.Entry>;
                if (entries == null)
                {
                    HomeBGMLogger.Warning("entries 不是 List<Entry> 类型");
                    return;
                }

                // 以自定义 BGM 作为唯一来源，彻底重建留声机的 entries 列表
                entries.Clear();

                int homeCount = HomeBGMManager.GetHomeCount();
                for (int i = 0; i < homeCount; i++)
                {
                    if (HomeBGMManager.TryGetHomeMusicInfo(i, out var name, out var author, out var filePath))
                    {
                        var entry = new BaseBGMSelector.Entry
                        {
                            musicName = name,
                            author = author,
                            switchName = $"custom_{i}", // 占位，不会被使用
                            filePath = filePath // 关键：设置文件路径，我们在 Set() Prefix 中拦截并使用 PostCustomSound 播放
                        };
                        entries.Add(entry);
                        HomeBGMLogger.Debug($"添加自定义 BGM Entry: {name} - {author}, filePath={filePath}");
                    }
                }

                // 校正 index，确保在新的范围内
                try
                {
                    var indexField = AccessTools.Field(typeof(BaseBGMSelector), "index");
                    if (indexField != null)
                    {
                        int idx = (int)indexField.GetValue(__instance);
                        if (entries.Count > 0)
                        {
                            idx = ((idx % entries.Count) + entries.Count) % entries.Count;
                        }
                        else
                        {
                            idx = 0;
                        }
                        indexField.SetValue(__instance, idx);
                    }
                }
                catch { }

                HomeBGMLogger.Info($"已重建 BaseBGMSelector.entries，总计 {homeCount} 首自定义 BGM");
            }
            catch (Exception ex)
            {
                HomeBGMLogger.Warning($"Awake_Postfix 错误: {ex.Message}");
            }
        }

        // 携带 Set() 调用的原始信息
        public struct SetState
        {
            public bool origPlay;
            public bool origShowInfo;
            public int realIndex;
        }

        // Set() Prefix：处理索引范围，拦截自定义 BGM 播放
        [HarmonyPatch("Set", new Type[] { typeof(int), typeof(bool), typeof(bool) })]
        [HarmonyPrefix]
        public static bool Set_Prefix(BaseBGMSelector __instance, ref int index, ref bool showInfo, ref bool play, ref SetState __state)
        {
            if (!HomeBGMConfig.Enabled) return true; // 模块未启用，放行

            // 防重入保护：如果已经在处理 Set 调用，直接放行避免递归
            if (_isProcessingSet)
            {
                HomeBGMLogger.Debug("检测到重入调用，跳过处理");
                return true;
            }

            __state = new SetState { origPlay = play, origShowInfo = showInfo, realIndex = index };
            var manualFlagAtEnter = _manualInvokeFlag;
            HomeBGMLogger.Verbose($"Set_Prefix ENTER: index={index}, play={play}, showInfo={showInfo}, manualFlag={manualFlagAtEnter}");

            if (HomeBGMManager.HasHomeSongs)
            {
                if (!play)
                {
                    // 加载阶段（原始 play=false），停止当前 BGM
                    HomeBGMManager.StopCurrentBGM(false);
                    HomeBGMLogger.Debug("Load 阶段停止 BGM");
                }

                // 获取 entries 总数（包含原版 + 自定义）
                var entriesField = AccessTools.Field(typeof(BaseBGMSelector), "entries");
                var entries = entriesField?.GetValue(__instance) as List<BaseBGMSelector.Entry>;
                int totalCount = (entries != null) ? entries.Count : 0;

                // 确保索引在有效范围内
                if (totalCount > 0)
                {
                    index = ((index % totalCount) + totalCount) % totalCount;
                }

                if (play && !manualFlagAtEnter && HomeBGMManager.IsStartStingerProtectionActive())
                {
                    try { AccessTools.Field(typeof(BaseBGMSelector), "waitForStinger")?.SetValue(__instance, true); } catch { }
                    try { AccessTools.Field(typeof(BaseBGMSelector), "index")?.SetValue(__instance, index); } catch { }
                    if (Time.realtimeSinceStartup - _lastStartStingerDelayLogTime >= 1f)
                    {
                        _lastStartStingerDelayLogTime = Time.realtimeSinceStartup;
                        HomeBGMLogger.Debug($"start.mp3 保护中：延后自动 HomeBGM 播放，index={index}");
                    }
                    return false;
                }

                // 检查是否是自定义 BGM（有 filePath）
                if (play && entries != null && index >= 0 && index < entries.Count)
                {
                    var entry = entries[index];
                    if (!string.IsNullOrEmpty(entry.filePath))
                    {
                        // 这是自定义 BGM，拦截并使用 PostCustomSound 播放
                        _isProcessingSet = true; // 设置防重入标志
                        try
                        {
                            // 停止当前 BGM
                            AudioManager.StopBGM();

                            // 播放自定义 BGM（循环播放）
                            HomeBGMManager.PlayHomeBGMByFilePath(entry.filePath, entry.musicName);
                                var isManual = _manualInvokeFlag;


                            // 启用自动切歌逻辑
                            HomeBGMManager.EnableAutoAdvance(index, entries.Count);

                            // 显示自定义 BGM 信息
                            if (showInfo || isManual)
                            {
                                HomeBGMLogger.Debug($"调用 ShowCustomBGMInfo: showInfo={showInfo}, manual={isManual}, name={entry.musicName}, author={entry.author}, displayIndex={index + 1}");
                                ShowCustomBGMInfo(__instance, entry.musicName, entry.author, index + 1);
                            }
                            else
                            {
                                HomeBGMLogger.Debug("showInfo=false，跳过信息气泡显示");
                            }

                            // 安全获取歌曲信息（防止空值导致日志截断）
                            string safeMusicName = string.IsNullOrEmpty(entry.musicName) ? "未知曲目" : entry.musicName;
                            string safeAuthor = string.IsNullOrEmpty(entry.author) ? "未知作者" : entry.author;
                            HomeBGMLogger.Info($"拦截留声机播放自定义 BGM: {safeMusicName} - {safeAuthor}");

                            // 设置 BaseBGMSelector 的状态，防止 Update() 再次调用 Set()
                            try { AccessTools.Field(typeof(BaseBGMSelector), "waitForStinger")?.SetValue(__instance, false); } catch { }
                            try { AccessTools.Field(typeof(BaseBGMSelector), "index")?.SetValue(__instance, index); } catch { }

                            // 跳过原方法
                            return false;
                        }
                        catch (Exception ex)
                        {
                            HomeBGMLogger.Warning($"播放自定义 BGM 失败: {ex.Message}");
                            // 出错时放行原方法
                        }
                        finally
                        {
                            _isProcessingSet = false; // 确保标志被重置
                        }
                    }
                }

                // 抑制原版 UI 提示，由我们在 Postfix 统一显示
                if (showInfo) showInfo = false;

                HomeBGMLogger.Debug($"Set Prefix: realIndex={__state.realIndex}, adjustedIndex={index}, play={play}");
            }

            // 放行原方法（原版 BGM）
            return true;
        }

        // 显示自定义 BGM 信息
        private static void ShowCustomBGMInfo(BaseBGMSelector instance, string name, string author, int displayIndex)
        {
            try
            {
                // 优先在 BaseBGMSelector 类型上拿 proxy 字段，其次退回到实例实际类型
                var proxyField = AccessTools.Field(typeof(BaseBGMSelector), "proxy") ?? AccessTools.Field(instance.GetType(), "proxy");
                var proxy = proxyField?.GetValue(instance);
                if (proxy == null)
                {
                    HomeBGMLogger.Warning("ShowCustomBGMInfo: 无法获取 DialogueBubbleProxy 实例（proxy==null）");
                    return;
                }

                string display = $"{name} - {author}  #{displayIndex}";
                var proxyType = proxy.GetType();

                // 尝试调用 proxy.Pop(string, float) 或 (string, double)
                var pop = proxyType.GetMethod("Pop", new Type[] { typeof(string), typeof(float) })
                          ?? proxyType.GetMethod("Pop", new Type[] { typeof(string), typeof(double) });
                if (pop != null)
                {
                    HomeBGMLogger.Debug("ShowCustomBGMInfo: 调用 proxy.Pop(...) 显示信息气泡");
                    pop.Invoke(proxy, new object[] { display, 200f });
                }
                else
                {
                    HomeBGMLogger.Warning($"ShowCustomBGMInfo: 未找到 Pop(string, float/double) 方法，proxyType={proxyType.FullName}");
                }
            }
            catch (Exception ex)
            {
                HomeBGMLogger.Warning($"ShowCustomBGMInfo 错误: {ex.Message}");
            }
        }

        // SetNext：处理随机模式和索引循环
        [HarmonyPatch("SetNext")]
        [HarmonyPrefix]
        public static bool SetNext_Prefix(BaseBGMSelector __instance)
        {
            if (!HomeBGMConfig.Enabled) return true; // 模块未启用，放行
            if (!HomeBGMManager.HasHomeSongs) return true;

            try
            {
                var entriesField = AccessTools.Field(typeof(BaseBGMSelector), "entries");
                var entries = entriesField?.GetValue(__instance) as List<BaseBGMSelector.Entry>;
                if (entries == null || entries.Count == 0) return true;

                var indexField = AccessTools.Field(typeof(BaseBGMSelector), "index");
                if (indexField == null) return true;

                int currentIndex = (int)indexField.GetValue(__instance);
                int nextIndex;

                // 随机模式
                if (HomeBGMConfig.RandomEnabled)
                {
                    nextIndex = HomeBGMManager.GetRandomHomeIndex(HomeBGMConfig.AvoidImmediateRepeat);
                }
                else
                {
                    nextIndex = currentIndex + 1;
                }

                // 调用 Set 方法（标记为手动切歌，以确保信息气泡）
                HomeBGMLogger.Debug($"SetNext_Prefix: current={currentIndex}, next={nextIndex}, markManual=true");
                try { _manualInvokeFlag = true; __instance.Set(nextIndex, true, true); }
                finally { _manualInvokeFlag = false; }
                return false; // 跳过原方法
            }
            catch (Exception ex)
            {
                HomeBGMLogger.Warning($"SetNext_Prefix 错误: {ex.Message}");
                return true; // 出错时放行原方法
            }
        }

        // SetPrevious：处理随机模式和索引循环
        [HarmonyPatch("SetPrevious")]
        [HarmonyPrefix]
        public static bool SetPrevious_Prefix(BaseBGMSelector __instance)
        {
            if (!HomeBGMConfig.Enabled) return true; // 模块未启用，放行
            if (!HomeBGMManager.HasHomeSongs) return true;

            try
            {
                var entriesField = AccessTools.Field(typeof(BaseBGMSelector), "entries");
                var entries = entriesField?.GetValue(__instance) as List<BaseBGMSelector.Entry>;
                if (entries == null || entries.Count == 0) return true;

                var indexField = AccessTools.Field(typeof(BaseBGMSelector), "index");
                if (indexField == null) return true;

                int currentIndex = (int)indexField.GetValue(__instance);
                int prevIndex;

                // 随机模式（如果启用了 Previous 也随机）
                if (HomeBGMConfig.RandomEnabled && HomeBGMConfig.RandomizePrevious)
                {
                    prevIndex = HomeBGMManager.GetRandomHomeIndex(HomeBGMConfig.AvoidImmediateRepeat);
                }
                else
                {
                    prevIndex = currentIndex - 1;
                }

                // 调用 Set 方法（标记为手动切歌，以确保信息气泡）
                HomeBGMLogger.Debug($"SetPrevious_Prefix: current={currentIndex}, prev={prevIndex}, markManual=true");
                try { _manualInvokeFlag = true; __instance.Set(prevIndex, true, true); }
                finally { _manualInvokeFlag = false; }
                return false; // 跳过原方法
            }
            catch (Exception ex)
            {
                HomeBGMLogger.Warning($"SetPrevious_Prefix 错误: {ex.Message}");
                return true; // 出错时放行原方法
            }



        }
    }

    // 额外补丁：拦截 BaseBGMSelector.Set(string switchName)（UI 直接点击条目）
    [HarmonyPatch(typeof(BaseBGMSelector))]
    public static class BaseBGMSelectorSetBySwitchPatch
    {
        [HarmonyPatch("Set", new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool Set_BySwitchName_Prefix(BaseBGMSelector __instance, string switchName)
        {
            if (!HomeBGMConfig.Enabled) return true; // 模块未启用，放行
            if (!HomeBGMManager.HasHomeSongs) return true;
            try
            {
                int idx = __instance.GetIndex(switchName);
                HomeBGMLogger.Debug($"Set_BySwitchName_Prefix: switchName={switchName}, idx={idx}, markManual=true");
                if (idx < 0) return true; // 未找到，放行原方法
                try { BaseBGMSelectorPatch._manualInvokeFlag = true; __instance.Set(idx, true, true); }
                finally { BaseBGMSelectorPatch._manualInvokeFlag = false; }
                return false; // 我们已处理
            }
            catch (Exception ex)
            {
                HomeBGMLogger.Warning($"Set_BySwitchName_Prefix 异常: {ex.Message}");
                return true; // 出错放行
            }
        }
    }

    // 加强保险：场景卸载时（LocalOnSubSceneWillBeUnloaded）停止 HomeBGM，避免过渡期误触发
    [HarmonyPatch(typeof(MultiSceneCore), "LocalOnSubSceneWillBeUnloaded")]
    internal static class MultiSceneCore_Unload_HomeBGMGuard
    {
        [HarmonyPostfix]
        private static void Postfix(UnityEngine.SceneManagement.Scene scene)
        {
            try { HomeBGMManager.StopCurrentBGM(false); } catch { }
        }
    }

}


/*
	// --- 补丁 4：抑制留声机 Stinger 事件，避免干扰自定义 BGM ---
	[HarmonyPatch(typeof(Duckov.AudioObject))]
	public static class AudioObjectStingerPatch
	{
	    [HarmonyPatch("Post", new System.Type[] { typeof(string), typeof(bool) })]
	    [HarmonyPrefix]
	    public static bool Post_Prefix(Duckov.AudioObject __instance, ref string eventName, bool doRelease, ref FMOD.Studio.EventInstance? __result)
	    {
	        try
	        {
	            if (!HomeBGMConfig.Enabled) return true;
	            if (string.IsNullOrEmpty(eventName)) return true;

	            // 仅在 StingerSource 上抑制 Stinger 事件
	            var goName = __instance != null && __instance.gameObject != null ? __instance.gameObject.name : null;
	            if (!string.IsNullOrEmpty(goName) && goName.Contains("StingerSource") && eventName.StartsWith("Music/Stinger/"))
	            {
	                HomeBGMLogger.Debug($"抑制 Stinger 事件: '{eventName}', go={goName}");
	                __result = null;
	                return false; // 跳过原方法
	            }
	        }
	        catch { }
	        return true; // 其他情况放行
	    }
	}
*/
