using UnityEngine;
using System.Collections.Generic;
using System.IO;
using FMOD;
using Debug = UnityEngine.Debug;

namespace DuckovCustomSounds.CustomBGM
{
    // 这个类现在是纯粹的 "BGM 管理器" , 补丁劫持AudioManager/Title/BaseBGMSelector
    public static class CustomBGM
    {
        // 直接以满音量播放/停止，避免调度失败导致静音
        private const float DefaultFadeSeconds = 1.0f;

        // 1. 资源：从 "一个" 变为 "列表"（携带元信息）
        private static Sound TitleBGMSound;
        private struct MusicInfo
        {
            public string Name;
            public string Author;
            public Sound Sound;
        }
        private static List<MusicInfo> HomeBGMList = new List<MusicInfo>();

        public static bool HasHomeSongs => HomeBGMList != null && HomeBGMList.Count > 0;

        // 2. 状态：跟踪当前播放的歌曲
        private static int currentHomeBGMIndex = 0;
        private static Channel currentBGMChannel; // 用来停止播放
        // 固定绑定到 Music 总线的锁，避免 Studio 重构导致 Channel 脱离 bus
        private static FMOD.Studio.Bus _lockedMusicBus;
        private static bool _musicBusLocked;
        private static Coroutine _bgmGuardRoutine;


        // 3. 加载逻辑 (由 ModBehaviour.cs 调用)
        public static void Load()
        {
            // --- 加载主菜单 BGM ---
            string titlePath = Path.Combine(ModBehaviour.ModFolderName, "TitleBGM", "title.mp3");
            if (File.Exists(titlePath))
            {
                var result = FMODUnity.RuntimeManager.CoreSystem.createSound(titlePath, MODE.LOOP_NORMAL, out TitleBGMSound);
                if (result != RESULT.OK) ModBehaviour.ErrorMessage += $"加载 {titlePath} 失败: {result}\n";
                else BGMLogger.Info($"成功加载 {titlePath}");
            }
            else
            {
                // 主菜单音乐是可选的，不显示错误信息
                BGMLogger.Info($"未找到主菜单音乐文件: {titlePath}");
            }

            // --- 加载 HomeBGM 文件夹 (新逻辑) ---
            // 构建HomeBGM文件夹的完整路径
            string homeBGMPath = Path.Combine(ModBehaviour.ModFolderName, "HomeBGM");

            // 检查文件夹是否存在，不存在则记录日志并返回
            if (!Directory.Exists(homeBGMPath))
            {
                // 大厅音乐是可选的，不显示错误信息
                Debug.Log($"[CustomSounds] 未找到大厅音乐文件夹（可选）: {homeBGMPath}");
                return; // 文件夹都不存在，直接返回
            }

            // 获取文件夹内所有 .mp3 文件（递归搜索）
            string[] musicFiles = Directory.GetFiles(homeBGMPath, "*.mp3");
            BGMLogger.Info($"在 HomeBGM 中找到 {musicFiles.Length} 首歌曲。");

            // 遍历所有找到的音乐文件
            foreach (string filePath in musicFiles)
            {
                // 解析文件名中的曲名/作者信息
                var info = ParseMusicInfo(filePath);
                // 创建FMOD声音对象，设置为循环播放模式


                var result = FMODUnity.RuntimeManager.CoreSystem.createSound(filePath, MODE.LOOP_NORMAL, out Sound newSong);
                if (result == RESULT.OK)
                {
                    // 将创建的声音对象关联到MusicInfo结构
                    info.Sound = newSong;
                    // 将解析后的音乐信息添加到播放列表
                    HomeBGMList.Add(info);
                    BGMLogger.Info($"成功加载 {filePath} → {info.Name} - {info.Author}");
                }
                else ModBehaviour.ErrorMessage += $"加载 {filePath} 失败: {result}\n";
            }
        }

        // 4. 卸载逻辑 (由 ModBehaviour.cs 调用)
        public static void Unload()
        {
            // 停止当前 BGM
            if (currentBGMChannel.hasHandle())
            {
                currentBGMChannel.stop();
            }

            // 释放主菜单 BGM
            if (TitleBGMSound.hasHandle()) TitleBGMSound.release();

            // 释放所有 Home BGM
            foreach (var info in HomeBGMList)
            {
                if (info.Sound.hasHandle()) info.Sound.release();
            }
            HomeBGMList.Clear();
        }

