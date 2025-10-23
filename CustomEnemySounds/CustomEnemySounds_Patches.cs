using HarmonyLib;
using System;
using UnityEngine;
using Duckov; // AudioManager, CharacterMainControl, AICharacterController
using FMOD;

namespace DuckovCustomSounds.CustomEnemySounds
{
    /// <summary>
    /// 用于捕获敌人上下文和路由语音播放的Harmony补丁。
    /// </summary>
    internal static class CustomEnemySounds_Patches
    {
        // 1) 在AI初始化时捕获敌人上下文
        [HarmonyPatch(typeof(AICharacterController))]
        [HarmonyPatch("Init", new Type[] { typeof(CharacterMainControl), typeof(Vector3), typeof(AudioManager.VoiceType), typeof(AudioManager.FootStepMaterialType) })]
        [HarmonyPostfix]
        private static void AI_Init_Postfix(CharacterMainControl _characterMainControl, Vector3 patrolCenter, AudioManager.VoiceType voiceType, AudioManager.FootStepMaterialType footStepMatType)
        {
            try
            {
                CESLogger.Debug("[CES:Hook] AICharacterController.Init Postfix ENTER");
                CustomEnemySounds.EnsureLoaded();
                if (_characterMainControl == null) return;
                EnemyContextRegistry.Register(_characterMainControl, voiceType, footStepMatType);
            }
            catch (Exception ex)
            {
                CESLogger.Error("AICharacterController.Init Postfix", ex);
            }
        }

        // 2) 在运行时跟踪语音类型变化
        [HarmonyPatch(typeof(CharacterMainControl))]
        [HarmonyPatch("set_AudioVoiceType")]
        [HarmonyPostfix]
        private static void CharacterMainControl_set_AudioVoiceType_Postfix(CharacterMainControl __instance, AudioManager.VoiceType value)
        {
            try { EnemyContextRegistry.UpdateVoiceType(__instance?.gameObject, value); } catch { }
        }

        // 3) 在销毁时清理
        [HarmonyPatch(typeof(CharacterMainControl))]
        [HarmonyPatch("OnDestroy")]
        [HarmonyPostfix]
        private static void CharacterMainControl_OnDestroy_Postfix(CharacterMainControl __instance)
        {
            try { EnemyContextRegistry.Remove(__instance?.gameObject); } catch { }
        }

        // 4) 核心路由点：拦截AudioManager.PostQuak
        [HarmonyPatch(typeof(AudioManager))]
        [HarmonyPatch("PostQuak", new Type[] { typeof(string), typeof(AudioManager.VoiceType), typeof(GameObject) })]
        [HarmonyPrefix]
        private static bool AudioManager_PostQuak_Prefix(string soundKey, AudioManager.VoiceType voiceType, GameObject gameObject)
        {
            try
            {
                CESLogger.Debug($"[CES:Hook] AudioManager.PostQuak ENTER soundKey={soundKey}, voiceType={voiceType}, go={(gameObject!=null?gameObject.name:"null")}");
                CustomEnemySounds.EnsureLoaded();
                if (gameObject == null) return true;
                if (!EnemyContextRegistry.TryGet(gameObject, out var ctx) || ctx == null)
                {
                    // 尽力而为：如果可能，现在尝试注册
                    try
                    {
                        var cmc = gameObject.GetComponent<CharacterMainControl>();
                        if (cmc != null)
                        {
                            ctx = EnemyContextRegistry.Register(cmc, voiceType, AudioManager.FootStepMaterialType.organic);
                        }
                    }
                    catch { }
                }
                if (ctx == null) return true; // 回退到原始音频

                if (CustomEnemySounds.Engine.TryRoute(ctx, soundKey, voiceType, out var route) && route != null && route.UseCustom && !string.IsNullOrEmpty(route.FileFullPath))
                {
                    var mode = ComputeModeForFile(route.FileFullPath);
                    var res = FMODUnity.RuntimeManager.CoreSystem.createSound(route.FileFullPath, mode, out var sound);
                    if (res == RESULT.OK && sound.hasHandle())
                    {
                        // 解析安全的通道组：尽可能使用SFX总线；回退到主总线
                        var group = ResolveSfxGroupSafe();
                        var playRes = FMODUnity.RuntimeManager.CoreSystem.playSound(sound, group, false, out var ch);
                        if (playRes == RESULT.OK)
                        {
                            CESLogger.Debug($"PostQuak: 使用自定义声音 -> {route.FileFullPath} (rule: {route.MatchRule})");
                            try { sound.release(); } catch { }
                            return false; // 跳过原始音频
                        }
                        else
                        {
                            CESLogger.Debug($"PostQuak: playSound 失败 {playRes}，回退原始。");
                        }
                    }
                    else
                    {
                        CESLogger.Debug($"PostQuak: createSound 失败 {res}，回退原始。");
                    }
                }
            }
            catch (Exception ex)
            {
                CESLogger.Error("AudioManager.PostQuak Prefix", ex);
            }
            // 回退策略


            return CustomEnemySounds.Config?.Fallback?.UseOriginalWhenMissing != false;
        }
        private static MODE ComputeModeForFile(string path)
        {
            try
            {
                var ext = System.IO.Path.GetExtension(path)?.ToLowerInvariant();
                var baseMode = MODE._2D | MODE.LOOP_OFF;
                if (ext == ".mp3" || ext == ".ogg" || ext == ".flac")
                    return baseMode | MODE.CREATESTREAM;
                // wav/aiff 等使用默认（内存采样）
                return baseMode;
            }
            catch { return MODE._2D | MODE.LOOP_OFF; }
        }

