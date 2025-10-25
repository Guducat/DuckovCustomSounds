using System;
using System.Collections.Generic;

namespace DuckovCustomSounds.CustomEnemySounds
{
    /// <summary>
    /// 声音优先级策略：将 soundKey 字符串映射为一个数值优先级，数值越大优先级越高。
    /// 可扩展：未来如需从配置表读取映射，可在此扩展。
    /// </summary>
    internal static class PriorityPolicy
    {
        private static readonly Dictionary<string, int> Map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            // 高 > 低：death > surprise > grenade > normal（其他未列出的走 normal）
            { "death", 100 },
            { "surprise", 50 },
            { "grenade", 40 },
            { "normal", 10 },
        };

        public static int GetPriority(string soundKey)
        {
            if (string.IsNullOrEmpty(soundKey)) return 0;
            if (Map.TryGetValue(soundKey, out var p)) return p;
            // 未知 soundKey 按普通优先级处理
            return 10;
        }
    }
}

