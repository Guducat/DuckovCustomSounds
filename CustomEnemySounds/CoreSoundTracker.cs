using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMOD;

namespace DuckovCustomSounds.CustomEnemySounds
{
    /// <summary>
    /// 负责管理通过 FMOD Core API 播放的自定义短音频（SFX）生命周期：
    /// - 不在 Postfix 中立即 release()
    /// - 追踪 Channel，当播放结束时再回收 Sound/Channel
    /// - 提供 EnsureStarted()/StopAndClear() 由模块装载/卸载时调用
    /// </summary>
    internal static class CoreSoundTracker
    {
        private class Entry
        {
            public int OwnerId;          // GameObject InstanceID
            public string SoundKey;      // 音效键
            public int Priority;         // 优先级
            public Sound Sound;
            public Channel Channel;
            public string Path;
            public float AddedAt;
        }

        // 以敌人 GameObject.InstanceID 为键，确保同一敌人同一时刻仅有一条语音在播
        private static readonly Dictionary<int, Entry> _byOwner = new Dictionary<int, Entry>();
        private static bool _running;
        private static Coroutine _routine;

        public static void EnsureStarted()
        {
            if (_running) return;
            if (ModBehaviour.Instance == null) return;
            _running = true;
            _routine = ModBehaviour.Instance.StartCoroutine(Run());
            CESLogger.Debug("[CES:Core] CoreSoundTracker 已启动");
        }

        /// <summary>
        /// 为指定 ownerId 添加音效到 Track
        /// </summary>
        public static void Track(int ownerId, Sound sound, Channel channel, string path, string soundKey, int priority)
        {
            if (!_running) EnsureStarted();
            // 如果已存在旧音效，先停止并释放
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
            };
        }

        /// <summary>
        /// 优先级检查和可能的打断
        /// 返回 true = 允许播放; false = 阻止播放
        /// </summary>
        public static bool PreCheckAndMaybeInterrupt(int ownerId, string newSoundKey, int newPriority)
        {
            if (!(CustomEnemySounds.Config?.PriorityInterruptEnabled ?? true)) return true; // 未启用优先级打断
            if (_byOwner.TryGetValue(ownerId, out var cur))
            {
                CESLogger.Debug($"[CES:Priority] 当前播放: soundKey={cur.SoundKey}, priority={cur.Priority}");
                if (newPriority > cur.Priority)
                {
                    CESLogger.Info($"[CES:Priority] 优先级打断: {cur.SoundKey}({cur.Priority}) -> {newSoundKey}({newPriority})");
                    try { cur.Channel.stop(); } catch { }
                    try { cur.Sound.release(); } catch { }
                    _byOwner.Remove(ownerId);
                    return true;
                }
                else
                {
                    CESLogger.Debug($"[CES:Priority] 优先级不足: {newSoundKey}({newPriority}) <= {cur.SoundKey}({cur.Priority})");
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
            CESLogger.Debug("[CES:Core] CoreSoundTracker 已清空");
        }

        private static IEnumerator Run()
        {
            var wait = new WaitForSeconds(0.05f);
            while (_running)
            {
                // 复制 keys 集合以避免修改时异常
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
                        CESLogger.Debug($"[CES:Core] 回收自定义声音 -> {e.Path}");
                        _byOwner.Remove(key);
                    }
                }
                yield return wait;
            }
        }
    }
}

