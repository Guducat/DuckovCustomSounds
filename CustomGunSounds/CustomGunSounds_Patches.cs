using HarmonyLib;
using Duckov; // AudioManager
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FMOD;
using FMODUnity;

namespace DuckovCustomSounds.CustomGunSounds
{
    // 主要补丁：拦截 枪械射击 SFX 并替换为自定义 3D 音效
    [HarmonyPatch(typeof(AudioManager))]
    public static class AudioManager_Post_GunShootReplace
    {
        private const string ShootPrefix = "SFX/Combat/Gun/Shoot/";

        [HarmonyPatch("Post", new Type[] { typeof(string), typeof(GameObject) })]
        [HarmonyPrefix]
        public static bool Prefix(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
        {
            try
            {
                if (string.IsNullOrEmpty(eventName)) return true;
                if (!eventName.StartsWith(ShootPrefix, StringComparison.OrdinalIgnoreCase))
                    return true; // 非射击事件 → 放行

                // 解析 soundkey
                string soundKey = eventName.Substring(ShootPrefix.Length);
                if (string.IsNullOrWhiteSpace(soundKey)) return true;

                // 双层查找策略：若可获取到 ItemAgent_Gun，则优先按 TypeID（含 _mute 变体）→ 再按 soundKey → 最后 default.mp3
                // 若无法获取到 ItemAgent_Gun，则回退为：soundKey → default.mp3
                string dir = Path.Combine(ModBehaviour.ModFolderName, "CustomGunSounds");

                // 记录旧逻辑首选路径（用于保留原有未找到时的日志输出）
                string legacySoundKeyPath = Path.Combine(dir, soundKey + ".mp3");

                // 捕获枪械组件（安全 null 检查 + 向父节点回溯）
                ItemAgent_Gun gun = null;
                try
                {
                    if (gameObject != null)
                        gun = gameObject.GetComponent<ItemAgent_Gun>() ?? gameObject.GetComponentInParent<ItemAgent_Gun>();
                }
                catch { }

                string typeIdStr = string.Empty;
                bool silenced = false;
                bool silencedByKey = soundKey.EndsWith("_mute", StringComparison.OrdinalIgnoreCase);
                try { if (gun != null) { silenced = gun.Silenced; } } catch { }
                if (silencedByKey) silenced = true; // 以事件名包含 _mute 为准，确保一致
                try { if (gun?.Item != null) typeIdStr = gun.Item.TypeID.ToString(); } catch { }

                var attempts = new List<string>();
                if (gun != null && !string.IsNullOrWhiteSpace(typeIdStr))
                {
                    if (silenced) attempts.Add(Path.Combine(dir, typeIdStr + "_mute.mp3"));
                    attempts.Add(Path.Combine(dir, typeIdStr + ".mp3"));
                    attempts.Add(legacySoundKeyPath); // 含 _mute 后缀的 soundKey（由事件名给出）

                    // 详细日志：TypeID、soundKey、Silenced 与完整查找链条
                    string chain = string.Join(" → ", attempts.Select(p => $"[{p}]").ToArray());
                    GunLogger.Debug($"[GunShoot] TypeID={typeIdStr}, soundKey={soundKey}, Silenced={silenced}, 查找顺序: {chain}");
                }
                else
                {
                    GunLogger.Debug($"[GunShoot] 未捕获到 ItemAgent_Gun 组件，回退到 soundKey 查找模式");
                    attempts.Add(legacySoundKeyPath);
                }

                string filePath = null;
                foreach (var p in attempts)
                {
                    if (File.Exists(p)) { filePath = p; break; }
                }
                if (filePath == null)
                {
                    string fallback = Path.Combine(dir, "default.mp3");
                    if (File.Exists(fallback))
                    {
                        filePath = fallback;
                        // 在成功命中 default 回退时，也补足最终选择日志
                        string chain = string.Join(" → ", attempts.Select(p => $"[{p}]").ToArray());
                        if (gun != null && !string.IsNullOrWhiteSpace(typeIdStr))
                            GunLogger.Debug($"[GunShoot] TypeID={typeIdStr}, soundKey={soundKey}, Silenced={silenced}, 查找顺序: {chain}, 最终使用: {filePath}");
                    }
                    else
                    {
                        // 兼容：保留原有未找到日志
                        GunLogger.Debug($"[GunShoot] 未找到自定义文件，放行原始事件: {eventName} (查找: {legacySoundKeyPath})");
                        if (gun != null && !string.IsNullOrWhiteSpace(typeIdStr))
                        {
                            string chain = string.Join(" → ", attempts.Select(p => $"[{p}]").ToArray());
                            GunLogger.Debug($"[GunShoot] TypeID={typeIdStr}, soundKey={soundKey}, Silenced={silenced}, 查找顺序: {chain}, 最终使用: 未找到自定义文件");
                        }
                        return true;
                    }
                }
                else
                {
                    // 命中自定义文件时输出“最终使用”日志（在已有“替换 ... → ...”日志之外，保留）
                    if (gun != null && !string.IsNullOrWhiteSpace(typeIdStr))
                    {
                        string chain = string.Join(" → ", attempts.Select(p => $"[{p}]").ToArray());
                        GunLogger.Debug($"[GunShoot] TypeID={typeIdStr}, soundKey={soundKey}, Silenced={silenced}, 查找顺序: {chain}, 最终使用: {filePath}");
                    }
                }

                try { if (!RuntimeManager.IsInitialized) { GunLogger.Info($"[GunShoot] FMOD 未初始化，放行原始事件: {eventName}"); return true; } } catch { }

                // 播放自定义 3D 声音，路由到 SFX→Master
                var mode = MODE.CREATESAMPLE | MODE._3D | MODE.LOOP_OFF;
                var r1 = RuntimeManager.CoreSystem.createSound(filePath, mode, out Sound sound);
                if (r1 != RESULT.OK || !sound.hasHandle())
                {
                    GunLogger.Info($"[GunShoot] createSound 失败({r1})，放行原始事件: {eventName}");
                    return true;
                }

                try { sound.set3DMinMaxDistance(1f, 50f); } catch { }

                // 解析 SFX 声道组（回退到 Master）
                ChannelGroup group = default;
                try
                {
                    // 优先用已缓存的总线
                    if (ModBehaviour.SfxGroup.hasHandle()) group = ModBehaviour.SfxGroup;
                }
                catch { }
                if (!group.hasHandle())
                {
                    try
                    {
                        var sfxBus = RuntimeManager.GetBus("bus:/Master/SFX");
                        if (sfxBus.getChannelGroup(out var cg) == RESULT.OK && cg.hasHandle()) group = cg;
                    }
                    catch { }
                    if (!group.hasHandle())
                    {
                        try
                        {
                            var sfxBusAlt = RuntimeManager.GetBus("bus:/SFX");
                            if (sfxBusAlt.getChannelGroup(out var cg2) == RESULT.OK && cg2.hasHandle()) group = cg2;
                        }
                        catch { }
                    }
                    if (!group.hasHandle())
                    {
                        try { RuntimeManager.CoreSystem.getMasterChannelGroup(out group); } catch { }
                    }
                }

                var r2 = RuntimeManager.CoreSystem.playSound(sound, group, true, out Channel channel);
                if (r2 != RESULT.OK || !channel.hasHandle())
                {
                    GunLogger.Info($"[GunShoot] playSound 失败({r2})，放行原始事件: {eventName}");
                    try { if (sound.hasHandle()) sound.release(); } catch { }
                    return true;
                }

                Vector3 pos = Vector3.zero;
                try { pos = gameObject?.transform?.position ?? Vector3.zero; } catch { }
                var fpos = new FMOD.VECTOR { x = pos.x, y = pos.y, z = pos.z };
                var fvel = new FMOD.VECTOR { x = 0, y = 0, z = 0 };
                try { channel.set3DAttributes(ref fpos, ref fvel); } catch { }
                try { channel.setPaused(false); } catch { }

                GunLogger.Debug($"[GunShoot] 替换 {eventName} → {Path.GetFileName(filePath)} @ ({pos.x:F1},{pos.y:F1},{pos.z:F1})");

                // 轻量清理：延后释放 Sound（不持有 Channel 回调，避免复杂性）
                try { ModBehaviour.Instance?.StartCoroutine(Cleanup(sound, channel, 6f)); } catch { }

                __result = new FMOD.Studio.EventInstance?();
                return false; // 阻止原始事件播放
            }
            catch (Exception ex)
            {
                GunLogger.Warn($"[GunShoot] Prefix 异常，放行原始事件: {ex.Message}");
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

    // 预置占位：换弹 / 上膛 等（当前不启用补丁，仅提供调试方法；如需启用请添加 HarmonyPatch 特性）
    internal static class GunReloadPatch_Template
    {
        // [HarmonyPatch(typeof(AudioManager))]
        // [HarmonyPatch("Post", new Type[] { typeof(string), typeof(GameObject) })]
        // [HarmonyPrefix]
        public static bool DebugOnly_LogReload(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
        {
            if (string.IsNullOrEmpty(eventName)) return true;
            if (eventName.StartsWith("SFX/Combat/Gun/Reload/", StringComparison.OrdinalIgnoreCase))
            {
                GunLogger.Debug($"[GunReload:Debug] 捕获 Reload 事件: {eventName}");
            }
            return true; // 放行
        }
    }

    internal static class GunChamberPatch_Template
    {
        // [HarmonyPatch(typeof(AudioManager))]
        // [HarmonyPatch("Post", new Type[] { typeof(string), typeof(GameObject) })]
        // [HarmonyPrefix]
        public static bool DebugOnly_LogChamber(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
        {
            if (string.IsNullOrEmpty(eventName)) return true;
            if (eventName.Contains("Gun") && eventName.Contains("chamber"))
            {
                GunLogger.Debug($"[GunChamber:Debug] 捕获可能的上膛事件: {eventName}");
            }
            return true; // 放行
        }
    }
}

