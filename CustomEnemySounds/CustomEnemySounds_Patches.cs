using HarmonyLib;
using System;
using UnityEngine;
using Duckov; // AudioManager, CharacterMainControl, AICharacterController
using FMOD;

namespace DuckovCustomSounds.CustomEnemySounds
{
    /// <summary>
    /// 自定义敌人语音的路由与播放：
    /// - 保持原事件生命周期（创建原事件，但将其音量设为0）
    /// - 使用 FMOD Core 播放自定义文件，按原事件的3D距离配置，且跟随发声体位置
    /// </summary>
    internal static class CustomEnemySounds_Patches
    {
        // 1) AI 初始化时注册上下文
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

        // 2) 语音类型改变时更新
        [HarmonyPatch(typeof(CharacterMainControl))]
        [HarmonyPatch("set_AudioVoiceType")]
        [HarmonyPostfix]
        private static void CharacterMainControl_set_AudioVoiceType_Postfix(CharacterMainControl __instance, AudioManager.VoiceType value)
        {
            try { EnemyContextRegistry.UpdateVoiceType(__instance?.gameObject, value); } catch { }
        }

        // 3) 销毁时清理
        [HarmonyPatch(typeof(CharacterMainControl))]
        [HarmonyPatch("OnDestroy")]
        [HarmonyPostfix]
        private static void CharacterMainControl_OnDestroy_Postfix(CharacterMainControl __instance)
        {
            try { EnemyContextRegistry.Remove(__instance?.gameObject); } catch { }
        }

        // 4) Postfix 拦截 AudioObject.PostQuak：保留原事件生命周期，替换为自定义3D播放
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

                    // 绑定/获取敌人上下文
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
                    if (ctx == null)
                    {
                        CESLogger.Info("[CES:Hook] Postfix: ctx==null，保留原声");
                        return;
                    }

                    // richer context logging
                    CESLogger.Info($"[CES:Hook] Postfix: 开始规则匹配: soundKey={soundKey}, vt={ctx.VoiceType}, team={ctx.GetTeamNormalized()}, rank={ctx.GetRank()}, icon={ctx.IconType}, nameKey={ctx.NameKey}, footMat={ctx.FootStepMaterialType}");
                    VoiceRoute route = null;
                    bool matched = CustomEnemySounds.Engine != null && CustomEnemySounds.Engine.TryRoute(ctx, soundKey, ctx.VoiceType, out route);
                    var routeInfo = route != null ? ($"UseCustom={route.UseCustom}, Path={(route.FileFullPath ?? "null")} ") : "null";
                    CESLogger.Info($"[CES:Hook] Postfix: 匹配结果: matched={matched}, route={routeInfo}");
                    if (!matched || route == null || !(route.UseCustom && !string.IsNullOrEmpty(route.FileFullPath)))
                    {
                        CESLogger.Info("[CES:Rule] 未匹配到自定义，使用原声");
                        return;
                    }

                    CESLogger.Info($"[CES:Rule] 命中: {route.MatchRule} -> {route.FileFullPath}");

                    // 优先级判断
                    var ownerId = go != null ? go.GetInstanceID() : 0;
                    var newPriority = PriorityPolicy.GetPriority(soundKey);
                    if (!CoreSoundTracker.PreCheckAndMaybeInterrupt(ownerId, soundKey, newPriority))
                    {
                        return;
                    }

                    // 若原事件已创建，则将其音量降为0，保留生命周期
                    if (evValid)
                    {
                        try
                        {
                            CESLogger.Debug("[CES:Hook] Postfix: 原始事件静音");
                            var ev = __result.Value;
                            try { ev.setVolume(0f); } catch { }
                        }
                        catch { }
                    }

                    // 从原事件描述继承3D距离（若失败则尝试通过TryCreateEventInstance获取）
                    float min = 1.5f, max = 25f;
                    try
                    {
                        if (evValid)
                        {
                            FMOD.Studio.EventDescription desc;
                            if (__result.Value.getDescription(out desc) == RESULT.OK)
                            {
                                try { desc.getMinMaxDistance(out min, out max); } catch { }
                            }
                        }
                        else
                        {
                            TryGetEvent3DDistances(soundKey, ctx.VoiceType, out min, out max);
                        }
                    }
                    catch { }

