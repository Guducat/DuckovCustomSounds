using System;
using DuckovCustomSounds.Logging;

namespace DuckovCustomSounds.CustomHitAndKillSounds
{
    internal static class HitAndKillLogger
    {
        private static readonly ILog Logger = LogManager.GetLogger("HitAndKill");

        public static bool IsDebugEnabled => LogManager.ShouldLog("HitAndKill", LogLevel.Debug);
        public static bool IsVerboseEnabled => LogManager.ShouldLog("HitAndKill", LogLevel.Verbose);

        public static ILog ForScope(params string[] scopes) => Logger.ForScope(scopes);
        public static void Error(string msg, Exception? ex = null) => Logger.Error(msg, ex);
        public static void Warning(string msg) => Logger.Warning(msg);
        public static void Info(string msg) => Logger.Info(msg);
        public static void Debug(string msg) => Logger.Debug(msg);
        public static void Verbose(string msg) => Logger.Verbose(msg);
    }
}
