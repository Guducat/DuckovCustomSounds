using System;
using System.IO;
using UnityEngine;

namespace DuckovCustomSounds.CustomEnemySounds
{
    public enum LogLevel
    {
        Error = 0,
        Info = 1,
        Debug = 2,
        Verbose = 3,
    }

    internal static class CESLogger
    {
        private static volatile LogLevel _level = LogLevel.Info;
        private static volatile bool _enabled = true;

        public static LogLevel CurrentLevel => _level;
        public static bool Enabled => _enabled;

        public static void Configure(bool enabled, LogLevel level)
        {
            _enabled = enabled;
            _level = level;
        }

        /// <summary>
        /// 应用基于文件的快速开关。如果存在DuckovCustomSounds/debug_off或DuckovCustomSounds/.nolog，
        /// 强制级别至多为Info（即禁用Debug/Verbose），但保留Error/Info。
        /// </summary>
        public static void ApplyFileSwitches(string modRoot)
        {
            try
            {
                if (string.IsNullOrEmpty(modRoot)) return;
                var path1 = Path.Combine(modRoot, "debug_off");
                var path2 = Path.Combine(modRoot, ".nolog");
                if (File.Exists(path1) || File.Exists(path2))
                {
                    if (_level > LogLevel.Info) _level = LogLevel.Info;
                }
            }
            catch { }
        }

        public static void Error(string msg, Exception ex = null)
        {
            if (!_enabled) return;
            UnityEngine.Debug.LogError("[CustomEnemySounds] " + msg + (ex != null ? ("\n" + ex) : string.Empty));
        }

        public static void Info(string msg)
        {
            if (!_enabled || _level < LogLevel.Info) return;
            UnityEngine.Debug.Log("[CustomEnemySounds] " + msg);
        }

        public static void Debug(string msg)
        {
            if (!_enabled || _level < LogLevel.Debug) return;
            UnityEngine.Debug.Log("[CustomEnemySounds:Debug] " + msg);
        }

        public static void Verbose(string msg)
        {
            if (!_enabled || _level < LogLevel.Verbose) return;
            UnityEngine.Debug.Log("[CustomEnemySounds:Verbose] " + msg);
        }
    }
}

