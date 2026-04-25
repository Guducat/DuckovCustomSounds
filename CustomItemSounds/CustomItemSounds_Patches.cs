using HarmonyLib;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Duckov;
using Duckov.ItemUsage;
using ItemStatsSystem;

namespace DuckovCustomSounds.CustomItemSounds
{
    /// <summary>
    /// 物品使用音效替换 - 使用新接口简化版本
    /// 保留 Action/Finish 阶段追踪逻辑
    /// </summary>
    public static class CustomItemSounds_Patches
    {
        private static readonly string[] Exts = new[] { ".mp3", ".wav", ".ogg", ".oga" };

        private static IEnumerable<string> ExpandCandidates(string dir, params string[] namesNoExt)
        {
            foreach (var name in namesNoExt)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                foreach (var ext in Exts)
                {
                    yield return Path.Combine(dir, name + ext);
                }
            }
        }

        private static IEnumerable<string> ExpandPhaseCandidates(string dir, string typeIdStr, string soundKey, ItemUsePhase phase)
        {
            // 支持与枪械类似的“分段文件”命名：*_action/*_start 与 *_finish/*_end
            // 优先顺序：TypeID_相位 → soundKey_相位 → default_相位
            var suffixes = phase == ItemUsePhase.Action
                ? new[] { "_action", "_start" }
                : new[] { "_finish", "_end" };

            foreach (var suf in suffixes)
            {
                if (!string.IsNullOrWhiteSpace(typeIdStr))
                    foreach (var p in ExpandCandidates(dir, typeIdStr + suf)) yield return p;
                foreach (var p in ExpandCandidates(dir, soundKey + suf)) yield return p;
                foreach (var p in ExpandCandidates(dir, "default" + suf)) yield return p;
            }
        }


            // 严格差分选择：仅接受 "<basename>_数字" 这种形式，避免把 "*_mute_1" 识别为 "*" 的变体
            private static string TryPickVariantStrict(string filePath)
            {
                if (string.IsNullOrWhiteSpace(filePath)) return filePath;
                try
                {
                    string dir = Path.GetDirectoryName(filePath);
                    string ext = Path.GetExtension(filePath);
                    string nameNoExt = Path.GetFileNameWithoutExtension(filePath);
                    if (string.IsNullOrWhiteSpace(dir) || string.IsNullOrWhiteSpace(ext) || string.IsNullOrWhiteSpace(nameNoExt))
                        return filePath;

                    string pattern = nameNoExt + "_*" + ext;
                    var candidates = new List<string>();
                    foreach (var p in Directory.EnumerateFiles(dir, pattern))
                    {
                        var file = Path.GetFileName(p);
                        if (file == null) continue;
                        string fileNoExt = Path.GetFileNameWithoutExtension(file);
                        int usIdx = fileNoExt.LastIndexOf('_');
                        if (usIdx <= 0) continue;
                        if (!string.Equals(fileNoExt.Substring(0, usIdx), nameNoExt, StringComparison.OrdinalIgnoreCase))
                            continue;
                        var numSpan = fileNoExt.Substring(usIdx + 1);
                        if (int.TryParse(numSpan, out var n) && n >= 1)
                        {
                            candidates.Add(Path.Combine(dir, file));
                        }
                    }

                    if (candidates.Count == 0) return filePath;
                    int pick = UnityEngine.Random.Range(0, candidates.Count);
                    return candidates[pick];
                }
                catch { return filePath; }
            }

            // 当未命中基准文件时，按基名（无扩展）尝试严格差分选择
            private static string TryPickVariantStrictByBase(string dir, string baseNameNoExt)
            {
                if (string.IsNullOrWhiteSpace(dir) || string.IsNullOrWhiteSpace(baseNameNoExt)) return null;
                try
                {
                    var candidates = new List<string>();
                    foreach (var ext in Exts)
                    {
                        string pattern = baseNameNoExt + "_*" + ext;
                        foreach (var p in Directory.EnumerateFiles(dir, pattern))
                        {
                            var file = Path.GetFileName(p);
                            if (file == null) continue;
                            string fileNoExt = Path.GetFileNameWithoutExtension(file);
                            int usIdx = fileNoExt.LastIndexOf('_');
                            if (usIdx <= 0) continue;
                            if (!string.Equals(fileNoExt.Substring(0, usIdx), baseNameNoExt, StringComparison.OrdinalIgnoreCase))
                                continue;
                            var numSpan = fileNoExt.Substring(usIdx + 1);
                            if (int.TryParse(numSpan, out var n) && n >= 1)
                            {
                                candidates.Add(Path.Combine(dir, file));
                            }
                        }
                    }

                    if (candidates.Count == 0) return null;
                    int pick = UnityEngine.Random.Range(0, candidates.Count);
                    return candidates[pick];
                }
                catch { return null; }
            }

