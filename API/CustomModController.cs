using System;
using System.IO;
using FMOD;

using DuckovCustomSounds.CustomEnemySounds; // 访问内部注册表
using UnityEngine;

namespace DuckovCustomSounds.API
{
    /// <summary>
    /// 对外统一门面（Facade）。
    /// 外部 Mod 推荐仅依赖本类与 IVoicePackProvider/EnemyContextData/PlaybackRequest。
    /// </summary>
    public static class CustomModController
    {
        /// <summary>注册外部 Provider</summary>
        public static bool RegisterVoicePackProvider(string modId, IVoicePackProvider provider) => ExternalRouter.Register(modId, provider);
        /// <summary>注销外部 Provider</summary>
        public static bool UnregisterVoicePackProvider(string modId) => ExternalRouter.Unregister(modId);

        /// <summary>
        /// 查询某个 GameObject（敌人/实体）的上下文快照。
        /// 当不可用时返回 false（例如该对象未在本 Mod 注册）。
        /// </summary>
        public static bool TryGetEnemyContext(GameObject owner, out EnemyContextData data)
        {
            data = null;
            try
            {
                if (owner == null) return false;
                if (EnemyContextRegistry.TryGet(owner, out var ctx) && ctx != null)
                {
                    data = ExternalRouter.FromInternal(ctx, ctx.VoiceType);
                    return data != null && data.IsValid;
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// 直接播放 3D 音效（强依赖方式）。
        /// 注意：完整实现位于后续任务，将在 Task 3 中补齐。
        /// 当前为占位实现，始终返回 false。
        /// </summary>
        public static bool Play3D(PlaybackRequest request, out object handle)
        {
            handle = null;
            try
            {
                if (request == null) return false;
                if (string.IsNullOrWhiteSpace(request.FileFullPath)) return false;

                var owner = request.Source;
                int ownerId = owner != null ? owner.GetInstanceID() : 0;

                // 优先级策略
                int priority = request.Priority ?? PriorityPolicy.GetPriority(request.SoundKey ?? "normal");
                if (!CoreSoundTracker.PreCheckAndMaybeInterrupt(ownerId, request.SoundKey ?? "normal", priority))
                {
                    return false; // 被更高优先级声音保留
                }

                // 规范化路径（相对路径相对于 Mod 根）
                var fullPath = request.FileFullPath;
                if (!Path.IsPathRooted(fullPath))
                {
                    fullPath = Path.Combine(DuckovCustomSounds.ModBehaviour.ModFolderName, request.FileFullPath.Replace('/', Path.DirectorySeparatorChar));
                }

                // 创建 Sound
                var mode = ComputeModeForFile(fullPath);
                var res = FMODUnity.RuntimeManager.CoreSystem.createSound(fullPath, mode, out var sound);
                if (res != RESULT.OK || !sound.hasHandle())
                {
                    return false; // 无法创建声音
                }

                try { sound.set3DMinMaxDistance(request.MinDistance, request.MaxDistance); } catch { }

                // 选择 SFX ChannelGroup
                ChannelGroup group = default;
                if (DuckovCustomSounds.ModBehaviour.SfxGroup.hasHandle()) group = DuckovCustomSounds.ModBehaviour.SfxGroup;
                else
                {
                    try
                    {
                        var sfx = FMODUnity.RuntimeManager.GetBus("bus:/Master/SFX");
                        var r2 = sfx.getChannelGroup(out var cg);
                        if (r2 == RESULT.OK && cg.hasHandle()) group = cg;
                    }
                    catch { }
                }

                // 播放（先暂停，设置3D属性后解除暂停）
                Channel ch;
                var ok = FMODUnity.RuntimeManager.CoreSystem.playSound(sound, group, true, out ch);
                if (ok != RESULT.OK || !ch.hasHandle())
                {
                    try { sound.release(); } catch { }
                    return false;
                }

                try
                {
                    if (owner != null && request.FollowTransform)
                    {
                        var pos = owner.transform.position;
                        var fpos = new FMOD.VECTOR { x = pos.x, y = pos.y, z = pos.z };
                        var fvel = new FMOD.VECTOR { x = 0f, y = 0f, z = 0f };
                        try { ch.set3DAttributes(ref fpos, ref fvel); } catch { }
                    }
                }
                catch { }

                try { ch.setPaused(false); } catch { }

                // 纳入生命周期管理
                var emitter = (owner != null && request.FollowTransform) ? owner.transform : null;
                CoreSoundTracker.Track(ownerId, sound, ch, fullPath, request.SoundKey ?? "normal", priority, emitter);

                handle = ownerId; // 简易句柄：返回所有者ID
                return true;
            }
            catch
            {
                // 任何异常都不应让游戏崩溃
            }
            return false;
        }

        /// <summary>
        /// 按所有者停止当前跟踪的 3D 播放。
        /// 注意：完整实现位于后续任务，将在 Task 3 中补齐。
        /// </summary>
        public static void StopByOwner(GameObject owner)
        {
            try
            {
                if (owner == null) return;
                CoreSoundTracker.StopByOwner(owner.GetInstanceID());
            }
            catch { }
        }

        // --- 私有工具 ---
        private static MODE ComputeModeForFile(string path)
        {
            var ext = string.Empty;
            try { ext = Path.GetExtension(path)?.ToLowerInvariant() ?? string.Empty; } catch { }
            bool streaming = ext == ".mp3" || ext == ".ogg" || ext == ".flac";
            var mode = MODE._3D | MODE.LOOP_OFF;
            mode |= streaming ? MODE.CREATESTREAM : MODE.CREATESAMPLE;
            return mode;
        }

    }
}