        private static ChannelGroup ResolveSfxGroupSafe()
        {
            try
            {
                // 优先获取一次 SFX bus 的 ChannelGroup
                try
                {
                    var sfxBus = FMODUnity.RuntimeManager.GetBus("bus:/Master/SFX");
                    if (sfxBus.getChannelGroup(out var cg) == RESULT.OK && cg.hasHandle())
                        return cg;
                }
                catch { }

                // 回退到缓存的 SfxGroup
                if (ModBehaviour.SfxGroup.hasHandle()) return ModBehaviour.SfxGroup;

                // 最后回退到 Master
                if (FMODUnity.RuntimeManager.CoreSystem.getMasterChannelGroup(out var master) == RESULT.OK && master.hasHandle())
                    return master;
            }
            catch { }
            return default;
        }


        // 5b) 更安全的策略：Postfix保留原始生命周期并仅替换输出
        [HarmonyPatch(typeof(AudioObject))]
        public static class AudioObject_PostQuak_PostfixPatch
        {
            [HarmonyPatch("PostQuak", new Type[] { typeof(string) })]
            [HarmonyPostfix]
            private static void Postfix(AudioObject __instance, string soundKey, ref FMOD.Studio.EventInstance? __result)
            {
                try
                {
                    CustomEnemySounds.EnsureLoaded();
                    var go = __instance != null ? __instance.gameObject : null;
                    var evValid = __result.HasValue && __result.Value.isValid();
                    CESLogger.Debug($"[CES:Hook] AudioObject.PostQuak Postfix ENTER: soundKey={soundKey}, 原始EventInstance有效={evValid}");

                    // 构建/获取敌人上下文
                    EnemyContext ctx = null;
                    if (go != null)
                    {
                        if (!EnemyContextRegistry.TryGet(go, out ctx) || ctx == null)
                        {
                            try
                            {
                                var cmc = go.GetComponent<CharacterMainControl>();
                                if (cmc != null)
                                    ctx = EnemyContextRegistry.Register(cmc, cmc.AudioVoiceType, cmc.FootStepMaterialType);
                            }
                            catch { }
                        }
                    }
                    if (ctx != null)
                    {
                        try { CESLogger.Debug($"[CES:Hook] Postfix ctx={ctx}"); } catch { }
                    }

                    // 无上下文则直接返回，保留原始音频
                    if (ctx == null)
                    {
                        CESLogger.Info("[CES:Hook] Postfix: ctx==null，保留原始音频");
                        return;
                    }

                    CESLogger.Info($"[CES:Hook] Postfix: 开始规则匹配: soundKey={soundKey}, ctx.VoiceType={ctx.VoiceType}");
                    VoiceRoute route = null;
                    bool matched = CustomEnemySounds.Engine != null && CustomEnemySounds.Engine.TryRoute(ctx, soundKey, ctx.VoiceType, out route);
                    var routeInfo = route != null ? ($"UseCustom={route.UseCustom}, Path={(route.FileFullPath ?? "null")}") : "null";
                    CESLogger.Info($"[CES:Hook] Postfix: 规则匹配结果: matched={matched}, route={routeInfo}");
                    if (!matched || route == null || !(route.UseCustom && !string.IsNullOrEmpty(route.FileFullPath)))
                    {
                        CESLogger.Info("[CES:Rule] 未匹配到自定义，使用原始音频");
                        return;
                    }

                    CESLogger.Info($"[CES:Rule] 匹配成功: {route.MatchRule} -> {route.FileFullPath}");


                    // 优先级检查：如已有更高/同级语音在播，则跳过；如更低优先级在播，则打断
                    var ownerId = go != null ? go.GetInstanceID() : 0;
                    var newPriority = PriorityPolicy.GetPriority(soundKey);
                    if (!CoreSoundTracker.PreCheckAndMaybeInterrupt(ownerId, soundKey, newPriority))
                    {
                        // 优先级不足，跳过播放
                        return;
                    }

                    // 步骤1：仅静音原生事件（不停止），避免双声且保留生命周期
                    if (evValid)
                    {
                        try
                        {
                            CESLogger.Debug("[CES:Hook] Postfix: 正在静音原始事件（不停止）...");
                            var ev = __result.Value;
                            try { ev.setVolume(0f); } catch { }
                        }
                        catch { }
                    }

                    // 步骤2：使用 Core API 播放自定义音频，并自行管理生命周期
                    var mode = ComputeModeForFile(route.FileFullPath);
                    var res = FMODUnity.RuntimeManager.CoreSystem.createSound(route.FileFullPath, mode, out var sound);
                    CESLogger.Debug($"[CES:Hook] Postfix: createSound 结果={res}, mode={mode}");
                    if (res == RESULT.OK && sound.hasHandle())
                    {
                        var group = ResolveSfxGroupSafe();
                        var playRes = FMODUnity.RuntimeManager.CoreSystem.playSound(sound, group, false, out var ch);
                        CESLogger.Debug($"[CES:Hook] Postfix: playSound 结果={playRes}");
                        if (playRes == RESULT.OK)
                        {
                            CESLogger.Info($"[CES:Hook] 播放自定义语音成功 -> {route.FileFullPath}");
                            try { CoreSoundTracker.Track(ownerId, sound, ch, route.FileFullPath, soundKey, newPriority); CESLogger.Debug("[CES:Hook] Postfix: 已加入播放跟踪"); } catch { }
                        }
                        else
                        {
                            try { sound.release(); } catch { }
                        }
                    }
                }
                catch (Exception ex)
                {
                    CESLogger.Error("AudioObject.PostQuak Postfix", ex);
                }
            }
        }


