using HarmonyLib;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using FMOD;
using FMODUnity;
using Duckov; // CharacterMainControl
using Duckov.ItemUsage; // FoodDrink
using ItemStatsSystem; // Item
using DuckovCustomSounds.CustomGunSounds; // 复用 GunLogger 与 ModBehaviour

namespace DuckovCustomSounds.CustomItemSounds
{
    // 消耗品使用：在 CA_UseItem.SetUseItem 时播放自定义 3D 音效（参考枪械/近战实现模式）
    [HarmonyPatch(typeof(FoodDrink))]
    public static class FoodDrink_OnUse_PlayCustomSfx
    {
        // 静态构造函数，用于确认补丁是否正确加载
        static FoodDrink_OnUse_PlayCustomSfx()
        {
            GunLogger.Debug("[ItemUse] FoodDrink_OnUse_PlayCustomSfx 静态构造函数被调用，补丁已加载");
        }
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

        private static System.Collections.IEnumerator FollowAndCleanup(Transform follow, Sound sound, Channel channel, float maxSec)
        {
            float end = Time.realtimeSinceStartup + Mathf.Max(1f, maxSec);
            try
            {
                while (Time.realtimeSinceStartup < end)
                {
                    bool playing = false;
                    try { if (channel.hasHandle()) channel.isPlaying(out playing); } catch { }
                    if (!playing) break;

                    // 跟踪玩家位置
                    Vector3 pos = Vector3.zero;
                    try { if (follow != null) pos = follow.position; } catch { }
                    var fpos = new FMOD.VECTOR { x = pos.x, y = pos.y, z = pos.z };
                    var fvel = new FMOD.VECTOR { x = 0, y = 0, z = 0 };
                    try { channel.set3DAttributes(ref fpos, ref fvel); } catch { }

                    yield return null;
                }
            }
            finally
            {
                try { if (channel.hasHandle()) channel.stop(); } catch { }
                try { if (sound.hasHandle()) sound.release(); } catch { }
            }
        }
    }

        // ItemUseContext：记录最近一次（按角色/其GameObject维度）使用物品的 TypeID，供 AudioManager.Post(use_*) 拦截时反查
        internal static class ItemUseContext
        {
            private static readonly System.Collections.Generic.Dictionary<int, string> LastTypeId = new System.Collections.Generic.Dictionary<int, string>();

