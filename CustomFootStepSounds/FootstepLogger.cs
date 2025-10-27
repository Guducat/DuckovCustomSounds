using System;
using DuckovCustomSounds.Logging;

namespace DuckovCustomSounds.CustomFootStepSounds
{
    public enum LogLevel
    {
        Error = 0,
        Info = 1,
        Debug = 2,
        Verbose = 3,
    }

    internal static class FootstepLogger
    {
        // 仅当 CFS 与 CES 的模块级别均达到 Debug 时，才输出“详细路由/路径”类日志
        public static bool DetailedRoutingEnabled
        {
            get
            {
                try
                {
                    return LogManager.ShouldLog("CustomFootStepSounds", Logging.LogLevel.Debug)
                        && LogManager.ShouldLog("CustomEnemySounds", Logging.LogLevel.Debug);
                }
                catch { return false; }
            }
        }

        // 供 [CFS:Route]/[CFS:Path] 等详细日志使用的便捷方法
        public static void DebugDetail(string msg)
        {
            if (!DetailedRoutingEnabled) return;
            // 复用模块前缀风格，避免双重判断
            UnityEngine.Debug.Log("[CustomFootStepSounds:Debug] " + msg);
        }

        public static LogLevel CurrentLevel
        {
            get
            {
                var lv = LogManager.GetModuleLevel("CustomFootStepSounds");
                return lv switch
                {
                    Logging.LogLevel.Error => LogLevel.Error,
                    Logging.LogLevel.Info => LogLevel.Info,
                    Logging.LogLevel.Debug => LogLevel.Debug,
                    Logging.LogLevel.Verbose => LogLevel.Verbose,
                    Logging.LogLevel.Warning => LogLevel.Info,
                    _ => LogLevel.Info,
                };
            }
        }
        public static bool Enabled => LogManager.GlobalEnabled && LogManager.IsModuleEnabled("CustomFootStepSounds");

        public static void Configure(bool enabled, LogLevel level)
        {
            LogManager.ApplyVoiceRulesFallback("CustomFootStepSounds", enabled, ToUnified(level));
        }

        public static void ApplyFileSwitches(string modRoot)
        {
            try { LogManager.ApplyFileSwitches(modRoot); } catch { }
        }

        public static void Error(string msg, Exception? ex = null)
        {
            if (!LogManager.ShouldLog("CustomFootStepSounds", Logging.LogLevel.Error)) return;
            UnityEngine.Debug.LogError("[CustomFootStepSounds] " + msg + (ex != null ? ("\n" + ex) : string.Empty));
        }
        public static void Info(string msg)
        {
            if (!LogManager.ShouldLog("CustomFootStepSounds", Logging.LogLevel.Info)) return;
            UnityEngine.Debug.Log("[CustomFootStepSounds] " + msg);
        }
        public static void Debug(string msg)
        {
            if (!LogManager.ShouldLog("CustomFootStepSounds", Logging.LogLevel.Debug)) return;
            UnityEngine.Debug.Log("[CustomFootStepSounds:Debug] " + msg);
        }
        public static void Verbose(string msg)
        {
            if (!LogManager.ShouldLog("CustomFootStepSounds", Logging.LogLevel.Verbose)) return;
            UnityEngine.Debug.Log("[CustomFootStepSounds:Verbose] " + msg);
        }

        private static Logging.LogLevel ToUnified(LogLevel lv)
        {
            return lv switch
            {
                LogLevel.Error => Logging.LogLevel.Error,
                LogLevel.Info => Logging.LogLevel.Info,
                LogLevel.Debug => Logging.LogLevel.Debug,
                LogLevel.Verbose => Logging.LogLevel.Verbose,
                _ => Logging.LogLevel.Info,
            };
        }
    }
}
