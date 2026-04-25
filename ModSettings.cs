using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DuckovCustomSounds.Logging;

namespace DuckovCustomSounds
{
    internal enum EnemyVoiceTriggerMode
    {
        Original = 0,
        PlayerOnly = 1,
        Hybrid = 2
    }

    /// <summary>
    /// 统一模块设置读取/写回（与 Logging.LogManager 共用同一个 settings.json）。
    /// 包含模块设置和声音包选择。
    /// </summary>
    internal static class ModSettings
    {
        private const string SettingsFileName = "settings.json";
        private static readonly ILog Log = LogManager.GetLogger("Core");
        private static readonly object SettingsSyncRoot = new object();

        // Sound pack selection
        public static string CurrentSoundPack { get; private set; } = "";

        // New: voice frequency controls (global rate limit)
        public static bool DeathVoiceEnabled { get; private set; } = true;
        public static float DeathVoiceMinInterval { get; private set; } = 0f;

        public static bool NPCGrenadeSurprisedEnabled { get; private set; } = true;
        public static float NPCGrenadeSurprisedMinInterval { get; private set; } = 0f;
        public static float NPCGrenadeSurprisedMaxDistance { get; private set; } = 10.0f;

        public static bool OverrideExtractionBGM { get; private set; } = false;

        public static bool LevelLoadLoggerEnabled { get; private set; } = false;

        public static bool AudioPostLoggerEnabled { get; private set; } = false;

        public static bool EnableAmbientIntercept { get; private set; } = false;

        // New: Footstep module master switch
        public static bool EnableCustomFootStepSounds { get; private set; } = true;

        // New: Footstep volume scale (0.0 - 2.0, default 1.0)
        public static float FootstepVolumeScale { get; private set; } = 1.0f;

        // Enemy voice integration controls
        public static bool EnableNPCtoNPCCombatVoices { get; private set; } = true;
        public static EnemyVoiceTriggerMode EnemyVoiceMode { get; private set; } = EnemyVoiceTriggerMode.Original;

        // DuckovCustomPlayerQuak module settings
        public static bool EnableDuckovCustomPlayerQuak { get; private set; } = true;
        public static float DuckovCustomPlayerQuakVolumeScale { get; private set; } = 1.0f;

        // Per-module logging visibility (proxy to unified LogManager)
        public static Logging.LogLevel CESLogLevel => LogManager.GetModuleLevel("CustomEnemySounds");
        public static Logging.LogLevel CFSLogLevel => LogManager.GetModuleLevel("CustomFootStepSounds");
        public static bool CESDebugEnabled => LogManager.ShouldLog("CustomEnemySounds", Logging.LogLevel.Debug);
        public static bool CFSDebugEnabled => LogManager.ShouldLog("CustomFootStepSounds", Logging.LogLevel.Debug);

        // 控制 Home BGM 的播完后行为：
        // true 表示自动播放下一首；false 表示单曲循环当前曲目。
        public static bool HomeBgmAutoPlayNext { get; private set; } = true;


            // 随机播放设置（默认：关闭随机、避免连播同曲、上一曲不随机）
            public static bool HomeBgmRandomEnabled { get; private set; } = false;
            public static bool HomeBgmRandomNoRepeat { get; private set; } = true;
            public static bool HomeBgmRandomizePrevious { get; private set; } = false;

        // 隐式开发者模式开关（默认false，不写入settings.json）
        public static bool GunShootDev { get; private set; } = false;
        
        // Developer-only: gun shoot rate limiting (no default writeback)
        public static bool GunShootRateLimitEnabled { get; private set; } = false; // 默认禁用，除非GunShootDev为true
        public static float GunShootMinIntervalMs { get; private set; } = 0f; // milliseconds
        // Developer-only: per-weapon-type overrides (typeId string -> interval ms, 0 disables)
        public static readonly Dictionary<string, float> GunShootRateLimitPerType = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);