            public static void Remember(CharacterMainControl character, string typeIdStr)
            {
                if (character == null || string.IsNullOrWhiteSpace(typeIdStr)) return;
                try
                {
                    var instanceID = character.GetInstanceID();
                    LastTypeId[instanceID] = typeIdStr;
                    GunLogger.Debug($"[ItemUseContext] Remember: Character={character.name}, InstanceID={instanceID}, TypeID={typeIdStr}");

                    // 同时尝试存储 GameObject 的 InstanceID（AudioManager.Post 传入的是该对象）
                    var go = character.gameObject;
                    if (go != null)
                    {
                        var goInstanceID = go.GetInstanceID();
                        if (goInstanceID != instanceID)
                        {
                            LastTypeId[goInstanceID] = typeIdStr;
                            GunLogger.Debug($"[ItemUseContext] Remember: 同时存储 GameObject InstanceID={goInstanceID}, TypeID={typeIdStr}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    GunLogger.Debug($"[ItemUseContext] Remember 异常: {ex.Message}");
                }
            }

            public static bool TryGet(GameObject go, out string typeIdStr)
            {
                typeIdStr = string.Empty;
                try { GunLogger.Debug($"[ItemUseContext] TryGet: go={(go?.name ?? "null")}, InstanceID={go?.GetInstanceID() ?? -1}"); } catch { }
                try
                {
                    if (go != null)
                    {
                        // 尝试多种方式查找 CharacterMainControl
                        CharacterMainControl cmc = null;

                        // 1. 直接在当前 GameObject 上查找
                        cmc = go.GetComponent<CharacterMainControl>();
                        if (cmc != null) GunLogger.Debug($"[ItemUseContext] 在当前 GameObject 上找到 CMC");

                        // 2. 在父级中查找
                        if (cmc == null)
                        {
                            cmc = go.GetComponentInParent<CharacterMainControl>();
                            if (cmc != null) GunLogger.Debug($"[ItemUseContext] 在父级中找到 CMC");
                        }

                        // 3. 在子级中查找
                        if (cmc == null)
                        {
                            cmc = go.GetComponentInChildren<CharacterMainControl>();
                            if (cmc != null) GunLogger.Debug($"[ItemUseContext] 在子级中找到 CMC");
                        }

                        // 4. 尝试通过 GameObject 的 InstanceID 直接查找
                        if (cmc == null)
                        {
                            var goInstanceID = go.GetInstanceID();
                            if (LastTypeId.TryGetValue(goInstanceID, out var v) && !string.IsNullOrWhiteSpace(v))
                            {
                                typeIdStr = v;
                                GunLogger.Debug($"[ItemUseContext] 通过 GameObject InstanceID={goInstanceID} 直接找到 TypeID={typeIdStr}");
                                return true;
                            }
                        }

                        if (cmc != null)
                        {
                            var key = cmc.GetInstanceID();
                            if (LastTypeId.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v))
                            {
                                typeIdStr = v;
                                GunLogger.Debug($"[ItemUseContext] 找到 CMC，InstanceID={key}, TypeID={typeIdStr}");
                                return true;
                            }
                        }

                        // 5. 调试：列出所有存储的映射（仅 Debug 模式）
                        GunLogger.Debug($"[ItemUseContext] 当前存储的映射数量: {LastTypeId.Count}");
                        foreach (var kvp in LastTypeId)
                        {
                            GunLogger.Debug($"[ItemUseContext] 映射: InstanceID={kvp.Key}, TypeID={kvp.Value}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    GunLogger.Debug($"[ItemUseContext] 异常: {ex.Message}");
                }
                GunLogger.Debug("[ItemUseContext] 未找到 CMC 或 TypeID 映射");
                return false;
            }
        }


        // 在设置使用物品时记录该角色最近一次使用的物品 TypeID（用于后续拦截 use_* 消耗品音效时匹配自定义音效）
        [HarmonyPatch(typeof(CA_UseItem))]
        public static class CA_UseItem_SetUseItem_RememberTypeId
        {
            [HarmonyPatch("SetUseItem")] // void SetUseItem(Item _item)
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
                    if (cmc == null) { try { cmc = __instance.GetComponentInChildren<CharacterMainControl>(); } catch { } }
                    if (cmc == null)
                    {
                        // 兜底：通过 Harmony Traverse 读取基类中的 characterController 字段
                        try { cmc = Traverse.Create(__instance).Field("characterController").GetValue<CharacterMainControl>(); } catch { }
                    }

                    if (cmc != null)
                    {
                        ItemUseContext.Remember(cmc, typeIdStr);
                        try { GunLogger.Debug($"[ItemUse] SetUseItem 记录: Character={cmc.name}, TypeID={typeIdStr}"); } catch { }
                    }
                    else
                    {
                        // 理论上不会发生（CA_UseItem 与 CharacterMainControl 在同一角色对象上）
                        try { GunLogger.Debug($"[ItemUse] SetUseItem: 未找到 CharacterMainControl，无法记录 TypeID={typeIdStr}"); } catch { }
                    }
                }
                catch (Exception ex)
                {
                    try { GunLogger.Debug($"[ItemUse] SetUseItem Prefix 异常: {ex.Message}"); } catch { }
                }
            }
        }

        [HarmonyPatch(typeof(AudioManager))]
        public static class AudioManager_Post_ItemUseReplace
        {
            // 所有可拦截的消耗品事件前缀：SFX/Item/use_*
            private const string EventPrefix = "SFX/Item/use_";
            private static readonly string[] Exts2 = new[] { ".mp3", ".wav", ".ogg", ".oga" };
            private static IEnumerable<string> ExpandCandidates2(string dir, params string[] namesNoExt)
            {
                foreach (var name in namesNoExt)
                {
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    foreach (var ext in Exts2)
                        yield return Path.Combine(dir, name + ext);
                }
            }

            // 查找形如 <typeId>_*.ext 的变体文件（顶层目录内）
            private static IEnumerable<string> FindVariants(string dir, string typeId)
            {
                if (string.IsNullOrWhiteSpace(dir) || string.IsNullOrWhiteSpace(typeId)) yield break;
                foreach (var ext in Exts2)
                {
                    IEnumerable<string> files = Array.Empty<string>();
                    try { files = Directory.EnumerateFiles(dir, $"{typeId}_*{ext}", SearchOption.TopDirectoryOnly); } catch { }
                    foreach (var f in files) yield return f;
                }
            }


            private static ChannelGroup AcquireSfxChannelGroup()
            {
                ChannelGroup group = default;
                try
                {
                    var sfxBus = RuntimeManager.GetBus("bus:/Master/SFX");
                    if (sfxBus.getChannelGroup(out var cg) == RESULT.OK && cg.hasHandle())
                    {
                        try { ModBehaviour.SfxGroup = cg; } catch { }
                        return cg;
                    }
                }
                catch { }
                try
                {
                    var sfxBusAlt = RuntimeManager.GetBus("bus:/SFX");
                    if (sfxBusAlt.getChannelGroup(out var cg2) == RESULT.OK && cg2.hasHandle())
                    {
                        try { ModBehaviour.SfxGroup = cg2; } catch { }
                        return cg2;
                    }
                }
                catch { }
                try { if (ModBehaviour.SfxGroup.hasHandle()) return ModBehaviour.SfxGroup; } catch { }
                try { RuntimeManager.CoreSystem.getMasterChannelGroup(out group); } catch { }
                return group;
            }

            [HarmonyPatch("Post", new Type[] { typeof(string), typeof(GameObject) })]
            [HarmonyPostfix]
            public static void Postfix(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
            {
                try
                {
                    if (string.IsNullOrEmpty(eventName)) return;
                    if (!eventName.StartsWith(EventPrefix, StringComparison.OrdinalIgnoreCase)) return;
                    var category = eventName.Substring(EventPrefix.Length).Trim(); // e.g. "food"/"meds"/"syringe"
                    if (!ItemSoundsConfig.Enabled) return;
                    if (!ItemSoundsConfig.IsCategoryEnabled(category)) return;

                    // 预留：这里可读取 item_voice_rules.json，将 category/typeId 的启用与映射交给规则（当前默认全启用）
                    try { if (!RuntimeManager.IsInitialized) { GunLogger.Info("[ItemUse] FMOD 未初始化，跳过自定义音效"); return; } } catch { }

                    // Determine 2D/3D from original event
                    bool is3D = true;
                    float min = 1f, max = 25f;
                    try
                    {
                        if (__result.HasValue && __result.Value.isValid())
                        {
                            if (__result.Value.getDescription(out FMOD.Studio.EventDescription desc) == RESULT.OK)
                            {
                                try { desc.is3D(out is3D); } catch { }
                                try { desc.getMinMaxDistance(out min, out max); } catch { }
                            }
                            // mute original (optional)
                            if (ItemSoundsConfig.ReplaceOriginal)
                            {
                                try { var ev = __result.Value; ev.setVolume(0f); } catch { }
                            }
                        }
                    }
                    catch { }

                    // Resolve candidates（新结构优先：CustomItemSounds/<category>/<typeId[_X]>.ext；兼容旧扁平：CustomItemSounds/<typeId[_X]>.ext）
                    string baseDir = ItemSoundsConfig.GetBaseDir();
                    string categoryDir = Path.Combine(baseDir, category);

                    string typeIdStr;
                    var found = ItemUseContext.TryGet(gameObject, out typeIdStr);
                    try { GunLogger.Debug($"[ItemUse] 事件={eventName}, 类别={category}, TryGet={(found ? "成功" : "失败")}, TypeID={(string.IsNullOrWhiteSpace(typeIdStr) ? "null" : typeIdStr)}"); } catch { }

                    var typeCandidates = new List<string>();
                    if (!string.IsNullOrWhiteSpace(typeIdStr))
                    {
                        // 分类目录下：精确 + 变体
                        typeCandidates.AddRange(ExpandCandidates2(categoryDir, typeIdStr).Where(File.Exists));
                        typeCandidates.AddRange(FindVariants(categoryDir, typeIdStr));
                        // 兼容旧扁平结构：精确 + 变体
                        typeCandidates.AddRange(ExpandCandidates2(baseDir, typeIdStr).Where(File.Exists));
                        typeCandidates.AddRange(FindVariants(baseDir, typeIdStr));
                    }

                    var defaultCandidates = ExpandCandidates2(categoryDir, "default").Concat(ExpandCandidates2(baseDir, "default")).Where(File.Exists).ToList();

                    // 随机选择一个变体（若有）
                    string filePath = null;
                    if (typeCandidates.Count > 0)
                    {
                        filePath = typeCandidates[UnityEngine.Random.Range(0, typeCandidates.Count)];
                    }
                    else if (defaultCandidates.Count > 0)
                    {
                        filePath = defaultCandidates[UnityEngine.Random.Range(0, defaultCandidates.Count)];
                    }

                    var chain = $"[{categoryDir}] + [{baseDir}] -> 类型({typeCandidates.Count})/默认({defaultCandidates.Count})";
                    if (filePath == null)
                    {
                        GunLogger.Info($"[ItemUse] 未找到自定义文件。事件={eventName}, 类别={category}, TypeID={typeIdStr ?? "?"}, 查找: {chain}");
                        return;
                    }
                    GunLogger.Debug($"[ItemUse] 最终使用: {filePath}. 事件={eventName}, 类别={category}, TypeID={typeIdStr ?? "?"}");

                    string fullPath = filePath;
                    try { fullPath = System.IO.Path.GetFullPath(filePath); } catch { }
                    string ext = string.Empty; try { ext = Path.GetExtension(fullPath)?.ToLowerInvariant() ?? string.Empty; } catch { }
                    var baseMode = MODE.LOOP_OFF;
                    if (is3D) baseMode |= MODE._3D | MODE._3D_LINEARROLLOFF;
                    var mode = ((ext == ".mp3" || ext == ".ogg" || ext == ".oga") ? MODE.CREATESTREAM : MODE.CREATESAMPLE) | baseMode;

                    var r1 = RuntimeManager.CoreSystem.createSound(fullPath, mode, out Sound sound);
                    if (r1 != RESULT.OK || !sound.hasHandle())
                    {
                        GunLogger.Info($"[ItemUse] createSound 失败({r1})，跳过自定义音效");
                        return;
                    }
                    if (is3D) try { sound.set3DMinMaxDistance(min, max); } catch { }

                    ChannelGroup group = AcquireSfxChannelGroup();
                    var r2 = RuntimeManager.CoreSystem.playSound(sound, group, true, out Channel channel);
                    if (r2 != RESULT.OK || !channel.hasHandle())
                    {
                        GunLogger.Info($"[ItemUse] playSound 失败({r2})，跳过自定义音效");
                        try { if (sound.hasHandle()) sound.release(); } catch { }
                        return;
                    }

                    if (is3D)
                    {
                        Vector3 pos = Vector3.zero;
                        try { pos = gameObject?.transform?.position ?? Vector3.zero; } catch { }
                        var fpos = new FMOD.VECTOR { x = pos.x, y = pos.y, z = pos.z };
                        var fvel = new FMOD.VECTOR { x = 0, y = 0, z = 0 };
                        try { channel.set3DAttributes(ref fpos, ref fvel); } catch { }
                        try { channel.set3DMinMaxDistance(min, max); } catch { }
                        try { channel.setMode(MODE._3D | MODE._3D_LINEARROLLOFF | MODE.LOOP_OFF); } catch { }
                    }
                    try { channel.setVolume(Mathf.Clamp(ItemSoundsConfig.Volume, 0f, 2f)); } catch { }
                    try { channel.setPaused(false); } catch { }

                    var modeStr = is3D ? "3D" : "2D";
                    GunLogger.Debug($"[ItemUse] 覆盖播放({modeStr}) {Path.GetFileName(filePath)}");

                    try { ModBehaviour.Instance?.StartCoroutine(Cleanup(sound, channel, 6f)); } catch { }
                }
                catch (Exception ex)
                {
                    GunLogger.Warn($"[ItemUse] Postfix 覆盖异常: {ex.Message}");
                }
            }

            private static System.Collections.IEnumerator Cleanup(Sound sound, Channel channel, float maxSec)
            {
                float end = Time.realtimeSinceStartup + Mathf.Max(1f, maxSec);
                try
                {
                    while (Time.realtimeSinceStartup < end)
                    {
                        bool playing = false;
                        try { if (channel.hasHandle()) channel.isPlaying(out playing); } catch { }
                        if (!playing) break;
                        yield return new WaitForSeconds(0.05f);
                    }
                }
                finally
                {
                    try { if (channel.hasHandle()) channel.stop(); } catch { }
                    try { if (sound.hasHandle()) sound.release(); } catch { }
                }
            }
        }

}

