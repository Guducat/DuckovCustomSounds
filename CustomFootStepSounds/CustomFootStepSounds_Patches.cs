using System;
using HarmonyLib;
using UnityEngine;
using Duckov; // AudioManager, CharacterSoundMaker, CharacterMainControl, AudioObject
using DuckovCustomSounds.CustomEnemySounds; // EnemyContext & Registry
using FMOD;

namespace DuckovCustomSounds.CustomFootStepSounds
{
    internal static class CustomFootStepSounds_Patches
    {
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
                        if (!DuckovCustomSounds.CustomEnemySounds.EnemyContextRegistry.TryGet(go, out ctx) || ctx == null)
                        {
                            try { ctx = DuckovCustomSounds.CustomEnemySounds.EnemyContextRegistry.Register(character, character.AudioVoiceType, character.FootStepMaterialType); } catch { }
                        }
                    }
                    if (ctx == null) return true; // 保留原声

                    // 规则匹配：优先具体（含材质），未命中再尝试通用
                    DuckovCustomSounds.CustomEnemySounds.VoiceRoute route = null;
                    bool matched = (CustomFootStepSounds.Engine != null && CustomFootStepSounds.Engine.TryRoute(ctx, skSpecific, ctx.VoiceType, out route));
                    if (!(matched && route != null && route.UseCustom && !string.IsNullOrEmpty(route.FileFullPath)))
                    {
                        matched = (CustomFootStepSounds.Engine != null && CustomFootStepSounds.Engine.TryRoute(ctx, skGeneric, ctx.VoiceType, out route));
                    }
                    if (!(matched && route != null && route.UseCustom && !string.IsNullOrEmpty(route.FileFullPath)))
                    {
                        return true; // 未匹配 - 让原逻辑继续
                    }

                    // 推导 3D 距离：从原 FMOD 事件 footstep_{material}_{strength} 读取
                    float min = 1.5f, max = 20f;
                    try { TryGetFootEvent3DDistances(material, strength, out min, out max); } catch { }

