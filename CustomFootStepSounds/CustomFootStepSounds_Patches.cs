using System;
using System.Collections.Concurrent;

using HarmonyLib;
using UnityEngine;
using Duckov; // AudioManager, CharacterSoundMaker, CharacterMainControl, AudioObject
using DuckovCustomSounds.CustomEnemySounds; // EnemyContext & Registry
using DuckovCustomSounds.CustomEnemySounds.Context;
using DuckovCustomSounds.CustomEnemySounds.Rules;
using FMOD;

namespace DuckovCustomSounds.CustomFootStepSounds
{

    internal static class CustomFootStepSounds_Patches
    {
        // Per-character cooldown states
        private struct CooldownState { public float lastFoot; public float lastDash; public float lastSeen; }
        private static readonly ConcurrentDictionary<int, CooldownState> s_cooldowns = new ConcurrentDictionary<int, CooldownState>();
        private static float s_lastCleanupAt = 0f;

        private static float GetMinCooldownSeconds()
        {
            try
            {
                var v = CustomFootStepSounds.Config?.MinCooldownSeconds ?? 0.3f;
                if (v <= 0f) v = 0.3f; // default for footsteps when missing
                if (v < 0.05f) v = 0.05f;
                if (v > 2f) v = 2f;
                return v;
            }
            catch { return 0.3f; }
        }
        private static bool IsOnCooldownAndTouch(int id, bool isDash, float minCooldown, out float remain)
        {
            remain = 0f;
            if (id == 0 || minCooldown <= 0f) return false;
            var now = Time.realtimeSinceStartup;
            if (!s_cooldowns.TryGetValue(id, out var st))
            {
                st = new CooldownState { lastFoot = -999f, lastDash = -999f, lastSeen = now };
                s_cooldowns[id] = st;
                CleanupIfNeeded(now);
                return false;
            }
            var last = isDash ? st.lastDash : st.lastFoot;
            var delta = now - last;
            st.lastSeen = now;
            s_cooldowns[id] = st;
            CleanupIfNeeded(now);
            if (last > 0f && delta < minCooldown)
            {
                remain = Math.Max(0f, minCooldown - delta);
                return true;
            }
            return false;
        }
        private static void MarkPlayed(int id, bool isDash)
        {
            if (id == 0) return;
            var now = Time.realtimeSinceStartup;
            s_cooldowns.AddOrUpdate(id,
                key => new CooldownState { lastFoot = isDash ? 0f : now, lastDash = isDash ? now : 0f, lastSeen = now },
                (key, old) => { if (isDash) old.lastDash = now; else old.lastFoot = now; old.lastSeen = now; return old; });
        }
        private static void CleanupIfNeeded(float now)
        {
            if (now - s_lastCleanupAt < 30f) return;
            s_lastCleanupAt = now;
            foreach (var kv in s_cooldowns)
            {
                if (now - kv.Value.lastSeen > 120f) { s_cooldowns.TryRemove(kv.Key, out _); }
            }
        }
        // 1) 拦截脚步声核心分发，优先使用自定义，命中后阻止原逻辑发声
        [HarmonyPatch(typeof(AudioManager))]
        [HarmonyPatch("OnFootStepSound", new Type[] { typeof(Vector3), typeof(CharacterSoundMaker.FootStepTypes), typeof(CharacterMainControl) })]
        private static class AudioManager_OnFootStepSound_PrefixPatch
        {
            [HarmonyPrefix]
            private static bool Prefix(Vector3 position, CharacterSoundMaker.FootStepTypes type, CharacterMainControl character)
            {
                try
                {
                    CustomFootStepSounds.EnsureLoaded();
                    // 检查 ModConfig UI 配置
                    if (!FootstepConfig.Enabled) return true; // 允许原逻辑
                    // 兼容旧配置
                    if (!ModSettings.EnableCustomFootStepSounds) return true; // 允许原逻辑
                    if (character == null) return true;

                    // 处理无声材质
                    if (character.FootStepMaterialType == AudioManager.FootStepMaterialType.noSound)
                        return true;

                    // 组装 soundKey（优先包含材质，回退为通用）
                    string move = (character.Running ? "run" : (type == CharacterSoundMaker.FootStepTypes.runLight || type == CharacterSoundMaker.FootStepTypes.runHeavy) ? "run" : "walk");


                    string strength = (type == CharacterSoundMaker.FootStepTypes.walkHeavy || type == CharacterSoundMaker.FootStepTypes.runHeavy) ? "heavy" : "light";
                    string material = character.FootStepMaterialType.ToString().ToLowerInvariant();
                    string skSpecific = $"footstep_{move}_{strength}_{material}";
                    string skGeneric = $"footstep_{move}_{strength}";

                    // 绑定/获取上下文
                    EnemyContext ctx = null;
                    var go = character.gameObject;
                    if (go != null)
                    {
                        if (!EnemyContextRegistry.TryGet(go, out ctx) || ctx == null)
                        {
                            try { ctx = EnemyContextRegistry.Register(character, character.AudioVoiceType, character.FootStepMaterialType); } catch { }
                        }


                    }
                    if (ctx == null) return true; // 保留原声

                        // 调试：记录本次匹配上下文与 soundKey
                        FootstepLogger.DebugDetail($"[CFS:Route] OnFootStep: skSpec={skSpecific}, skGen={skGeneric}, vt={ctx.VoiceType}, team={ctx.GetTeamNormalized()}, rank={ctx.GetRank()}, nameKey={ctx.NameKey}, icon={ctx.IconType}, mat={material}");


                    // 规则匹配：优先具体（含材质），未命中再尝试通用
                    VoiceRoute route = null;
                    FootstepLogger.DebugDetail($"[CFS:Route] 开始匹配: sk={skSpecific}, engine={(CustomFootStepSounds.Engine != null ? "已加载" : "未加载")}");
                    bool matched = (CustomFootStepSounds.Engine != null && CustomFootStepSounds.Engine.TryRoute(ctx, skSpecific, ctx.VoiceType, out route));
                    FootstepLogger.DebugDetail($"[CFS:Route] 首次匹配结果: matched={matched}, route={(route != null ? $"UseCustom={route.UseCustom}, File={route.FileFullPath}" : "null")}");

                        // Cooldown check (footstep) after acquiring GameObject
                        int id = go != null ? go.GetInstanceID() : 0;
                        float minCd = GetMinCooldownSeconds();
                        if (minCd > 0f && IsOnCooldownAndTouch(id, false, minCd, out var remain))
                        {
                            FootstepLogger.DebugDetail($"[CFS:Cooldown] footstep SKIP id={id} remain={remain:F2}s (min={minCd:F2}s)");
                            return false; // mute original as well
                        }


                    if (!(matched && route != null && route.UseCustom && !string.IsNullOrEmpty(route.FileFullPath)))
                    {
                        matched = (CustomFootStepSounds.Engine != null && CustomFootStepSounds.Engine.TryRoute(ctx, skGeneric, ctx.VoiceType, out route));
                    }
                    if (!(matched && route != null && route.UseCustom && !string.IsNullOrEmpty(route.FileFullPath)))


                    {
                        bool useSimple = CustomFootStepSounds.Config != null && CustomFootStepSounds.Config.UseSimpleRules;
                        if (useSimple)
                        {
                            int sCount = CustomFootStepSounds.Config?.SimpleRules?.Count ?? 0;
                            FootstepLogger.DebugDetail($"[CFS:Route] 未命中（UseSimpleRules=true）：未命中任何 SimpleRules（SimpleRules={sCount}）");
                        }
                        else
                        {
                            FootstepLogger.DebugDetail("[CFS:Route] 未命中（UseSimpleRules=false）：复杂规则/默认模板未命中；可开启 CustomEnemySounds=Debug 查看 [CES:Path] cand/exists");
                        }
                        return true; // 未匹配 - 让原逻辑继续
                    }

                        // 记录命中信息与候选路径（仅在命中时打印）
                        FootstepLogger.DebugDetail($"[CFS:Route] 命中: rule={route.MatchRule}, file={route.FileFullPath}");
                        try
                        {
                            if (route.TriedPaths != null && route.TriedPaths.Count > 0)
                            {
                                for (int i = 0; i < route.TriedPaths.Count; i++)
                                {
                                    FootstepLogger.DebugDetail($"[CFS:Path] tried[{i}]: {route.TriedPaths[i]}");
                                }
                            }
                        }
                        catch { }


                    // 使用新接口播放自定义 3D 脚步声
                    // 注意：新接口自动处理 3D 距离、自动跟随 GameObject、自动资源清理
                    try
                    {
                        var eventInstance = Duckov.AudioManager.PostCustomSFX(route.FileFullPath, go, loop: false);
                        if (eventInstance.HasValue && eventInstance.Value.isValid())
                        {
                            // 设置音量（如果需要）
                            try { eventInstance.Value.setVolume(ModSettings.FootstepVolumeScale); } catch { }

                            // 标记已播放
                            try { MarkPlayed(id, false); } catch { }

                            // 追踪 EventInstance
                            try { FootstepSoundTracker.Track(go.GetInstanceID(), eventInstance.Value, route.FileFullPath, skGeneric); } catch { }

                            FootstepLogger.Info($"[CFS] footstep -> {route.FileFullPath}");
                        }
                        else
                        {
                            FootstepLogger.Warning($"[CFS] PostCustomSFX 返回无效的 EventInstance");
                        }
                    }
                    catch (Exception playEx)
                    {
                        FootstepLogger.Error($"[CFS] 使用新接口播放脚步声失败", playEx);
                    }

                    // 阻止原方法执行（避免原声叠加）
                    return false;
                }
                catch (Exception ex)
                {
                    FootstepLogger.Error("OnFootStepSound Prefix", ex);
                    return true; // 容错：保留原声
                }
            }
        }

