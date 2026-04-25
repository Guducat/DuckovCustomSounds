using System;

namespace DuckovCustomSounds.CustomGrenadeSounds
{
    /// <summary>
    /// 自定义手雷音效模块
    /// 负责手雷音效的替换和管理
    /// </summary>
    internal static class CustomGrenadeSounds
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
                GrenadeConfig.Initialize();

                GrenadeLogger.Info("CustomGrenadeSounds 模块已初始化");
            }
            catch (Exception ex)
            {
                GrenadeLogger.Error("CustomGrenadeSounds 模块初始化失败", ex);
            }
        }
    }
}
