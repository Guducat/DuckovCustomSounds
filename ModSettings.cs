using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DuckovCustomSounds.Logging;

namespace DuckovCustomSounds
{
    /// <summary>
    /// 统一模块设置读取/写回（与 Logging.LogManager 共用同一个 settings.json）。
    /// 只关注我们新增的键：overrideExtractionBGM。
    /// </summary>
    internal static class ModSettings
    {
        private const string SettingsFileName = "settings.json";
        private static readonly ILog Log = LogManager.GetLogger("Core");

        public static bool OverrideExtractionBGM { get; private set; } = false;

        public static void Initialize()
        {
            try
            {
                // 与 LogManager 一致的路径：DuckovCustomSounds/settings.json（由 LogManager.Initialize 保障目录存在）
                string path = Path.Combine(ModBehaviour.ModFolderName, SettingsFileName);

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

                // 读取并带默认值写回
                bool hadKey = root.TryGetValue("overrideExtractionBGM", StringComparison.OrdinalIgnoreCase, out var token);
                bool value = token?.Type == JTokenType.Boolean ? token.Value<bool>() : false;
                if (!hadKey)
                {
                    root["overrideExtractionBGM"] = value; // 默认 false
                }
                OverrideExtractionBGM = value;

                // 如果文件原本不存在或缺失键，则写回（非破坏式，仅合并这个键）
                if (!exists || !hadKey)
                {
                    try
                    {
                        File.WriteAllText(path, root.ToString(Formatting.Indented));
                        Log.Info($"settings.json 已{(exists ? "补充" : "创建")}键 overrideExtractionBGM={OverrideExtractionBGM}");
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
    }
}