        // --- 文件名解析与元信息 ---
        // 解析音乐文件名，提取曲名和作者信息
        // 支持格式：曲名 - 作者.mp3 或直接曲名.mp3
        private static MusicInfo ParseMusicInfo(string filePath)
        {
            try
            {
                // 获取不含扩展名的文件名并去除前后空格
                string fileName = Path.GetFileNameWithoutExtension(filePath)?.Trim() ?? string.Empty;
                // 默认使用完整文件名作为曲名
                string name = fileName;
                // 默认作者为"群星"（避免作者为空）
                string author = "群星";

                // 查找文件名中的分隔符"-"，用于区分曲名和作者
                int dash = fileName.IndexOf('-');
                if (dash >= 0)
                {
                    // 提取分隔符左侧部分作为曲名
                    string left = fileName.Substring(0, dash).Trim();
                    // 提取分隔符右侧部分作为作者
                    string right = fileName.Substring(dash + 1).Trim();
                    // 如果左侧部分不为空，则作为曲名
                    if (!string.IsNullOrEmpty(left)) name = left;
                    // 如果右侧部分不为空，则作为作者
                    if (!string.IsNullOrEmpty(right)) author = right;
                }

                // 如果最终曲名为空，使用原始文件名作为后备
                if (string.IsNullOrEmpty(name)) name = Path.GetFileNameWithoutExtension(filePath) ?? "未知曲目";
                // 返回包含解析信息的MusicInfo结构，Sound对象将在后续加载
                return new MusicInfo { Name = name, Author = author, Sound = default };
            }
            catch (System.Exception ex)
            {
                // 解析失败时记录错误信息
                ModBehaviour.ErrorMessage += $"解析文件名失败: {filePath} -> {ex.Message}\n";
                // 返回默认的MusicInfo结构，使用原始文件名作为曲名
                return new MusicInfo { Name = Path.GetFileNameWithoutExtension(filePath) ?? "未知曲目", Author = "群星", Sound = default };
            }
        }

        public static bool TryGetCurrentMusicInfo(out string name, out string author)
        {
            name = null; author = null;
            if (!HasHomeSongs) return false;
            int idx = currentHomeBGMIndex;
            if (idx < 0 || idx >= HomeBGMList.Count) return false;
            var info = HomeBGMList[idx];
            name = info.Name; author = info.Author;
            return true;
        }


            public static bool TryGetCurrentHomeIndex(out int index)
            {
                if (!HasHomeSongs)
                {
                    index = -1; return false;
                }
                index = currentHomeBGMIndex; return true;
            }

            public static int GetHomeCount() => HasHomeSongs ? HomeBGMList.Count : 0;

        public static void StopCurrentBGM(bool fade)
        {
            try { if (_bgmGuardRoutine != null && ModBehaviour.Instance != null) { ModBehaviour.Instance.StopCoroutine(_bgmGuardRoutine); _bgmGuardRoutine = null; } } catch { }
            if (currentBGMChannel.hasHandle())
            {
                try { currentBGMChannel.stop(); } catch { }
            }
        }


        // --- 5. 播放器控制 (给我们的补丁用) ---

        // 播放主菜单 BGM
        public static void PlayTitleBGM()
        {
            // 在点击进入/开始播放前预热一次总线句柄，提升首次路由成功率
            ModBehaviour.KickBusWarmup(2f, 0.1f);

            if (currentBGMChannel.hasHandle())
            {
                currentBGMChannel.stop();
            }
            // 等待 Studio 就绪后再播放，避免 ERR_STUDIO_NOT_LOADED
            if (ModBehaviour.Instance != null && TitleBGMSound.hasHandle())
            {
                ModBehaviour.Instance.StartCoroutine(WaitForBusThenPlay(TitleBGMSound, true));
                return;
            }

        }

