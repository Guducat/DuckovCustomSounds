using System;

namespace DuckovCustomSounds.CustomBGM.Core
{
    /// <summary>
    /// BGMConfig 已废弃：HomeBGM 配置已迁移到 HomeBGM.HomeBGMConfig
    /// 保留此类仅为向后兼容（避免编译错误）
    /// </summary>
    [Obsolete("HomeBGM 配置已迁移到 HomeBGM.HomeBGMConfig，请使用新配置")]
    public static class BGMConfig
    {
        // 向后兼容属性（委托到新配置）
        public static bool RandomEnabled => HomeBGM.HomeBGMConfig.RandomEnabled;
        public static bool RandomizePrevious => HomeBGM.HomeBGMConfig.RandomizePrevious;
        public static bool AvoidImmediateRepeat => HomeBGM.HomeBGMConfig.AvoidImmediateRepeat;

        [Obsolete("不再需要调用，HomeBGMConfig 会自动初始化")]
        public static void Initialize()
        {
            // 空实现，保持兼容性
        }
    }
}
