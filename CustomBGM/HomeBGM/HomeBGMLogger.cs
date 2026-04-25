using DuckovCustomSounds.Logging;

namespace DuckovCustomSounds.CustomBGM.HomeBGM
{
    /// <summary>
    /// HomeBGM 模块专用日志
    /// </summary>
    internal static class HomeBGMLogger
    {
        private static readonly ILog _log = LogManager.GetLogger("HomeBGM");

        public static void Debug(string message) => _log.Debug(message);
        public static void Info(string message) => _log.Info(message);
        public static void Warning(string message) => _log.Warning(message);
        public static void Error(string message, System.Exception ex = null)
        {
            if (ex != null)
                _log.Error($"{message}: {ex.Message}");
            else
                _log.Error(message);
        }
    }
}
