using System;
using DuckovCustomSounds.CustomEnemySounds; // reuse Engine/Config, PathBuilder
using DuckovCustomSounds.CustomEnemySounds.Config;
using DuckovCustomSounds.CustomEnemySounds.Rules;
using DuckovCustomSounds.Logging;

namespace DuckovCustomSounds.CustomFootStepSounds
{
    internal static class CustomFootStepSounds
    {
        public static VoiceConfig Config { get; private set; }
        public static VoiceRuleEngine Engine { get; private set; }
        private static bool _loaded;
        private static bool _initialized;

        /// <summary>
        /// 初始化模块（加载 ModConfig UI 配置）
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                // 初始化 ModConfig UI 配置
                FootstepConfig.Initialize();

                FootstepLogger.Info("CustomFootStepSounds 模块已初始化");
            }
            catch (Exception ex)
            {
                FootstepLogger.Error("CustomFootStepSounds 模块初始化失败", ex);
            }
        }

        public static void EnsureLoaded()
        {
            if (_loaded) return;
            if (!ModSettings.EnableCustomFootStepSounds)
            {
                FootstepLogger.Info("[CFS] CustomFootStepSounds disabled by settings");
                _loaded = true; // prevent repeated attempts
                return;
            }
            try
            {
                Config = FootstepConfigLoader.Load();
                if (Engine == null) Engine = new VoiceRuleEngine();
                Engine.Reload(Config);
                FootstepSoundTracker.EnsureStarted();
                _loaded = true;
                FootstepLogger.Info("[CFS] Loaded");
            }
            catch (Exception ex)
            {
                _loaded = true; // avoid repeated crashes
                FootstepLogger.Error("EnsureLoaded failed", ex);
            }
        }

        /// <summary>
        /// 重新加载配置（用于热重载）
        /// </summary>
        public static void Reload()
        {
            try
            {
                Config = FootstepConfigLoader.Load();
                if (Engine == null) Engine = new VoiceRuleEngine();
                Engine.Reload(Config);
                FootstepLogger.Info("[CFS] 配置已重新加载");
            }
            catch (Exception ex)
            {
                FootstepLogger.Error("重新加载配置失败", ex);
            }
        }

        public static void Unload()
        {
            try { FootstepSoundTracker.StopAndClear(); } catch { }
            _loaded = false;
        }
    }
}

