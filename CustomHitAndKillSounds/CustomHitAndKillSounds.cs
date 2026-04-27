using System;
using System.IO;

namespace DuckovCustomSounds.CustomHitAndKillSounds
{
    internal static class CustomHitAndKillSounds
    {
        private static bool initialized;

        public static string ModuleDirectory => Path.Combine(ModBehaviour.ModFolderName, "CustomHitAndKillSounds");

        public static void Initialize()
        {
            if (initialized)
                return;

            try
            {
                HitAndKillConfig.Initialize();
                HitAndKillRuntimeHooks.Subscribe();
                initialized = true;
                HitAndKillLogger.Info("CustomHitAndKillSounds 模块已初始化");
            }
            catch (Exception ex)
            {
                HitAndKillLogger.Error("CustomHitAndKillSounds 模块初始化失败", ex);
            }
        }

        public static void Unload()
        {
            try
            {
                HitAndKillRuntimeHooks.Unsubscribe();
                HitAndKillConfig.Deinitialize();
                initialized = false;
                HitAndKillLogger.Info("CustomHitAndKillSounds 模块已卸载");
            }
            catch (Exception ex)
            {
                HitAndKillLogger.Warning($"CustomHitAndKillSounds 模块卸载失败: {ex.Message}");
            }
        }
    }
}
