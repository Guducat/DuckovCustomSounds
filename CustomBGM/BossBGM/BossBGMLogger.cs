using System;
using DuckovCustomSounds.CustomBGM.Core; // BGMLogger

namespace DuckovCustomSounds.CustomBGM.BossBGM
{
    /// <summary>
    /// BOSS BGM 专用日志器
    /// 包装 Core.BGMLogger，添加 [BossBGM] 前缀
    /// </summary>
    internal static class BossBGMLogger
    {
        private static string Prefix => "[BossBGM]";

        public static void Info(string message)
        {
            if (BGMLogger.IsInfoEnabled)
            {
                BGMLogger.Info($"{Prefix} {message}");
            }
        }

        public static void Warning(string message)
        {
            if (BGMLogger.IsWarningEnabled)
            {
                BGMLogger.Warning($"{Prefix} {message}");
            }
        }

        public static void Error(string message)
        {
            BGMLogger.Error($"{Prefix} {message}");
        }

        public static void Error(string message, Exception ex)
        {
            BGMLogger.Error($"{Prefix} {message}", ex);
        }

        public static void Debug(string message)
        {
            if (BGMLogger.IsDebugEnabled)
            {
                BGMLogger.Debug($"{Prefix} {message}");
            }
        }
    }
}
