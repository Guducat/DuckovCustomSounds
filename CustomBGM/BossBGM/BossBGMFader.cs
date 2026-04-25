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
        private static GameObject _host;
        private static BossBGMFaderHost _runner;

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
            _runner.StartFade(instance, bossName, Mathf.Max(0.01f, seconds));
            BossBGMLogger.Info($"[BossBGM] 死亡淡出启动: {bossName}, {seconds:F1}s");
        }

        public static void ForceStopAll(string reason)
        {
            if (_runner == null) return;
            _runner.ForceStopAll(reason);
        }
    }

    internal class BossBGMFaderHost : MonoBehaviour
    {
        private class Entry
        {
            public FMOD.Studio.EventInstance Inst;
            public string Name;
            public float TimeLeft;
            public bool Stopped;
        }

        private readonly List<Entry> _entries = new List<Entry>();

        public void StartFade(FMOD.Studio.EventInstance instance, string name, float seconds)
        {
            // 若实例无效，直接忽略
            bool valid = instance.isValid();
            if (!valid)
            {
                BossBGMLogger.Debug($"[BossBGM] 死亡淡出跳过（实例无效）: {name}");
                return;
            }
            _entries.Add(new Entry { Inst = instance, Name = name, TimeLeft = seconds, Stopped = false });
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

        private void Update()
        {
            if (_entries.Count == 0) return;

            float dt = Time.unscaledDeltaTime; // 使用不受时间缩放影响的淡出
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var e = _entries[i];
                if (!e.Inst.isValid())
                {
                    _entries.RemoveAt(i);
                    continue;
                }

                if (!e.Stopped)
                {
                    // 按线性比例降低音量
                    float step = (e.TimeLeft > 0.0001f) ? dt / e.TimeLeft : 1f;
                    float actual, final;
                    try
                    {
                        e.Inst.getVolume(out actual, out final);
                        float newVol = Mathf.Clamp01(actual - step);
                        e.Inst.setVolume(newVol);
                    }
                    catch { }

                    e.TimeLeft -= dt;
                    if (e.TimeLeft <= 0f)
                    {
                        try
                        {
                            e.Inst.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                            e.Inst.release();
                            e.Stopped = true;
                            BossBGMLogger.Debug($"[BossBGM] 死亡淡出完成并释放: {e.Name}");
                        }
                        catch { }
                        _entries.RemoveAt(i);
                    }
                }
            }
        }
    }
}

