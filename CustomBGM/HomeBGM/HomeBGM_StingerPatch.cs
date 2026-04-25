using HarmonyLib;
using Duckov;
using System;

namespace DuckovCustomSounds.CustomBGM.HomeBGM
{
    // 独立文件：抑制留声机 Stinger 事件，避免干扰自定义 BGM（仅限 StingerSource）
    [HarmonyPatch(typeof(AudioObject))]
    public static class HomeBGM_StingerPatch
    {
        [HarmonyPatch("Post", new Type[] { typeof(string), typeof(bool) })]
        [HarmonyPrefix]
        public static bool Post_Prefix(AudioObject __instance, ref string eventName, bool doRelease, ref FMOD.Studio.EventInstance? __result)
        {
            try
            {
                if (!HomeBGMConfig.Enabled) return true;
                if (string.IsNullOrEmpty(eventName)) return true;

                // 仅在 StingerSource 上抑制 Stinger 事件，且仅当不在游戏中时（在主菜单/大厅）
                var goName = __instance != null && __instance.gameObject != null ? __instance.gameObject.name : null;
                if (!string.IsNullOrEmpty(goName) && goName.Contains("StingerSource") && eventName.StartsWith("Music/Stinger/"))
                {
                    // 检查是否在主基地/主菜单中
                    bool inBase = false;
                    try
                    {
                        // 使用 MapDetector 检查是否在主基地/主菜单中
                        inBase = MapDetection.MapDetector.IsInBase();
                    }
                    catch { }
                    
                    // 只在主基地/主菜单时抑制 Stinger 事件，游戏中允许播放（如撤离音乐）
                    if (inBase)
                    {
                        HomeBGMLogger.Debug($"抑制 Stinger 事件: '{eventName}', go={goName}, 位置=主基地/主菜单");
                        __result = null;
                        return false; // 跳过原方法
                    }
                    else
                    {
                        HomeBGMLogger.Debug($"允许 Stinger 事件: '{eventName}', go={goName}, 位置=游戏中");
                    }
                }
            }
            catch { }
            return true; // 其他情况放行
        }
    }
}

