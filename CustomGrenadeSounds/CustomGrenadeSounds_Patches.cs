using HarmonyLib;
using System;
using UnityEngine;
using Duckov;

using System.IO;
using System.Collections;
using System.Collections.Generic;
using FMOD;
using FMODUnity;
using DuckovCustomSounds;
using ItemStatsSystem; // for SkillReleaseContext

namespace DuckovCustomSounds.CustomGrenadeSounds
{
    // 1) 监控手雷掷出
    [HarmonyPatch(typeof(Grenade))]
    public static class Grenade_Launch_Monitor
    {
        [HarmonyPatch("Launch",
            new Type[] { typeof(Vector3), typeof(Vector3), typeof(CharacterMainControl), typeof(bool) })]
        [HarmonyPostfix]
        public static void Postfix(Grenade __instance, Vector3 startPoint, Vector3 velocity,
            CharacterMainControl fromCharacter, bool canHurtSelf)
        {
            try
            {
                if (__instance == null) return;
                int id = __instance.GetInstanceID();
                bool hasCollide = false;
                string collidePath = null;
                try
                {
                    hasCollide = __instance.hasCollideSound;
                }
                catch
                {
                }

                try
                {
                    collidePath = __instance.collideSound;
                }
                catch
                {
                }

                GrenadeLogger.Info(
                    $"[GrenadeMonitor] 手雷掷出 - 实例: Grenade_{id}, 时间: {Time.time:F2}s, hasCollideSound: {hasCollide}, collideSound: \"{(collidePath ?? string.Empty)}\"");
            }
            catch (Exception ex)
            {
                GrenadeLogger.Warn($"[GrenadeMonitor] 监控异常: {ex.Message}");
            }
        }
    }

    // 2) 监控手雷碰撞/弹跳
    [HarmonyPatch(typeof(Grenade))]
    public static class Grenade_OnCollisionEnter_Monitor
    {
        [HarmonyPatch("OnCollisionEnter", new Type[] { typeof(Collision) })]
        [HarmonyPostfix]
        public static void Postfix(Grenade __instance, Collision collision)
        {
            try
            {
                if (__instance == null) return;
                bool hasCollide = false;
                string collidePath = null;
                try
                {
                    hasCollide = __instance.hasCollideSound;
                }
                catch
                {
                }

                try
                {
                    collidePath = __instance.collideSound;
                }
                catch
                {
                }

                Vector3 pos = Vector3.zero;
                try
                {
                    pos = __instance.transform.position;
                }
                catch
                {
                }

                GrenadeLogger.Info(
                    $"[GrenadeMonitor] 手雷碰撞 - 音效启用: {hasCollide}, 音效路径: \"{(collidePath ?? string.Empty)}\", 位置: ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})");
            }
            catch (Exception ex)
            {
                GrenadeLogger.Warn($"[GrenadeMonitor] 监控异常: {ex.Message}");
            }
        }
    }

    // 3) 监控手雷爆炸（ExplosionManager.CreateExplosion）
    [HarmonyPatch(typeof(ExplosionManager))]
    public static class ExplosionManager_CreateExplosion_Monitor
    {
        [HarmonyPatch("CreateExplosion",
            new Type[]
            {
                typeof(Vector3), typeof(float), typeof(DamageInfo), typeof(ExplosionFxTypes), typeof(float),
                typeof(bool)
            })]
        [HarmonyPostfix]
        public static void Postfix(ExplosionManager __instance, Vector3 center, float radius, DamageInfo dmgInfo,
            ExplosionFxTypes fxType, float shakeStrength, bool canHurtSelf)
        {
            try
            {
                string prefabType = fxType.ToString();
                string prefabName = null;
                try
                {
                    switch (fxType)
                    {
                        case ExplosionFxTypes.normal:
                            prefabName = (__instance != null && __instance.normalFxPfb != null)
                                ? __instance.normalFxPfb.name
                                : null;
                            break;
                        case ExplosionFxTypes.flash:
                            prefabName = (__instance != null && __instance.flashFxPfb != null)
                                ? __instance.flashFxPfb.name
                                : null;
                            break;
                        default:
                            // custom / others
                            if (__instance != null)
                                prefabName = (__instance.normalFxPfb != null)
                                    ? __instance.normalFxPfb.name
                                    : (__instance.flashFxPfb != null ? __instance.flashFxPfb.name : null);
                            break;
                    }
                }
                catch
                {
                    /* ignore */
                }

                GrenadeLogger.Info(
                    $"[GrenadeMonitor] 手雷爆炸 - 类型: {prefabType}, 中心: ({center.x:F1}, {center.y:F1}, {center.z:F1}), 预制体: {prefabType}{(string.IsNullOrEmpty(prefabName) ? string.Empty : $"({prefabName})")}");
            }
            catch (Exception ex)
            {
                GrenadeLogger.Warn($"[GrenadeMonitor] 监控异常: {ex.Message}");
            }
        }
    }

