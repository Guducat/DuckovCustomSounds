using System;

namespace DuckovCustomSounds.CustomItemSounds
{
    /// <summary>
    /// 自定义物品音效模块
    /// 负责物品使用音效的替换和管理
    /// </summary>
    internal static class CustomItemSounds
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
                ItemConfig.Initialize();
                // 加载物品声音映射（JSON）
                ItemSoundMap.Initialize();

                ItemLogger.Info("CustomItemSounds 模块已初始化");
            }
            catch (Exception ex)
            {
                ItemLogger.Error("CustomItemSounds 模块初始化失败", ex);
            }
        }
    }
}
