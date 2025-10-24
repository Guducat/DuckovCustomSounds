using System.Collections.Generic;
using UnityEngine;

namespace DuckovCustomSounds.CustomEnemySounds
{
    /// <summary>
    /// 敌人 -> 语音变体索引 绑定表（可选启用）。
    /// - 仅在配置 BindVariantIndexPerEnemy=true 时生效
    /// - 首次请求时在 [0, availableCount) 之间随机分配并缓存
    /// - 敌人销毁/移除时需清理映射
    /// </summary>
    internal static class VariantIndexBinder
    {
        private static readonly Dictionary<int, int> _byOwner = new Dictionary<int, int>();

        private static bool Enabled => (CustomEnemySounds.Config?.BindVariantIndexPerEnemy ?? false);

        public static bool TryGet(int ownerId, out int index)
        {
            return _byOwner.TryGetValue(ownerId, out index);
        }

        /// <summary>
        /// 获取或分配绑定索引。仅在启用时有效；未启用直接返回 0。
        /// </summary>
        public static int GetOrAllocate(int ownerId, int availableCount)
        {
            if (!Enabled)
            {
                return 0;
            }
            if (availableCount <= 0) availableCount = 1;
            if (_byOwner.TryGetValue(ownerId, out var idx))
            {
                return idx;
            }
            int newIndex = Random.Range(0, availableCount); // [0, availableCount)
            _byOwner[ownerId] = newIndex;
            CESLogger.Debug($"[CES:Variant] 为敌人 {ownerId} 绑定变体索引 {newIndex}（共 {availableCount} 个变体）");
            return newIndex;
        }

        public static void Remove(int ownerId)
        {
            _byOwner.Remove(ownerId);
        }

        public static void Clear()
        {
            _byOwner.Clear();
        }
    }
}

