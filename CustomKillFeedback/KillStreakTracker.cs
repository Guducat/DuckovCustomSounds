using System;
using UnityEngine;

namespace DuckovCustomSounds.CustomKillFeedback
{
    /// <summary>
    /// 连杀追踪：基于时间窗口统计连续击杀
    /// </summary>
    internal static class KillStreakTracker
    {
        private static bool _initialized;
        private static int _streak;
        private static float _lastKillRealtime;

        public static event Action<int, bool> OnStreak; // (streakCount, isHeadshot)

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            Reset();
        }

        public static void Deinitialize()
        {
            _initialized = false;
            _streak = 0;
            _lastKillRealtime = 0f;
            OnStreak = null;
        }

        public static void Reset()
        {
            _streak = 0;
            _lastKillRealtime = 0f;
        }

        public static int RegisterKill(bool isHeadshot)
        {
            float now = Time.realtimeSinceStartup;
            if (_streak <= 0 || now - _lastKillRealtime > Mathf.Max(0.1f, KillFeedbackConfig.ComboWindowSeconds))
                _streak = 1;
            else
                _streak++;

            _lastKillRealtime = now;

            try { OnStreak?.Invoke(_streak, isHeadshot); } catch { }
            return _streak;
        }

        public static int CurrentStreak => _streak;
    }
}

