using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Duckov;
using FMODUnity;

namespace DuckovCustomSounds.CustomGunSounds
{
    /// <summary>
    /// 枪械音效工具类 - 使用新接口简化版本
    /// </summary>
    internal static class GunUtil
    {
        private static string BaseDir => Path.Combine(ModBehaviour.ModFolderName, "CustomGunSounds");

        // --- 射击速率限制（保留） ---
        private static readonly Dictionary<int, float> s_LastShootAt = new Dictionary<int, float>(128);
        private static readonly object s_RateLock = new object();

        private static bool ShouldThrottleShoot(GameObject go, string typeIdStr, string soundKey)
        {
            try
            {
                bool enabled = DuckovCustomSounds.ModSettings.GunShootRateLimitEnabled;
                if (!enabled) return false;

                float minMs = DuckovCustomSounds.ModSettings.GunShootMinIntervalMs;
                string src = "global";
                try
                {
                    if (!string.IsNullOrWhiteSpace(typeIdStr))
                    {
                        var map = DuckovCustomSounds.ModSettings.GunShootRateLimitPerType;
                        if (map != null && map.TryGetValue(typeIdStr, out var perMs))
                        {
                            if (perMs <= 0f) return false;
                            minMs = perMs;
                            src = $"type:{typeIdStr}";
                        }
                    }
                }
                catch { }

                float minSec = Mathf.Max(0f, minMs) / 1000f;
                if (minSec <= 0f) return false;

                int key = 0;
                try { key = go != null ? go.GetInstanceID() : 0; } catch { }
                float now = Time.realtimeSinceStartup;
                lock (s_RateLock)
                {
                    if (s_LastShootAt.TryGetValue(key, out var last) && (now - last) < minSec)
                    {
                        // 性能优化：仅当Debug日志启用时才计算和格式化字符串
                        if (GunLogger.IsDebugEnabled)
                        {
                            float deltaMs = (now - last) * 1000f;
                            GunLogger.Debug($"[GunShoot:RateLimit] 抑制: typeId={typeIdStr}, key={soundKey}, Δ={deltaMs:F0}ms < {minMs}ms, src={src}");
                        }
                        return true;
                    }
                    s_LastShootAt[key] = now;
                    return false;
                }
            }
            catch { return false; }
        }

        // --- 换弹音效追踪（保留，用于取消换弹） ---
        private struct TrackedReload
        {
            public FMOD.Studio.EventInstance? EventInstance;
            public float StartTime;
            public bool IsEndSound;
        }

        private static readonly Dictionary<int, List<TrackedReload>> s_ReloadPlaying = new Dictionary<int, List<TrackedReload>>();
        private static readonly object s_ReloadLock = new object();

        private static int GetOwnerId(ItemAgent_Gun gun)
        {
            try { return gun != null ? gun.GetInstanceID() : 0; } catch { return 0; }
        }

        internal static void TrackReloadSound(ItemAgent_Gun gun, FMOD.Studio.EventInstance? eventInstance, bool isEndSound = false)
        {
            int id = GetOwnerId(gun);
            if (id == 0) return;
            lock (s_ReloadLock)
            {
                if (!s_ReloadPlaying.TryGetValue(id, out var list))
                {
                    list = new List<TrackedReload>();
                    s_ReloadPlaying[id] = list;
                }
                list.Add(new TrackedReload
                {
                    EventInstance = eventInstance,
                    StartTime = Time.realtimeSinceStartup,
                    IsEndSound = isEndSound
                });
            }
        }

        public static void StopCustomReloadFor(ItemAgent_Gun gun)
        {
            int id = GetOwnerId(gun);
            if (id == 0) return;

            List<TrackedReload> toStop = null;
            lock (s_ReloadLock)
            {
                if (s_ReloadPlaying.TryGetValue(id, out var list) && list != null && list.Count > 0)
                {
                    // 只停止非结束音效（保留 *_reload_end）
                    toStop = list.Where(t => !t.IsEndSound).ToList();

                    if (toStop.Count == 0)
                        return;

                    // 从列表移除要停止的条目
                    foreach (var t in toStop)
                    {
                        list.Remove(t);
                    }

                    // 若列表已空，清理字典项
                    if (list.Count == 0)
                        s_ReloadPlaying.Remove(id);
                }
            }

            if (toStop == null) return;
            foreach (var t in toStop)
            {
                try
                {
                    if (t.EventInstance.HasValue && t.EventInstance.Value.isValid())
                    {
                        t.EventInstance.Value.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                    }
                }
                catch { }
            }
            GunLogger.Debug("[GunReload] 因取消/停止换弹而停止自定义换弹音效（保留结束音效）");
        }

