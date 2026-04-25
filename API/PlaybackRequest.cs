using UnityEngine;

namespace DuckovCustomSounds.API
{
    /// <summary>
    /// 直接 3D 播放的请求参数。
    /// </summary>
    public sealed class PlaybackRequest
    {
        /// <summary>发声体（用于 3D 属性与跟随、以及所有者标识）</summary>
        public GameObject Source { get; set; }
        /// <summary>音频文件完整路径（绝对路径或可被解析为绝对路径）</summary>
        public string FileFullPath { get; set; }
        /// <summary>语音键（用于优先级策略；可选，留空按 normal 处理）</summary>
        public string SoundKey { get; set; } = "normal";
        /// <summary>最小距离（3D 衰减起点）</summary>
        public float MinDistance { get; set; } = 1.5f;
        /// <summary>最大距离（3D 衰减终点）</summary>
        public float MaxDistance { get; set; } = 25f;
        /// <summary>是否跟随 Source 的 Transform（默认 true）</summary>
        public bool FollowTransform { get; set; } = true;
        /// <summary>自定义优先级（可选；不设置则按 SoundKey 映射）</summary>
        public int? Priority { get; set; }
    }
}

