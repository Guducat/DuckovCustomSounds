using System.Collections.Generic;

namespace DuckovCustomSounds.CustomEnemySounds
{
    internal sealed class VoiceRoute
    {
        public bool UseCustom { get; set; }
        public string FileFullPath { get; set; }
        public string MatchRule { get; set; }
        public List<string> TriedPaths { get; set; }
    }
}