    // 4) 监控技能释放音效（包括手雷掷出音效）
    [HarmonyPatch(typeof(SkillBase))]
    public static class SkillBase_ReleaseSkill_Monitor
    {
        [HarmonyPatch("ReleaseSkill", new Type[] { typeof(SkillReleaseContext), typeof(CharacterMainControl) })]
        [HarmonyPrefix]
        public static void Prefix(SkillBase __instance, SkillReleaseContext releaseContext, CharacterMainControl from)
        {
            try
            {
                if (__instance == null) return;

                bool hasReleaseSound = false;
                string soundEventName = null;
                string skillType = __instance.GetType().Name;

                try { hasReleaseSound = __instance.hasReleaseSound; } catch { }
                try { soundEventName = __instance.onReleaseSound; } catch { }

                Vector3 charPos = Vector3.zero;
                try { charPos = from.transform.position; } catch { }

                GrenadeLogger.Info($"[SkillMonitor] 技能释放 - 类型: {skillType}, 音效启用: {hasReleaseSound}, 音效事件: \"{(soundEventName ?? "null")}\", 位置: ({charPos.x:F1}, {charPos.y:F1}, {charPos.z:F1})");
            }
            catch (Exception ex)
            {
                GrenadeLogger.Warn($"[SkillMonitor] 技能释放监控异常: {ex.Message}");
            }
        }
    }

    // 5) 专门监控手雷技能释放
    [HarmonyPatch(typeof(Skill_Grenade))]
    public static class Skill_Grenade_OnRelease_Monitor
    {
        [HarmonyPatch("OnRelease")]
        [HarmonyPrefix]
        public static void Prefix(Skill_Grenade __instance)
        {
            try
            {
                if (__instance == null) return;

                bool hasReleaseSound = false;
                string soundEventName = null;
                string fromCharacterName = "Unknown";
                Vector3 position = Vector3.zero;

                try { hasReleaseSound = __instance.hasReleaseSound; } catch { }
                try { soundEventName = __instance.onReleaseSound; } catch { }

                // 使用反射访问 protected 字段 fromCharacter
                try
                {
                    var fromCharacterField = typeof(SkillBase).GetField("fromCharacter",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (fromCharacterField != null)
                    {
                        var fromCharacter = fromCharacterField.GetValue(__instance) as CharacterMainControl;
                        if (fromCharacter != null)
                        {
                            fromCharacterName = fromCharacter.name ?? "Unknown";
                            position = fromCharacter.transform.position;
                        }
                    }
                }
                catch { }

                GrenadeLogger.Info($"[GrenadeSkillMonitor] 手雷技能释放 - 音效启用: {hasReleaseSound}, 音效事件: \"{(soundEventName ?? "null")}\", 角色: {fromCharacterName}, 位置: ({position.x:F1}, {position.y:F1}, {position.z:F1})");
            }
            catch (Exception ex)
            {
                GrenadeLogger.Warn($"[GrenadeSkillMonitor] 手雷技能释放监控异常: {ex.Message}");
            }
        }
    }

    // 6) 通用监控所有 AudioManager.Post 调用
    [HarmonyPatch(typeof(AudioManager))]
    public static class AudioManager_Post_Monitor
    {
        private static int callCount = 0;

