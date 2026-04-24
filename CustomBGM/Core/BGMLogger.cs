using System;
using DuckovCustomSounds.Logging;

namespace DuckovCustomSounds.CustomBGM.Core
{
    /// <summary>
    /// BGM 模块统一日志器（供所有 BGM 子模块使用）
    /// </summary>
    internal static class BGMLogger
    {
        private static readonly ILog _logger = LogManager.GetLogger("BGM");

        // 快速日志级别检查（避免不必要的字符串构建）
        public static bool IsErrorEnabled => LogManager.ShouldLog("BGM", LogLevel.Error);
        public static bool IsWarningEnabled => LogManager.ShouldLog("BGM", LogLevel.Warning);
        public static bool IsInfoEnabled => LogManager.ShouldLog("BGM", LogLevel.Info);
        public static bool IsDebugEnabled => LogManager.ShouldLog("BGM", LogLevel.Debug);
        public static bool IsVerboseEnabled => LogManager.ShouldLog("BGM", LogLevel.Verbose);

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
