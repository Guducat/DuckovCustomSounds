using System;
using System.Collections.Generic;

namespace DuckovCustomSounds.ModConfig
{
    public sealed class ModConfigScope
    {
        public ModConfigScope(string storageName, string displayName)
        {
            if (string.IsNullOrWhiteSpace(storageName))
                throw new ArgumentException("storageName 不能为空", nameof(storageName));

            StorageName = storageName.Trim();
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? StorageName : displayName.Trim();
        }

        public string StorageName { get; }
        public string DisplayName { get; }
    }

    internal static class ModConfigScopes
    {
        public static readonly ModConfigScope SoundPack = new ModConfigScope("DuckovCustomSounds", "DCSSoundPack | 声音包");
        public static readonly ModConfigScope Logging = new ModConfigScope("DuckovCustomSoundsLogging", "DCSLogging | 音效日志");
        public static readonly ModConfigScope EnemyVoice = new ModConfigScope("EnemyVoice", "DCSEnemyVoice | 敌人语音");
        public static readonly ModConfigScope Footstep = new ModConfigScope("Footstep", "DCSFootstep | 脚步音效");
        public static readonly ModConfigScope Gun = new ModConfigScope("Gun", "DCSGun | 枪械音效");
        public static readonly ModConfigScope Grenade = new ModConfigScope("Grenade", "DCSGrenade | 手雷音效");
        public static readonly ModConfigScope Item = new ModConfigScope("Item", "DCSItem | 物品音效");
        public static readonly ModConfigScope Melee = new ModConfigScope("Melee", "DCSMelee | 近战音效");
        public static readonly ModConfigScope HitAndKill = new ModConfigScope("HitAndKill", "DCSHitAndKill | 命中与击杀音效");
        public static readonly ModConfigScope BossBGM = new ModConfigScope("BossBGM", "DCSBossBGM | 首领音乐");
        public static readonly ModConfigScope HomeBGM = new ModConfigScope("HomeBGM", "DCSHomeBGM | 基地音乐");
        public static readonly ModConfigScope SceneBGM = new ModConfigScope("SceneBGM", "DCSSceneBGM | 场景音乐");
        public static readonly ModConfigScope ExtractionBGM = new ModConfigScope("ExtractionBGM", "DCSExtractionBGM | 撤离音乐");
        public static readonly ModConfigScope AmbientIntercept = new ModConfigScope("AmbientIntercept", "DCSAmbientIntercept | 环境音拦截");

        private static readonly Dictionary<string, string> LoggingModuleDisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Core"] = "核心",
            ["SoundPack"] = "声音包",
            ["Enemy"] = "敌人语音",
            ["Footstep"] = "脚步音效",
            ["BGM"] = "背景音乐总控",
            ["HomeBGM"] = "基地音乐",
            ["SceneBGM"] = "场景音乐",
            ["ExtractionBGM"] = "撤离音乐",
            ["Gun"] = "枪械音效",
            ["Grenade"] = "手雷音效",
            ["Item"] = "物品音效",
            ["Melee"] = "近战音效",
            ["HitAndKill"] = "命中与击杀音效",
        };

        public static string GetLoggingModuleDisplayName(string module)
        {
            if (string.IsNullOrWhiteSpace(module))
                return "核心";

            return LoggingModuleDisplayNames.TryGetValue(module.Trim(), out var displayName)
                ? displayName
                : module.Trim();
        }
    }
}