        public static void Initialize()
        {
            try
            {
                // 与 LogManager 一致的路径：DuckovCustomSounds/settings.json（由 LogManager.Initialize 保障目录存在）
                // 注意：这里使用 RootFolderName，因为 settings.json 存放在根目录
                string path = Path.Combine(ModBehaviour.RootFolderName, SettingsFileName);

                JObject root = new JObject();
                bool exists = File.Exists(path);
                if (exists)
                {
                    try
                    {
                        var text = File.ReadAllText(path);
                        if (!string.IsNullOrWhiteSpace(text))
                            root = JObject.Parse(text);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"读取 settings.json 失败，使用空白对象继续：{ex.Message}");
                        root = new JObject();
                    }
                }

                bool needsWriteBack = false;

                // 0) currentSoundPack（声音包选择，由 SoundPackManager 管理）
                const string SoundPackKey = "currentSoundPack";
                bool hadSoundPackKey = root.TryGetValue(SoundPackKey, StringComparison.OrdinalIgnoreCase, out var soundPackToken);
                string soundPackVal = soundPackToken?.Type == JTokenType.String ? soundPackToken.Value<string>() : "";
                if (!hadSoundPackKey)
                {
                    root[SoundPackKey] = soundPackVal; // 默认空字符串 = Default
                    needsWriteBack = true;
                }
                CurrentSoundPack = soundPackVal ?? "";

                // 1) overrideExtractionBGM
                bool hadOverride = root.TryGetValue("overrideExtractionBGM", StringComparison.OrdinalIgnoreCase, out var overrideToken);
                bool overrideVal = overrideToken?.Type == JTokenType.Boolean ? overrideToken.Value<bool>() : false;
                if (!hadOverride)
                {
                    root["overrideExtractionBGM"] = overrideVal; // 默认 false
                    needsWriteBack = true;
                }
                OverrideExtractionBGM = overrideVal;

                // 2) deathVoiceFrequency（"always"/number seconds/"off"/true/false/"6.0f"等）
                const string DeathKey = "deathVoiceFrequency";
                bool hadDeathKey = root.TryGetValue(DeathKey, StringComparison.OrdinalIgnoreCase, out var deathToken);
                ParseRateControl(deathToken, /*defaultEnabled*/ true, /*defaultInterval*/ 0f,
                    out bool dvEnabled, out float dvInterval);
                if (!hadDeathKey)
                {
                    root[DeathKey] = "always"; // 写回默认值
                    needsWriteBack = true;
                }
                DeathVoiceEnabled = dvEnabled;
                DeathVoiceMinInterval = dvInterval;

                // 3) npcGrenadeSurprisedFrequency（同上）
                const string GrenadeKey = "npcGrenadeSurprisedFrequency";
                bool hadGrenadeKey = root.TryGetValue(GrenadeKey, StringComparison.OrdinalIgnoreCase, out var grenadeToken);
                ParseRateControl(grenadeToken, /*defaultEnabled*/ true, /*defaultInterval*/ 0f,
                    out bool gvEnabled, out float gvInterval);
                if (!hadGrenadeKey)
                {
                    root[GrenadeKey] = "always";
                    needsWriteBack = true;
                }

                // 3.1) npcGrenadeSurprisedMaxDistance（敌人发现手雷的最大距离）
                const string GrenadeDistanceKey = "npcGrenadeSurprisedMaxDistance";
                bool hadGrenadeDistanceKey = root.TryGetValue(GrenadeDistanceKey, StringComparison.OrdinalIgnoreCase, out var grenadeDistanceToken);
                float grenadeDistanceVal = 10.0f;
                try
                {
                    if (grenadeDistanceToken != null && grenadeDistanceToken.Type != JTokenType.Null && grenadeDistanceToken.Type != JTokenType.Undefined)
                    {
                        if (grenadeDistanceToken.Type == JTokenType.Integer || grenadeDistanceToken.Type == JTokenType.Float)
                        {
                            grenadeDistanceVal = Math.Clamp(grenadeDistanceToken.Value<float>(), 5f, 50f);
                        }
                        else if (grenadeDistanceToken.Type == JTokenType.String)
                        {
                            var s = (grenadeDistanceToken.Value<string>() ?? "").Trim();
                            if (float.TryParse(CleanFloatString(s), NumberStyles.Float, CultureInfo.InvariantCulture, out var fv))
                                grenadeDistanceVal = Math.Clamp(fv, 5f, 50f);
                        }
                    }
                }
                catch { grenadeDistanceVal = 10.0f; }
                if (!hadGrenadeDistanceKey)
                {
                    root[GrenadeDistanceKey] = grenadeDistanceVal;
                    needsWriteBack = true;
                }

                // 4) enableLevelLoadLogger（仅调试用途，默认 false）
                const string LoggerKey = "enableLevelLoadLogger";
                bool hadLoggerKey = root.TryGetValue(LoggerKey, StringComparison.OrdinalIgnoreCase, out var loggerToken);
                bool loggerVal = loggerToken?.Type == JTokenType.Boolean ? loggerToken.Value<bool>() : false;
                if (!hadLoggerKey)
                {
                    root[LoggerKey] = loggerVal;
                    needsWriteBack = true;
                }
                // 5) enableAudioPostLogger（捕获所有 AudioManager.Post/AudioObject.Post 调用，默认 false）
                const string AudioLoggerKey = "enableAudioPostLogger";
                bool hadAudioLoggerKey = root.TryGetValue(AudioLoggerKey, StringComparison.OrdinalIgnoreCase, out var audioLoggerToken);
                bool audioLoggerVal = audioLoggerToken?.Type == JTokenType.Boolean ? audioLoggerToken.Value<bool>() : false;
                if (!hadAudioLoggerKey)
                {
                    root[AudioLoggerKey] = audioLoggerVal;
                    needsWriteBack = true;
                }
                AudioPostLoggerEnabled = audioLoggerVal;

                // 6) enableAmbientIntercept（内部测试开关，默认 false）
                const string AmbientInterceptKey = "enableAmbientIntercept";
                bool hadAmbientKey = root.TryGetValue(AmbientInterceptKey, StringComparison.OrdinalIgnoreCase, out var ambientToken);
                bool ambientVal = ambientToken?.Type == JTokenType.Boolean ? ambientToken.Value<bool>() : false;
                if (!hadAmbientKey)
                {
                    root[AmbientInterceptKey] = ambientVal;
                    needsWriteBack = true;
                }
                EnableAmbientIntercept = ambientVal;

                // 7) enableCustomFootStepSounds（脚步声自定义开关，默认 true）
                const string FootstepSwitchKey = "enableCustomFootStepSounds";
                bool hadFootSwitchKey = root.TryGetValue(FootstepSwitchKey, StringComparison.OrdinalIgnoreCase, out var footSwitchToken);
                bool footSwitchVal = footSwitchToken?.Type == JTokenType.Boolean ? footSwitchToken.Value<bool>() : true;
                if (!hadFootSwitchKey)
                {
                    root[FootstepSwitchKey] = footSwitchVal;
                    needsWriteBack = true;
                }
                EnableCustomFootStepSounds = footSwitchVal;

                // 7.1) footstepVolumeScale（脚步声音量缩放，默认 1.0，范围 0.0 - 2.0）
                const string FootstepVolKey = "footstepVolumeScale";
                bool hadFootVol = root.TryGetValue(FootstepVolKey, StringComparison.OrdinalIgnoreCase, out var footVolToken);
                float footVolVal = 1.0f;
                try
                {
                    if (footVolToken != null && footVolToken.Type != JTokenType.Null && footVolToken.Type != JTokenType.Undefined)
                    {
                        if (footVolToken.Type == JTokenType.Integer || footVolToken.Type == JTokenType.Float)
                        {
                            footVolVal = Math.Clamp(footVolToken.Value<float>(), 0f, 2f);
                        }
                        else if (footVolToken.Type == JTokenType.String)
                        {
                            var s = (footVolToken.Value<string>() ?? "").Trim();
                            if (float.TryParse(CleanFloatString(s), NumberStyles.Float, CultureInfo.InvariantCulture, out var fv))
                                footVolVal = Math.Clamp(fv, 0f, 2f);
                        }
                    }
                }
                catch { footVolVal = 1.0f; }
                if (!hadFootVol)
                {
                    root[FootstepVolKey] = footVolVal;
                    needsWriteBack = true;
                }
                FootstepVolumeScale = footVolVal;
                // Enemy voice integration switches (compatible with EnemyVoiceMod)
                const string NPCCombatKey = "enableNPCtoNPCCombatVoices";
                bool hadNPCCombatKey = root.TryGetValue(NPCCombatKey, StringComparison.OrdinalIgnoreCase, out var npcCombatToken);
                bool npcCombatValid = TryReadBoolean(npcCombatToken, out bool npcCombatVal);
                if (!npcCombatValid)
                {
                    npcCombatVal = true;
                }
                if (!hadNPCCombatKey || !npcCombatValid)
                {
                    root[NPCCombatKey] = npcCombatVal;
                    needsWriteBack = true;
                }
                EnableNPCtoNPCCombatVoices = npcCombatVal;

                const string EnemyVoiceModeKey = "enemyVoiceTriggerMode";
                bool hadEnemyVoiceMode = root.TryGetValue(EnemyVoiceModeKey, StringComparison.OrdinalIgnoreCase, out var enemyVoiceModeToken);
                bool voiceModeValid = TryParseEnemyVoiceMode(enemyVoiceModeToken, out var voiceModeVal);
                if (!voiceModeValid)
                {
                    voiceModeVal = EnemyVoiceTriggerMode.Original;
                }
                if (!hadEnemyVoiceMode || !voiceModeValid || enemyVoiceModeToken?.Type != JTokenType.String)
                {
                    root[EnemyVoiceModeKey] = voiceModeVal.ToString();
                    needsWriteBack = true;
                }
                EnemyVoiceMode = voiceModeVal;

                // DuckovCustomPlayerQuak module settings
                const string PlayerQuakEnabledKey = "enableDuckovCustomPlayerQuak";
                bool hadPlayerQuakEnabled = root.TryGetValue(PlayerQuakEnabledKey, StringComparison.OrdinalIgnoreCase, out var playerQuakEnabledToken);
                bool playerQuakEnabledVal = playerQuakEnabledToken?.Type == JTokenType.Boolean ? playerQuakEnabledToken.Value<bool>() : true;
                if (!hadPlayerQuakEnabled)
                {
                    root[PlayerQuakEnabledKey] = playerQuakEnabledVal;
                    needsWriteBack = true;
                }
                EnableDuckovCustomPlayerQuak = playerQuakEnabledVal;

                const string PlayerQuakVolumeKey = "duckovCustomPlayerQuakVolumeScale";
                bool hadPlayerQuakVolume = root.TryGetValue(PlayerQuakVolumeKey, StringComparison.OrdinalIgnoreCase, out var playerQuakVolumeToken);
                float playerQuakVolumeVal = 1.0f;
                try
                {
                    if (playerQuakVolumeToken != null && playerQuakVolumeToken.Type != JTokenType.Null && playerQuakVolumeToken.Type != JTokenType.Undefined)
                    {
                        if (playerQuakVolumeToken.Type == JTokenType.Integer || playerQuakVolumeToken.Type == JTokenType.Float)
                        {
                            playerQuakVolumeVal = Math.Clamp(playerQuakVolumeToken.Value<float>(), 0f, 2f);
                        }
                        else if (playerQuakVolumeToken.Type == JTokenType.String)
                        {
                            var s = (playerQuakVolumeToken.Value<string>() ?? "").Trim();
                            if (float.TryParse(CleanFloatString(s), NumberStyles.Float, CultureInfo.InvariantCulture, out var fv))
                                playerQuakVolumeVal = Math.Clamp(fv, 0f, 2f);
                        }
                    }
                }
                catch { playerQuakVolumeVal = 1.0f; }
                if (!hadPlayerQuakVolume)
                {
                    root[PlayerQuakVolumeKey] = playerQuakVolumeVal;
                    needsWriteBack = true;
                }
                DuckovCustomPlayerQuakVolumeScale = playerQuakVolumeVal;

                // 7) homeBgmAutoPlayNext：控制 Home BGM 是自动切歌还是单曲循环（默认 true 自动切歌）
                const string HomeBgmAutoNextKey = "homeBgmAutoPlayNext";
                bool hadHomeBgmAutoNext = root.TryGetValue(HomeBgmAutoNextKey, StringComparison.OrdinalIgnoreCase, out var homeAutoNextToken);
                bool homeAutoNextVal = homeAutoNextToken?.Type == JTokenType.Boolean ? homeAutoNextToken.Value<bool>() : true;

                // 7.2) homeBgmRandomEnabled（默认 false）
                const string HomeBgmRandomEnabledKey = "homeBgmRandomEnabled";
                bool hadHomeBgmRandomEnabled = root.TryGetValue(HomeBgmRandomEnabledKey, StringComparison.OrdinalIgnoreCase, out var homeRandomEnabledToken);
                bool homeRandomEnabledVal = homeRandomEnabledToken?.Type == JTokenType.Boolean ? homeRandomEnabledToken.Value<bool>() : false;
                if (!hadHomeBgmRandomEnabled)
                {
                    root[HomeBgmRandomEnabledKey] = homeRandomEnabledVal;
                    needsWriteBack = true;
                }
                HomeBgmRandomEnabled = homeRandomEnabledVal;

                // 7.3) homeBgmRandomNoRepeat（默认 true）
                const string HomeBgmRandomNoRepeatKey = "homeBgmRandomNoRepeat";
                bool hadHomeBgmRandomNoRepeat = root.TryGetValue(HomeBgmRandomNoRepeatKey, StringComparison.OrdinalIgnoreCase, out var homeRandomNoRepeatToken);
                bool homeRandomNoRepeatVal = homeRandomNoRepeatToken?.Type == JTokenType.Boolean ? homeRandomNoRepeatToken.Value<bool>() : true;
                if (!hadHomeBgmRandomNoRepeat)
                {
                    root[HomeBgmRandomNoRepeatKey] = homeRandomNoRepeatVal;
                    needsWriteBack = true;
                }
                HomeBgmRandomNoRepeat = homeRandomNoRepeatVal;

                // 7.4) homeBgmRandomizePrevious（默认 false）
                const string HomeBgmRandomizePrevKey = "homeBgmRandomizePrevious";
                bool hadHomeBgmRandomizePrev = root.TryGetValue(HomeBgmRandomizePrevKey, StringComparison.OrdinalIgnoreCase, out var homeRandomizePrevToken);
                bool homeRandomizePrevVal = homeRandomizePrevToken?.Type == JTokenType.Boolean ? homeRandomizePrevToken.Value<bool>() : false;
                if (!hadHomeBgmRandomizePrev)
                {
                    root[HomeBgmRandomizePrevKey] = homeRandomizePrevVal;
                    needsWriteBack = true;
                }
                HomeBgmRandomizePrevious = homeRandomizePrevVal;

                if (!hadHomeBgmAutoNext)
                {

                    root[HomeBgmAutoNextKey] = homeAutoNextVal;
                    needsWriteBack = true;
                }
                HomeBgmAutoPlayNext = homeAutoNextVal;

                // 8.1) 隐式开发者模式开关（不写入settings.json）
                const string GunShootDevKey = "gunShootDev";
                bool gunShootDev = false;
                try
                {
                    if (root.TryGetValue(GunShootDevKey, StringComparison.OrdinalIgnoreCase, out var gunShootDevToken))
                    {
                        if (gunShootDevToken.Type == JTokenType.Boolean)
                            gunShootDev = gunShootDevToken.Value<bool>();
                    }
                }
                catch { gunShootDev = false; }
                GunShootDev = gunShootDev;

                // 8) Optional developer knobs: gun shoot rate limit (do NOT write defaults)
                const string GunRateSwitchKey = "enableGunShootRateLimit";
                try
                {
                    if (root.TryGetValue(GunRateSwitchKey, StringComparison.OrdinalIgnoreCase, out var gunRateToken))
                    {
                        if (gunRateToken.Type == JTokenType.Boolean)
                            GunShootRateLimitEnabled = gunShootDev ? gunRateToken.Value<bool>() : false;
                    }
                    else
                    {
                        GunShootRateLimitEnabled = gunShootDev; // 只有在开发者模式下才默认启用
                    }
                }
                catch { GunShootRateLimitEnabled = gunShootDev; }

                const string GunRateMsKey = "gunShootMinIntervalMs";
                float gunMs = gunShootDev ? 95f : 0f; // 只有在开发者模式下才设置默认间隔
                try
                {
                    if (root.TryGetValue(GunRateMsKey, StringComparison.OrdinalIgnoreCase, out var gunMsToken))
                    {
                        if (gunMsToken.Type == JTokenType.Integer || gunMsToken.Type == JTokenType.Float)
                        {
                            gunMs = gunShootDev ? Math.Clamp(gunMsToken.Value<float>(), 0f, 1000f) : 0f;
                        }
                        else if (gunMsToken.Type == JTokenType.String)
                        {
                            var s = (gunMsToken.Value<string>() ?? "").Trim();
                            if (float.TryParse(CleanFloatString(s), NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                                gunMs = gunShootDev ? Math.Clamp(v, 0f, 1000f) : 0f;
                        }
                    }

                 // Per-type overrides: { "typeId": intervalMs, ... } (developer-only; do NOT write defaults)
                 const string GunRatePerTypeKey = "gunShootRateLimitPerType";
                 try
                 {
                     GunShootRateLimitPerType.Clear();
                     if (gunShootDev && root.TryGetValue(GunRatePerTypeKey, StringComparison.OrdinalIgnoreCase, out var perTypeToken)
                         && perTypeToken is JObject obj)
                     {
                         foreach (var prop in obj.Properties())
                         {
                             var k = (prop.Name ?? string.Empty).Trim();
                             if (k.Length == 0) continue;
                             float ms = 0f;
                             var v = prop.Value;
                             if (v.Type == JTokenType.Integer || v.Type == JTokenType.Float)
                             {
                                 ms = Math.Clamp(v.Value<float>(), 0f, 1000f);
                             }
                             else if (v.Type == JTokenType.String)
                             {
                                 var s = (v.Value<string>() ?? string.Empty).Trim();
                                 if (float.TryParse(CleanFloatString(s), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                                     ms = Math.Clamp(parsed, 0f, 1000f);
                             }
                             // Ignore other types silently
                             GunShootRateLimitPerType[k] = ms;
                         }
                     }
                 }
                 catch { /* ignore parse errors; keep empty map */ }

                }
                catch { gunMs = gunShootDev ? 95f : 0f; }
                GunShootMinIntervalMs = gunMs;



                LevelLoadLoggerEnabled = loggerVal;

                NPCGrenadeSurprisedEnabled = gvEnabled;
                NPCGrenadeSurprisedMinInterval = gvInterval;
                NPCGrenadeSurprisedMaxDistance = grenadeDistanceVal;

                // 统一写回：仅当文件原本不存在或新增键需要补充
                if (!exists || needsWriteBack)
                {
                    try
                    {
                        File.WriteAllText(path, root.ToString(Formatting.Indented));
                        Log.Info($"settings.json {(exists ? "已补充" : "已创建")}默认键：{SoundPackKey}, overrideExtractionBGM, {DeathKey}, {GrenadeKey}, {GrenadeDistanceKey}, {LoggerKey}, {AudioLoggerKey}, {AmbientInterceptKey}, {FootstepSwitchKey}, {FootstepVolKey}, {HomeBgmAutoNextKey}, {HomeBgmRandomEnabledKey}, {HomeBgmRandomNoRepeatKey}, {HomeBgmRandomizePrevKey}, {NPCCombatKey}, {EnemyVoiceModeKey}, {PlayerQuakEnabledKey}, {PlayerQuakVolumeKey}");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"写回 settings.json 失败：{ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"ModSettings.Initialize 异常：{ex.Message}");
            }
        }

        public static void ApplyEnemyVoiceSettings(bool enableNpcCombatVoices, EnemyVoiceTriggerMode mode, bool persist = false)
        {
            EnableNPCtoNPCCombatVoices = enableNpcCombatVoices;
            EnemyVoiceMode = mode;

            if (!persist)
                return;

            PersistSettings(root =>
            {
                root["enableNPCtoNPCCombatVoices"] = enableNpcCombatVoices;
                root["enemyVoiceTriggerMode"] = mode.ToString();
            });
        }

        private static void PersistSettings(Action<JObject> mutator)
        {
            if (mutator == null) return;

            try
            {
                lock (SettingsSyncRoot)
                {
                    string path = Path.Combine(ModBehaviour.RootFolderName, SettingsFileName);
                    JObject root = new JObject();

                    if (File.Exists(path))
                    {
                        try
                        {
                            var text = File.ReadAllText(path);
                            if (!string.IsNullOrWhiteSpace(text))
                                root = JObject.Parse(text);
                        }
                        catch (Exception ex)
                        {
                            Log.Warning($"ModSettings.PersistSettings 读取失败，使用空白对象继续：{ex.Message}");
                            root = new JObject();
                        }
                    }
                    else
                    {
                        try { Directory.CreateDirectory(ModBehaviour.RootFolderName); } catch { }
                    }

                    try
                    {
                        mutator(root);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"ModSettings.PersistSettings mutate 异常：{ex.Message}");
                    }

                    File.WriteAllText(path, root.ToString(Formatting.Indented));
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"ModSettings.PersistSettings 异常：{ex.Message}");
            }
        }

        private static bool TryReadBoolean(JToken token, out bool value)
        {
            value = false;
            if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
                return false;

            try
            {
                switch (token.Type)
                {
                    case JTokenType.Boolean:
                        value = token.Value<bool>();
                        return true;
                    case JTokenType.Integer:
                        value = token.Value<int>() != 0;
                        return true;
                    case JTokenType.Float:
                        value = Math.Abs(token.Value<double>()) > double.Epsilon;
                        return true;
                    case JTokenType.String:
                        var s = (token.Value<string>() ?? string.Empty).Trim();
                        if (string.IsNullOrEmpty(s)) return false;

                        if (bool.TryParse(s, out var boolVal))
                        {
                            value = boolVal;
                            return true;
                        }
                        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intVal))
                        {
                            value = intVal != 0;
                            return true;
                        }
                        if (string.Equals(s, "yes", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(s, "on", StringComparison.OrdinalIgnoreCase))
                        {
                            value = true;
                            return true;
                        }
                        if (string.Equals(s, "no", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(s, "off", StringComparison.OrdinalIgnoreCase))
                        {
                            value = false;
                            return true;
                        }
                        return false;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool TryParseEnemyVoiceMode(JToken token, out EnemyVoiceTriggerMode mode)
        {
            mode = EnemyVoiceTriggerMode.Original;
            if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
                return false;

            try
            {
                if (token.Type == JTokenType.String)
                {
                    var s = (token.Value<string>() ?? string.Empty).Trim();
                    if (Enum.TryParse(s, true, out EnemyVoiceTriggerMode parsed) && Enum.IsDefined(typeof(EnemyVoiceTriggerMode), parsed))
                    {
                        mode = parsed;
                        return true;
                    }
                    if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intFromString) &&
                        Enum.IsDefined(typeof(EnemyVoiceTriggerMode), intFromString))
                    {
                        mode = (EnemyVoiceTriggerMode)intFromString;
                        return true;
                    }
                    return false;
                }

                if (token.Type == JTokenType.Integer)
                {
                    var intVal = token.Value<int>();
                    if (Enum.IsDefined(typeof(EnemyVoiceTriggerMode), intVal))
                    {
                        mode = (EnemyVoiceTriggerMode)intVal;
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }
        // Rate control helper parsing rules:
        // - null: use defaults
        // - bool: true => always (interval 0), false => off
        // - number: treated as seconds (negative clamped to 0)
        // - string: supports "always"/"off"/numeric values (optional f/F/s suffix)
        private static void ParseRateControl(JToken token, bool defaultEnabled, float defaultInterval,
            out bool enabled, out float interval)
        {
            enabled = defaultEnabled;

            interval = defaultInterval;

            try
            {
                if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
                {
                    return;
                }

                switch (token.Type)
                {
                    case JTokenType.Boolean:
                        enabled = token.Value<bool>();
                        interval = 0f;
                        return;
                    case JTokenType.Integer:
                    case JTokenType.Float:
                        enabled = true;
                        interval = Math.Max(0f, token.Value<float>());
                        return;
                    case JTokenType.String:
                        var s = (token.Value<string>() ?? string.Empty).Trim();
                        if (string.Equals(s, "always", StringComparison.OrdinalIgnoreCase))
                        {
                            enabled = true; interval = 0f; return;
                        }
                        if (string.Equals(s, "off", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(s, "disabled", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(s, "false", StringComparison.OrdinalIgnoreCase))
                        {
                            enabled = false; interval = 0f; return;
                        }
                        // 提取可解析的数值（忽略末尾常见后缀，如 f/F/s/sec/秒）
                        string cleaned = CleanFloatString(s);
                        if (float.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out var sec))
                        {
                            enabled = true;
                            interval = Math.Max(0f, sec);
                            return;
                        }
                        // 解析失败：使用默认
                        return;
                    default:
                        return; // 使用默认
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"ParseRateControl 解析异常，使用默认：{ex.Message}");
            }
        }

        private static string CleanFloatString(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            // 保留数字、点号和负号，忽略其余字符
            char[] buf = new char[s.Length];
            int j = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if ((c >= '0' && c <= '9') || c == '.' || c == '-')
                    buf[j++] = c;
            }
            return new string(buf, 0, j);
        }
    }
}

