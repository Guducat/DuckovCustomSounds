using System;
using DuckovCustomSounds.Logging;

namespace DuckovCustomSounds.CustomGrenadeSounds
{
    internal static class GrenadeLogger
    {
        private static readonly ILog _logger = LogManager.GetLogger("Grenade");

        // 快速日志级别检查（避免不必要的字符串构建）
        public static bool IsErrorEnabled => LogManager.ShouldLog("Grenade", LogLevel.Error);
        public static bool IsWarningEnabled => LogManager.ShouldLog("Grenade", LogLevel.Warning);
        public static bool IsInfoEnabled => LogManager.ShouldLog("Grenade", LogLevel.Info);
        public static bool IsDebugEnabled => LogManager.ShouldLog("Grenade", LogLevel.Debug);
        public static bool IsVerboseEnabled => LogManager.ShouldLog("Grenade", LogLevel.Verbose);

        public static ILog ForScope(params string[] scopes) => _logger.ForScope(scopes);
        public static void Error(string msg, Exception? ex = null) => _logger.Error(msg, ex);
        public static void Warning(string msg) => _logger.Warning(msg);
        public static void Info(string msg) => _logger.Info(msg);
        public static void Debug(string msg) => _logger.Debug(msg);
        public static void Verbose(string msg) => _logger.Verbose(msg);

        // 向后兼容：Warn() -> Warning()
        [Obsolete("Use Warning() instead")]
        public static void Warn(string msg) => Warning(msg);
    }
}