        [HarmonyPatch("Post", new Type[] { typeof(string), typeof(GameObject) })]
        [HarmonyPrefix]
        public static void Prefix(string eventName, GameObject gameObject)
        {
            try
            {
                callCount++;
                string objName = gameObject?.name ?? "null";
                string objType = "Unknown";

                if (gameObject != null)
                {
                    if (gameObject.GetComponent<Grenade>() != null)
                        objType = "Grenade";
                    else if (gameObject.GetComponent<CharacterMainControl>() != null)
                        objType = "Character";
                    else if (gameObject.GetComponent<Skill_Grenade>() != null)
                        objType = "Skill_Grenade";
                }

                Vector3 pos = Vector3.zero;
                try { pos = gameObject?.transform.position ?? Vector3.zero; } catch { }

                // GrenadeLogger.Info($"[AudioMonitor] #{callCount} Post调用 - 事件: \"{eventName}\", 对象: {objName}({objType}), 位置: ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})");
            }
            catch (Exception ex)
            {
                GrenadeLogger.Warn($"[AudioMonitor] AudioManager.Post 监控异常: {ex.Message}");
            }
        }
    }

    // 7) 监控所有 SFX/Combat/Explosive/ 下的音效（包括可能的掷出手雷音效）
    [HarmonyPatch(typeof(AudioManager))]
    public static class AudioManager_Post_Explosive_Monitor
    {
        private static int explosiveCallCount = 0;

        [HarmonyPatch("Post", new Type[] { typeof(string), typeof(GameObject) })]
        [HarmonyPrefix]
        public static void Prefix(string eventName, GameObject gameObject)
        {
            try
            {
                // 只监控 SFX/Combat/Explosive/ 下的音效
                if (!eventName.StartsWith("SFX/Combat/Explosive/", StringComparison.OrdinalIgnoreCase))
                    return;

                explosiveCallCount++;
                string objName = gameObject?.name ?? "null";
                string objType = "Unknown";

                if (gameObject != null)
                {
                    if (gameObject.GetComponent<Grenade>() != null)
                        objType = "Grenade";
                    else if (gameObject.GetComponent<CharacterMainControl>() != null)
                        objType = "Character";
                    else if (gameObject.GetComponent<Skill_Grenade>() != null)
                        objType = "Skill_Grenade";
                }

                Vector3 pos = Vector3.zero;
                try { pos = gameObject?.transform.position ?? Vector3.zero; } catch { }

                GrenadeLogger.Info($"[ExplosiveMonitor] #{explosiveCallCount} 爆炸音效 - 事件: \"{eventName}\", 对象: {objName}({objType}), 位置: ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})");
            }
            catch (Exception ex)
            {
                GrenadeLogger.Warn($"[ExplosiveMonitor] 爆炸音效监控异常: {ex.Message}");
            }
        }
    }
}

namespace DuckovCustomSounds.CustomGrenadeSounds
{
    internal static class GrenadeSfxHelpers
    {
        public static ChannelGroup ResolveSfxGroupSafe()
        {
            try
            {
                try
                {
                    // Try common SFX bus first
                    var sfxBus = RuntimeManager.GetBus("bus:/Master/SFX");
                    if (sfxBus.getChannelGroup(out var cg) == RESULT.OK && cg.hasHandle())
                        return cg;
                }
                catch { }
                try
                {
                    var sfxBusAlt = RuntimeManager.GetBus("bus:/SFX");
                    if (sfxBusAlt.getChannelGroup(out var cg2) == RESULT.OK && cg2.hasHandle())
                        return cg2;
                }
                catch { }

                if (RuntimeManager.CoreSystem.getMasterChannelGroup(out var master) == RESULT.OK && master.hasHandle())
                    return master;
            }
            catch { }
            return default;
        }

        public static FMOD.VECTOR ToFMODVector(Vector3 v) => new FMOD.VECTOR { x = v.x, y = v.y, z = v.z };

