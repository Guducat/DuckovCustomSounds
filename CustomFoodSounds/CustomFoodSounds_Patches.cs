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

namespace DuckovCustomSounds.CustomFoodSounds
{
    // 食物使用：在 FoodDrink.OnUse 时播放自定义 3D 音效（参考枪械/近战实现模式）
    [HarmonyPatch(typeof(FoodDrink))]
    public static class FoodDrink_OnUse_PlayCustomSfx
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

        [HarmonyPatch("OnUse", new Type[] { typeof(Item), typeof(object) })]
        [HarmonyPostfix]
        public static void Postfix(FoodDrink __instance, Item item, object user)
        {
            try
            {
                // 仅当由角色实际使用时播放
                CharacterMainControl character = null;
                try { character = user as CharacterMainControl; } catch { }
                if (character == null) return;
                if (item == null) return;

                string dir = Path.Combine(ModBehaviour.ModFolderName, "CustomFoodSounds");

                // 仅按 item.TypeID 匹配，失败则放弃
                string typeIdStr = string.Empty;
                try { typeIdStr = item.TypeID.ToString(); } catch { }

                if (string.IsNullOrWhiteSpace(typeIdStr))
                {
                    GunLogger.Debug("[FoodUse] item.TypeID 为空，跳过播放");
                    return;
                }

                var attempts = new List<string>(ExpandCandidates(dir, typeIdStr));
                var chain = string.Join(" → ", attempts.Select(p => $"[{p}]"));
                string filePath = attempts.FirstOrDefault(File.Exists);
                if (filePath == null)
                {
                    GunLogger.Debug($"[FoodUse] TypeID={typeIdStr}, 查找顺序: {chain}, 最终使用: 未找到自定义文件");
                    return;
                }
                else
                {
                    GunLogger.Debug($"[FoodUse] TypeID={typeIdStr}, 查找顺序: {chain}, 最终使用: {filePath}");
                }

                // 记录 TypeID 映射，等 AudioManager.Post 覆盖播放
                try { FoodUseContext.Remember(character, typeIdStr); GunLogger.Debug($"[FoodUse] 记录 TypeID={typeIdStr} (等待 AudioManager.Post 覆盖)"); } catch { }
                return;
            }
            catch (Exception ex)
            {
                GunLogger.Warn($"[FoodUse] Postfix 异常: {ex.Message}");
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

        internal static class FoodUseContext
        {
            private static readonly System.Collections.Generic.Dictionary<int, string> LastTypeId = new System.Collections.Generic.Dictionary<int, string>();

            public static void Remember(CharacterMainControl character, string typeIdStr)
            {
                if (character == null || string.IsNullOrWhiteSpace(typeIdStr)) return;
                try { LastTypeId[character.GetInstanceID()] = typeIdStr; } catch { }
            }

            public static bool TryGet(GameObject go, out string typeIdStr)
            {
                typeIdStr = string.Empty;
                try
                {
                    if (go != null)
                    {
                        var cmc = go.GetComponent<CharacterMainControl>() ?? go.GetComponentInParent<CharacterMainControl>();
                        if (cmc != null)
                        {
                            var key = cmc.GetInstanceID();
                            if (LastTypeId.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v))
                            {
                                typeIdStr = v;
                                return true;
                            }
                        }
                    }
                }
                catch { }
                return false;
            }
        }

        [HarmonyPatch(typeof(AudioManager))]
        public static class AudioManager_Post_FoodUseReplace
        {
            private const string FoodEvent = "SFX/Item/use_food";
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
                    if (!string.Equals(eventName, FoodEvent, StringComparison.OrdinalIgnoreCase)) return;

                    try { if (!RuntimeManager.IsInitialized) { GunLogger.Info("[FoodUse] FMOD 未初始化，跳过自定义音效"); return; } } catch { }

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
                            // mute original
                            try { var ev = __result.Value; ev.setVolume(0f); } catch { }
                        }
                    }
                    catch { }

                    // Resolve candidates
                    string dir = Path.Combine(ModBehaviour.ModFolderName, "CustomFoodSounds");
                    string typeIdStr = string.Empty;
                    FoodUseContext.TryGet(gameObject, out typeIdStr);
                    var attempts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(typeIdStr))
                        attempts.AddRange(ExpandCandidates2(dir, typeIdStr));
                    var chain = string.Join(" → ", attempts.Select(p => $"[{p}]").ToArray());
                    string filePath = attempts.FirstOrDefault(File.Exists);
                    if (filePath == null)
                    {
                        var fallbacks = ExpandCandidates2(dir, "default");
                        filePath = fallbacks.FirstOrDefault(File.Exists);
                        if (filePath == null)
                        {
                            GunLogger.Debug($"[FoodUse] TypeID={typeIdStr ?? "?"}, 查找顺序: {chain}, 最终使用: 未找到自定义文件");
                            return;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(typeIdStr))
                        GunLogger.Debug($"[FoodUse] TypeID={typeIdStr}, 查找顺序: {chain}, 最终使用: {filePath}");
                    else
                        GunLogger.Debug($"[FoodUse] 查找顺序: {chain}, 最终使用: {filePath}");

                    string fullPath = filePath;
                    try { fullPath = System.IO.Path.GetFullPath(filePath); } catch { }
                    string ext = string.Empty; try { ext = Path.GetExtension(fullPath)?.ToLowerInvariant(); } catch { }
                    var baseMode = MODE.LOOP_OFF;
                    if (is3D) baseMode |= MODE._3D | MODE._3D_LINEARROLLOFF;
                    var mode = ((ext == ".mp3" || ext == ".ogg" || ext == ".oga") ? MODE.CREATESTREAM : MODE.CREATESAMPLE) | baseMode;

                    var r1 = RuntimeManager.CoreSystem.createSound(fullPath, mode, out Sound sound);
                    if (r1 != RESULT.OK || !sound.hasHandle())
                    {
                        GunLogger.Info($"[FoodUse] createSound 失败({r1})，跳过自定义音效");
                        return;
                    }
                    if (is3D) try { sound.set3DMinMaxDistance(min, max); } catch { }

                    ChannelGroup group = AcquireSfxChannelGroup();
                    var r2 = RuntimeManager.CoreSystem.playSound(sound, group, true, out Channel channel);
                    if (r2 != RESULT.OK || !channel.hasHandle())
                    {
                        GunLogger.Info($"[FoodUse] playSound 失败({r2})，跳过自定义音效");
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
                    try { channel.setPaused(false); } catch { }

                    var modeStr = is3D ? "3D" : "2D";
                    GunLogger.Debug($"[FoodUse] 覆盖播放({modeStr}) {Path.GetFileName(filePath)}");

                    try { ModBehaviour.Instance?.StartCoroutine(Cleanup(sound, channel, 6f)); } catch { }
                }
                catch (Exception ex)
                {
                    GunLogger.Warn($"[FoodUse] Postfix 覆盖异常: {ex.Message}");
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

