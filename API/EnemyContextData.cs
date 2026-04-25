using UnityEngine;

namespace DuckovCustomSounds.API
{
    /// <summary>
    /// 敌人上下文对外只读数据（供外部 Mod 使用）。
    /// 该数据为拍扁后的轻量 DTO，不暴露游戏内部复杂对象，仅包含必要信息。
    /// </summary>
    public class EnemyContextData
    {
        /// <summary>上下文所属实体的 InstanceID（可用于 StopByOwner 等）</summary>
        public int InstanceId { get; set; }
        /// <summary>队伍（归一化为小写，如 scavs/bear/usec/rogue/boss/...）</summary>
        public string Team { get; set; }
        /// <summary>阶级/档位（boss/elite/normal 等）</summary>
        public string Rank { get; set; }
        /// <summary>敌人类型（拍扁后的字符串）</summary>
        public string EnemyType { get; set; }
        /// <summary>名字键（游戏内标识）</summary>
        public string NameKey { get; set; }
        /// <summary>当前生命值（快照）</summary>
        public float Health { get; set; }
        /// <summary>图标类型（原始）</summary>
        public string IconType { get; set; }
        /// <summary>Transform（用于 3D 跟随；可能为 null）</summary>
        public Transform Transform { get; set; }
        /// <summary>是否有效（内部校验）</summary>
        public bool IsValid { get; set; }
    }
}

