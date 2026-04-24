using System;
using Newtonsoft.Json;

namespace DuckovCustomSounds.SoundPack
{
    /// <summary>
    /// 声音包元数据信息（从 pack.json 读取）
    /// </summary>
    public class SoundPackInfo
    {
        /// <summary>
        /// 声音包唯一标识符（文件夹名称）
        /// </summary>
        [JsonIgnore]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 声音包显示名称（必需）
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 作者（必需）
        /// </summary>
        [JsonProperty("author")]
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// 版本号（必需）
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// 描述（可选）
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 兼容的 Mod 版本（可选）
        /// </summary>
        [JsonProperty("compatibleModVersion")]
        public string CompatibleModVersion { get; set; } = string.Empty;

        /// <summary>
        /// 必需的模块列表（可选）
        /// </summary>
        [JsonProperty("requiredModules")]
        public string[] RequiredModules { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 额外的可选字段（如 homepage, qq 等）
        /// </summary>
        [JsonProperty("optional")]
        public OptionalFields Optional { get; set; } = new OptionalFields();

        /// <summary>
        /// 验证元数据完整性
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Name)
                && !string.IsNullOrWhiteSpace(Author)
                && !string.IsNullOrWhiteSpace(Version);
        }

        /// <summary>
        /// 获取显示文本（用于 UI）
        /// </summary>
        public string GetDisplayText()
        {
            if (!string.IsNullOrWhiteSpace(Description))
                return $"{Name} v{Version} by {Author} - {Description}";
            return $"{Name} v{Version} by {Author}";
        }

        public class OptionalFields
        {
            [JsonProperty("homepage")]
            public string Homepage { get; set; } = string.Empty;

            [JsonProperty("qq")]
            public string qq { get; set; } = string.Empty;
        }
    }
}