        public static IEnumerator CleanupAfterPlay(Sound sound, Channel channel, float maxDurationSec = 8f)
        {
            float deadline = Time.realtimeSinceStartup + Mathf.Max(1f, maxDurationSec);
            try
            {
                while (Time.realtimeSinceStartup < deadline)
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

    [HarmonyPatch(typeof(AudioManager))]
    public static class AudioManager_Post_GrenadeUnifiedReplace
    {
        [HarmonyPatch("Post", new Type[] { typeof(string), typeof(GameObject) })]
        [HarmonyPrefix]
        public static bool Prefix(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
        {
            try
            {
                if (string.IsNullOrEmpty(eventName)) return true;

                string fileName = null;
                float min = 1f, max = 30f;

                // 精确匹配三种手雷相关事件（不区分大小写）
                // 注意：可能还有其他变种未被处理，例如 throw_pipe 等
                if (string.Equals(eventName, "SFX/Combat/Explosive/throw_grenade", StringComparison.OrdinalIgnoreCase))
                {
                    fileName = "throw.mp3"; min = 1f; max = 20f;
                }
                else if (string.Equals(eventName, "SFX/Combat/Explosive/GrenadeFall", StringComparison.OrdinalIgnoreCase))
                {
                    fileName = "collide.mp3"; min = 1f; max = 30f;
                }
                else if (string.Equals(eventName, "SFX/Combat/Explosive/explode_grenade", StringComparison.OrdinalIgnoreCase))
                {
                    fileName = "explode.mp3"; min = 2f; max = 60f;
                }
                else
                {
                    return true; // 不是我们处理的手雷事件 → 允许原始事件播放
                }

                string filePath = Path.Combine(ModBehaviour.ModFolderName, "CustomGrenadeSounds", fileName);
                if (!File.Exists(filePath))
                {
                    GrenadeLogger.Info($"[GrenadeSound] 自定义文件缺失，放行原始事件: {eventName} (路径: {filePath})");
                    return true;
                }
                try { if (!RuntimeManager.IsInitialized) { GrenadeLogger.Info($"[GrenadeSound] FMOD 未初始化，放行原始事件: {eventName}"); return true; } } catch { }

                // 通过 Core API 播放自定义 3D 音效，路由到 SFX→Master
                var mode = MODE.CREATESAMPLE | MODE._3D | MODE.LOOP_OFF;
                var r1 = RuntimeManager.CoreSystem.createSound(filePath, mode, out Sound sound);
                if (r1 != RESULT.OK || !sound.hasHandle())
                {
                    GrenadeLogger.Info($"[GrenadeSound] createSound 失败({r1})，放行原始事件: {eventName}");
                    return true;
                }

                try { sound.set3DMinMaxDistance(min, max); } catch { }

                var group = GrenadeSfxHelpers.ResolveSfxGroupSafe();
                var r2 = RuntimeManager.CoreSystem.playSound(sound, group, true, out Channel channel);
                if (r2 != RESULT.OK || !channel.hasHandle())
                {
                    GrenadeLogger.Info($"[GrenadeSound] playSound 失败({r2})，放行原始事件: {eventName}");
                    try { if (sound.hasHandle()) sound.release(); } catch { }
                    return true;
                }

                Vector3 pos = Vector3.zero;
                try { pos = gameObject?.transform?.position ?? Vector3.zero; } catch { }
                var fpos = GrenadeSfxHelpers.ToFMODVector(pos);
                var fvel = GrenadeSfxHelpers.ToFMODVector(Vector3.zero);
                try { channel.set3DAttributes(ref fpos, ref fvel); } catch { }
                try { channel.setPaused(false); } catch { }

                GrenadeLogger.Debug($"[GrenadeSound] 替换 {eventName} → 播放自定义音效（SFX→Master, 3D） @ ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})");

                try { ModBehaviour.Instance?.StartCoroutine(GrenadeSfxHelpers.CleanupAfterPlay(sound, channel, 8f)); } catch { }

                __result = new FMOD.Studio.EventInstance?();
                return false; // 阻止原始事件播放
            }
            catch (Exception ex)
            {
                GrenadeLogger.Info($"[GrenadeSound] Prefix 异常，放行原始事件: {ex.Message}");
                return true;
            }
        }
    }
}

