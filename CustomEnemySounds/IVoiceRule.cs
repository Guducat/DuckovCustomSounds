using Duckov;

namespace DuckovCustomSounds.CustomEnemySounds
{
    internal interface IVoiceRule
    {
        bool TryMatch(EnemyContext ctx, string soundKey, AudioManager.VoiceType voiceType, out VoiceRoute route);
        string Describe();
    }
}

