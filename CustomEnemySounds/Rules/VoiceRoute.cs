using System.Collections.Generic;

namespace DuckovCustomSounds.CustomEnemySounds.Rules
{
    internal sealed class VoiceRoute
    {
        public bool UseCustom { get; set; }
        public string? FileFullPath { get; set; }
        public string MatchRule { get; set; } = string.Empty;
        public List<string> TriedPaths { get; set; } = new List<string>();
    }
}