                    // 播放自定义 3D 声音
                    var mode = ComputeModeForFile(route.FileFullPath);
                    var res = FMODUnity.RuntimeManager.CoreSystem.createSound(route.FileFullPath, mode, out var sound);
                    if (res == RESULT.OK && sound.hasHandle())
                    {
                        var group = ResolveSfxGroupSafe();
                        try { sound.set3DMinMaxDistance(min, max); } catch { }
                        var playRes = FMODUnity.RuntimeManager.CoreSystem.playSound(sound, group, true, out var ch);
                        if (playRes == RESULT.OK)
                        {
                            Transform tr = null; Vector3 pos = Vector3.zero;
                            try { tr = go != null ? go.transform : null; pos = tr != null ? tr.position : position; } catch { }
                            var fpos = ToFMODVector(pos);
                            var fvel = ToFMODVector(Vector3.zero);
                            try { ch.set3DAttributes(ref fpos, ref fvel); } catch { }
                            try { ch.set3DMinMaxDistance(min, max); } catch { }
                            try { ch.setMode(MODE._3D | MODE._3D_LINEARROLLOFF | MODE.LOOP_OFF); } catch { }
                            try { ch.setPaused(false); } catch { }

                            try { FootstepSoundTracker.Track(go.GetInstanceID(), sound, ch, route.FileFullPath, skGeneric, tr); } catch { }
                            FootstepLogger.Info($"[CFS] footstep -> {route.FileFullPath} (min={min:F1}, max={max:F1})");
                        }
                        else { try { sound.release(); } catch { } }
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
                    if (!ModSettings.EnableCustomFootStepSounds) return true;

                    // 获取 ctx
                    EnemyContext ctx = null;
                    if (!DuckovCustomSounds.CustomEnemySounds.EnemyContextRegistry.TryGet(gameObject, out ctx) || ctx == null)
                    {
                        try
                        {
                            var cmc = gameObject.GetComponent<CharacterMainControl>();
                            if (cmc != null) ctx = DuckovCustomSounds.CustomEnemySounds.EnemyContextRegistry.Register(cmc, cmc.AudioVoiceType, cmc.FootStepMaterialType);
                        }
                        catch { }
                    }
                    if (ctx == null) return true;

                    // 规则匹配（dash 可根据材质扩展：dash_{material}）
                    string material = ctx.FootStepMaterialType.ToString().ToLowerInvariant();
                    DuckovCustomSounds.CustomEnemySounds.VoiceRoute route = null;
                    bool matched = CustomFootStepSounds.Engine != null && CustomFootStepSounds.Engine.TryRoute(ctx, $"dash_{material}", ctx.VoiceType, out route);
                    if (!(matched && route != null && route.UseCustom && !string.IsNullOrEmpty(route.FileFullPath)))
                    {
                        matched = CustomFootStepSounds.Engine != null && CustomFootStepSounds.Engine.TryRoute(ctx, "dash", ctx.VoiceType, out route);
                    }
                    if (!(matched && route != null && route.UseCustom && !string.IsNullOrEmpty(route.FileFullPath)))
                    {
                        return true; // 未匹配，继续原逻辑
                    }

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

                    float min = 2f, max = 30f;
                    try { TryGetDash3DDistances(out min, out max); } catch { }

                    var mode = ComputeModeForFile(route.FileFullPath);
                    var res = FMODUnity.RuntimeManager.CoreSystem.createSound(route.FileFullPath, mode, out var sound);
                    if (res == RESULT.OK && sound.hasHandle())
                    {
                        var group = ResolveSfxGroupSafe();
                        try { sound.set3DMinMaxDistance(min, max); } catch { }
                        var playRes = FMODUnity.RuntimeManager.CoreSystem.playSound(sound, group, true, out var ch);
                        if (playRes == RESULT.OK)
                        {
                            Transform tr = null; Vector3 pos = Vector3.zero;
                            try { tr = gameObject.transform; pos = tr.position; } catch { }
                            var fpos = ToFMODVector(pos);
                            var fvel = ToFMODVector(Vector3.zero);
                            try { ch.set3DAttributes(ref fpos, ref fvel); } catch { }
                            try { ch.set3DMinMaxDistance(min, max); } catch { }
                            try { ch.setMode(MODE._3D | MODE._3D_LINEARROLLOFF | MODE.LOOP_OFF); } catch { }
                            try { ch.setPaused(false); } catch { }
                            try { FootstepSoundTracker.Track(gameObject.GetInstanceID(), sound, ch, route.FileFullPath, "dash", tr); } catch { }
                            FootstepLogger.Info($"[CFS] dash -> {route.FileFullPath} (min={min:F1}, max={max:F1})");
                        }
                        else { try { sound.release(); } catch { } }
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

        private static MODE ComputeModeForFile(string path)
        {
            try
            {
                var ext = System.IO.Path.GetExtension(path)?.ToLowerInvariant();
                var baseMode = MODE._3D | MODE._3D_LINEARROLLOFF | MODE.LOOP_OFF;
                if (ext == ".mp3" || ext == ".ogg" || ext == ".flac")
                    return baseMode | MODE.CREATESTREAM;
                return baseMode; // wav/aiff
            }
            catch { return MODE._3D | MODE._3D_LINEARROLLOFF | MODE.LOOP_OFF; }
        }
        private static FMOD.VECTOR ToFMODVector(UnityEngine.Vector3 v) => new FMOD.VECTOR { x = v.x, y = v.y, z = v.z };
        private static ChannelGroup ResolveSfxGroupSafe()
        {
            try
            {
                try
                {
                    var sfxBus = FMODUnity.RuntimeManager.GetBus("bus:/Master/SFX");
                    if (sfxBus.getChannelGroup(out var cg) == RESULT.OK && cg.hasHandle()) return cg;
                }
                catch { }
                if (ModBehaviour.SfxGroup.hasHandle()) return ModBehaviour.SfxGroup;
                if (FMODUnity.RuntimeManager.CoreSystem.getMasterChannelGroup(out var master) == RESULT.OK && master.hasHandle())
                    return master;
            }
            catch { }
            return default;
        }

        private static bool TryGetFootEvent3DDistances(string material, string strength, out float min, out float max)
        {
            min = 1.5f; max = 20f;
            try
            {
                string eventPath = $"Char/Footstep/footstep_{material}_{strength}";
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

        private static bool TryGetDash3DDistances(out float min, out float max)
        {
            min = 2f; max = 30f;
            try
            {
                FMOD.Studio.EventInstance ev;
                if (AudioManager.TryCreateEventInstance("Char/Footstep/dash", out ev))
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
    }
}

