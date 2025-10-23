using HarmonyLib;
using Duckov; // 引用 AudioManager
using System;
using UnityEngine; // 引用 Type

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

}