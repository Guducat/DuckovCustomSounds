using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMOD;

namespace DuckovCustomSounds.CustomEnemySounds
{
    /// <summary>
    /// 追踪通过 FMOD Core API 播放的自定义语音SFX的生命周期：
    /// - 避免在 Postfix 里立即 release()
    /// - 跟踪 Channel，在结束时回收 Sound/Channel
    /// - EnsureStarted()/StopAndClear() 由模块装载/卸载时调用
    /// - 新增：在播放期间持续更新 3D 属性以跟随发声体
    /// </summary>
    internal static class CoreSoundTracker
    {
        private class Entry
        {
            public int OwnerId;          // GameObject InstanceID
            public string SoundKey;      // 声音键
            public int Priority;         // 优先级
            public Sound Sound;
            public Channel Channel;
            public string Path;
            public Transform Emitter;    // 跟随的发声体
            public float AddedAt;
        }

        // 以发声体 GameObject.InstanceID 为键，确保同一对象同一时间只有一个条目
        private static readonly Dictionary<int, Entry> _byOwner = new Dictionary<int, Entry>();
        private static bool _running;
        private static Coroutine _routine;

        public static void EnsureStarted()
        {
            if (_running) return;
            if (ModBehaviour.Instance == null) return;
            _running = true;
            _routine = ModBehaviour.Instance.StartCoroutine(Run());
            CESLogger.Debug("[CES:Core] CoreSoundTracker 启动");
        }

        /// <summary>
        /// 为指定 ownerId 的语音进行 Track
        /// </summary>
        public static void Track(int ownerId, Sound sound, Channel channel, string path, string soundKey, int priority, Transform emitter)
        {
            if (!_running) EnsureStarted();
            // 若已存在条目，先停止并释放
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
                AddedAt = Time.realtimeSinceStartup,
                SoundKey = soundKey,
                Priority = priority,
                Emitter = emitter,
            };
        }

        /// <summary>
        /// 优先级中断判断
        /// 返回 true = 允许新声音； false = 拒绝新声音
        /// </summary>
        public static bool PreCheckAndMaybeInterrupt(int ownerId, string newSoundKey, int newPriority)
        {
            if (!(CustomEnemySounds.Config?.PriorityInterruptEnabled ?? true)) return true; // 未开启优先级中断
            if (_byOwner.TryGetValue(ownerId, out var cur))
            {
                CESLogger.Debug($"[CES:Priority] 当前={cur.SoundKey}({cur.Priority})");
                if (newPriority > cur.Priority)
                {
                    CESLogger.Info($"[CES:Priority] 中断: {cur.SoundKey}({cur.Priority}) -> {newSoundKey}({newPriority})");
                    try { cur.Channel.stop(); } catch { }
                    try { cur.Sound.release(); } catch { }
                    _byOwner.Remove(ownerId);
                    return true;
                }
                else
                {
                    CESLogger.Debug($"[CES:Priority] 保留: {newSoundKey}({newPriority}) <= {cur.SoundKey}({cur.Priority})");
                    return false;
                }
            }
            return true;
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
            CESLogger.Debug("[CES:Core] CoreSoundTracker 停止");
        }

        private static IEnumerator Run()
        {
            var wait = new WaitForSeconds(0.05f);
            while (_running)
            {
                // 拷贝 keys，避免遍历时删除引发异常
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
                        CESLogger.Debug($"[CES:Core] 结束自定义语音 -> {e.Path}");
                        _byOwner.Remove(key);
                    }
                    else if (e.Emitter != null)
                    {
                        // 播放中则尝试更新3D属性以跟随发声体
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
