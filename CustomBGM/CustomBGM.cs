using DuckovCustomSounds.CustomBGM.HomeBGM;

namespace DuckovCustomSounds.CustomBGM
{
    /// <summary>
    /// 向后兼容包装类：委托到 HomeBGMManager
    /// 保持原有接口不变，方便其他模块调用
    /// </summary>
    public static class CustomBGM
    {
        // --- 向后兼容属性 ---
        public static bool HasHomeSongs => HomeBGMManager.HasHomeSongs;

        // --- 加载/卸载逻辑（委托到 HomeBGMManager）---
        public static void Load() => HomeBGMManager.Load();
        public static void Unload() => HomeBGMManager.Unload();

        // --- 公共接口：获取音乐信息（委托）---
        public static bool TryGetCurrentMusicInfo(out string name, out string author)
            => HomeBGMManager.TryGetCurrentMusicInfo(out name, out author);

        public static bool TryGetHomeMusicInfo(int index, out string name, out string author, out string filePath)
            => HomeBGMManager.TryGetHomeMusicInfo(index, out name, out author, out filePath);

        public static bool TryGetCurrentHomeIndex(out int index)
            => HomeBGMManager.TryGetCurrentHomeIndex(out index);

        public static int GetHomeCount() => HomeBGMManager.GetHomeCount();

        // --- 播放控制（委托）---
        public static void PlayTitleBGM() => HomeBGMManager.PlayTitleBGM();
        public static void PlayHomeBGM(int index) => HomeBGMManager.PlayHomeBGM(index);
        public static void PlayNextHomeBGM() => HomeBGMManager.PlayNextHomeBGM();
        public static void PlayPreviousHomeBGM() => HomeBGMManager.PlayPreviousHomeBGM();
        public static void PlayHomeBGMByFilePath(string filePath, string bgmName)
            => HomeBGMManager.PlayHomeBGMByFilePath(filePath, bgmName);

        public static int GetRandomHomeIndex(bool avoidImmediateRepeat)
            => HomeBGMManager.GetRandomHomeIndex(avoidImmediateRepeat);

        public static void StopCurrentBGM(bool fade) => HomeBGMManager.StopCurrentBGM(fade);

        // --- 自动切歌控制（委托）---
        public static void EnableAutoAdvance(int currentIndex, int totalCount)
            => HomeBGMManager.EnableAutoAdvance(currentIndex, totalCount);
    }
}