        private const int MaxShootInstancesPerKey = 10;

        private struct ActiveShootInstance
        {
            public FMOD.Studio.EventInstance Instance;
            public float AddedAt;
        }

        private static readonly Dictionary<string, LinkedList<ActiveShootInstance>> s_ActiveShootInstances =
            new Dictionary<string, LinkedList<ActiveShootInstance>>(StringComparer.OrdinalIgnoreCase);

        private static readonly object s_ShootInstanceLock = new object();

        private static void PruneInactiveShots(LinkedList<ActiveShootInstance> list)
        {
            if (list == null || list.Count == 0) return;
            var node = list.First;
            while (node != null)
            {
                var next = node.Next;
                if (!IsInstanceActive(node.Value.Instance))
                {
                    list.Remove(node);
                }
                node = next;
            }
        }

        private static bool IsInstanceActive(FMOD.Studio.EventInstance instance)
        {
            try
            {
                bool valid = instance.isValid();
                if (!valid) return false;
            }
            catch { return false; }

            try
            {
                if (instance.getPlaybackState(out var state) == FMOD.RESULT.OK)
                {
                    return state == FMOD.Studio.PLAYBACK_STATE.PLAYING ||
                           state == FMOD.Studio.PLAYBACK_STATE.SUSTAINING ||
                           state == FMOD.Studio.PLAYBACK_STATE.STARTING;
                }
            }
            catch { }

            return true;
        }

        private static void TryStopInstance(FMOD.Studio.EventInstance instance, string soundKey)
        {
            try
            {
                if (!instance.isValid()) return;
            }
            catch { return; }

            try
            {
                instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                GunLogger.Debug($"[GunShoot:Limit] Forcing stop for key={soundKey}, enforcing max {MaxShootInstancesPerKey} instances.");
            }
            catch { }
        }

        private static void RegisterActiveShoot(string soundKey, FMOD.Studio.EventInstance? instance)
        {
            if (string.IsNullOrWhiteSpace(soundKey) || !instance.HasValue) return;
            var ev = instance.Value;
            try { if (!ev.isValid()) return; } catch { return; }

            lock (s_ShootInstanceLock)
            {
                if (!s_ActiveShootInstances.TryGetValue(soundKey, out var list))
                {
                    list = new LinkedList<ActiveShootInstance>();
                    s_ActiveShootInstances[soundKey] = list;
                }

                PruneInactiveShots(list);
                list.AddLast(new ActiveShootInstance
                {
                    Instance = ev,
                    AddedAt = Time.realtimeSinceStartup
                });

                while (list.Count > MaxShootInstancesPerKey)
                {
                    var node = list.First;
                    if (node == null)
                        break;
                    list.RemoveFirst();
                    TryStopInstance(node.Value.Instance, soundKey);
                }

                if (list.Count == 0)
                {
                    s_ActiveShootInstances.Remove(soundKey);
                }
            }
        }

        // --- �ļ����Ҹ��� ---
        private static readonly string[] Exts = new[] { ".mp3", ".wav", ".ogg", ".oga" };

        private static IEnumerable<string> ExpandCandidates(params string[] namesNoExt)
        {
            foreach (var name in namesNoExt)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                foreach (var ext in Exts)
                {
                    yield return Path.Combine(BaseDir, name + ext);
                }
            }
        }

