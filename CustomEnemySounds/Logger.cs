using System;
using System.IO;
using UnityEngine;
using DuckovCustomSounds.Logging; // [compat] unified log manager

namespace DuckovCustomSounds.CustomEnemySounds
{
    // 保留原有枚举以兼容现有 voice_rules.json 的解析与旧调用
    public enum LogLevel
    {
        Error = 0,
        Info = 1,
        Debug = 2,
        Verbose = 3,
    }

    internal static class CESLogger
    {
        // 兼容属性：从 LogManager 的实时生效值映射
        public static LogLevel CurrentLevel
        {
            get
            {
                var lv = LogManager.GetModuleLevel("CustomEnemySounds");
                return lv switch
                {
                    Logging.LogLevel.Error => LogLevel.Error,
                    Logging.LogLevel.Info => LogLevel.Info,
                    Logging.LogLevel.Debug => LogLevel.Debug,
                    Logging.LogLevel.Verbose => LogLevel.Verbose,
                    // Warning 在旧枚举中不存在，按 Info 处理（允许 Error/Warning/Info）
                    Logging.LogLevel.Warning => LogLevel.Info,
                    _ => LogLevel.Info,
                };
            }
        }
        public static bool Enabled => LogManager.GlobalEnabled && LogManager.IsModuleEnabled("CustomEnemySounds");

        // 兼容 voice_rules.json 的回退配置：仅在 settings.json 未显式指定该模块时生效
        public static void Configure(bool enabled, LogLevel level)
        {
            LogManager.ApplyVoiceRulesFallback("CustomEnemySounds", enabled, ToUnified(level));
        }

        /// <summary>
        /// 应用基于文件的快速开关。如果存在 DuckovCustomSounds/debug_off 或 DuckovCustomSounds/.nolog，
        /// 将所有模块日志级别钳制至至多 Info（仍保留 Error/Info）。
        /// </summary>
        public static void ApplyFileSwitches(string modRoot)
        {
            try { LogManager.ApplyFileSwitches(modRoot); } catch { }
        }

        public static void Error(string msg, Exception? ex = null)
        {
            if (!LogManager.ShouldLog("CustomEnemySounds", Logging.LogLevel.Error)) return;
            UnityEngine.Debug.LogError("[CustomEnemySounds] " + msg + (ex != null ? ("\n" + ex) : string.Empty));
        }

        public static void Info(string msg)
        {
            if (!LogManager.ShouldLog("CustomEnemySounds", Logging.LogLevel.Info)) return;
            UnityEngine.Debug.Log("[CustomEnemySounds] " + msg);
        }

        public static void Debug(string msg)
        {
            if (!LogManager.ShouldLog("CustomEnemySounds", Logging.LogLevel.Debug)) return;
            UnityEngine.Debug.Log("[CustomEnemySounds:Debug] " + msg);
        }

        public static void Verbose(string msg)
        {
            if (!LogManager.ShouldLog("CustomEnemySounds", Logging.LogLevel.Verbose)) return;
            UnityEngine.Debug.Log("[CustomEnemySounds:Verbose] " + msg);
        }

        private static Logging.LogLevel ToUnified(LogLevel lv)
        {
            return lv switch
            {
                LogLevel.Error => Logging.LogLevel.Error,
                LogLevel.Info => Logging.LogLevel.Info,
                LogLevel.Debug => Logging.LogLevel.Debug,
                LogLevel.Verbose => Logging.LogLevel.Verbose,
                _ => Logging.LogLevel.Info
            };
        }
    }
}