                    // 使用 Core API 播放自定义3D声音
                    var mode = ComputeModeForFile(route.FileFullPath);
                    var res = FMODUnity.RuntimeManager.CoreSystem.createSound(route.FileFullPath, mode, out var sound);
                    CESLogger.Debug($"[CES:Hook] Postfix: createSound 结果={res}, mode={mode}");
                    if (res == RESULT.OK && sound.hasHandle())
                    {
                        var group = ResolveSfxGroupSafe();
                        try { sound.set3DMinMaxDistance(min, max); } catch { }

                        var playRes = FMODUnity.RuntimeManager.CoreSystem.playSound(sound, group, true, out var ch);
                        CESLogger.Debug($"[CES:Hook] Postfix: playSound 结果={playRes}");
                        if (playRes == RESULT.OK)
                        {
                            Vector3 pos = Vector3.zero;
                            Transform tr = null;
                            try { tr = go != null ? go.transform : null; pos = tr != null ? tr.position : Vector3.zero; } catch { }
                            var fpos = ToFMODVector(pos);
                            var fvel = ToFMODVector(Vector3.zero);
                            try { ch.set3DAttributes(ref fpos, ref fvel); } catch { }
                            try { ch.set3DMinMaxDistance(min, max); } catch { }
                            try { ch.setMode(MODE._3D | MODE._3D_LINEARROLLOFF | MODE.LOOP_OFF); } catch { }
                            try { ch.setPaused(false); } catch { }

                            try { CoreSoundTracker.Track(ownerId, sound, ch, route.FileFullPath, soundKey, newPriority, tr); CESLogger.Debug("[CES:Hook] Postfix: 已加入跟踪"); } catch { }
                            CESLogger.Info($"[CES:Hook] 已替换为自定义3D语音 -> {route.FileFullPath} (min={min:F1}, max={max:F1})");
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

        // 5) 死亡语音兜底（某些流程不经由 PostQuak 时）
        private static readonly System.Collections.Generic.HashSet<int> _deathPlayed = new System.Collections.Generic.HashSet<int>();
        private static float _lastDeathGlobalTime = -999f;

        private static void TryPlayDeathVoice(UnityEngine.GameObject go)
        {
            try
            {
                if (go == null) return;
                int id = go.GetInstanceID();
                if (_deathPlayed.Contains(id)) return;

                // Settings: global rate limit for death voice
                if (!DuckovCustomSounds.ModSettings.DeathVoiceEnabled) return;
                float __now = Time.realtimeSinceStartup;
                float __min = DuckovCustomSounds.ModSettings.DeathVoiceMinInterval;
                if (__min > 0f && (__now - _lastDeathGlobalTime) < __min) return;

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
                CESLogger.Debug($"[CES:Hook] Death: 匹配结果: matched={matched}, route={routeInfo}");
                if (!matched || route == null || !(route.UseCustom && !string.IsNullOrEmpty(route.FileFullPath)))
                {
                    CESLogger.Info("[CES:Hook] Death: 未匹配自定义，跳过");
                    return;
                }

                // 优先级判断
                var deathPrio = PriorityPolicy.GetPriority("death");
                if (!CoreSoundTracker.PreCheckAndMaybeInterrupt(id, "death", deathPrio))
                {
                    return;
                }

                // 继承原事件3D距离
                float min = 1.5f, max = 25f;
                try { TryGetEvent3DDistances("death", ctx.VoiceType, out min, out max); } catch { }

                var mode = ComputeModeForFile(route.FileFullPath);
                var res = FMODUnity.RuntimeManager.CoreSystem.createSound(route.FileFullPath, mode, out var sound);
                CESLogger.Debug($"[CES:Hook] Death: createSound 结果={res}, mode={mode}");
                if (res == RESULT.OK && sound.hasHandle())
                {
                    var group = ResolveSfxGroupSafe();
                    try { sound.set3DMinMaxDistance(min, max); } catch { }

                    var playRes = FMODUnity.RuntimeManager.CoreSystem.playSound(sound, group, true, out var ch);
                    CESLogger.Debug($"[CES:Hook] Death: playSound 结果={playRes}");
                    if (playRes == RESULT.OK)
                    {
                        _deathPlayed.Add(id);
                        Vector3 pos = Vector3.zero; Transform tr = null;
                        try { tr = go.transform; pos = tr.position; } catch { }
                        var fpos = ToFMODVector(pos);
                        var fvel = ToFMODVector(Vector3.zero);
                        try { ch.set3DAttributes(ref fpos, ref fvel); } catch { }
                        try { ch.set3DMinMaxDistance(min, max); } catch { }
                        try { ch.setMode(MODE._3D | MODE._3D_LINEARROLLOFF | MODE.LOOP_OFF); } catch { }
                        try { ch.setPaused(false); } catch { }
                        _lastDeathGlobalTime = Time.realtimeSinceStartup;


                        CESLogger.Info($"[CES:Hook] Death: 播放自定义3D语音 -> {route.FileFullPath}");
                        try { CoreSoundTracker.Track(id, sound, ch, route.FileFullPath, "death", deathPrio, tr); } catch { }
                    }
                    else
                    {
                        try { sound.release(); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                CESLogger.Error("Death Voice 处理异常", ex);
            }
        }

        // 更安全的做法：直接订阅 Health.OnDead 静态事件，避免修改/干扰原始调用链参数
        private static bool _deathEventHooked;
        internal static void EnableDeathEventHook()
        {
            if (_deathEventHooked) return;
            try
            {
                CESLogger.Info("[CES:Hook] Subscribe Health.OnDead");
                Health.OnDead += OnHealthDead_Handler;
                _deathEventHooked = true;
            }
            catch (Exception ex)
            {
                CESLogger.Error("EnableDeathEventHook failed", ex);
            }
        }

        internal static void DisableDeathEventHook()
        {
            if (!_deathEventHooked) return;
            try { Health.OnDead -= OnHealthDead_Handler; } catch { }
            _deathEventHooked = false;
        }

        private static void OnHealthDead_Handler(Health h, DamageInfo dmgInfo)
        {
            try
            {
                CESLogger.Info("[CES:Hook] Death via Health.OnDead");
                var comp = h as UnityEngine.Component;
                TryPlayDeathVoice(comp != null ? comp.gameObject : null);
            }
            catch (Exception ex) { CESLogger.Error("OnHealthDead_Handler", ex); }
        }

        private static MODE ComputeModeForFile(string path)
        {
            try
            {
                var ext = System.IO.Path.GetExtension(path)?.ToLowerInvariant();
                var baseMode = MODE._3D | MODE._3D_LINEARROLLOFF | MODE.LOOP_OFF;
                if (ext == ".mp3" || ext == ".ogg" || ext == ".flac")
                    return baseMode | MODE.CREATESTREAM;
                return baseMode; // wav/aiff 使用内存sample
            }
            catch { return MODE._3D | MODE._3D_LINEARROLLOFF | MODE.LOOP_OFF; }
        }

        private static bool TryGetEvent3DDistances(string soundKey, AudioManager.VoiceType voiceType, out float min, out float max)
        {
            min = 1.5f; max = 25f;
            try
            {
                if (TryGetEvent3DDistancesForKey(soundKey, voiceType, out min, out max))
                    return true;
                // 回退：优先采用“surprise”的3D距离以保证与普通惊吓语音响度一致
                if (!string.Equals(soundKey, "surprise", StringComparison.OrdinalIgnoreCase) &&
                    TryGetEvent3DDistancesForKey("surprise", voiceType, out min, out max))
                    return true;
                // 次级回退：normal
                if (!string.Equals(soundKey, "normal", StringComparison.OrdinalIgnoreCase) &&
                    TryGetEvent3DDistancesForKey("normal", voiceType, out min, out max))
                    return true;
            }
            catch { }
            return false;
        }

        private static bool TryGetEvent3DDistancesForKey(string key, AudioManager.VoiceType voiceType, out float min, out float max)
        {
            min = 1.5f; max = 25f;
            try
            {
                string vt = voiceType.ToString().ToLowerInvariant();
                string eventPath = $"Char/Voice/vo_{vt}_{key}";
                FMOD.Studio.EventInstance ev;
                if (AudioManager.TryCreateEventInstance(eventPath, out ev))
                {
                    try
                    {
                        FMOD.Studio.EventDescription desc;
                        if (ev.getDescription(out desc) == RESULT.OK)
                        {
                            try { desc.getMinMaxDistance(out min, out max); } catch { }
                        }
                    }
                    finally { try { ev.release(); } catch { } }
                    return true;
                }
            }
            catch { }
            return false;
        }

        private static FMOD.VECTOR ToFMODVector(Vector3 v) => new FMOD.VECTOR { x = v.x, y = v.y, z = v.z };

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

                // 其次使用已缓存的 SfxGroup
                if (ModBehaviour.SfxGroup.hasHandle()) return ModBehaviour.SfxGroup;

                // 最后回退 Master
                if (FMODUnity.RuntimeManager.CoreSystem.getMasterChannelGroup(out var master) == RESULT.OK && master.hasHandle())
                    return master;
            }
            catch { }
            return default;
        }
    }
}