        private static void SuppressEventInstance(FMOD.Studio.EventInstance? instance, bool stop)
        {
            try
            {
                if (!instance.HasValue) return;
                var ev = instance.Value;
                bool valid = false;
                try { valid = ev.isValid(); }
                catch { valid = false; }
                if (!valid) return;

                try { ev.setParameterByName("Mute", 1f); } catch { }
                try { ev.setVolume(0f); } catch { }
                if (stop)
                {
                    try { ev.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); } catch { }
                }
            }
            catch { }
        }

        // --- 获取枪械信息 ---
        private static (ItemAgent_Gun gun, string typeIdStr) GetGunAndTypeId(GameObject gameObject)
        {
            ItemAgent_Gun gun = null;
            try
            {
                if (gameObject != null)
                {
                    gun = gameObject.GetComponent<ItemAgent_Gun>() ??
                          gameObject.GetComponentInParent<ItemAgent_Gun>();
                }
            }
            catch { }

            string typeIdStr = null;
            try
            {
                if (gun != null)
                {
                    try
                    {
                        var item = gun.Item;
                        if (item != null)
                        {
                            var typeId = item.TypeID;
                            if (typeId != null)
                                typeIdStr = typeId.ToString();
                        }
                    }
                    catch { }

                    if (string.IsNullOrWhiteSpace(typeIdStr))
                    {
                        try
                        {
                            var typeIdProp = gun.GetType().GetProperty("TypeID",
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);

                            if (typeIdProp != null)
                            {
                                var typeId = typeIdProp.GetValue(gun);
                                if (typeId != null)
                                    typeIdStr = typeId.ToString();
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }

            if (string.IsNullOrWhiteSpace(typeIdStr)) typeIdStr = null;

            return (gun, typeIdStr);
        }

        // --- 射击音效覆盖（Postfix 模式，使用新接口） ---
        public static void PostfixOverrideShoot_Safe(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
        {
            const string prefix = "SFX/Combat/Gun/Shoot/";
            try
            {
                if (string.IsNullOrEmpty(eventName) || !eventName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return;

                string soundKey = eventName.Substring(prefix.Length);
                if (string.IsNullOrWhiteSpace(soundKey)) return;

                var (gun, typeIdStr) = GetGunAndTypeId(gameObject);
                string limitKey = !string.IsNullOrWhiteSpace(typeIdStr) ? typeIdStr : soundKey;
                bool silenced = false;
                try { if (gun != null) silenced = gun.Silenced; } catch { }

                // 速率限制检查
                if (ShouldThrottleShoot(gameObject, typeIdStr, soundKey))
                {
                    // 静音原版事件
                    SuppressEventInstance(__result, stop: false);
                    return;
                }

                if (soundKey.EndsWith("_mute", StringComparison.OrdinalIgnoreCase)) silenced = true;

                // 规范化 soundKey（去掉结尾的 _mute，便于组合）
                string baseSoundKey = soundKey;
                if (baseSoundKey.EndsWith("_mute", StringComparison.OrdinalIgnoreCase))
                {
                    baseSoundKey = baseSoundKey.Substring(0, baseSoundKey.Length - 5);
                }

                // 构建候选文件列表（按“任何消音器音效优先”的策略）
                var attempts = new List<string>();
                if (gun != null && !string.IsNullOrWhiteSpace(typeIdStr))
                {
                    if (silenced)
                    {
                        // 1) TypeID 的消音器
                        attempts.AddRange(ExpandCandidates(typeIdStr + "_mute"));
                        // 2) 枪族的消音器（如 rifle_ak_mute）
                        attempts.AddRange(ExpandCandidates(baseSoundKey + "_mute"));
                    }

                    // 3) TypeID 普通
                    attempts.AddRange(ExpandCandidates(typeIdStr));
                    // 4) 枪族普通
                    attempts.AddRange(ExpandCandidates(baseSoundKey));
                }
                else
                {
                    if (silenced)
                    {
                        attempts.AddRange(ExpandCandidates(baseSoundKey + "_mute"));
                    }
                    attempts.AddRange(ExpandCandidates(baseSoundKey));
                }

                var fallbacks = ExpandCandidates("default").ToList();

                // 查找文件（先确定基准文件，再进行差分变体选择）
                string filePath = attempts.FirstOrDefault(File.Exists) ?? fallbacks.FirstOrDefault(File.Exists);

                // 性能优化：仅当Debug日志启用时才构建字符串
                if (GunLogger.IsDebugEnabled)
                {
                    string chain = string.Join(" → ", attempts.Select(p => $"[{Path.GetFileName(p)}]").ToArray());
                    GunLogger.Debug($"[GunShoot] TypeID={typeIdStr ?? "N/A"}, soundKey={soundKey}, 查找: {chain}, 结果: {(filePath != null ? Path.GetFileName(filePath) : "未找到")}");
                }

                if (filePath == null) return; // 无自定义文件

                // 命中基准文件后，检测差分变体（xxx_1/xxx_2/...），若存在则随机选择其一
                try
                {
                    string picked = TryPickVariant(filePath);
                    if (!string.Equals(picked, filePath, StringComparison.OrdinalIgnoreCase))
                    {
                        if (GunLogger.IsDebugEnabled)
                        {
                            GunLogger.Debug($"[GunShoot] 发现变体，替换 {Path.GetFileName(filePath)} → {Path.GetFileName(picked)}");
                        }
                        filePath = picked;
                    }
                }
                catch { }

                // 使用新接口播放自定义音效（SFX总线）
                try
                {
                    // 在静音/停止原实例之前，尽可能从原事件读取3D距离参数
                    float minDistance = 0f;
                    float maxDistance = 0f;
                    bool hasDistance = false;
                    try
                    {
                        if (__result.HasValue && __result.Value.isValid())
                        {
                            hasDistance = AudioDistanceHelper.TryExtractFromEventInstance(__result.Value, out minDistance, out maxDistance);
                        }
                        if (!hasDistance)
                        {
                            hasDistance = AudioDistanceHelper.TryExtractFromEventName(eventName, out minDistance, out maxDistance);
                        }
                    }
                    catch { }

                    // 静音并停止原事件实例
                    SuppressEventInstance(__result, stop: true);

                    // 播放自定义音频
                    __result = AudioManager.PostCustomSFX(filePath, gameObject, loop: false);

                    // 应用距离衰减（若成功提取到原版的 min/max）
                    if (hasDistance && __result.HasValue)
                    {
                        try
                        {
                            var ev = __result.Value;
                            if (ev.isValid())
                            {
                                AudioDistanceHelper.ApplyToEventInstance(ev, minDistance, maxDistance);
                                if (GunLogger.IsDebugEnabled)
                                {
                                    GunLogger.Debug($"[GunShoot] 继承距离: min={minDistance:F1}m, max={maxDistance:F1}m");
                                }
                            }
                        }
                        catch { }
                    }

                    // 应用音量（ModConfig UI 可调 0.0~2.0，默认 1.0）
                    if (__result.HasValue && GunConfig.Volume >= 0f)
                    {
                        try { __result.Value.setVolume(GunConfig.Volume); } catch { }
                    }

                    RegisterActiveShoot(limitKey, __result);
                    GunLogger.Info($"[GunShoot] 使用SFX接口播放: {Path.GetFileName(filePath)}");
                }
                catch (Exception ex)
                {
                    GunLogger.Warning($"[GunShoot] 新接口播放失败: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                GunLogger.Warning($"[GunShoot] Postfix 覆盖异常: {ex.Message}");
            }
        }

        // 变体选择：若存在与 filePath 同名的 "_1"、"_2" 等后缀文件（同扩展名），随机选取其一；否则返回原路径
        private static string TryPickVariant(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return filePath;
            try
            {
                string dir = Path.GetDirectoryName(filePath);
                string ext = Path.GetExtension(filePath);
                string nameNoExt = Path.GetFileNameWithoutExtension(filePath);
                if (string.IsNullOrWhiteSpace(dir) || string.IsNullOrWhiteSpace(ext) || string.IsNullOrWhiteSpace(nameNoExt))
                    return filePath;

                // 搜索同扩展名的差分文件：name_*.ext
                string searchPattern = nameNoExt + "_*" + ext; // Directory.EnumerateFiles 支持通配符
                var candidates = new List<string>();
                foreach (var p in Directory.EnumerateFiles(dir, searchPattern))
                {
                    var file = Path.GetFileName(p);
                    if (file == null) continue;

                    // 严格规则：仅接受 "<basename>_数字"，不允许额外下划线（避免把 "*_mute_1" 作为 "*" 的变体）
                    string fileNoExt = Path.GetFileNameWithoutExtension(file);
                    int usIdx = fileNoExt.LastIndexOf('_');
                    if (usIdx <= 0) continue;
                    // 左侧必须与基准名完全一致
                    if (!string.Equals(fileNoExt.Substring(0, usIdx), nameNoExt, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var numSpan = fileNoExt.Substring(usIdx + 1);
                    if (int.TryParse(numSpan, out var n) && n >= 1)
                    {
                        candidates.Add(Path.Combine(dir, file));
                    }
                }

                if (candidates.Count == 0) return filePath; // 无变体

                // 仅在存在变体时，从变体集合中随机选取（不包含基准文件）
                int pick = UnityEngine.Random.Range(0, candidates.Count);
                return candidates[pick];
            }
            catch { return filePath; }
        }

        // --- 换弹音效覆盖（Postfix 模式，使用新接口） ---
        public static void PostfixOverrideReload_Safe(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
        {
            const string prefix = "SFX/Combat/Gun/Reload/";
            try
            {
                if (string.IsNullOrEmpty(eventName) || !eventName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return;

                string soundKey = eventName.Substring(prefix.Length);
                if (string.IsNullOrWhiteSpace(soundKey)) return;

                bool isStart = soundKey.EndsWith("_start", StringComparison.OrdinalIgnoreCase);
                bool isEnd = soundKey.EndsWith("_end", StringComparison.OrdinalIgnoreCase);

                var (gun, typeIdStr) = GetGunAndTypeId(gameObject);

                // 构建候选文件列表
                var attempts = new List<string>();
                if (gun != null && !string.IsNullOrWhiteSpace(typeIdStr))
                {
                    if (isStart)
                    {
                        attempts.AddRange(ExpandCandidates(typeIdStr + "_reload_start"));
                        attempts.AddRange(ExpandCandidates(typeIdStr + "_reload"));
                    }
                    else if (isEnd)
                    {
                        attempts.AddRange(ExpandCandidates(typeIdStr + "_reload_end"));
                    }
                    else
                    {
                        attempts.AddRange(ExpandCandidates(typeIdStr + "_reload"));
                    }
                }
                attempts.AddRange(ExpandCandidates(soundKey));

                var fallbacks = new List<string>();
                if (isStart)
                {
                    fallbacks.AddRange(ExpandCandidates("default_reload_start"));
                    fallbacks.AddRange(ExpandCandidates("default_reload"));
                }
                else if (isEnd)
                {
                    fallbacks.AddRange(ExpandCandidates("default_reload_end"));
                }
                else
                {
                    fallbacks.AddRange(ExpandCandidates("default_reload"));
                }
                fallbacks.AddRange(ExpandCandidates("default"));

                string filePath = attempts.FirstOrDefault(File.Exists) ?? fallbacks.FirstOrDefault(File.Exists);

                // 性能优化：仅当Debug日志启用时才构建字符串
                if (GunLogger.IsDebugEnabled)
                {
                    string chain = string.Join(" → ", attempts.Select(p => $"[{Path.GetFileName(p)}]").ToArray());
                    GunLogger.Debug($"[GunReload] TypeID={typeIdStr ?? "N/A"}, soundKey={soundKey}, 查找: {chain}, 结果: {(filePath != null ? Path.GetFileName(filePath) : "未找到")}");
                }

                if (filePath == null) return; // 无自定义文件

                // 命中基准文件后，检查是否存在差分变体（*_1、*_2...）
                try
                {
                    string picked = TryPickVariant(filePath);
                    if (!string.Equals(picked, filePath, StringComparison.OrdinalIgnoreCase))
                    {
                        if (GunLogger.IsDebugEnabled)
                        {
                            GunLogger.Debug($"[GunReload] 发现变体，替换 {Path.GetFileName(filePath)} → {Path.GetFileName(picked)}");
                        }
                        filePath = picked;
                    }
                }
                catch { }

                // 使用新接口播放自定义音效（SFX总线）
                try
                {
                    // 在静音/停止原实例之前，尽可能从原事件读取3D距离参数
                    float minDistance = 0f;
                    float maxDistance = 0f;
                    bool hasDistance = false;
                    try
                    {
                        if (__result.HasValue && __result.Value.isValid())
                        {
                            hasDistance = AudioDistanceHelper.TryExtractFromEventInstance(__result.Value, out minDistance, out maxDistance);
                        }
                        if (!hasDistance)
                        {
                            hasDistance = AudioDistanceHelper.TryExtractFromEventName(eventName, out minDistance, out maxDistance);
                        }
                    }
                    catch { }

                    // 停止原事件
                    SuppressEventInstance(__result, stop: true);

                    // 选择发声体：优先传入的 gameObject，其次枪械本体
                    var go = gameObject != null ? gameObject : (gun != null ? gun.gameObject : null);

                    __result = AudioManager.PostCustomSFX(filePath, go, loop: false);

                    if (__result.HasValue && __result.Value.isValid())
                    {
                        // 立即绑定一次3D位置，避免首帧未赋位导致空间定位错误
                        try
                        {
                            if (go != null)
                                __result.Value.set3DAttributes(go.transform.position.To3DAttributes());
                        }
                        catch { }

                        // 应用从原事件复制的3D最小/最大距离
                        if (hasDistance)
                        {
                            try { AudioDistanceHelper.ApplyToEventInstance(__result.Value, minDistance, maxDistance); } catch { }
                        }

                        // 应用音量（ModConfig UI 可调 0.0~2.0，默认 1.0）
                        if (GunConfig.Volume >= 0f)
                        {
                            try { __result.Value.setVolume(GunConfig.Volume); } catch { }
                        }
                    }

                    // 追踪换弹音效（用于取消换弹时停止）
                    if (gun != null)
                    {
                        TrackReloadSound(gun, __result, isEnd);
                    }

                    GunLogger.Info($"[GunReload] 使用SFX接口播放: {Path.GetFileName(filePath)}");
                }
                catch (Exception ex)
                {
                    GunLogger.Warning($"[GunReload] 新接口播放失败: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                GunLogger.Warning($"[GunReload] Postfix 覆盖异常: {ex.Message}");
            }
        }
    }
}

