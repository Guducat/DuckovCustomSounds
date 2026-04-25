namespace DuckovCustomSounds.API
{
    /// <summary>
    /// 外部语音包提供者接口。
    /// 外部 Mod 可实现该接口并注册到本 Mod，在敌人语音规则解析前提供自定义音频文件。
    /// </summary>
    public interface IVoicePackProvider
    {
        /// <summary>
        /// 基于敌人上下文与 soundKey 解析音频文件。
        /// 返回 true 表示命中；false 表示交由本 Mod 内部规则继续处理。
        /// </summary>
        /// <param name="ctx">敌人上下文数据。</param>
        /// <param name="soundKey">语音键，如 normal、surprise、grenade、death。</param>
        /// <param name="voiceType">游戏语音类型的字符串形式。</param>
        /// <param name="fileFullPath">解析出的音频文件，绝对文件名或相对当前声音包根目录的文件名。</param>
        /// <returns>是否命中外部语音文件。</returns>
        bool TryResolve(EnemyContextData ctx, string soundKey, string voiceType, out string fileFullPath);
    }
}
