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

        [HarmonyPatch("Post", new Type[] { typeof(string), typeof(GameObject) })]
        [HarmonyPrefix]
        public static bool Prefix(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
        {
            try
            {
                if (string.IsNullOrEmpty(eventName)) return true;
                if (!eventName.StartsWith(MeleeAttackPrefix, StringComparison.OrdinalIgnoreCase)) return true; // 非近战攻击事件 → 放行

                // 解析 soundKey（CA_Attack 里为 attack_ + meleeWeapon.SoundKey.ToLower()）
                string soundKey = eventName.Substring(MeleeAttackPrefix.Length);
                if (string.IsNullOrWhiteSpace(soundKey)) return true;

                string dir = Path.Combine(ModBehaviour.ModFolderName, "CustomMeleeSounds");
                string legacyPath = Path.Combine(dir, soundKey + ".mp3");

                // 捕获近战组件，优先尝试按 TypeID 定位资源，其次回退到 soundKey，最后 default.mp3
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
                    attempts.Add(Path.Combine(dir, typeIdStr + ".mp3"));
                    attempts.Add(legacyPath);
                    string chain = string.Join(" → ", attempts.Select(p => $"[{p}]").ToArray());
                    GunLogger.Debug($"[MeleeAttack] TypeID={typeIdStr}, soundKey={soundKey}, 查找顺序: {chain}");
                }
                else
                {
                    GunLogger.Debug($"[MeleeAttack] 未捕获到 ItemAgent_MeleeWeapon，回退到 soundKey 查找模式");
                    attempts.Add(legacyPath);
                }

                string filePath = null;
                foreach (var p in attempts) { if (File.Exists(p)) { filePath = p; break; } }
                if (filePath == null)
                {
                    string fallback = Path.Combine(dir, "default.mp3");
                    if (File.Exists(fallback))
                    {
                        filePath = fallback;
                        if (!string.IsNullOrWhiteSpace(typeIdStr))
                        {
                            string chain = string.Join(" → ", attempts.Select(p => $"[{p}]").ToArray());
                            GunLogger.Debug($"[MeleeAttack] TypeID={typeIdStr}, soundKey={soundKey}, 查找顺序: {chain}, 最终使用: {filePath}");
                        }
                    }
                    else
                    {
                        GunLogger.Debug($"[MeleeAttack] 未找到自定义文件，放行原始事件: {eventName} (查找: {legacyPath})");
                        return true;
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(typeIdStr))
                    {
                        string chain = string.Join(" → ", attempts.Select(p => $"[{p}]").ToArray());
                        GunLogger.Debug($"[MeleeAttack] TypeID={typeIdStr}, soundKey={soundKey}, 查找顺序: {chain}, 最终使用: {filePath}");
                    }
                }

                try { if (!RuntimeManager.IsInitialized) { GunLogger.Info($"[MeleeAttack] FMOD 未初始化，放行原始事件: {eventName}"); return true; } } catch { }

                var mode = MODE.CREATESAMPLE | MODE._3D | MODE.LOOP_OFF;
                var r1 = RuntimeManager.CoreSystem.createSound(filePath, mode, out Sound sound);
                if (r1 != RESULT.OK || !sound.hasHandle())
                {
                    GunLogger.Info($"[MeleeAttack] createSound 失败({r1})，放行原始事件: {eventName}");
                    return true;
                }
                try { sound.set3DMinMaxDistance(1f, 50f); } catch { }

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
                    GunLogger.Info($"[MeleeAttack] playSound 失败({r2})，放行原始事件: {eventName}");
                    try { if (sound.hasHandle()) sound.release(); } catch { }
                    return true;
                }

                Vector3 pos = Vector3.zero;
                try { pos = melee?.transform?.position ?? gameObject?.transform?.position ?? Vector3.zero; } catch { }
                var fpos = new FMOD.VECTOR { x = pos.x, y = pos.y, z = pos.z };
                var fvel = new FMOD.VECTOR { x = 0, y = 0, z = 0 };
                try { channel.set3DAttributes(ref fpos, ref fvel); } catch { }
                try { channel.setPaused(false); } catch { }

                GunLogger.Debug($"[MeleeAttack] 替换 {eventName} → {Path.GetFileName(filePath)} @ ({pos.x:F1},{pos.y:F1},{pos.z:F1})");
                try { ModBehaviour.Instance?.StartCoroutine(Cleanup(sound, channel, 6f)); } catch { }

                __result = new FMOD.Studio.EventInstance?();
                return false; // 阻止原事件
            }
            catch (Exception ex)
            {
                GunLogger.Warn($"[MeleeAttack] Prefix 异常，放行原始事件: {ex.Message}");
                return true;
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