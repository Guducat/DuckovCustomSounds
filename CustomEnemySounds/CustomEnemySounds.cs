using System;

namespace DuckovCustomSounds.CustomEnemySounds
{
    /// <summary>
    /// 自定义敌人音效模块入口。保存配置/引擎状态和生命周期钩子。
    /// </summary>
    internal static class CustomEnemySounds
    {
        public static VoiceConfig Config { get; private set; }
        public static VoiceRuleEngine Engine { get; } = new VoiceRuleEngine();
        public static bool IsLoaded { get; private set; }

        public static void Load()
        {
            try
            {
                Config = ConfigLoader.Load();
                Engine.Reload(Config);
                IsLoaded = true;
                CESLogger.Info("CustomEnemySounds 已加载，等待触发。");
                try { CoreSoundTracker.EnsureStarted(); } catch { }
            }
            catch (Exception ex)
            {
                CESLogger.Error("CustomEnemySounds.Load 失败", ex);
            }
        }

        public static void EnsureLoaded()
        {
            if (!IsLoaded) Load();
        }

        public static void Unload()
        {
            try
            {
                EnemyContextRegistry.Clear();
                try { CoreSoundTracker.StopAndClear(); } catch { }
                IsLoaded = false;
            }
            catch (Exception ex)
            {
                CESLogger.Error("CustomEnemySounds.Unload 失败", ex);
            }
        }
    }
}