        // --- 物品使用上下文（记录 TypeID） ---
        internal static class ItemUseContext
        {
            private static readonly Dictionary<int, string> LastTypeId = new Dictionary<int, string>();

            public static void Remember(CharacterMainControl character, string typeIdStr)
            {
                if (character == null || string.IsNullOrWhiteSpace(typeIdStr)) return;
                try
                {
                    var instanceID = character.GetInstanceID();
                    LastTypeId[instanceID] = typeIdStr;

                    var go = character.gameObject;
                    if (go != null)
                    {
                        var goInstanceID = go.GetInstanceID();
                        if (goInstanceID != instanceID)
                        {
                            LastTypeId[goInstanceID] = typeIdStr;
                        }
                    }
                }
                catch { }
            }

            public static bool TryGet(GameObject go, out string typeIdStr)
            {
                typeIdStr = null;
                if (go == null) return false;
                try
                {
                    var id = go.GetInstanceID();
                    return LastTypeId.TryGetValue(id, out typeIdStr);
                }
                catch { return false; }
            }
        }

        // --- 阶段追踪（用于停止 Action 阶段音效） ---
        internal enum ItemUsePhase { Action, Finish }

        internal static class ItemUseSoundRegistry
        {
            private struct TrackedSound
            {
                public FMOD.Studio.EventInstance? EventInstance;
                public ItemUsePhase Phase;
                public int GoId;
            }

            private static readonly Dictionary<int, List<TrackedSound>> _map = new Dictionary<int, List<TrackedSound>>();

            public static void Track(GameObject go, FMOD.Studio.EventInstance? eventInstance, ItemUsePhase phase)
            {
                int id = 0;
                try { id = go != null ? go.GetInstanceID() : 0; } catch { }
                if (id == 0) return;

                var ts = new TrackedSound { EventInstance = eventInstance, Phase = phase, GoId = id };
                lock (_map)
                {
                    if (!_map.TryGetValue(id, out var list))
                    {
                        list = new List<TrackedSound>();
                        _map[id] = list;
                    }
                    list.Add(ts);
                }
            }

            public static void StopByPhase(GameObject go, ItemUsePhase phase, FMOD.Studio.STOP_MODE mode = FMOD.Studio.STOP_MODE.ALLOWFADEOUT)
            {
                int id = 0;
                try { id = go != null ? go.GetInstanceID() : 0; } catch { }
                if (id == 0) return;

                List<TrackedSound> snapshot = null;
                lock (_map)
                {
                    if (_map.TryGetValue(id, out var list) && list != null && list.Count > 0)
                        snapshot = list.ToList();
                }

                if (snapshot == null) return;
                foreach (var ts in snapshot)
                {
                    if (ts.Phase == phase)
                    {
                        try
                        {
                            if (ts.EventInstance.HasValue && ts.EventInstance.Value.isValid())
                            {
                                ts.EventInstance.Value.stop(mode);
                            }
                        }
                        catch { }
                    }
                }

                lock (_map)
                {
                    if (_map.TryGetValue(id, out var list))
                    {
                        list.RemoveAll(t => t.Phase == phase);
                        if (list.Count == 0) _map.Remove(id);
                    }
                }
            }
        }

        // --- 使用周期跟踪（区分“正常完成”与“取消/中断”） ---
        internal static class ItemUseCycle
        {
            internal struct Cycle
            {
                public bool HasActionPosted;
                public bool HasFinishPosted;
                public bool FinishCalled;
                public float LastActionTime;
                public float LastFinishTime;
                public float LastFinishCalledTime;
                // Whether an SFX/Item/use_* Post was observed within the phase
                public bool ObservedActionSfx;
                public bool ObservedFinishSfx;
            }

