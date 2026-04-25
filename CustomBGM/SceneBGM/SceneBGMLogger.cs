using System;
using DuckovCustomSounds.Logging;

namespace DuckovCustomSounds.CustomBGM.SceneBGM
{
    /// <summary>
    /// SceneBGM 日志工具
    /// </summary>
    internal static class SceneBGMLogger
    {
        private static readonly ILog Log = LogManager.GetLogger("SceneBGM");

        public static void Info(string message)
        {
            Log.Info(message);
        }

        public static void Debug(string message)
        {
            Log.Debug(message);
        }

        public static void Warning(string message)
        {
            Log.Warning(message);
        }

        public static void Error(string message, Exception ex = null)
        {
            if (ex != null)
            {
                Log.Error(message, ex);
            }
            else
            {
                Log.Error(message);
            }
        }
    }
}
