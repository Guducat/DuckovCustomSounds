using HarmonyLib;
using Duckov; // AudioManager
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FMOD;
using FMODUnity;
using DuckovCustomSounds.CustomGunSounds; // 复用 GunLogger 与 ModBehaviour

namespace DuckovCustomSounds.CustomMeleeSounds
{
    // 近战攻击：拦截并替换为自定义 3D 音效（参考 CustomGunSounds 模式）
    [HarmonyPatch(typeof(AudioManager))]
    public static class AudioManager_Post_MeleeAttackReplace
    {
        private const string MeleeAttackPrefix = "SFX/Combat/Melee/attack_";
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


        [HarmonyPatch("Post", new Type[] { typeof(string), typeof(GameObject) })]
        [HarmonyPostfix]
        public static void Postfix(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
        {
            try
            {
                if (string.IsNullOrEmpty(eventName)) return;
                if (!eventName.StartsWith(MeleeAttackPrefix, StringComparison.OrdinalIgnoreCase)) return; // 非近战攻击事件 → 忽略

                // 解析 soundKey（CA_Attack 里为 attack_ + meleeWeapon.SoundKey.ToLower()）
                string soundKey = eventName.Substring(MeleeAttackPrefix.Length);
                if (string.IsNullOrWhiteSpace(soundKey)) return;

                string dir = Path.Combine(ModBehaviour.ModFolderName, "CustomMeleeSounds");

                // 捕获近战组件，优先尝试按 TypeID 定位资源，其次回退到 soundKey，最后 default.*
                ItemAgent_MeleeWeapon melee = null;
                // 优先：从角色对象拿 CharacterMainControl 再取当前持有的近战武器
                try
                {
                    if (gameObject != null)
                    {
                        var cmc = gameObject.GetComponent<CharacterMainControl>() ?? gameObject.GetComponentInParent<CharacterMainControl>();
                        if (cmc != null)
                        {
                            try { melee = cmc.GetMeleeWeapon(); } catch { }
                        }
                    }
                }
                catch { }
                // 次优：直接在当前对象/父子层级中尝试查找 ItemAgent_MeleeWeapon
                if (melee == null)
                {
                    try { melee = gameObject?.GetComponent<ItemAgent_MeleeWeapon>(); } catch { }
                }
                if (melee == null)
                {
                    try { melee = gameObject?.GetComponentInParent<ItemAgent_MeleeWeapon>(); } catch { }
                }
                if (melee == null)
                {
                    try { melee = gameObject?.GetComponentInChildren<ItemAgent_MeleeWeapon>(); } catch { }
                }

                string typeIdStr = string.Empty;
                try { if (melee?.Item != null) typeIdStr = melee.Item.TypeID.ToString(); } catch { }

                var attempts = new List<string>();
                if (!string.IsNullOrWhiteSpace(typeIdStr))
                {
                    attempts.AddRange(ExpandCandidates(dir, typeIdStr));
                    attempts.AddRange(ExpandCandidates(dir, soundKey));
                }
                else
                {
                    GunLogger.Debug($"[MeleeAttack] 未捕获到 ItemAgent_MeleeWeapon，回退到 soundKey 查找模式");
                    attempts.AddRange(ExpandCandidates(dir, soundKey));
                }

                var chain = string.Join(" → ", attempts.Select(p => $"[{p}]").ToArray());
                string filePath = attempts.FirstOrDefault(File.Exists);
                if (filePath == null)
                {
                    var fallbacks = ExpandCandidates(dir, "default");
                    filePath = fallbacks.FirstOrDefault(File.Exists);
                    if (filePath == null)
                    {
                        GunLogger.Debug($"[MeleeAttack] 查找顺序: {chain}, 最终使用: 未找到自定义文件");
                        return;
                    }
                }

                if (!string.IsNullOrWhiteSpace(typeIdStr))
                {
                    GunLogger.Debug($"[MeleeAttack] TypeID={typeIdStr}, soundKey={soundKey}, 查找顺序: {chain}, 最终使用: {filePath}");
                }
                else
                {
                    GunLogger.Debug($"[MeleeAttack] 查找顺序: {chain}, 最终使用: {filePath}");
                }

                try { if (!RuntimeManager.IsInitialized) { GunLogger.Info($"[MeleeAttack] FMOD 未初始化，跳过自定义音效"); return; } } catch { }

                // 静音原 Studio 事件（若存在）
                try { if (__result.HasValue) { var ev = __result.Value; ev.setVolume(0f); } } catch { }

                // 继承原事件 3D 距离（若可用）
                float min = 1f, max = 50f;
                try
                {
                    if (__result.HasValue && __result.Value.isValid())
                    {
                        if (__result.Value.getDescription(out FMOD.Studio.EventDescription desc) == RESULT.OK)
                        {
                            try { desc.getMinMaxDistance(out min, out max); } catch { }
                        }
                    }
                }
                catch { }

                // Determine 2D/3D from original event description
                bool is3D = true;
                try
                {
                    if (__result.HasValue && __result.Value.isValid())
                    {
                        if (__result.Value.getDescription(out FMOD.Studio.EventDescription d2) == RESULT.OK)
                        {
                            bool tmp3D = true;
                            try { d2.is3D(out tmp3D); } catch { }
                            is3D = tmp3D;
                        }
                    }
                }
                catch { }

                // 选择合适的创建模式（mp3/ogg 用 STREAM）
                string fullPath = filePath;
                try { fullPath = System.IO.Path.GetFullPath(filePath); } catch { }
                string ext = null; try { ext = Path.GetExtension(fullPath)?.ToLowerInvariant(); } catch { }
                var baseMode = MODE.LOOP_OFF;
                if (is3D) baseMode |= MODE._3D | MODE._3D_LINEARROLLOFF;
                var mode = ((ext == ".mp3" || ext == ".ogg" || ext == ".oga") ? MODE.CREATESTREAM : MODE.CREATESAMPLE) | baseMode;
                var r1 = RuntimeManager.CoreSystem.createSound(fullPath, mode, out Sound sound);
                if (r1 != RESULT.OK || !sound.hasHandle())
                {
                    GunLogger.Info($"[MeleeAttack] createSound 失败({r1})，跳过自定义音效");
                    return;
                }
                if (is3D) try { sound.set3DMinMaxDistance(min, max); } catch { }

                ChannelGroup group = default;
                try { if (ModBehaviour.SfxGroup.hasHandle()) group = ModBehaviour.SfxGroup; } catch { }
                if (!group.hasHandle())
                {
                    try { var sfxBus = RuntimeManager.GetBus("bus:/Master/SFX"); if (sfxBus.getChannelGroup(out var cg) == RESULT.OK && cg.hasHandle()) group = cg; } catch { }
                    if (!group.hasHandle()) { try { var sfxBusAlt = RuntimeManager.GetBus("bus:/SFX"); if (sfxBusAlt.getChannelGroup(out var cg2) == RESULT.OK && cg2.hasHandle()) group = cg2; } catch { } }
                    if (!group.hasHandle()) { try { RuntimeManager.CoreSystem.getMasterChannelGroup(out group); } catch { } }
                }

                var r2 = RuntimeManager.CoreSystem.playSound(sound, group, true, out Channel channel);
                if (r2 != RESULT.OK || !channel.hasHandle())
                {
                    GunLogger.Info($"[MeleeAttack] playSound 失败({r2})，跳过自定义音效");
                    try { if (sound.hasHandle()) sound.release(); } catch { }
                    return;
                }

                Vector3 pos = Vector3.zero;
                try { pos = melee?.transform?.position ?? gameObject?.transform?.position ?? Vector3.zero; } catch { }
                var fpos = new FMOD.VECTOR { x = pos.x, y = pos.y, z = pos.z };
                var fvel = new FMOD.VECTOR { x = 0, y = 0, z = 0 };
                if (is3D)
                {
                    try { channel.set3DAttributes(ref fpos, ref fvel); } catch { }
                    try { channel.set3DMinMaxDistance(min, max); } catch { }
                    try { channel.setMode(MODE._3D | MODE._3D_LINEARROLLOFF | MODE.LOOP_OFF); } catch { }
                }
                try { channel.setPaused(false); } catch { }

                GunLogger.Debug($"[MeleeAttack] 覆盖 {eventName} → {Path.GetFileName(filePath)} @ ({pos.x:F1},{pos.y:F1},{pos.z:F1}), 模式={(is3D ? "3D" : "2D")} ");
                try { ModBehaviour.Instance?.StartCoroutine(FollowAndCleanup(melee?.transform ?? gameObject?.transform, is3D, sound, channel, 6f)); } catch { }
            }
            catch (Exception ex)
            {
                GunLogger.Warn($"[MeleeAttack] Postfix 异常: {ex.Message}");
            }
        }

        private static System.Collections.IEnumerator FollowAndCleanup(Transform follow, bool is3D, Sound sound, Channel channel, float maxSec)
        {
            float end = Time.realtimeSinceStartup + Mathf.Max(1f, maxSec);
            try
            {
                while (Time.realtimeSinceStartup < end)
                {
                    bool playing = false;
                    try { if (channel.hasHandle()) channel.isPlaying(out playing); } catch { }
                    if (!playing) break;

                    if (is3D)
                    {
                        Vector3 pos = Vector3.zero;
                        try { if (follow != null) pos = follow.position; } catch { }
                        var fpos = new FMOD.VECTOR { x = pos.x, y = pos.y, z = pos.z };
                        var fvel = new FMOD.VECTOR { x = 0, y = 0, z = 0 };
                        try { channel.set3DAttributes(ref fpos, ref fvel); } catch { }
                    }

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

    // 可选：保留一个轻量监控（不阻止原事件）
    [HarmonyPatch(typeof(AudioManager))]
    public static class AudioManager_Post_MeleeAttackMonitor
    {
        private const string MeleeAttackPrefix = "SFX/Combat/Melee/attack_";

        [HarmonyPatch("Post", new Type[] { typeof(string), typeof(GameObject) })]
        [HarmonyPrefix]
        public static void MonitorOnly(string eventName, GameObject gameObject)
        {
            try
            {
                if (string.IsNullOrEmpty(eventName)) return;
                if (!eventName.StartsWith(MeleeAttackPrefix, StringComparison.OrdinalIgnoreCase)) return;
                string soundKey = eventName.Substring(MeleeAttackPrefix.Length);
                if (string.IsNullOrWhiteSpace(soundKey)) return;
                Vector3 pos = Vector3.zero; try { pos = gameObject?.transform?.position ?? Vector3.zero; } catch { }
                GunLogger.Info($"[MeleeAttackMonitor] 捕获事件: \"{eventName}\" (key=\"{soundKey}\") @ ({pos.x:F1},{pos.y:F1},{pos.z:F1})");
            }
            catch (Exception ex)
            {
                GunLogger.Warn($"[MeleeAttackMonitor] 监控异常: {ex.Message}");
            }
        }
    }
}