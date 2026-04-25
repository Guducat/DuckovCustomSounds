using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMOD;

namespace DuckovCustomSounds.CustomFootStepSounds
{
    /// <summary>
    /// 跟踪脚步/冲刺等循环或短促 SFX 的生命周期，独立于语音的 CoreSoundTracker，避免互相中断。
    /// 使用新接口 PostCustomSound 替代 FMOD Core API。
    /// </summary>
    internal static class FootstepSoundTracker
    {
        private class Entry
        {
            public int OwnerId;                      // GameObject InstanceID
            public string SoundKey;                  // 声音键
            public FMOD.Studio.EventInstance EventInstance; // 新接口返回的 EventInstance
            public string Path;                      // 文件路径
        }

        private static readonly Dictionary<int, Entry> _byOwner = new Dictionary<int, Entry>();
        private static bool _running;
        private static Coroutine _routine;

        public static void EnsureStarted()
        {
            if (_running) return;
            if (ModBehaviour.Instance == null) return;
            _running = true;
            _routine = ModBehaviour.Instance.StartCoroutine(Run());
            FootstepLogger.Debug("[CFS:Core] FootstepSoundTracker 启动");
        }

        public static void Track(int ownerId, FMOD.Studio.EventInstance eventInstance, string path, string soundKey)
        {
            if (!_running) EnsureStarted();
            // 对于脚步声：同一 owner 仅保留一个条目（新替旧），避免叠加
            if (_byOwner.TryGetValue(ownerId, out var old))
            {
                try { old.EventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); } catch { }
                try { old.EventInstance.release(); } catch { }
                _byOwner.Remove(ownerId);
            }
            _byOwner[ownerId] = new Entry
            {
                OwnerId = ownerId,
                EventInstance = eventInstance,
                Path = path,
                SoundKey = soundKey,
            };
        }

        public static void StopByOwner(int ownerId)
        {
            try
            {
                if (_byOwner.TryGetValue(ownerId, out var e))
                {
                    try { e.EventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); } catch { }
                    try { e.EventInstance.release(); } catch { }
                    _byOwner.Remove(ownerId);
                }
            }
            catch { }
        }

        public static void StopAndClear()
        {
            _running = false;
            try
            {
                foreach (var kv in _byOwner)
                {
                    var e = kv.Value;
                    try { e.EventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); } catch { }
                    try { e.EventInstance.release(); } catch { }
                }
                _byOwner.Clear();
            }
            catch { }
            FootstepLogger.Debug("[CFS:Core] FootstepSoundTracker 停止");
        }

        private static IEnumerator Run()
        {
            var wait = new WaitForSeconds(0.05f);
            while (_running)
            {
                var keys = new List<int>(_byOwner.Keys);
                for (int i = keys.Count - 1; i >= 0; i--)
                {
                    int key = keys[i];
                    if (!_byOwner.TryGetValue(key, out var e)) continue;

                    // 使用 EventInstance 的 isValid() 和 isPlaying() 查询
                    bool valid = false;
                    bool playing = false;
                    try
                    {
                        valid = e.EventInstance.isValid();
                        if (valid)
                        {
                            FMOD.Studio.PLAYBACK_STATE state;
                            if (e.EventInstance.getPlaybackState(out state) == RESULT.OK)
                            {
                                playing = (state == FMOD.Studio.PLAYBACK_STATE.PLAYING ||
                                          state == FMOD.Studio.PLAYBACK_STATE.STARTING);
                            }
                        }
                    }
                    catch
                    {
                        valid = false;
                        playing = false;
                    }

                    // 如果 EventInstance 无效或已停止播放，则清理
                    if (!valid || !playing)
                    {
                        try { e.EventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); } catch { }
                        try { e.EventInstance.release(); } catch { }
                        _byOwner.Remove(key);
                    }
                    // 注意：新接口自动跟随 GameObject，无需手动更新 3D 属性
                }
                yield return wait;
            }
        }
    }
}

