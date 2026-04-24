using System;
using DuckovCustomSounds.Logging;

namespace DuckovCustomSounds.CustomFootStepSounds
{
    internal static class FootstepLogger
    {
        private static readonly ILog _logger = LogManager.GetLogger("Footstep");

        // 仅当 Footstep 模块级别达到 Debug 时，才输出详细路由类日志
        public static bool DetailedRoutingEnabled
        {
            get
            {
                try
                {
                    return LogManager.ShouldLog("Footstep", LogLevel.Debug);
                }
                catch { return false; }
            }
        }

        public static bool DetailedVerboseEnabled
        {
            get
            {
                try
                {
                    return LogManager.ShouldLog("Footstep", LogLevel.Verbose);
                }
                catch { return false; }
            }
        }

        // 供详细路由/路径日志使用的便捷方法
        public static void DebugDetail(string msg)
        {
            if (!DetailedRoutingEnabled) return;
            _logger.Debug(msg);
        }

        public static void VerboseDetail(string msg)
        {
            if (!DetailedVerboseEnabled) return;
            _logger.Verbose(msg);
        }

        public static LogLevel CurrentLevel => LogManager.GetModuleLevel("Footstep");
        public static bool Enabled => LogManager.GlobalEnabled && LogManager.IsModuleEnabled("Footstep");

        // 快速日志级别检查（避免不必要的字符串构建）
        public static bool IsErrorEnabled => LogManager.ShouldLog("Footstep", LogLevel.Error);
        public static bool IsWarningEnabled => LogManager.ShouldLog("Footstep", LogLevel.Warning);
        public static bool IsInfoEnabled => LogManager.ShouldLog("Footstep", LogLevel.Info);
        public static bool IsDebugEnabled => LogManager.ShouldLog("Footstep", LogLevel.Debug);
        public static bool IsVerboseEnabled => LogManager.ShouldLog("Footstep", LogLevel.Verbose);

        public static void Configure(bool enabled, LogLevel level)
        {
            LogManager.ApplyVoiceRulesFallback("Footstep", enabled, level);
        }

        public static void ApplyFileSwitches(string modRoot)
        {
            try { LogManager.ApplyFileSwitches(modRoot); } catch { }
        }

        public static ILog ForScope(params string[] scopes) => _logger.ForScope(scopes);
        public static void Error(string msg, Exception? ex = null) => _logger.Error(msg, ex);
        public static void Warning(string msg) => _logger.Warning(msg);
        public static void Info(string msg) => _logger.Info(msg);
        public static void Debug(string msg) => _logger.Debug(msg);
        public static void Verbose(string msg) => _logger.Verbose(msg);
    }
}
