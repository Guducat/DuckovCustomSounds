using System;
using DuckovCustomSounds.Logging;
using DuckovCustomSounds.CustomEnemySounds.Audio;
using DuckovCustomSounds.CustomEnemySounds.Config;
using DuckovCustomSounds.CustomEnemySounds.Context;
using DuckovCustomSounds.CustomEnemySounds.Filters;
using DuckovCustomSounds.CustomEnemySounds.Rules;

namespace DuckovCustomSounds.CustomEnemySounds
{
    /// <summary>
    /// CustomEnemySounds 主模块 - 敌人自定义声音管理器
    /// 负责初始化所有子模块，管理预设缓存，提供统一的加载/卸载接口
    /// </summary>
    internal static class CustomEnemySounds
    {
        public static VoiceConfig Config { get; private set; } = new VoiceConfig();
        public static VoiceRuleEngine Engine { get; } = new VoiceRuleEngine(CESLogger.Info, CESLogger.Debug, CESLogger.Verbose, "CES:Rule");
        public static bool IsLoaded { get; private set; }

        /// <summary>
        /// 加载 CustomEnemySounds 模块
        /// 由 ModBehaviour.OnEnable() 调用
        /// </summary>
        public static void Load()
        {
            try
            {
                CESLogger.Info("[CES] 开始加载 CustomEnemySounds...");

                // 1. 加载配置
                Config = ConfigLoader.Load();
                Engine.Reload(Config);

                // 2. 加载预设缓存（优化性能）
                PresetCache.Load();

                // 3. 初始化其他模块
                EnemyVoiceOptions.Initialize();

                IsLoaded = true;
                CESLogger.Info("[CES] CustomEnemySounds 已加载，等待触发。");

                // 4. 启动后台服务
                try { CoreSoundTracker.EnsureStarted(); } catch { }
                try { CustomEnemySounds_Patches.EnableDeathEventHook(); } catch { }
            }
            catch (Exception ex)
            {
                CESLogger.Error("[CES] CustomEnemySounds.Load 失败", ex);
            }
        }

        /// <summary>
        /// 确保模块已加载
        /// </summary>
        public static void EnsureLoaded()
        {
            if (!IsLoaded) Load();
        }

        /// <summary>
        /// 卸载 CustomEnemySounds 模块
        /// </summary>
        public static void Unload()
        {
            try
            {
                EnemyContextRegistry.Clear();
                PresetCache.Clear();
                try { CoreSoundTracker.StopAndClear(); } catch { }
                try { CustomEnemySounds_Patches.DisableDeathEventHook(); } catch { }
                EnemyVoiceOptions.Deinitialize();
                EnemyVoiceFilter.ClearCaches();
                IsLoaded = false;
                CESLogger.Info("[CES] CustomEnemySounds 已卸载");
            }
            catch (Exception ex)
            {
                CESLogger.Error("[CES] CustomEnemySounds.Unload 失败", ex);
            }
        }

        /// <summary>
        /// 重新加载预设缓存（用于处理动态添加的预设）
        /// </summary>
        public static void ReloadPresets()
        {
            try
            {
                PresetCache.Reload();
                CESLogger.Info("[CES] 预设缓存已重新加载");
            }
            catch (Exception ex)
            {
                CESLogger.Error("[CES] 重新加载预设失败", ex);
            }
        }

        /// <summary>
        /// 获取模块状态信息
        /// </summary>
        public static string GetStatusInfo()
        {
            if (!IsLoaded)
                return "未加载";

            return $"已加载 | 预设缓存: {PresetCache.PresetCount}个 | 配置: {(Config != null ? "已加载" : "未加载")}";
        }
    }
}
