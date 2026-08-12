using System.Collections.Generic;
using UnityEngine;

namespace DuckovCustomSounds.CustomBGM.BossBGM
{
    /// <summary>
    /// BOSS 死亡淡出宿主：在控制器销毁后继续以指定时长将音量从当前值平滑下降至 0，随后停止并释放实例。
    /// - 通过静态入口 FadeOutAndRelease 触发
    /// - 通过 ForceStopAll 在场景切换时强制立即清理
    /// </summary>
    internal static class BossBGMFader
    {
        private static GameObject? _host;
        private static BossBGMFaderHost? _runner;

        private static void EnsureHost()
        {
            if (_runner != null) return;
            _host = new GameObject("BossBGM_FaderHost");
            Object.DontDestroyOnLoad(_host);
            _runner = _host.AddComponent<BossBGMFaderHost>();
        }

        public static void FadeOutAndRelease(FMOD.Studio.EventInstance instance, string bossName, float seconds)
        {
            EnsureHost();
            if (_runner == null) return;
            _runner.StartFade(instance, null, bossName, Mathf.Max(0.01f, seconds));
            BossBGMLogger.Info($"[BossBGM] 死亡淡出启动: {bossName}, {seconds:F1}s");
        }

        public static void FadeOutAndRelease(
            FMOD.Studio.EventInstance instance,
            BossBGMController owner,
            string bossName,
            float seconds)
        {
            EnsureHost();
            if (_runner == null) return;
            _runner.StartFade(instance, owner, bossName, Mathf.Max(0.01f, seconds));
            BossBGMLogger.Info($"[BossBGM] 淡出启动: {bossName}, {seconds:F1}s");
        }

        public static void ForceStopAll(string reason)
        {
            if (_runner == null) return;
            _runner.ForceStopAll(reason);
        }

        public static void StopPending(BossBGMController owner, string reason)
        {
            if (_runner == null) return;
            _runner.StopOwner(owner, reason);
        }
    }

    internal class BossBGMFaderHost : MonoBehaviour
    {
        private class Entry
        {
            public FMOD.Studio.EventInstance Inst;
            public BossBGMController? Owner;
            public string Name = string.Empty;
            public float StartVolume;
            public float Duration;
            public float Elapsed;
        }

        private readonly List<Entry> _entries = new List<Entry>();

        public void StartFade(FMOD.Studio.EventInstance instance, string name, float seconds)
        {
            StartFade(instance, null, name, seconds);
        }

        public void StartFade(
            FMOD.Studio.EventInstance instance,
            BossBGMController? owner,
            string name,
            float seconds)
        {
            // 若实例无效，直接忽略
            bool valid = instance.isValid();
            if (!valid)
            {
                BossBGMLogger.Debug($"[BossBGM] 死亡淡出跳过（实例无效）: {name}");
                return;
            }

            float startVolume = 1f;
            try { instance.getVolume(out startVolume, out _); } catch { }

            _entries.Add(new Entry
            {
                Inst = instance,
                Owner = owner,
                Name = name,
                StartVolume = Mathf.Clamp01(startVolume),
                Duration = Mathf.Max(0.01f, seconds),
                Elapsed = 0f
            });
        }

        public void ForceStopAll(string reason)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                try
                {
                    if (e.Inst.isValid())
                    {
                        e.Inst.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                        e.Inst.release();
                    }
                }
                catch { }
            }
            _entries.Clear();
            BossBGMLogger.Debug($"[BossBGM] 死亡淡出被强制清理：{reason}");
        }

        public void StopOwner(BossBGMController owner, string reason)
        {
            int stoppedCount = 0;
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var entry = _entries[i];
                if (!ReferenceEquals(entry.Owner, owner))
                    continue;

                try
                {
                    if (entry.Inst.isValid())
                    {
                        entry.Inst.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                        entry.Inst.release();
                    }
                }
                catch { }

                _entries.RemoveAt(i);
                stoppedCount++;
            }

            if (stoppedCount > 0)
            {
                BossBGMLogger.Debug(
                    $"[BossBGM] 已清理 Controller 的待淡出实例: {stoppedCount}, 原因: {reason}");
            }
        }

        private void Update()
        {
            AdvanceFades(Time.unscaledDeltaTime);
        }

        internal void AdvanceFades(float deltaTime)
        {
            if (_entries.Count == 0)
                return;

            float dt = Mathf.Max(0f, deltaTime);
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var e = _entries[i];
                if (!e.Inst.isValid())
                {
                    _entries.RemoveAt(i);
                    continue;
                }

                e.Elapsed += dt;
                float progress = Mathf.Clamp01(e.Elapsed / e.Duration);
                try
                {
                    e.Inst.setVolume(e.StartVolume * (1f - progress));
                }
                catch { }

                if (progress >= 1f)
                {
                    try
                    {
                        e.Inst.setVolume(0f);
                        e.Inst.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                        e.Inst.release();
                        BossBGMLogger.Debug($"[BossBGM] 淡出完成并释放: {e.Name}");
                    }
                    catch { }
                    _entries.RemoveAt(i);
                }
            }
        }
    }
}