        // 2) dash 拦截：拦截所有 AudioManager.Post(eventName, go) 中的 dash 事件
        [HarmonyPatch(typeof(AudioManager))]
        public static class AudioManager_Post_FootstepDash_Prefix
        {
            [HarmonyPatch("Post", new Type[] { typeof(string), typeof(UnityEngine.GameObject) })]
            [HarmonyPrefix]
            private static bool Prefix(string eventName, UnityEngine.GameObject gameObject, ref FMOD.Studio.EventInstance? __result)
            {
                try
                {
                    if (string.IsNullOrEmpty(eventName) || gameObject == null) return true;
                    if (!eventName.Equals("Char/Footstep/dash", StringComparison.OrdinalIgnoreCase)) return true;
                    CustomFootStepSounds.EnsureLoaded();
                    // 检查 ModConfig UI 配置
                    if (!FootstepConfig.Enabled) return true;
                    // 兼容旧配置
                    if (!ModSettings.EnableCustomFootStepSounds) return true;

                    // 获取 ctx


                    EnemyContext ctx = null;
                    if (!EnemyContextRegistry.TryGet(gameObject, out ctx) || ctx == null)
                    {
                        try
                        {
                            var cmc = gameObject.GetComponent<CharacterMainControl>();
                            if (cmc != null) ctx = EnemyContextRegistry.Register(cmc, cmc.AudioVoiceType, cmc.FootStepMaterialType);
                        }
                        catch { }
                    }
                    if (ctx == null) return true;




                    // 规则匹配（dash 可根据材质扩展：dash_{material}）
                    string material = ctx.FootStepMaterialType.ToString().ToLowerInvariant();
                    VoiceRoute route = null;
                    bool matched = CustomFootStepSounds.Engine != null && CustomFootStepSounds.Engine.TryRoute(ctx, $"dash_{material}", ctx.VoiceType, out route);
                    if (!(matched && route != null && route.UseCustom && !string.IsNullOrEmpty(route.FileFullPath)))
                    {
                        matched = CustomFootStepSounds.Engine != null && CustomFootStepSounds.Engine.TryRoute(ctx, "dash", ctx.VoiceType, out route);

                        // 调试：dash 匹配上下文
                        FootstepLogger.DebugDetail($"[CFS:Route] dash: vt={ctx.VoiceType}, team={ctx.GetTeamNormalized()}, rank={ctx.GetRank()}, nameKey={ctx.NameKey}, icon={ctx.IconType}, mat={material}");

                    }
                    if (!(matched && route != null && route.UseCustom && !string.IsNullOrEmpty(route.FileFullPath)))
                    {
                        bool useSimple = CustomFootStepSounds.Config != null && CustomFootStepSounds.Config.UseSimpleRules;
                        if (useSimple)
                        {
                            int sCount = CustomFootStepSounds.Config?.SimpleRules?.Count ?? 0;
                            FootstepLogger.DebugDetail($"[CFS:Route] dash 未命中（UseSimpleRules=true）：未命中任何 SimpleRules（SimpleRules={sCount}）");
                        }
                        else
                        {
                            FootstepLogger.DebugDetail("[CFS:Route] dash 未命中（UseSimpleRules=false）：复杂规则/默认模板未命中；可开启 CustomEnemySounds=Debug 查看 [CES:Path] cand/exists");
                        }
                        return true; // 未匹配，继续原逻辑
                    }

                        // 记录 dash 命中信息与候选路径
                        FootstepLogger.DebugDetail($"[CFS:Route] dash 命中: rule={route.MatchRule}, file={route.FileFullPath}");
                        try
                        {
                            if (route.TriedPaths != null && route.TriedPaths.Count > 0)
                            {
                                for (int i = 0; i < route.TriedPaths.Count; i++)
                                {
                                    FootstepLogger.DebugDetail($"[CFS:Path] tried[{i}]: {route.TriedPaths[i]}");
                                }
                            }
                        }
                        catch { }

                    // 创建一个原事件实例并静音，保留生命周期（避免依赖 internal AudioObject.GetOrCreate）
                    try
                    {
                        FMOD.Studio.EventInstance ev;
                        if (AudioManager.TryCreateEventInstance(eventName, out ev))
                        {
                            try
                            {
                                // 静音后启动并释放，保持与原 Post 语义一致
                                try { ev.setVolume(0f); } catch { }
                                try { ev.start(); } catch { }
                                try { ev.release(); } catch { }
                            }
                            catch { }
                            __result = ev;
                        }
                    }
                    catch { }

                        // Cooldown check (dash) after creating silent original instance
                        int did = gameObject != null ? gameObject.GetInstanceID() : 0;
                        float dMinCd = GetMinCooldownSeconds();
                        if (dMinCd > 0f && IsOnCooldownAndTouch(did, true, dMinCd, out var dRemain))
                        {
                            FootstepLogger.DebugDetail($"[CFS:Cooldown] dash SKIP id={did} remain={dRemain:F2}s (min={dMinCd:F2}s)");
                            return false;
                        }


                    // 使用新接口播放自定义 3D 冲刺音效
                    // 注意：新接口自动处理 3D 距离、自动跟随 GameObject、自动资源清理
                    try
                    {
                        var eventInstance = Duckov.AudioManager.PostCustomSFX(route.FileFullPath, gameObject, loop: false);
                        if (eventInstance.HasValue && eventInstance.Value.isValid())
                        {
                            // 设置音量（如果需要）
                            try { eventInstance.Value.setVolume(ModSettings.FootstepVolumeScale); } catch { }

                            // 追踪 EventInstance
                            try { FootstepSoundTracker.Track(gameObject.GetInstanceID(), eventInstance.Value, route.FileFullPath, "dash"); } catch { }

                            // 标记已播放
                            try { MarkPlayed(gameObject != null ? gameObject.GetInstanceID() : 0, true); } catch { }

                            FootstepLogger.Info($"[CFS] dash -> {route.FileFullPath}");
                        }
                        else
                        {
                            FootstepLogger.Warning($"[CFS] PostCustomSFX 返回无效的 EventInstance");
                        }
                    }
                    catch (Exception playEx)
                    {
                        FootstepLogger.Error($"[CFS] 使用新接口播放冲刺音效失败", playEx);
                    }

                    // 阻止原方法内部再发声
                    return false;
                }
                catch (Exception ex)
                {
                    FootstepLogger.Error("AudioManager.Post(dash) Prefix", ex);
                    return true;
                }
            }
        }

        // 注意：以下辅助函数已删除，因为新接口自动处理：
        // - ComputeModeForFile：不再需要手动设置 FMOD 模式
        // - ToFMODVector：不再需要手动转换坐标
        // - ResolveSfxGroupSafe：不再需要手动获取 ChannelGroup
        // - TryGetFootEvent3DDistances：不再需要手动获取 3D 距离
        // - TryGetDash3DDistances：不再需要手动获取 3D 距离
    }
}
