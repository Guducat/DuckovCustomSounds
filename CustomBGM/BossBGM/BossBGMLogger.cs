using System;
using DuckovCustomSounds.CustomBGM.Core; // BGMLogger
using DuckovCustomSounds.Logging;

namespace DuckovCustomSounds.CustomBGM.BossBGM
{
    /// <summary>
    /// BOSS BGM 专用日志器
    /// 包装 Core.BGMLogger，添加 BossBGM 结构化作用域
    /// </summary>
    internal static class BossBGMLogger
    {
        private static readonly ILog Log = BGMLogger.ForScope("BossBGM");

        public static void Info(string message)
        {
            if (BGMLogger.IsInfoEnabled)
            {
                Log.Info(message);
            }
        }

        public static void Warning(string message)
        {
            if (BGMLogger.IsWarningEnabled)
            {
                Log.Warning(message);
            }
        }

        public static void Error(string message)
        {
            Log.Error(message);
        }

        public static void Error(string message, Exception ex)
        {
            Log.Error(message, ex);
        }

        public static void Debug(string message)
        {
            if (BGMLogger.IsDebugEnabled)
            {
                Log.Debug(message);
            }
        }
    }
}
