using System;
using System.Collections.Generic;
using System.IO;
using Duckov; // 仅用于 VoiceType to string
using DuckovCustomSounds.CustomEnemySounds; // 访问内部 EnemyContext

namespace DuckovCustomSounds.API
{
    /// <summary>
    /// 外部 Provider 注册中心与聚合器。
    /// - 外部 Mod 通过 Register/Unregister 注册自身的 IVoicePackProvider
    /// - 本 Mod 在内部路由前调用 TryResolve，若返回 true 则直接使用外部路径
    /// </summary>
    public static class ExternalRouter
    {
        private static readonly object _gate = new object();
        private static readonly Dictionary<string, IVoicePackProvider> _providers = new Dictionary<string, IVoicePackProvider>(StringComparer.OrdinalIgnoreCase);

        /// <summary>注册一个语音包提供者（按 modId 识别并可覆盖同名）</summary>
        public static bool Register(string modId, IVoicePackProvider provider)
        {
            if (string.IsNullOrWhiteSpace(modId) || provider == null) return false;
            lock (_gate) { _providers[modId] = provider; }
            return true;
        }
        /// <summary>注销一个语音包提供者</summary>
        public static bool Unregister(string modId)
        {
            if (string.IsNullOrWhiteSpace(modId)) return false;
            lock (_gate) { return _providers.Remove(modId); }
        }

        /// <summary>
        /// 尝试让已注册的 Provider 解析路径。按注册顺序遍历，先命中者优先。
        /// </summary>
        public static bool TryResolve(EnemyContextData ctx, string soundKey, string voiceType, out string fullPath)
        {
            fullPath = null;
            IVoicePackProvider[] snapshot;
            lock (_gate) { snapshot = new List<IVoicePackProvider>(_providers.Values).ToArray(); }
            foreach (var p in snapshot)
            {
                try
                {
                    if (p != null && p.TryResolve(ctx, soundKey, voiceType, out var path) && !string.IsNullOrWhiteSpace(path))
                    {
                        // 将相对路径规整为完整路径（相对于本 Mod 根目录）
                        var normalized = path;
                        if (!Path.IsPathRooted(normalized))
                        {
                            normalized = Path.Combine(ModBehaviour.ModFolderName, path.Replace('/', Path.DirectorySeparatorChar));
                        }
                        fullPath = normalized;
                        return true;
                    }
                }
                catch (Exception)
                {
                    // 外部 Provider 失败不应影响本 Mod 正常工作，吞掉异常继续尝试下一个
                }
            }
            return false;
        }

        /// <summary>
        /// 由内部调用：将内部 EnemyContext 映射为对外 DTO
        /// </summary>
        internal static EnemyContextData FromInternal(EnemyContext ctx, AudioManager.VoiceType voiceType)
        {
            if (ctx == null) return new EnemyContextData { IsValid = false };
            return new EnemyContextData
            {
                InstanceId = ctx.InstanceId,
                Team = ctx.GetTeamNormalized(),
                Rank = ctx.GetRank(),
                EnemyType = ctx.EnemyType,
                NameKey = ctx.NameKey,
                Health = ctx.Health,
                IconType = ctx.IconType,
                Transform = ctx.GameObject != null ? ctx.GameObject.transform : null,
                IsValid = true,
            };
        }
    }
}

