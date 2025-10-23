using System;
using UnityEngine;
using DuckovCustomSounds.CustomEnemySounds; // reuse CESLogger gating

namespace DuckovCustomSounds.CustomBGM
{
    internal static class BGMLogger
    {
        public static void Info(string msg)
        {
            if (!CESLogger.Enabled || CESLogger.CurrentLevel < LogLevel.Info) return;
            UnityEngine.Debug.Log("[CustomSounds:BGM] " + msg);
        }

        public static void Debug(string msg)
        {
            if (!CESLogger.Enabled || CESLogger.CurrentLevel < LogLevel.Debug) return;
            UnityEngine.Debug.Log("[CustomSounds:BGM:Debug] " + msg);
        }

        public static void Warn(string msg)
        {
            UnityEngine.Debug.LogWarning("[CustomSounds:BGM] " + msg);
        }

        public static void Error(string msg, Exception ex = null)
        {
            UnityEngine.Debug.LogError("[CustomSounds:BGM] " + msg + (ex != null ? ("\n" + ex) : string.Empty));
        }
    }
}

