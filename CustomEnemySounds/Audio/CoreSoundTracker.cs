using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMOD;
using DuckovCustomSounds.CustomEnemySounds.Filters;

namespace DuckovCustomSounds.CustomEnemySounds.Audio
{
    /// <summary>
    /// 追踪通过新接口 PostCustomSound 播放的自定义语音 SFX 的生命周期：
    /// - 使用 EventInstance 替代 Sound/Channel
    /// - 自动跟随 GameObject（新接口自动处理）
    /// - 自动资源清理（新接口自动处理）
    /// - 保留优先级中断逻辑
    /// </summary>
    internal static class CoreSoundTracker
    {
        private class Entry
        {
            public int OwnerId;                      // GameObject InstanceID
            public string SoundKey;                  // 声音键
            public int Priority;                     // 优先级
            public FMOD.Studio.EventInstance EventInstance; // 新接口返回的 EventInstance
            public string Path;                      // 文件路径
            public float AddedAt;                    // 添加时间
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
        public static void Track(int ownerId, FMOD.Studio.EventInstance eventInstance, string path, string soundKey, int priority)
        {
            if (!_running) EnsureStarted();
            // 若已存在条目，先停止并释放
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
                AddedAt = Time.realtimeSinceStartup,
                SoundKey = soundKey,
                Priority = priority,
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
                    try { cur.EventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); } catch { }
                    try { cur.EventInstance.release(); } catch { }
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

        /// <summary>
        /// 按所有者 InstanceID 停止当前正在跟踪的自定义声音（若有）。
        /// </summary>
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
                        CESLogger.Debug($"[CES:Core] 结束自定义语音 -> {e.Path}");
                        _byOwner.Remove(key);
                    }
                    // 注意：新接口自动跟随 GameObject，无需手动更新 3D 属性
                }
                yield return wait;
            }
        }
    }
}
