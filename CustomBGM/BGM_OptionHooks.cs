using HarmonyLib;
using Duckov;

namespace DuckovCustomSounds.CustomBGM
{
    /// <summary>
    /// 监听游戏音频设置应用，当 Music 总线被静音或音量为 0 时，停止自定义 BGM，避免任何回退到 Core Master 的机会。
    /// </summary>
    [HarmonyPatch(typeof(AudioManager))]
    public static class BGM_OptionHooks
    {
        [HarmonyPatch(typeof(AudioManager.Bus))]
        public static class AudioManager_Bus_Apply_Postfix
        {
            [HarmonyPatch("Apply")]
            [HarmonyPostfix]
            public static void Postfix(object __instance)
            {
                try
                {
                    // 反射获取 Bus 的关键信息
                    var type = __instance.GetType(); // AudioManager+Bus
                    string rtpc = null;
                    float volume = 1f;
                    bool mute = false;

                    try
                    {
                        var fRtpc = AccessTools.Field(type, "volumeRTPC");
                        if (fRtpc != null) rtpc = fRtpc.GetValue(__instance) as string;
                    }
                    catch { }

                    try
                    {
                        var pVol = AccessTools.Property(type, "Volume");
                        if (pVol != null) volume = (float)pVol.GetValue(__instance, null);
                    }
                    catch { }

                    try
                    {
                        var pMute = AccessTools.Property(type, "Mute");
                        if (pMute != null) mute = (bool)pMute.GetValue(__instance, null);
                    }
                    catch { }

                    if (string.Equals(rtpc, "Master/Music", System.StringComparison.OrdinalIgnoreCase))
                    {
                        if (mute || volume <= 0.0001f)
                        {
                            // 用户将 Music 拉为 0 或静音：立刻停止我们自定义 BGM
                            CustomBGM.StopCurrentBGM(false);
                            BGMLogger.Info("Music 配置变更为静音/0音量：已停止自定义 BGM。");
                        }
                    }
                }
                catch { }
            }
        }
    }
}

