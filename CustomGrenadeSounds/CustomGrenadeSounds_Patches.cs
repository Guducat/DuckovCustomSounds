using HarmonyLib;
using System;
using System.IO;
using System.Linq;
using Duckov;
using UnityEngine;

namespace DuckovCustomSounds.CustomGrenadeSounds
{
    /// <summary>
    /// 手雷与爆炸物音效替换。
    /// </summary>
    [HarmonyPatch(typeof(AudioManager))]
    public static class AudioManager_Post_GrenadeReplace
    {
        private const string GrenadePrefix = "SFX/Combat/Explosive/";

        [HarmonyPatch("Post", new Type[] { typeof(string), typeof(GameObject) })]
        [HarmonyPostfix]
        public static void Postfix(ref FMOD.Studio.EventInstance? __result, string eventName, GameObject gameObject)
        {
            try
            {
                if (!GrenadeConfig.Enabled) return;
                if (string.IsNullOrEmpty(eventName)) return;
                if (!eventName.StartsWith(GrenadePrefix, StringComparison.OrdinalIgnoreCase)) return;

                var originalKey = eventName.Substring(GrenadePrefix.Length);
                if (string.IsNullOrWhiteSpace(originalKey)) return;

                ExplosionSoundContext.MarkObserved(originalKey);

                var baseDir = GrenadeSoundResolver.GetBaseDir();
                var context = ExplosionSoundContext.Current;
                if (!GrenadeSoundResolver.TryResolve(baseDir, context, originalKey, out var resolution))
                {
                    GrenadeLogger.Debug($"[Grenade] source={context.Source}, TypeID={context.TypeIdStr ?? "N/A"}, soundKey={originalKey}, 未找到自定义文件");
                    return;
                }

                if (GrenadeLogger.IsDebugEnabled)
                {
                    var chain = string.Join(" → ", resolution.Attempts.Select(p => $"[{Path.GetFileName(p)}]").ToArray());
                    GrenadeLogger.Debug($"[Grenade] source={resolution.Source}, TypeID={resolution.TypeIdStr ?? "N/A"}, soundKey={resolution.SoundKey}, fileBase={resolution.FileBase}, 查找: {chain}, 结果: {Path.GetFileName(resolution.FilePath)}");
                }

                ReplaceOriginalEvent(ref __result, eventName, gameObject, resolution.FilePath);
            }
            catch (Exception ex)
            {
                GrenadeLogger.Warning($"[Grenade] Postfix 覆盖异常: {ex.Message}");
            }
        }

        private static void ReplaceOriginalEvent(
            ref FMOD.Studio.EventInstance? result,
            string eventName,
            GameObject gameObject,
            string filePath)
        {
            try
            {
                var originalInstance = result;
                var minDistance = 0f;
                var maxDistance = 0f;
                var hasDistance = false;

                if (originalInstance.HasValue && originalInstance.Value.isValid())
                {
                    hasDistance = AudioDistanceHelper.TryExtractFromEventInstance(originalInstance.Value, out minDistance, out maxDistance);
                    try { originalInstance.Value.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); } catch { }
                    try { originalInstance.Value.release(); } catch { }
                }

                if (!hasDistance)
                {
                    hasDistance = AudioDistanceHelper.TryExtractFromEventName(eventName, out minDistance, out maxDistance);
                }

                result = AudioManager.PostCustomSFX(filePath, gameObject, loop: false);
                ApplyPlaybackSettings(result, hasDistance, minDistance, maxDistance);
                GrenadeLogger.Info($"[Grenade] 使用SFX接口播放: {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                GrenadeLogger.Warning($"[Grenade] 新接口播放失败: {ex.Message}");
            }
        }

        internal static void InjectIfMissing(ExplosionSoundFrame current)
        {
            try
            {
                if (!GrenadeConfig.Enabled) return;
                if (current.Source != ExplosionSoundSource.Grenade) return;
                if (!current.ObservedOriginalEvent)
                {
                    InjectMappedSound(current);
                }
            }
            catch (Exception ex)
            {
                GrenadeLogger.Warning($"[Grenade] 注入无原版事件音效失败: {ex.Message}");
            }
        }

