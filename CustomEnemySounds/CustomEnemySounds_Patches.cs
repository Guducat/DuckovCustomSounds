using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using Duckov; // AudioManager, CharacterMainControl, AICharacterController
using FMOD;
using FMODUnity;
using DuckovCustomSounds.CustomEnemySounds.Audio;
using DuckovCustomSounds.CustomEnemySounds.Config;
using DuckovCustomSounds.CustomEnemySounds.Context;
using DuckovCustomSounds.CustomEnemySounds.Filters;
using DuckovCustomSounds.CustomEnemySounds.Rules;

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
        [HarmonyPatch("Init",
            new Type[]
            {
                typeof(CharacterMainControl), typeof(Vector3), typeof(AudioManager.VoiceType),
                typeof(AudioManager.FootStepMaterialType)
            })]
        [HarmonyPostfix]
        private static void AI_Init_Postfix(CharacterMainControl _characterMainControl, Vector3 patrolCenter,
            AudioManager.VoiceType voiceType, AudioManager.FootStepMaterialType footStepMatType)
        {
            try
            {
                CESLogger.Debug("[CES:Hook] AICharacterController.Init Postfix ENTER");
                CESLogger.Info("[CES:Hook] AI.Init Postfix: ENTER");
                CustomEnemySounds.EnsureLoaded();
                if (_characterMainControl == null) { CESLogger.Info("[CES:Hook] AI.Init Postfix: _characterMainControl==null, skip Register()"); return; }
                try
                {
                    CESLogger.Info("[CES:Hook] AI.Init Postfix: Register() begin");
                    var __ctx = EnemyContextRegistry.Register(_characterMainControl, voiceType, footStepMatType);
                    CESLogger.Info($"[CES:Hook] AI.Init Postfix: Register() done -> hasCtx={(__ctx!=null)}");
                }
                catch (Exception regEx)
                {
                    CESLogger.Error("AI.Init Postfix: Register() failed", regEx);
                }
            // 新增：BOSS BGM 检测（合并补丁，避免 Harmony 冲突）
            try
            {
                if (DuckovCustomSounds.CustomBGM.BossBGM.BossBGMConfig.Enabled &&
                    DuckovCustomSounds.CustomBGM.BossBGM.BossMusicResolver.HasAnyMusic)
                {
                    var go = _characterMainControl != null ? _characterMainControl.gameObject : null;
                    EnemyContext ctx = null;
                    if (go != null && EnemyContextRegistry.TryGet(go, out ctx) && ctx != null)
                    {
                        if (ctx.GetRank() == "boss" && IsSureBoss(ctx))
                        {
                            var existing = go.GetComponent<DuckovCustomSounds.CustomBGM.BossBGM.BossBGMController>();
                            if (existing == null)
                            {
                                var controller = go.AddComponent<DuckovCustomSounds.CustomBGM.BossBGM.BossBGMController>();
                                controller.Initialize(ctx);
                                DuckovCustomSounds.CustomBGM.BossBGM.BossBGMLogger.Debug($"[Patch] 检测到 BOSS 生成: {ctx.NameKey}, rank=boss");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DuckovCustomSounds.CustomBGM.BossBGM.BossBGMLogger.Error("BOSS BGM 检测失败", ex);
            }

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
        private static void CharacterMainControl_set_AudioVoiceType_Postfix(CharacterMainControl __instance,
            AudioManager.VoiceType value)
        {
            try
            {
                EnemyContextRegistry.UpdateVoiceType(__instance?.gameObject, value);
            }
            catch
            {
            }
        }

        // 3) 销毁时清理
        [HarmonyPatch(typeof(CharacterMainControl))]
        [HarmonyPatch("OnDestroy")]
        [HarmonyPostfix]
        private static void CharacterMainControl_OnDestroy_Postfix(CharacterMainControl __instance)
        {
            try
            {
                EnemyContextRegistry.Remove(__instance?.gameObject);
            }
            catch
            {
            }
        }

        // 4) Postfix 拦截 AudioObject.PostQuak：保留原事件生命周期，替换为自定义3D播放
        [HarmonyPatch(typeof(AudioObject))]
        public static class AudioObject_PostQuak_PostfixPatch
        {
            [HarmonyPatch("PostQuak", new Type[] { typeof(string) })]
            [HarmonyPostfix]
            private static void Postfix(AudioObject __instance, string soundKey,
                ref FMOD.Studio.EventInstance? __result)
            {
                try
                {
                    CustomEnemySounds.EnsureLoaded();
                    var go = __instance != null ? __instance.gameObject : null;
                    var evValid = __result.HasValue && __result.Value.isValid();
                    CESLogger.Debug(
                        $"[CES:Hook] AudioObject.PostQuak Postfix ENTER: soundKey={soundKey}, 原始EventInstance有效={evValid}");

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
                                    ctx = EnemyContextRegistry.Register(cmc, cmc.AudioVoiceType,
                                        cmc.FootStepMaterialType);
                            }
                            catch
                            {
                            }
                        }
                    }

                    if (ctx == null)
                    {
                        CESLogger.Info("[CES:Hook] Postfix: ctx==null，保留原声");
                        return;
                    }
                    var speaker = ctx.GameObject != null
                        ? ctx.GameObject.GetComponent<CharacterMainControl>()
                        : null;
                    if (speaker != null)
                    {
                        var voiceContext = EnemyVoiceFilter.CreateContext(ctx);
                        if (!EnemyVoiceFilter.ShouldAllow(speaker, soundKey, voiceContext))
                        {
                            CESLogger.Debug("[CES:Hook] EnemyVoiceFilter blocked this voice event.");
                            if (evValid)
                            {
                                try
                                {
                                    var ev = __result.Value;
                                    ev.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                                    ev.release();
                                }
                                catch (Exception stopEx)
                                {
                                    CESLogger.Debug($"[CES:Hook] Failed to stop native voice event: {stopEx.Message}");
                                }
                            }

                            __result = null;
                            return;
                        }
                    }


                    // richer context logging
                    CESLogger.Info(
                        $"[CES:Hook] Postfix: 开始规则匹配: soundKey={soundKey}, vt={ctx.VoiceType}, team={ctx.GetTeamNormalized()}, rank={ctx.GetRank()}, icon={ctx.IconType}, nameKey={ctx.NameKey}, footMat={ctx.FootStepMaterialType}");
                    VoiceRoute route = null;
                    bool matched = CustomEnemySounds.Engine != null &&
                                   CustomEnemySounds.Engine.TryRoute(ctx, soundKey, ctx.VoiceType, out route);
                    var routeInfo = route != null
                        ? ($"UseCustom={route.UseCustom}, Path={(route.FileFullPath ?? "null")} ")
                        : "null";
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
                    float minDistance = 0f;
                    float maxDistance = 0f;
                    bool hasDistance = false;

                    if (evValid)
                    {
                        try
                        {
                            CESLogger.Debug("[CES:Hook] Postfix: 原始事件静音");
                            var ev = __result.Value;
                            try
                            {
                                hasDistance = AudioDistanceHelper.TryExtractFromEventInstance(ev, out minDistance, out maxDistance);
                                if (hasDistance)
                                {
                                    CESLogger.Debug($"[CES:Hook] Postfix: copied native 3D distance min={minDistance:F2}, max={maxDistance:F2}");
                                }
                            }
                            catch
                            {
                            }
                            try
                            {
                                ev.setVolume(0f);
                            }
                            catch
                            {
                            }
                        }
                        catch
                        {
                        }
                    }
                    if (!hasDistance)
                    {
                        hasDistance = TryGetVoice3DDistances(soundKey, ctx.VoiceType, out minDistance, out maxDistance);
                        if (hasDistance)
                        {
                            CESLogger.Debug($"[CES:Hook] Postfix: fallback 3D distance min={minDistance:F2}, max={maxDistance:F2}");
                        }
                    }

                    // 使用新接口播放自定义 3D 语音
                    // 注意：新接口自动处理 3D 距离、自动跟随 GameObject、自动资源清理
                    try
                    {
                        var eventInstance = Duckov.AudioManager.PostCustomSFX(route.FileFullPath, go, loop: false);
                        if (eventInstance.HasValue && eventInstance.Value.isValid())
                        {
                            // 立即绑定3D位置，避免首帧未赋位导致远距离衰减过大（对象很快销毁/禁用时尤为明显）
                            try
                            {
                                if (go != null)
                                    eventInstance.Value.set3DAttributes(go.transform.position.To3DAttributes());
                            }
                            catch { }

                            if (hasDistance)
                            {
                                AudioDistanceHelper.ApplyToEventInstance(eventInstance.Value, minDistance, maxDistance);
                            }

                            // 追踪 EventInstance（用于优先级中断）
                            try
                            {
                                CoreSoundTracker.Track(ownerId, eventInstance.Value, route.FileFullPath, soundKey, newPriority);
                                CESLogger.Debug("[CES:Hook] Postfix: 已加入跟踪");
                            }
                            catch (Exception trackEx)
                            {
                                CESLogger.Warning($"[CES:Hook] 追踪 EventInstance 失败: {trackEx.Message}");
                            }

                            CESLogger.Info($"[CES:Hook] 已替换为自定义3D语音 -> {route.FileFullPath}");
                        }
                        else
                        {
                            CESLogger.Warning($"[CES:Hook] PostCustomSFX 返回无效的 EventInstance");
                        }
                    }
                    catch (Exception playEx)
                    {
                        CESLogger.Error($"[CES:Hook] 使用新接口播放自定义语音失败", playEx);
                    }
                }
                catch (Exception ex)
                {
                    CESLogger.Error("AudioObject.PostQuak Postfix", ex);
                }
            }
        }

        // 5) 死亡语音兜底（某些流程不经由 PostQuak 时）
        private static readonly System.Collections.Generic.HashSet<int> _deathPlayed =
            new System.Collections.Generic.HashSet<int>();

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
                        if (cmc != null)
                            ctx = EnemyContextRegistry.Register(cmc, cmc.AudioVoiceType, cmc.FootStepMaterialType);
                    }
                    catch
                    {
                    }
                }

                if (ctx == null)
                {
                    CESLogger.Info("[CES:Hook] Death: ctx==null，跳过");
                    return;
                }

                CESLogger.Info($"[CES:Hook] Death: 开始规则匹配: soundKey=death, ctx.VoiceType={ctx.VoiceType}");
                VoiceRoute route = null;
                bool matched = CustomEnemySounds.Engine != null &&
                               CustomEnemySounds.Engine.TryRoute(ctx, "death", ctx.VoiceType, out route);
                var routeInfo = route != null
                    ? ($"UseCustom={route.UseCustom}, Path={(route.FileFullPath ?? "null")}")
                    : "null";
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

                // 使用新接口播放自定义死亡语音
                try
                {
                    float minDistance = 0f;
                    float maxDistance = 0f;
                    bool hasDistance = TryGetVoice3DDistances("death", ctx.VoiceType, out minDistance, out maxDistance);

                    var eventInstance = Duckov.AudioManager.PostCustomSFX(route.FileFullPath, go, loop: false);
                    if (eventInstance.HasValue && eventInstance.Value.isValid())
                    {
                        // 立即绑定3D位置，避免首帧未赋位导致远距离衰减过大（对象很快销毁/禁用时尤为明显）
                        try
                        {
                            if (go != null)
                                eventInstance.Value.set3DAttributes(go.transform.position.To3DAttributes());
                        }
                        catch { }

                        if (hasDistance)
                        {
                            AudioDistanceHelper.ApplyToEventInstance(eventInstance.Value, minDistance, maxDistance);
                        }

                        _deathPlayed.Add(id);
                        _lastDeathGlobalTime = Time.realtimeSinceStartup;

                        // 追踪 EventInstance（用于优先级中断）
                        try
                        {
                            CoreSoundTracker.Track(id, eventInstance.Value, route.FileFullPath, "death", deathPrio);
                        }
                        catch (Exception trackEx)
                        {
                            CESLogger.Warning($"[CES:Hook] Death: 追踪 EventInstance 失败: {trackEx.Message}");
                        }

                        CESLogger.Info($"[CES:Hook] Death: 播放自定义3D语音 -> {route.FileFullPath}");
                    }
                    else
                    {
                        CESLogger.Warning($"[CES:Hook] Death: PostCustomSFX 返回无效的 EventInstance");
                    }
                }
                catch (Exception playEx)
                {
                    CESLogger.Error($"[CES:Hook] Death: 使用新接口播放自定义语音失败", playEx);
                }
            }
            catch (Exception ex)
            {
                CESLogger.Error("Death Voice 处理异常", ex);
            }
        }

        // 更安全的做法：直接订阅 Health.OnDead 静态事件，避免修改/干扰原始调用链参数
        private static bool TryGetVoice3DDistances(string soundKey, AudioManager.VoiceType voiceType, out float min, out float max)
        {
            if (TryGetVoice3DDistancesForKey(soundKey, voiceType, out min, out max)) return true;
            if (!string.Equals(soundKey, "surprise", StringComparison.OrdinalIgnoreCase) &&
                TryGetVoice3DDistancesForKey("surprise", voiceType, out min, out max))
                return true;
            if (!string.Equals(soundKey, "normal", StringComparison.OrdinalIgnoreCase) &&
                TryGetVoice3DDistancesForKey("normal", voiceType, out min, out max))
                return true;

            min = 1.5f;
            max = 25f;
            return false;
        }

        private static bool TryGetVoice3DDistancesForKey(string key, AudioManager.VoiceType voiceType, out float min, out float max)
        {
            min = 0f;
            max = 0f;
            try
            {
                string vt = voiceType.ToString().ToLowerInvariant();
                string eventPath = $"Char/Voice/vo_{vt}_{key}";
                return AudioDistanceHelper.TryExtractFromEventName(eventPath, out min, out max);
            }
            catch
            {
                min = 0f;
                max = 0f;
                return false;
            }
        }

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
            try
            {
                Health.OnDead -= OnHealthDead_Handler;
            }
            catch
            {
            }

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
            catch (Exception ex)
            {
                CESLogger.Error("OnHealthDead_Handler", ex);
            }
        }

        // BossBGM: 更严格的 BOSS 名称过滤
        private static bool IsSureBoss(EnemyContext ctx)
        {
            try
            {
                var nk = ctx?.NameKey ?? string.Empty;
                if (string.IsNullOrEmpty(nk)) return false;

                if (!nk.StartsWith("Cname_Boss_", StringComparison.OrdinalIgnoreCase)) return false;
                if (nk.IndexOf("child", StringComparison.OrdinalIgnoreCase) >= 0) return false;
                if (nk.IndexOf("minion", StringComparison.OrdinalIgnoreCase) >= 0) return false;
                return true;
            }
            catch { return false; }
        }
        // 注意：以下辅助函数已删除，因为新接口自动处理：
        // - ToFMODVector：不再需要手动转换坐标
        // - ResolveSfxGroupSafe：不再需要手动获取 ChannelGroup
        // - ComputeModeForFile：不再需要手动设置 FMOD 模式
        // - TryGetEvent3DDistances：不再需要手动获取 3D 距离
        // - TryGetEvent3DDistancesForKey：不再需要手动获取 3D 距离
    }
}