        // 播放指定索引的 Home BGM
        public static void PlayHomeBGM(int index)
        {
            // 列表为空，不播放
            if (!HasHomeSongs) return;

            // 使用数学取模确保索引有效（支持负数）
            int count = HomeBGMList.Count;
            int safeIndex = ((index % count) + count) % count;
            currentHomeBGMIndex = safeIndex;

            // 预热一次总线句柄，尽量让路由在首次就命中 Studio 总线
            ModBehaviour.KickBusWarmup(2f, 0.1f);

            // 等待总线就绪再播放（与标题页一致的延迟策略）
            var info = HomeBGMList[currentHomeBGMIndex];
            var songToPlay = info.Sound;
            if (ModBehaviour.Instance != null && songToPlay.hasHandle())
            {
                ModBehaviour.Instance.StartCoroutine(WaitForBusThenPlay(songToPlay, false));
                BGMLogger.Debug($"正在请求播放索引 {currentHomeBGMIndex}（延迟路由） -> {info.Name} - {info.Author}");
                return;
            }

            // 兜底：无法启动协程时，直接立即播放
            PlaySoundImmediate(songToPlay);
            BGMLogger.Debug($"正在播放索引 {currentHomeBGMIndex}，曲目句柄有效: {songToPlay.hasHandle()} -> {info.Name} - {info.Author}");
        }

        // 播放下一首
        public static void PlayNextHomeBGM()
        {
            if (!HasHomeSongs) return;
            PlayHomeBGM(currentHomeBGMIndex + 1);
        }

        // 播放上一首
        public static void PlayPreviousHomeBGM()
        {
            if (!HasHomeSongs) return;
            PlayHomeBGM(currentHomeBGMIndex - 1);
        }

        private static FMOD.Channel PlaySoundImmediate(FMOD.Sound sound)
        {
            if (!sound.hasHandle()) return default;

            // 严格仅在 Music bus 上播放；不可用或静音/音量为0则取消
            if (!TryResolveMusicGroup(out var group))
            {
                BGMLogger.Info("自定义BGM取消：Music总线不可用。");
                return default;
            }
            if (IsMusicMutedOrZero())
            {
                BGMLogger.Info("自定义BGM取消：Music总线音量为0或被静音。");
                return default;
            }

            var result = FMODUnity.RuntimeManager.CoreSystem.playSound(sound, group, false, out var ch);
            if (result == FMOD.RESULT.OK && ch.hasHandle())
            {
                currentBGMChannel = ch;
                BGMLogger.Info("BGM 路由到: bus:/Master/Music");
                StartBGMRouteGuard();
            }
            else
            {
                BGMLogger.Info($"自定义BGM播放失败: {result}");
            }
            return currentBGMChannel;
        }

        private static bool TryResolveMusicGroup(out FMOD.ChannelGroup cg)
        {
            cg = default;
            try
            {
                // 直接解析 Music bus 的 ChannelGroup；不依赖锁定
                if (ModBehaviour.MusicGroup.hasHandle() && !ModBehaviour.MusicGroupIsFallback)
                {
                    cg = ModBehaviour.MusicGroup; return true;
                }
                var bus = FMODUnity.RuntimeManager.GetBus("bus:/Master/Music");
                if (bus.getChannelGroup(out var g) == FMOD.RESULT.OK && g.hasHandle())
                {
                    ModBehaviour.MusicGroup = g;
                    ModBehaviour.MusicGroupIsFallback = false;
                    cg = g; return true;
                }
            }
            catch { }
            return false;
        }

        private static bool IsMusicMutedOrZero()
        {
            try
            {
                var bus = FMODUnity.RuntimeManager.GetBus("bus:/Master/Music");
                bool mute = false; float vol = 1f;
                try { bus.getMute(out mute); } catch { }
                try { bus.getVolume(out vol); } catch { }
                return mute || vol <= 0.0001f;
            }
            catch { return false; }
        }


        private static void StartBGMRouteGuard()
        {
            if (ModBehaviour.Instance == null) return;
            if (_bgmGuardRoutine != null) return;
            _bgmGuardRoutine = ModBehaviour.Instance.StartCoroutine(BGMRouteGuard());
        }

        private static System.Collections.IEnumerator BGMRouteGuard()
        {
            var wait = new UnityEngine.WaitForSeconds(0.1f);
            while (true)
            {
                bool has = false; bool playing = false;
                try { has = currentBGMChannel.hasHandle(); } catch { has = false; }
                if (!has) break;
                try { currentBGMChannel.isPlaying(out playing); } catch { playing = false; }
                if (!playing) break;

                if (IsMusicMutedOrZero())
                {
                    try { currentBGMChannel.stop(); } catch { }
                    break;
                }

                try
                {
                    if (!ModBehaviour.MusicGroup.hasHandle())
                    {
                        var bus = FMODUnity.RuntimeManager.GetBus("bus:/Master/Music");
                        if (bus.getChannelGroup(out var cg) == FMOD.RESULT.OK && cg.hasHandle())
                        {
                            ModBehaviour.MusicGroup = cg;
                            ModBehaviour.MusicGroupIsFallback = false;
                        }
                    }
                    if (ModBehaviour.MusicGroup.hasHandle())
                    {
                        currentBGMChannel.setChannelGroup(ModBehaviour.MusicGroup);
                    }
                }
                catch { }

                yield return wait;
            }
            _bgmGuardRoutine = null;
            yield break;
        }