        private static void InjectMappedSound(ExplosionSoundFrame current)
        {
            var targetKey = GrenadeSoundMap.ResolveForInjection(current.TypeIdStr);
            if (string.IsNullOrWhiteSpace(targetKey)) return;

            var baseDir = GrenadeSoundResolver.GetBaseDir();
            if (!GrenadeSoundResolver.TryResolve(baseDir, current, targetKey, out var resolution, applySoundMap: false))
            {
                GrenadeLogger.Debug($"[Grenade] 注入失败: source={current.Source}, TypeID={current.TypeIdStr ?? "N/A"}, soundKey={targetKey}, 未找到自定义文件");
                return;
            }

            var sourceObject = current.SourceObject;
            var filePath = resolution.FilePath;
            var instance = AudioManager.PostCustomSFX(filePath, sourceObject, loop: false);
            ApplyPlaybackSettings(instance, hasDistance: false, minDistance: 0f, maxDistance: 0f);

            if (GrenadeLogger.IsDebugEnabled)
            {
                GrenadeLogger.Debug($"[Grenade] 已注入无原版事件音效: source={resolution.Source}, TypeID={resolution.TypeIdStr ?? "N/A"}, soundKey={resolution.SoundKey}, fileBase={resolution.FileBase}, file={Path.GetFileName(filePath)}");
            }
            GrenadeLogger.Info($"[Grenade] 使用SFX接口注入: {Path.GetFileName(filePath)}");
        }

        private static void ApplyPlaybackSettings(
            FMOD.Studio.EventInstance? instance,
            bool hasDistance,
            float minDistance,
            float maxDistance)
        {
            if (!instance.HasValue || !instance.Value.isValid()) return;

            if (hasDistance)
            {
                try { AudioDistanceHelper.ApplyToEventInstance(instance.Value, minDistance, maxDistance); } catch { }
            }

            try
            {
                instance.Value.setVolume(GrenadeConfig.Volume);
                GrenadeLogger.Debug($"[Grenade] 已应用音量: {GrenadeConfig.Volume:F2}");
            }
            catch (Exception ex)
            {
                GrenadeLogger.Warning($"[Grenade] 设置音量失败: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(Grenade), "Explode")]
    internal static class Grenade_Explode_SoundContextPatch
    {
        private static void Prefix(Grenade __instance, out ExplosionSoundScope __state)
        {
            var typeIdStr = ReadTypeId(__instance);
            __state = ExplosionSoundContext.Enter(ExplosionSoundSource.Grenade, typeIdStr, __instance != null ? __instance.gameObject : null);
        }

        private static Exception? Finalizer(ExplosionSoundScope __state, Exception? __exception)
        {
            try
            {
                var current = __state.Current;
                if (current != null)
                {
                    AudioManager_Post_GrenadeReplace.InjectIfMissing(current);
                }
            }
            catch { }
            finally
            {
                __state.Restore();
            }

            return __exception;
        }

        private static string? ReadTypeId(Grenade grenade)
        {
            try
            {
                var typeId = grenade.damageInfo.fromWeaponItemID;
                return typeId > 0 ? typeId.ToString() : null;
            }
            catch
            {
                return null;
            }
        }
    }

    [HarmonyPatch(typeof(Breakable), "OnDead")]
    internal static class Breakable_OnDead_SoundContextPatch
    {
        private static void Prefix(Breakable __instance, out ExplosionSoundScope __state)
        {
            __state = ExplosionSoundContext.Enter(ExplosionSoundSource.Breakable, null, __instance != null ? __instance.gameObject : null);
        }

        private static Exception? Finalizer(ExplosionSoundScope __state, Exception? __exception)
        {
            __state.Restore();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(ExplosionProxy), "DoExplode")]
    internal static class ExplosionProxy_DoExplode_SoundContextPatch
    {
        private static void Prefix(ExplosionProxy __instance, out ExplosionSoundScope __state)
        {
            __state = ExplosionSoundContext.Enter(ExplosionSoundSource.Proxy, null, __instance != null ? __instance.gameObject : null);
        }

        private static Exception? Finalizer(ExplosionSoundScope __state, Exception? __exception)
        {
            __state.Restore();
            return __exception;
        }
    }
}
