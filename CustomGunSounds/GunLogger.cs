using System;
using UnityEngine;
using DuckovCustomSounds.Logging;

namespace DuckovCustomSounds.CustomGunSounds
{
    internal static class GunLogger
    {
        private const string ModuleName = "CustomGunSounds";

        public static void Info(string msg)
        {
            if (!LogManager.ShouldLog(ModuleName, LogLevel.Info)) return;
            UnityEngine.Debug.Log("[CustomSounds:Gun] " + msg);
        }

        public static void Debug(string msg)
        {
            if (!LogManager.ShouldLog(ModuleName, LogLevel.Debug)) return;
            UnityEngine.Debug.Log("[CustomSounds:Gun:Debug] " + msg);
        }

        public static void Warn(string msg)
        {
            if (!LogManager.ShouldLog(ModuleName, LogLevel.Warning)) return;
            UnityEngine.Debug.LogWarning("[CustomSounds:Gun] " + msg);
        }

        public static void Error(string msg, Exception? ex = null)
        {
            if (!LogManager.ShouldLog(ModuleName, LogLevel.Error)) return;
            UnityEngine.Debug.LogError("[CustomSounds:Gun] " + msg + (ex != null ? ("\n" + ex) : string.Empty));
        }
    }
}

