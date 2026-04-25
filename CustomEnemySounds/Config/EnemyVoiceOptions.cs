using System;
using System.Collections.Generic;
using Duckov.Modding;
using DuckovCustomSounds.ModConfig;

namespace DuckovCustomSounds.CustomEnemySounds.Config
{
    /// <summary>
    /// Bridges ModSettings enemy voice options with ModConfig UI so users can toggle modes at runtime.
    /// </summary>
    internal static class EnemyVoiceOptions
    {
        private const string ModName = "EnemyVoice";
        private static bool _initialized;
        private static bool _uiRegistered;
        private static readonly Action<string> OptionsChangedHandler = OnOptionsChanged;

        public static void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            try { ModManager.OnModActivated += OnModActivated; } catch { }

            if (ModConfigAPI.IsAvailable())
            {
                SetupModConfigUI();
                LoadFromModConfig();
            }
        }

        public static void Deinitialize()
        {
            try { ModManager.OnModActivated -= OnModActivated; } catch { }
            try { ModConfigAPI.SafeRemoveOnOptionsChangedDelegate(OptionsChangedHandler); } catch { }

            _uiRegistered = false;
            _initialized = false;
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
            if (_uiRegistered)
                return;
            if (!ModConfigAPI.IsAvailable())
                return;

            try
            {
                ModConfigAPI.SafeAddOnOptionsChangedDelegate(OptionsChangedHandler);

                var triggerModeOptions = new SortedDictionary<string, object>(StringComparer.Ordinal)
                {
                    { "1) 原版: 所有战斗语音", (int)EnemyVoiceTriggerMode.Original },
                    { "2) 玩家相关: 仅玩家触发", (int)EnemyVoiceTriggerMode.PlayerOnly },
                    { "3) 混合: 距离智能切换", (int)EnemyVoiceTriggerMode.Hybrid }
                };

                ModConfigAPI.SafeAddBoolDropdownList(ModName, "npc_combat_voices", "允许 NPC-对-NPC 战斗语音", ModSettings.EnableNPCtoNPCCombatVoices);
                ModConfigAPI.SafeAddDropdownList(ModName, "voice_mode", "语音触发模式", triggerModeOptions, typeof(int), (int)ModSettings.EnemyVoiceMode);

                _uiRegistered = true;
            }
            catch
            {
                // Swallow errors so initialization of other systems continues.
            }
        }

        private static void OnOptionsChanged(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;
            if (!key.StartsWith(ModName + "_", StringComparison.OrdinalIgnoreCase))
                return;

            LoadFromModConfig();
        }

        private static void LoadFromModConfig()
        {
            if (!ModConfigAPI.IsAvailable())
            {
                ModSettings.ApplyEnemyVoiceSettings(ModSettings.EnableNPCtoNPCCombatVoices, ModSettings.EnemyVoiceMode, persist: false);
                return;
            }

            bool enableNpcVoices = ModConfigAPI.SafeLoad(ModName, "npc_combat_voices", ModSettings.EnableNPCtoNPCCombatVoices);
            int modeValue = ModConfigAPI.SafeLoad(ModName, "voice_mode", (int)ModSettings.EnemyVoiceMode);

            EnemyVoiceTriggerMode mode = Enum.IsDefined(typeof(EnemyVoiceTriggerMode), modeValue)
                ? (EnemyVoiceTriggerMode)modeValue
                : EnemyVoiceTriggerMode.Original;

            bool changed = enableNpcVoices != ModSettings.EnableNPCtoNPCCombatVoices || mode != ModSettings.EnemyVoiceMode;
            ModSettings.ApplyEnemyVoiceSettings(enableNpcVoices, mode, persist: changed);
        }
    }
}
