using DuckovCustomSounds.Logging;

namespace DuckovCustomSounds.CustomBGM.ExtractionBGM
{
    /// <summary>
    /// ExtractionBGM 模块专用日志
    /// </summary>
    internal static class ExtractionBGMLogger
    {
        private static readonly ILog _log = LogManager.GetLogger("ExtractionBGM");

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