            private static readonly Dictionary<int, Cycle> _cycles = new Dictionary<int, Cycle>();

            public static void ResetFor(GameObject go)
            {
                if (go == null) return;
                try { _cycles[go.GetInstanceID()] = default; } catch { }
            }

            public static void MarkPhase(GameObject go, ItemUsePhase phase)
            {
                if (go == null) return;
                try
                {
                    int id = go.GetInstanceID();
                    if (!_cycles.TryGetValue(id, out var c)) c = default;
                    if (phase == ItemUsePhase.Action) { c.HasActionPosted = true; c.LastActionTime = Time.realtimeSinceStartup; }
                    else { c.HasFinishPosted = true; c.LastFinishTime = Time.realtimeSinceStartup; }
                    _cycles[id] = c;
                }
                catch { }
            }

            public static void MarkFinishCalled(GameObject go)
            {
                if (go == null) return;
                try
                {
                    int id = go.GetInstanceID();
                    if (!_cycles.TryGetValue(id, out var c)) c = default;
                    c.FinishCalled = true;
                    c.LastFinishCalledTime = Time.realtimeSinceStartup;
                    _cycles[id] = c;
                }
                catch { }
            }

            public static void ClearObserved(GameObject go, ItemUsePhase phase)
            {
                if (go == null) return;
                try
                {
                    int id = go.GetInstanceID();
                    if (!_cycles.TryGetValue(id, out var c)) c = default;
                    if (phase == ItemUsePhase.Action) c.ObservedActionSfx = false; else c.ObservedFinishSfx = false;
                    _cycles[id] = c;
                }
                catch { }
            }

            public static void MarkObserved(GameObject go, ItemUsePhase phase)
            {
                if (go == null) return;
                try
                {
                    int id = go.GetInstanceID();
                    if (!_cycles.TryGetValue(id, out var c)) c = default;
                    if (phase == ItemUsePhase.Action) c.ObservedActionSfx = true; else c.ObservedFinishSfx = true;
                    _cycles[id] = c;
                }
                catch { }
            }

            public static bool WasObserved(GameObject go, ItemUsePhase phase)
            {
                if (go == null) return false;
                try
                {
                    if (_cycles.TryGetValue(go.GetInstanceID(), out var c))
                    {
                        return phase == ItemUsePhase.Action ? c.ObservedActionSfx : c.ObservedFinishSfx;
                    }
                }
                catch { }
                return false;
            }

            public static bool TryGet(GameObject go, out Cycle c)
            {
                c = default;
                if (go == null) return false;
                try { return _cycles.TryGetValue(go.GetInstanceID(), out c); } catch { return false; }
            }

            public static void Clear(GameObject go)
            {
                if (go == null) return;
                try { _cycles.Remove(go.GetInstanceID()); } catch { }
            }
        }

        // --- 记录物品 TypeID ---
        [HarmonyPatch(typeof(CA_UseItem))]
        public static class CA_UseItem_SetUseItem_RememberTypeId
        {
            [HarmonyPatch("SetUseItem")]
            [HarmonyPrefix]
            public static void Prefix(CA_UseItem __instance, Item _item)
            {
                try
                {
                    if (_item == null) return;

                    string typeIdStr = string.Empty;
                    try { typeIdStr = _item.TypeID.ToString(); } catch { }
                    if (string.IsNullOrWhiteSpace(typeIdStr)) return;

                    CharacterMainControl cmc = null;
                    try { cmc = __instance.GetComponent<CharacterMainControl>(); } catch { }
                    if (cmc == null) { try { cmc = __instance.GetComponentInParent<CharacterMainControl>(); } catch { } }
                    if (cmc == null) { try { cmc = Traverse.Create(__instance).Field("characterController").GetValue<CharacterMainControl>(); } catch { } }

                    if (cmc != null)
                    {
                        ItemUseContext.Remember(cmc, typeIdStr);
                        ItemLogger.Debug($"[ItemUse] 记录 TypeID: {typeIdStr}");
                    }

                    // 开启新一轮的“使用周期”跟踪
                    try { ItemUseCycle.ResetFor(__instance?.gameObject); } catch { }
                }
                catch { }
            }
        }

