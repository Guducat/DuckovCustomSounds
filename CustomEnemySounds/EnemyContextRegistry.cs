using System.Collections.Concurrent;
using UnityEngine;
using Duckov;

namespace DuckovCustomSounds.CustomEnemySounds
{
    /// <summary>
    /// 线程安全的运行时注册表，将GameObject实例ID映射到EnemyContext。
    /// </summary>
    internal static class EnemyContextRegistry
    {
        private static readonly ConcurrentDictionary<int, EnemyContext> Map = new ConcurrentDictionary<int, EnemyContext>();

        public static EnemyContext Register(CharacterMainControl cmc, AudioManager.VoiceType voiceType, AudioManager.FootStepMaterialType foot)
        {
            if (cmc == null) return null;
            var ctx = EnemyContext.FromCharacter(cmc, voiceType, foot);
            if (ctx.GameObject == null)
            {
                CESLogger.Debug("Register called with null GameObject");
                return ctx;
            }
            Map[ctx.InstanceId] = ctx;
            CESLogger.Debug($"登记敌人上下文: {ctx}");
            return ctx;
        }

        public static bool TryGet(GameObject go, out EnemyContext ctx)
        {
            ctx = null;
            if (go == null) return false;
            return Map.TryGetValue(go.GetInstanceID(), out ctx);
        }

        public static void UpdateVoiceType(GameObject go, AudioManager.VoiceType voiceType)
        {
            if (go == null) return;
            if (Map.TryGetValue(go.GetInstanceID(), out var ctx))
            {
                ctx.VoiceType = voiceType;
                CESLogger.Debug($"更新敌人语音类型: go={ctx.InstanceId} -> {voiceType}");
            }
        }

        public static void Remove(GameObject go)
        {
            if (go == null) return;
            Map.TryRemove(go.GetInstanceID(), out _);
        }

        public static void Clear()
        {
            Map.Clear();
        }
    }
}

