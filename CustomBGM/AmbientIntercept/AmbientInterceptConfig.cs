using System;
using DuckovCustomSounds.CustomBGM.Core;
using DuckovCustomSounds.ModConfig;

namespace DuckovCustomSounds.CustomBGM.AmbientIntercept
{
    /// <summary>
    /// 环境音拦截配置管理。
    /// 优先使用 ModConfig UI 配置，回退到 settings.json。
    /// </summary>
    internal static class AmbientInterceptConfig
    {
        private static readonly ModConfigScope Scope = ModConfigScopes.AmbientIntercept;
        private static readonly Action<string> _onChangedHandler = OnOptionsChanged;

        public static bool Enabled { get; private set; } = false;
        public static bool InterceptStormStingers { get; private set; } = false;

        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                LoadFromSettingsFallback();

                if (ModConfigAPI.IsAvailable())
                {
                    try
                    {
                        SetupModConfigUI();
                        ModConfigAPI.SafeAddOnOptionsChangedDelegate(_onChangedHandler);
                        LoadFromModConfig();
                        BGMLogger.Debug("环境音拦截已集成 ModConfig UI");
                    }
                    catch (Exception ex)
                    {
                        BGMLogger.Debug($"环境音拦截 ModConfig 集成异常，已使用回退配置: {ex.Message}");
                    }
                }
                else
                {
                    BGMLogger.Debug("ModConfig 不可用，环境音拦截使用 settings.json 配置");
                }

                BGMLogger.Info($"环境音拦截配置加载完成: Enabled={Enabled}, InterceptStormStingers={InterceptStormStingers}");
            }
            catch (Exception ex)
            {
                BGMLogger.Warning($"环境音拦截配置加载失败，使用默认配置: {ex.Message}");
            }
        }

        private static void SetupModConfigUI()
        {
            ModConfigAPI.SafeAddBoolDropdownList(Scope, "enabled", "启用环境音拦截（实验性）", Enabled);
            ModConfigAPI.SafeAddBoolDropdownList(Scope, "interceptStormStingers", "拦截风暴阶段提示音（实验性）", InterceptStormStingers);
        }

        private static void LoadFromModConfig()
        {
            Enabled = ModConfigAPI.SafeLoad(Scope, "enabled", Enabled);
            InterceptStormStingers = ModConfigAPI.SafeLoad(Scope, "interceptStormStingers", InterceptStormStingers);
        }

        private static void LoadFromSettingsFallback()
        {
            Enabled = DuckovCustomSounds.ModSettings.EnableAmbientIntercept;
            InterceptStormStingers = DuckovCustomSounds.ModSettings.InterceptStormStingers;
        }

        private static void OnOptionsChanged(string key)
        {
            if (!ModConfigAPI.IsKeyForMod(key, Scope))
                return;

            bool oldEnabled = Enabled;
            bool oldInterceptStormStingers = InterceptStormStingers;
            LoadFromModConfig();

            if (oldEnabled != Enabled)
                BGMLogger.Info($"环境音拦截配置已更新: Enabled {oldEnabled} -> {Enabled}");

            if (oldInterceptStormStingers != InterceptStormStingers)
                BGMLogger.Info($"环境音拦截配置已更新: InterceptStormStingers {oldInterceptStormStingers} -> {InterceptStormStingers}");
        }
    }
}