        // --- 标记阶段 ---
        private static ItemUsePhase? s_CurrentPhase = null;

        [HarmonyPatch(typeof(CA_UseItem))]
        public static class CA_UseItem_SoundPhaseMarkers
        {
            [HarmonyPatch("PostActionSound")]
            [HarmonyPrefix]
            public static void PostActionSound_Prefix(CA_UseItem __instance)
            {
                s_CurrentPhase = ItemUsePhase.Action;
                try { ItemUseCycle.MarkPhase(__instance?.gameObject, ItemUsePhase.Action); } catch { }
                try { ItemUseCycle.ClearObserved(__instance?.gameObject, ItemUsePhase.Action); } catch { }
            }

            [HarmonyPatch("PostActionSound")]
            [HarmonyPostfix]
            public static void PostActionSound_Postfix(CA_UseItem __instance)
            {
                try { TryInjectIfMissing(__instance?.gameObject, ItemUsePhase.Action); } catch { }
                s_CurrentPhase = null;
            }

            [HarmonyPatch("PostUseSound")]
            [HarmonyPrefix]
            public static void PostUseSound_Prefix(CA_UseItem __instance)
            {
                s_CurrentPhase = ItemUsePhase.Finish;
                try { ItemUseCycle.MarkPhase(__instance?.gameObject, ItemUsePhase.Finish); } catch { }
                try { ItemUseCycle.ClearObserved(__instance?.gameObject, ItemUsePhase.Finish); } catch { }
            }

            [HarmonyPatch("PostUseSound")]
            [HarmonyPostfix]
            public static void PostUseSound_Postfix(CA_UseItem __instance)
            {
                try { TryInjectIfMissing(__instance?.gameObject, ItemUsePhase.Finish); } catch { }
                s_CurrentPhase = null;
            }
        }

        // --- 停止 Action 阶段音效 ---
        [HarmonyPatch(typeof(CA_UseItem))]
        public static class CA_UseItem_StopActionSounds
        {
            [HarmonyPatch("StopSound")]
            [HarmonyPrefix]
            public static bool Prefix(CA_UseItem __instance)
            {
                try
                {
                    var go = __instance?.gameObject;
                    if (go != null)
                    {
                        bool skipOriginalStop = false;
                        // 如果是正常完成且没有发布完成音，允许 Action 音效自然结束，避免误判为中断导致的戛然而止
                        if (ItemUseCycle.TryGet(go, out var cyc))
                        {
                            if (cyc.FinishCalled && !cyc.HasFinishPosted && cyc.HasActionPosted)
                            {
                                skipOriginalStop = true;
                                ItemLogger.Debug("[ItemUse] 正常完成但无完成音，跳过停止，允许 Action 自然结束");
                            }
                        }

                        // 兼顾"打针过短/长音频被截断"的问题：若 Action 音效过短
                        //  1) 若 OnFinish 已调用且尚未发布 Finish 音：跳过停止（允许自然结束）
                        //  2) 否则，保证一个最小可听时间（默认 ~0.05s），在此时间窗内跳过停止
                        bool shouldSkipStopForMinAudible = false;
                        try
                        {
                            float MIN_ACTION_AUDIBLE_SEC = Mathf.Max(0f, ItemConfig.MinActionAudibleSeconds);
                            if (ItemUseCycle.TryGet(go, out var cyc2) && cyc2.HasActionPosted)
                            {
                                float since = Time.realtimeSinceStartup - cyc2.LastActionTime;
                                if (since < MIN_ACTION_AUDIBLE_SEC && !cyc2.FinishCalled)
                                {
                                    shouldSkipStopForMinAudible = true;
                                    ItemLogger.Debug($"[ItemUse] Action 播放时长 {since:F2}s < {MIN_ACTION_AUDIBLE_SEC:F2}s, 跳过停止以避免过短");
                                }
                            }
                        }
                        catch { }

                        if (!skipOriginalStop && !shouldSkipStopForMinAudible)
                        {
                            ItemUseSoundRegistry.StopByPhase(go, ItemUsePhase.Action, FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                            ItemLogger.Debug("[ItemUse] 停止 Action 阶段音效（淡出）");
                            return true; // 继续执行原 StopSound（会停止 actionInstance）
                        }
                        else
                        {
                            return false; // 跳过原 StopSound
                        }
                    }
                }
                catch { }
                return true; // 兜底：继续执行原方法
            }
        }

