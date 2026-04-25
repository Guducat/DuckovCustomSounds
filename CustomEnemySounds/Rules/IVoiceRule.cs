using Duckov;
using DuckovCustomSounds.CustomEnemySounds.Context;

namespace DuckovCustomSounds.CustomEnemySounds.Rules
{
    internal interface IVoiceRule
    {
        bool TryMatch(EnemyContext ctx, string soundKey, AudioManager.VoiceType voiceType, out VoiceRoute route);
        string Describe();
    }
}