        private static bool _rebindScheduled;
        private static string _lastResolvedBus;
        private static void MaybeScheduleRebindToMusicAfterPlay()
        {
            if (ModBehaviour.Instance == null) return;
            if (!currentBGMChannel.hasHandle()) return;
            if (_rebindScheduled) return;
            _rebindScheduled = true;
            ModBehaviour.Instance.StartCoroutine(TryRebindToMusicAfterPlay(5f, 0.2f));
        }

        private static System.Collections.IEnumerator TryRebindToMusicAfterPlay(float timeoutSeconds, float pollInterval)
        {
            float deadline = Time.realtimeSinceStartup + Mathf.Max(0.5f, timeoutSeconds);
            while (Time.realtimeSinceStartup < deadline)
            {
                if (!currentBGMChannel.hasHandle()) break;
                if (ModBehaviour.MusicGroup.hasHandle())
                {
                    var setRes = currentBGMChannel.setChannelGroup(ModBehaviour.MusicGroup);
                    if (setRes == FMOD.RESULT.OK)
                    {
                        _lastResolvedBus = "bus:/Master/Music";
                        BGMLogger.Info("重新绑定播放中的 BGM 到 Music bus。");
                        break;
                    }
                    else
                    {
                        BGMLogger.Warn($"重新绑定 setChannelGroup 失败: {setRes}");
                    }
                }
                yield return new UnityEngine.WaitForSeconds(Mathf.Max(0.05f, pollInterval));
            }
            _rebindScheduled = false;
        }


        private static System.Collections.IEnumerator WaitForBusThenPlay(FMOD.Sound sound, bool isTitle)
        {
            // 始终要求 Music 总线可用；不可用则取消播放
            bool requireMusic = true;
            float deadline = Time.realtimeSinceStartup + (isTitle ? 2.0f : 30.0f);
            bool musicOk = false;

            while (Time.realtimeSinceStartup < deadline)
            {
                try
                {
                    var bus = FMODUnity.RuntimeManager.GetBus("bus:/Master/Music");
                    var res = bus.getChannelGroup(out var cg);
                    if (res == FMOD.RESULT.OK && cg.hasHandle())
                    {
                        ModBehaviour.MusicGroup = cg;
                        ModBehaviour.MusicGroupIsFallback = false;
                        musicOk = true;
                        BGMLogger.Info("(延迟) Music bus 已就绪。");
                        break;
                    }
                }
                catch { }
                yield return new UnityEngine.WaitForSeconds(0.1f);
            }

            if (requireMusic && !musicOk)
            {
                BGMLogger.Info("自定义BGM取消：Music总线在限定时间内不可用。");
                yield break;
            }

            if (IsMusicMutedOrZero())
            {
                BGMLogger.Info("自定义BGM取消：Music总线音量为0或被静音。");
                yield break;
            }
            // 若有 Stinger 正在播放，等待其结束（最多 10s），避免时序冲突
            bool stingerTimeout = false;
            float stDeadline = Time.realtimeSinceStartup + 10f;
            while (Time.realtimeSinceStartup < stDeadline)
            {
                bool stPlaying = false;
                try { stPlaying = Duckov.AudioManager.IsStingerPlaying; } catch { stPlaying = false; }
                if (!stPlaying) break;
                yield return new UnityEngine.WaitForSeconds(0.1f);
            }
            bool stillPlaying = false;
            try { stillPlaying = Duckov.AudioManager.IsStingerPlaying; } catch { stillPlaying = false; }
            if (stillPlaying) stingerTimeout = true;
            if (stingerTimeout)
            {
                BGMLogger.Info("自定义BGM取消：Stinger仍在播放（超时）。");
                yield break;
            }


            if (currentBGMChannel.hasHandle())
            {
                currentBGMChannel.stop();
            }
            if (sound.hasHandle())
            {
                PlaySoundImmediate(sound);
            }
        }

    }
}
