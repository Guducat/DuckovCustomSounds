using System;
using Duckov.Modding;
using DuckovCustomSounds.ModConfig;
using UnityEngine;

namespace DuckovCustomSounds.CustomHitAndKillSounds
{
    internal static class HitAndKillConfig
    {
        private static readonly ModConfigScope Scope = ModConfigScopes.HitAndKill;
        private static readonly Action<string> OnChangedHandler = OnOptionsChanged;
        private static bool initialized;
        private static bool uiRegistered;

        public static bool Enabled { get; private set; } = true;
        public static bool ReplaceMarkerSounds { get; private set; } = true;
        public static bool PlayHurtSounds { get; private set; } = true;
        public static bool ReflectionDiagnostics { get; private set; } = false;
        public static float Volume { get; private set; } = 1.0f;
        public static float MarkerCooldownMs { get; private set; } = 30f;
        public static float HurtCooldownMs { get; private set; } = 120f;

        public static void Initialize()
        {
            if (initialized)
                return;

            initialized = true;

            try
            {
                try { ModManager.OnModActivated += OnModActivated; } catch { }

                if (ModConfigAPI.IsAvailable())
                {
                    SetupModConfigUI();
                    LoadFromModConfig();
                    HitAndKillLogger.Debug("已集成 ModConfig UI");
                }
                else
                {
                    HitAndKillLogger.Debug("ModConfig 不可用，使用默认配置");
                }

                HitAndKillLogger.Info($"配置加载完成: Enabled={Enabled}, Marker={ReplaceMarkerSounds}, Hurt={PlayHurtSounds}, Volume={Volume:F2}");
            }
            catch (Exception ex)
            {
                HitAndKillLogger.Error("配置加载失败，使用默认配置", ex);
            }
        }

        public static void Deinitialize()
        {
            try { ModManager.OnModActivated -= OnModActivated; } catch { }
            try { ModConfigAPI.SafeRemoveOnOptionsChangedDelegate(OnChangedHandler); } catch { }

            uiRegistered = false;
            initialized = false;
        }

        private static void OnModActivated(ModInfo info, Duckov.Modding.ModBehaviour behaviour)
        {
            if (!string.Equals(info.name, ModConfigAPI.ModConfigName, StringComparison.OrdinalIgnoreCase))
                return;

            SetupModConfigUI();
            LoadFromModConfig();
        }

        private static void SetupModConfigUI()
        {
            if (uiRegistered)
                return;

            if (!ModConfigAPI.IsAvailable())
                return;

            ModConfigAPI.SafeAddOnOptionsChangedDelegate(OnChangedHandler);
            ModConfigAPI.SafeAddBoolDropdownList(Scope, "enabled", "启用命中与击杀音效", Enabled);
            ModConfigAPI.SafeAddBoolDropdownList(Scope, "replaceMarkerSounds", "替换命中/击杀提示音", ReplaceMarkerSounds);
            ModConfigAPI.SafeAddBoolDropdownList(Scope, "playHurtSounds", "播放受击音效", PlayHurtSounds);
            ModConfigAPI.SafeAddBoolDropdownList(Scope, "reflectionDiagnostics", "启用反射诊断日志", ReflectionDiagnostics);
            ModConfigAPI.SafeAddInputWithSlider(Scope, "volume", "音量倍率(0~2)", typeof(float), Volume, new Vector2(0f, 2f));
            ModConfigAPI.SafeAddInputWithSlider(Scope, "markerCooldownMs", "提示音冷却毫秒(0~500)", typeof(float), MarkerCooldownMs, new Vector2(0f, 500f));
            ModConfigAPI.SafeAddInputWithSlider(Scope, "hurtCooldownMs", "受击音效冷却毫秒(0~1000)", typeof(float), HurtCooldownMs, new Vector2(0f, 1000f));
            uiRegistered = true;
        }

        private static void LoadFromModConfig()
        {
            Enabled = ModConfigAPI.SafeLoad(Scope, "enabled", Enabled);
            ReplaceMarkerSounds = ModConfigAPI.SafeLoad(Scope, "replaceMarkerSounds", ReplaceMarkerSounds);
            PlayHurtSounds = ModConfigAPI.SafeLoad(Scope, "playHurtSounds", PlayHurtSounds);
            ReflectionDiagnostics = ModConfigAPI.SafeLoad(Scope, "reflectionDiagnostics", ReflectionDiagnostics);
            Volume = Mathf.Clamp(ModConfigAPI.SafeLoad(Scope, "volume", Volume), 0f, 2f);
            MarkerCooldownMs = Mathf.Clamp(ModConfigAPI.SafeLoad(Scope, "markerCooldownMs", MarkerCooldownMs), 0f, 500f);
            HurtCooldownMs = Mathf.Clamp(ModConfigAPI.SafeLoad(Scope, "hurtCooldownMs", HurtCooldownMs), 0f, 1000f);
        }

        private static void OnOptionsChanged(string key)
        {
            if (!ModConfigAPI.IsKeyForMod(key, Scope))
                return;

            var oldEnabled = Enabled;
            var oldVolume = Volume;
            LoadFromModConfig();

            if (oldEnabled != Enabled)
                HitAndKillLogger.Info($"配置已热重载: Enabled {oldEnabled} -> {Enabled}");

            if (Math.Abs(oldVolume - Volume) > 0.01f)
                HitAndKillLogger.Debug($"配置已热重载: Volume {oldVolume:F2} -> {Volume:F2}");
        }
    }
}
