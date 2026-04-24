using System;
using DuckovCustomSounds.Logging;

namespace DuckovCustomSounds.CustomEnemySounds
{
    internal static class CESLogger
    {
        private static readonly ILog _logger = LogManager.GetLogger("Enemy");

        public static LogLevel CurrentLevel => LogManager.GetModuleLevel("Enemy");
        public static bool Enabled => LogManager.GlobalEnabled && LogManager.IsModuleEnabled("Enemy");

        // 快速日志级别检查（避免不必要的字符串构建）
        public static bool IsErrorEnabled => LogManager.ShouldLog("Enemy", LogLevel.Error);
        public static bool IsWarningEnabled => LogManager.ShouldLog("Enemy", LogLevel.Warning);
        public static bool IsInfoEnabled => LogManager.ShouldLog("Enemy", LogLevel.Info);
        public static bool IsDebugEnabled => LogManager.ShouldLog("Enemy", LogLevel.Debug);
        public static bool IsVerboseEnabled => LogManager.ShouldLog("Enemy", LogLevel.Verbose);

        // 兼容 voice_rules.json 的回退配置：仅在 settings.json 未显式指定该模块时生效
        public static void Configure(bool enabled, LogLevel level)
        {
            LogManager.ApplyVoiceRulesFallback("Enemy", enabled, level);
        }

        /// <summary>
        /// 应用基于文件的快速开关。如果存在 DuckovCustomSounds/debug_off 或 DuckovCustomSounds/.nolog，
        /// 将所有模块日志级别钳制至至多 Info（仍保留 Error/Info）。
        /// </summary>
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
