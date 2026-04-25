using System;

namespace DuckovCustomSounds.CustomMeleeSounds
{
    /// <summary>
    /// 自定义近战音效模块
    /// 负责近战音效的替换和管理
    /// </summary>
    internal static class CustomMeleeSounds
    {
        private static bool _initialized;

        /// <summary>
        /// 初始化模块
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                // 加载配置
                MeleeConfig.Initialize();

                MeleeLogger.Info("CustomMeleeSounds 模块已初始化");
            }
            catch (Exception ex)
            {
                MeleeLogger.Error("CustomMeleeSounds 模块初始化失败", ex);
            }
        }
    }
}
