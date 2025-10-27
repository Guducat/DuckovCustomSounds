using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMOD;

namespace DuckovCustomSounds.CustomFootStepSounds
{
    /// <summary>
    /// 跟踪脚步/冲刺等循环或短促 SFX 的生命周期，独立于语音的 CoreSoundTracker，避免互相中断。
    /// </summary>
    internal static class FootstepSoundTracker
    {
        private class Entry
        {
            public int OwnerId;
            public string SoundKey;
            public Sound Sound;
            public Channel Channel;
            public string Path;
            public Transform Emitter;
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

        public static void Track(int ownerId, Sound sound, Channel channel, string path, string soundKey, Transform emitter)
        {
            if (!_running) EnsureStarted();
            // 对于脚步声：同一 owner 仅保留一个条目（新替旧），避免叠加
            if (_byOwner.TryGetValue(ownerId, out var old))
            {
                try { old.Channel.stop(); } catch { }
                try { old.Sound.release(); } catch { }
                _byOwner.Remove(ownerId);
            }
            _byOwner[ownerId] = new Entry
            {
                OwnerId = ownerId,
                Sound = sound,
                Channel = channel,
                Path = path,
                SoundKey = soundKey,
                Emitter = emitter,
            };
        }

        public static void StopByOwner(int ownerId)
        {
            try
            {
                if (_byOwner.TryGetValue(ownerId, out var e))
                {
                    try { e.Channel.stop(); } catch { }
                    try { e.Sound.release(); } catch { }
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
                    try { e.Channel.stop(); } catch { }
                    try { e.Sound.release(); } catch { }
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
                    bool playing = false;
                    RESULT r = RESULT.OK;
                    try { r = e.Channel.isPlaying(out playing); } catch { playing = false; r = RESULT.ERR_INVALID_HANDLE; }
                    if (r != RESULT.OK || !playing)
                    {
                        try { e.Channel.stop(); } catch { }
                        try { e.Sound.release(); } catch { }
                        _byOwner.Remove(key);
                    }
                    else if (e.Emitter != null)
                    {
                        try
                        {
                            Vector3 pos = Vector3.zero;
                            try { pos = e.Emitter.position; } catch { }
                            var fpos = new FMOD.VECTOR { x = pos.x, y = pos.y, z = pos.z };
                            var fvel = new FMOD.VECTOR { x = 0f, y = 0f, z = 0f };
                            try { e.Channel.set3DAttributes(ref fpos, ref fvel); } catch { }
                        }
                        catch { }
                    }
                }
                yield return wait;
            }
        }
    }
}

