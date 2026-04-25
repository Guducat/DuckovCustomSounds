namespace DuckovCustomSounds.CustomKillFeedback
{
    /// <summary>
    /// 击杀事件上下文：用于在 UI / 音效 / 日志之间共享信息。
    /// </summary>
    internal readonly struct KillEventContext
    {
        public KillEventContext(int streak, bool isHeadshot, bool isGoldenHeadshot = false, bool isExplosion = false, bool isMelee = false)
        {
            if (streak < 1) streak = 1;
            Streak = streak;
            IsHeadshot = isHeadshot;
            IsGoldenHeadshot = isGoldenHeadshot;
            IsExplosion = isExplosion;
            IsMelee = isMelee;
        }

        public int Streak { get; }
        public bool IsHeadshot { get; }
        public bool IsGoldenHeadshot { get; }
        public bool IsExplosion { get; }
        public bool IsMelee { get; }

        public KillEventContext WithStreak(int streak) =>
            new KillEventContext(streak, IsHeadshot, IsGoldenHeadshot, IsExplosion, IsMelee);

        public static KillEventContext Legacy(int streak, bool isHeadshot) =>
            new KillEventContext(streak, isHeadshot);
    }
}
