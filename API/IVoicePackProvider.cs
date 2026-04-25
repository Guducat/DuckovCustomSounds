using System;

namespace DuckovCustomSounds.API
{
    /// <summary>
    /// 外部语音包提供者接口。
    /// 外部 Mod 可实现该接口并注册到本 Mod，从而在“敌人语音路由”之前优先提供自定义音频文件路径。
    /// 注意：返回的路径应为可被 FMOD Core 直接读取的本地文件完整路径（绝对路径或可解析为绝对路径）。
    /// </summary>
    public interface IVoicePackProvider
    {
        /// <summary>
        /// 尝试基于上下文与 soundKey 解析一个音频文件完整路径。
        /// 返回 true 表示命中（使用外部路径覆盖）；false 表示不处理，交由本 Mod 内部规则继续处理。
        /// </summary>
        /// <param name="ctx">敌人上下文数据（只读快照）</param>
        /// <param name="soundKey">语音键（如 normal/surprise/grenade/death 等）</param>
        /// <param name="voiceType">语音类型（字符串形式，避免对 Duckov 枚举的强耦合）。注意：该字段并非唯一标识，强烈建议以 ctx.NameKey 为主进行判断。</param>
        /// <param name="fileFullPath">解析出的音频文件完整路径。仅在返回 true 时有效。</param>
        /// <returns>是否成功解析并希望覆盖播放</returns>
        bool TryResolve(EnemyContextData ctx, string soundKey, string voiceType, out string fileFullPath);
    }
}

