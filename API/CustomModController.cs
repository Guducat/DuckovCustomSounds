using System;
using System.IO;
using FMOD;

using DuckovCustomSounds.CustomEnemySounds.Audio;
using DuckovCustomSounds.CustomEnemySounds.Context;
using DuckovCustomSounds.CustomEnemySounds.Filters;
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
        /// 使用新接口 PostCustomSFX 替代 FMOD Core API。
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

                // 使用新接口播放 3D 音效
                // 注意：新接口自动处理 3D 距离、自动跟随 GameObject、自动资源清理
                var eventInstance = Duckov.AudioManager.PostCustomSFX(fullPath, owner, loop: false);
                if (!eventInstance.HasValue || !eventInstance.Value.isValid())
                {
                    return false; // 无法创建 EventInstance
                }

                // 纳入生命周期管理
                CoreSoundTracker.Track(ownerId, eventInstance.Value, fullPath, request.SoundKey ?? "normal", priority);

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

        // 注意：ComputeModeForFile 辅助函数已删除，因为新接口自动处理 FMOD 模式
    }
}

