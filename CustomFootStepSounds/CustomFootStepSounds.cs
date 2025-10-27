using System;
using DuckovCustomSounds.CustomEnemySounds; // reuse Engine/Config, PathBuilder
using DuckovCustomSounds.Logging;

namespace DuckovCustomSounds.CustomFootStepSounds
{
    internal static class CustomFootStepSounds
    {
        public static VoiceConfig Config { get; private set; }
        public static VoiceRuleEngine Engine { get; private set; }
        private static bool _loaded;

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

        public static void Unload()
        {
            try { FootstepSoundTracker.StopAndClear(); } catch { }
            _loaded = false;
        }
    }
}