        // 标记正常完成（OnFinish 已调用）
        [HarmonyPatch(typeof(CA_UseItem))]
        public static class CA_UseItem_OnFinish_Marker
        {
            [HarmonyPatch("OnFinish")]
            [HarmonyPostfix]
            public static void Postfix(CA_UseItem __instance)
            {
                try { ItemUseCycle.MarkFinishCalled(__instance?.gameObject); } catch { }
                // 若原版从不调用 PostUseSound，则在 OnFinish 兜底注入完成段
                try { TryInjectIfMissing(__instance?.gameObject, ItemUsePhase.Finish); } catch { }
            }
        }

        /// <summary>
        /// 当本阶段未观察到任何 SFX/Item/use_* 调用时，依据 JSON 映射尝试主动注入音效。
        /// </summary>
        private static void TryInjectIfMissing(GameObject go, ItemUsePhase phase)
        {
            if (go == null) return;
            try
            {
                // 若本阶段已有 SFX 调用，跳过注入
                if (ItemUseCycle.WasObserved(go, phase)) return;

                // 获取 TypeID
                string typeIdStr = null;
                try { ItemUseContext.TryGet(go, out typeIdStr); } catch { }

                // 解析应注入的 soundKey
                string targetKey = ItemSoundMap.ResolveForInjection(typeIdStr, phase);
                if (string.IsNullOrWhiteSpace(targetKey)) return; // 无注入配置

                // 类别开关
                if (!ItemConfig.IsCategoryEnabled(targetKey))
                {
                    ItemLogger.Debug($"[ItemUse] 注入取消（类别禁用）: {targetKey}");
                    return;
                }

                string dir = ItemConfig.GetBaseDir();
                string catDir = null;
                try { catDir = Path.Combine(dir, targetKey); } catch { }

                // 允许 JSON 指定文件基名（fileBase），实现多个 TypeID 共享同一文件
                string targetBase = ItemSoundMap.ResolveFileBase(typeIdStr, targetKey, phase) ?? targetKey;

                var attempts = new List<string>();
                if (!string.IsNullOrWhiteSpace(catDir))
                {
                    // 分段优先（按 fileBase）
                    attempts.AddRange(ExpandPhaseCandidates(catDir, typeIdStr, targetBase, phase));
                    if (!string.IsNullOrWhiteSpace(typeIdStr)) attempts.AddRange(ExpandCandidates(catDir, typeIdStr));
                    // 分类目录下允许直接以 fileBase 命名（如 bandage/64.mp3）
                    attempts.AddRange(ExpandCandidates(catDir, targetBase));
                    attempts.AddRange(ExpandCandidates(catDir, "default"));
                }
                if (!string.IsNullOrWhiteSpace(typeIdStr))
                {
                    attempts.AddRange(ExpandPhaseCandidates(dir, typeIdStr, targetBase, phase));
                    attempts.AddRange(ExpandCandidates(dir, typeIdStr));
                }
                // 根目录也允许以 fileBase 命名（兼容旧结构）
                attempts.AddRange(ExpandCandidates(dir, targetBase));

                string filePath = attempts.FirstOrDefault(File.Exists);
                if (filePath == null)
                {
                    var fallbacks = ExpandCandidates(dir, "default").ToList();
                    filePath = fallbacks.FirstOrDefault(File.Exists);
                    if (filePath == null)
                    {
                        string pickDefault = TryPickVariantStrictByBase(catDir, "default");
                        if (string.IsNullOrEmpty(pickDefault)) pickDefault = TryPickVariantStrictByBase(dir, "default");
                        if (!string.IsNullOrEmpty(pickDefault)) filePath = pickDefault;
                    }
                    if (filePath == null)
                    {
                        ItemLogger.Debug($"[ItemUse] 注入失败：未找到文件 TypeID={typeIdStr ?? "N/A"}, key={targetKey}");
                        return;
                    }
                }

                filePath = TryPickVariantStrict(filePath);

                var inst = AudioManager.PostCustomSFX(filePath, go, loop: false);
                if (inst.HasValue && inst.Value.isValid())
                {
                    try { inst.Value.setVolume(ItemConfig.Volume); } catch { }
                }

                ItemUseSoundRegistry.Track(go, inst, phase);
                ItemUseCycle.MarkObserved(go, phase);
                ItemLogger.Info($"[ItemUse] 已注入{phase}音效: {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                ItemLogger.Warning($"[ItemUse] 注入{phase}音效失败: {ex.Message}");
            }
        }

        // --- 拦截物品使用音效 ---
        [HarmonyPatch(typeof(AudioManager))]
        public static class AudioManager_Post_ItemUseReplace
        {
            private const string ItemUsePrefix = "SFX/Item/use_";

            [HarmonyPatch("Post", new Type[] { typeof(string), typeof(GameObject) })]
            [HarmonyPostfix]
            public static void Postfix(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
            {
                try
                {
                    // 检查配置是否启用
                    if (!ItemConfig.Enabled) return;

                    if (string.IsNullOrEmpty(eventName)) return;
                    if (!eventName.StartsWith(ItemUsePrefix, StringComparison.OrdinalIgnoreCase)) return;

                    string soundKey = eventName.Substring(ItemUsePrefix.Length);
                    if (string.IsNullOrWhiteSpace(soundKey)) return;

                    // 获取 TypeID（若无则允许为 null）
                    string typeIdStr = null;
                    ItemUseContext.TryGet(gameObject, out typeIdStr);

                    // 若处于已知阶段，标记“已观察到原版事件”
                    if (s_CurrentPhase.HasValue)
                    {
                        try { ItemUseCycle.MarkObserved(gameObject, s_CurrentPhase.Value); } catch { }
                    }

                    // 通过映射修正/覆盖 soundKey（含别名规则），确保分类正确
                    string originalKey = soundKey;
                    string resolvedKey = ItemSoundMap.ResolveForReplace(typeIdStr, soundKey, s_CurrentPhase);
                    if (!string.IsNullOrWhiteSpace(resolvedKey)) soundKey = resolvedKey;
                    if (!string.Equals(originalKey, soundKey, StringComparison.OrdinalIgnoreCase))
                    {
                        ItemLogger.Debug($"[ItemUse] 映射覆盖: {originalKey} -> {soundKey}");
                    }

                    // 分类开关（如禁用某一类，则保持原版音效）
                    if (!ItemConfig.IsCategoryEnabled(soundKey))
                    {
                        ItemLogger.Debug($"[ItemUse] 类别已禁用: {soundKey}");
                        return;
                    }

                    string dir = ItemConfig.GetBaseDir();
                    string catDir = null;
                    try { catDir = Path.Combine(dir, soundKey); } catch { }

                    if (string.IsNullOrEmpty(typeIdStr))
                    {
                        ItemLogger.Debug($"[ItemUse] 未找到 TypeID，使用 soundKey: {soundKey}");
                    }

                    // 构建候选文件列表（优先分类目录 → 回退根目录）
                    // 分类结构优先级：
                    //   (A) 分段优先（如果存在 *_action/*_start 或 *_finish/*_end）
                    //   1) CustomItemSounds/<category>/<TypeID>.*
                    //   2) CustomItemSounds/<category>/<fileBase>.* （支持共享文件，如 64.mp3）
                    //   3) CustomItemSounds/<category>/default.*
                    //   4) CustomItemSounds/<TypeID>.*
                    //   5) CustomItemSounds/<fileBase>.*
                    //   6) CustomItemSounds/default.*

                    // 允许 JSON 指定文件基名（fileBase），实现共享同一文件
                    string targetBase = ItemSoundMap.ResolveFileBase(typeIdStr, soundKey, s_CurrentPhase) ?? soundKey;

                    var attempts = new List<string>();

                    if (!string.IsNullOrWhiteSpace(catDir))
                    {
                        // 先尝试“分段”命名（Action/Finish）
                        if (s_CurrentPhase.HasValue)
                        {
                            attempts.AddRange(ExpandPhaseCandidates(catDir, typeIdStr, targetBase, s_CurrentPhase.Value));
                        }
                        if (!string.IsNullOrWhiteSpace(typeIdStr))
                        {
                            attempts.AddRange(ExpandCandidates(catDir, typeIdStr));
                        }
                        // 分类目录直接以 fileBase 命名
                        attempts.AddRange(ExpandCandidates(catDir, targetBase));
                        attempts.AddRange(ExpandCandidates(catDir, "default"));
                    }

                    if (!string.IsNullOrWhiteSpace(typeIdStr))
                    {
                        if (s_CurrentPhase.HasValue)
                        {
                            attempts.AddRange(ExpandPhaseCandidates(dir, typeIdStr, targetBase, s_CurrentPhase.Value));
                        }
                        attempts.AddRange(ExpandCandidates(dir, typeIdStr));
                    }
                    // 根目录兼容 fileBase 命名
                    attempts.AddRange(ExpandCandidates(dir, targetBase));

                    if (ItemLogger.IsVerboseEnabled)
                    {
                        try { ItemLogger.Verbose($"[ItemUse] 查找顺序: {string.Join(" | ", attempts)}"); } catch { }
                    }

                    string filePath = attempts.FirstOrDefault(File.Exists);

                    // 未命中基准文件时，尝试按候选基名进行严格差分选择
                    if (filePath == null)
                    {
                        foreach (var attempt in attempts)
                        {
                            try
                            {
                                var attemptDir = Path.GetDirectoryName(attempt);
                                var baseNoExt = Path.GetFileNameWithoutExtension(attempt);
                                var pick = TryPickVariantStrictByBase(attemptDir, baseNoExt);
                                if (!string.IsNullOrEmpty(pick)) { filePath = pick; break; }
                            }
                            catch { }
                        }
                    }

                    // 仍未命中则回退 default（含其差分）
                    if (filePath == null)
                    {
                        var fallbacks = ExpandCandidates(dir, "default").ToList();
                        filePath = fallbacks.FirstOrDefault(File.Exists);
                        if (filePath == null)
                        {
                            // 优先尝试分类目录 default 的差分，其次根目录 default 的差分
                            string pickDefault = TryPickVariantStrictByBase(catDir, "default");
                            if (string.IsNullOrEmpty(pickDefault)) pickDefault = TryPickVariantStrictByBase(dir, "default");
                            if (!string.IsNullOrEmpty(pickDefault)) filePath = pickDefault;
                        }
                        if (filePath == null)
                        {
                            ItemLogger.Debug($"[ItemUse] soundKey={soundKey}, 未找到自定义文件");
                            return;
                        }
                    }

                    // 命中后再进行一次严格差分选择（若存在 *_1、*_2... 则随机其一）
                    filePath = TryPickVariantStrict(filePath);

                    ItemLogger.Debug($"[ItemUse] TypeID={typeIdStr ?? "N/A"}, soundKey={soundKey}, 使用: {Path.GetFileName(filePath)}");

                    // 使用新接口播放自定义音效（SFX总线）
                    try
                    {
                        __result = AudioManager.PostCustomSFX(filePath, gameObject, loop: false);

                        // 应用音量配置
                        if (__result.HasValue && __result.Value.isValid())
                        {
                            try
                            {
                                __result.Value.setVolume(ItemConfig.Volume);
                                ItemLogger.Debug($"[ItemUse] 已应用音量: {ItemConfig.Volume:F2}");
                            }
                            catch (Exception volEx)
                            {
                                ItemLogger.Warning($"[ItemUse] 设置音量失败: {volEx.Message}");
                            }
                        }

                        // 追踪阶段
                        if (s_CurrentPhase.HasValue)
                        {
                            ItemUseSoundRegistry.Track(gameObject, __result, s_CurrentPhase.Value);
                            ItemLogger.Debug($"[ItemUse] 追踪阶段: {s_CurrentPhase.Value}");
                        }

                        ItemLogger.Info($"[ItemUse] 使用SFX接口播放: {Path.GetFileName(filePath)}");
                    }
                    catch (Exception ex)
                    {
                        ItemLogger.Warning($"[ItemUse] 新接口播放失败: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    ItemLogger.Warning($"[ItemUse] Postfix 覆盖异常: {ex.Message}");
                }
            }
        }
    }
}
