using System;
using DuckovCustomSounds.Logging;

namespace DuckovCustomSounds.CustomKillFeedback
{
    internal static class KillFeedbackLogger
    {
        private static readonly ILog _logger = LogManager.GetLogger("KillFeedback");

        public static LogLevel CurrentLevel => LogManager.GetModuleLevel("KillFeedback");
        public static bool Enabled => LogManager.GlobalEnabled && LogManager.IsModuleEnabled("KillFeedback");

        public static bool IsErrorEnabled => LogManager.ShouldLog("KillFeedback", LogLevel.Error);
        public static bool IsWarningEnabled => LogManager.ShouldLog("KillFeedback", LogLevel.Warning);
        public static bool IsInfoEnabled => LogManager.ShouldLog("KillFeedback", LogLevel.Info);
        public static bool IsDebugEnabled => LogManager.ShouldLog("KillFeedback", LogLevel.Debug);
        public static bool IsVerboseEnabled => LogManager.ShouldLog("KillFeedback", LogLevel.Verbose);

        public static void Configure(bool enabled, LogLevel level)
        {
            LogManager.ApplyVoiceRulesFallback("KillFeedback", enabled, level);
        }

        public static void ApplyFileSwitches(string modRoot)
        {
            try { LogManager.ApplyFileSwitches(modRoot); } catch { }
        }

        public static void Error(string msg, Exception? ex = null) => _logger.Error(msg, ex);
        public static void Warning(string msg) => _logger.Warning(msg);
        public static void Info(string msg) => _logger.Info(msg);
        public static void Debug(string msg) => _logger.Debug(msg);
        public static void Verbose(string msg) => _logger.Verbose(msg);
    }
}

