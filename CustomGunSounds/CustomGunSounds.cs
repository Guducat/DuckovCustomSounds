using System;

namespace DuckovCustomSounds.CustomGunSounds
{
    /// <summary>
    /// 自定义枪械音效模块
    /// 负责枪械音效的替换和管理
    /// </summary>
    internal static class CustomGunSounds
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
                GunConfig.Initialize();

                GunLogger.Info("CustomGunSounds 模块已初始化");
            }
            catch (Exception ex)
            {
                GunLogger.Error("CustomGunSounds 模块初始化失败", ex);
            }
        }
    }
}
