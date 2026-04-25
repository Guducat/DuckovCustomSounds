using System;
using System.Collections.Generic;
using System.IO;
using DuckovCustomSounds.CustomEnemySounds.Context;

namespace DuckovCustomSounds.API
{
    /// <summary>
    /// 外部 Provider 注册中心与聚合器。
    /// </summary>
    public static class ExternalRouter
    {
        private static readonly object _gate = new object();
        private static readonly List<ProviderEntry> _providers = new List<ProviderEntry>();

        private sealed class ProviderEntry
        {
            public string ModId { get; set; } = string.Empty;
            public IVoicePackProvider Provider { get; set; } = null!;
        }

        /// <summary>注册一个语音包提供者。相同 modId 会替换原 Provider，并保留当前位置。</summary>
        public static bool Register(string modId, IVoicePackProvider provider)
        {
            if (string.IsNullOrWhiteSpace(modId) || provider == null) return false;
            lock (_gate)
            {
                for (var i = 0; i < _providers.Count; i++)
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals(_providers[i].ModId, modId))
                    {
                        _providers[i] = new ProviderEntry { ModId = modId, Provider = provider };
                        return true;
                    }
                }

                _providers.Add(new ProviderEntry { ModId = modId, Provider = provider });
            }
            return true;
        }

        /// <summary>注销一个语音包提供者。</summary>
        public static bool Unregister(string modId)
        {
            if (string.IsNullOrWhiteSpace(modId)) return false;
            lock (_gate)
            {
                for (var i = 0; i < _providers.Count; i++)
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals(_providers[i].ModId, modId))
                    {
                        _providers.RemoveAt(i);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 让已注册的 Provider 解析语音文件。按注册顺序遍历，先命中者优先。
        /// </summary>
        public static bool TryResolve(EnemyContextData ctx, string soundKey, string voiceType, out string fullPath)
        {
            fullPath = string.Empty;
            IVoicePackProvider[] snapshot;
            lock (_gate)
            {
                var list = new List<IVoicePackProvider>(_providers.Count);
                foreach (var entry in _providers)
                {
                    list.Add(entry.Provider);
                }
                snapshot = list.ToArray();
            }

            foreach (var provider in snapshot)
            {
                try
                {
                    if (provider.TryResolve(ctx, soundKey, voiceType, out var filePath) && !string.IsNullOrWhiteSpace(filePath))
                    {
                        fullPath = Path.IsPathRooted(filePath)
                            ? filePath
                            : Path.Combine(ModBehaviour.ModFolderName, filePath.Replace('/', Path.DirectorySeparatorChar));
                        return true;
                    }
                }
                catch
                {
                }
            }
            return false;
        }

        internal static EnemyContextData FromInternal(EnemyContext? ctx)
        {
            if (ctx == null) return new EnemyContextData { IsValid = false };
            return new EnemyContextData
            {
                InstanceId = ctx.InstanceId,
                Team = ctx.GetTeamNormalized(),
                Rank = ctx.GetRank(),
                EnemyType = ctx.EnemyType ?? string.Empty,
                NameKey = ctx.NameKey ?? string.Empty,
                Health = ctx.Health,
                IconType = ctx.IconType ?? string.Empty,
                Transform = ctx.GameObject != null ? ctx.GameObject.transform : null,
                IsValid = true,
            };
        }
    }
}