        // 6) 敌人死亡语音：尝试多个可能的钩子并通过CoreSoundTracker播放自定义"death" soundKey
        private static readonly System.Collections.Generic.HashSet<int> _deathPlayed = new System.Collections.Generic.HashSet<int>();
        private static void TryPlayDeathVoice(UnityEngine.GameObject go)
        {
            try
            {
                if (go == null) return;
                var id = go.GetInstanceID();
                if (_deathPlayed.Contains(id)) return; // 避免重复触发

                // 获取上下文
                EnemyContext ctx = null;
                if (!EnemyContextRegistry.TryGet(go, out ctx) || ctx == null)
                {
                    try
                    {
                        var cmc = go.GetComponent<CharacterMainControl>();
                        if (cmc != null) ctx = EnemyContextRegistry.Register(cmc, cmc.AudioVoiceType, cmc.FootStepMaterialType);
                    }
                    catch { }
                }
                if (ctx == null)
                {
                    CESLogger.Info("[CES:Hook] Death: ctx==null，跳过");
                    return;
                }


                CESLogger.Info($"[CES:Hook] Death: 开始规则匹配: soundKey=death, ctx.VoiceType={ctx.VoiceType}");
                VoiceRoute route = null;
                bool matched = CustomEnemySounds.Engine != null && CustomEnemySounds.Engine.TryRoute(ctx, "death", ctx.VoiceType, out route);
                var routeInfo = route != null ? ($"UseCustom={route.UseCustom}, Path={(route.FileFullPath ?? "null")}") : "null";
                CESLogger.Debug($"[CES:Hook] Death: 规则匹配结果: matched={matched}, route={routeInfo}");
                if (!matched || route == null || !(route.UseCustom && !string.IsNullOrEmpty(route.FileFullPath)))
                {
                    CESLogger.Info("[CES:Hook] Death: 未匹配到自定义，跳过播放");
                    return;
                }


                // 优先级检查：仅在确定会播放（已匹配到文件）后再进行，避免误打断
                var deathPrio = PriorityPolicy.GetPriority("death");
                if (!CoreSoundTracker.PreCheckAndMaybeInterrupt(id, "death", deathPrio))
                {
                    return;
                }

                CESLogger.Info($"[CES:Rule] 匹配成功: {route.MatchRule} -> {route.FileFullPath}");


                var mode = ComputeModeForFile(route.FileFullPath);
                var res = FMODUnity.RuntimeManager.CoreSystem.createSound(route.FileFullPath, mode, out var sound);
                CESLogger.Debug($"[CES:Hook] Death: createSound 结果={res}, mode={mode}");
                if (res == RESULT.OK && sound.hasHandle())
                {
                    var group = ResolveSfxGroupSafe();

                    var playRes = FMODUnity.RuntimeManager.CoreSystem.playSound(sound, group, false, out var ch);
                    CESLogger.Debug($"[CES:Hook] Death: playSound 结果={playRes}");
                    if (playRes == RESULT.OK)
                    {
                        _deathPlayed.Add(id);
                        CESLogger.Info($"[CES:Hook] Death: 播放自定义死亡语音 -> {route.FileFullPath}");
                        try { CoreSoundTracker.Track(id, sound, ch, route.FileFullPath, "death", PriorityPolicy.GetPriority("death")); } catch { }
                    }
                    else
                    {
                        try { sound.release(); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                CESLogger.Error("Death Voice 播放异常", ex);
            }
        }


        [HarmonyPatch(typeof(CharacterMainControl))]
        public static class CharacterMainControl_OnDead_Postfix
        {
            // 准确匹配私有方法 OnDead(DamageInfo)
            [HarmonyPatch("OnDead", new Type[] { typeof(DamageInfo) })]
            [HarmonyPostfix]
            private static void Postfix(CharacterMainControl __instance, DamageInfo dmgInfo)
            {
                try
                {
                    CESLogger.Info("[CES:Hook] Death Postfix ENTER (CharacterMainControl.OnDead)");
                    TryPlayDeathVoice(__instance != null ? __instance.gameObject : null);
                }
                catch (Exception ex) { CESLogger.Error("CharacterMainControl.OnDead Postfix", ex); }
            }
        }

        // 覆盖更多死亡路径：Health.Hurt(DamageInfo) 内部会在致死时设置 isDead=true 并触发 OnDead 事件
        [HarmonyPatch(typeof(Health))]
        public static class Health_Hurt_Postfix
        {
            [HarmonyPatch("Hurt", new Type[] { typeof(DamageInfo) })]
            [HarmonyPostfix]
            private static void Postfix(Health __instance, DamageInfo damageInfo)
            {
                try
                {
                    if (__instance != null && __instance.IsDead)
                    {
                        CESLogger.Info("[CES:Hook] Death Postfix ENTER (Health.Hurt -> isDead)");
                        var comp = __instance as UnityEngine.Component;
                        TryPlayDeathVoice(comp != null ? comp.gameObject : null);
                    }
                }
                catch (Exception ex) { CESLogger.Error("Health.Hurt Postfix", ex); }
            }
        }

    }
}

