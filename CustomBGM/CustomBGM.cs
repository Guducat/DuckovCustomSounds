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
            if (!currentBGMChannel.hasHandle()) return;
            currentBGMChannel.stop();
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
            var group = ResolveBestGroup();
            if (!group.hasHandle()) { BGMLogger.Warn("没有有效的 ChannelGroup；为安全起见在 Core master 上播放。"); }
            var result = FMODUnity.RuntimeManager.CoreSystem.playSound(sound, group.hasHandle() ? group : default, false, out var ch);
            if (result == FMOD.RESULT.OK && ch.hasHandle())
            {
                currentBGMChannel = ch;
                if (!string.IsNullOrEmpty(_lastResolvedBus))
                    BGMLogger.Info($"BGM 路由到: {_lastResolvedBus}");
                MaybeScheduleRebindToMusicAfterPlay();
            }
            else
            {
                BGMLogger.Warn($"立即播放声音失败: {result}");
            }
            return currentBGMChannel;
        }
        private static string _lastResolvedBus;
        private static FMOD.ChannelGroup ResolveBestGroup()
        {
            // 1) 尝试绑定到 Music bus（如果尚未有效）
            if (!ModBehaviour.MusicGroup.hasHandle() || ModBehaviour.MusicGroupIsFallback)
            {
                var candidates = new string[] { "bus:/Master/Music" };
                foreach (var path in candidates)
                {
                    try
                    {
                        var bus = FMODUnity.RuntimeManager.GetBus(path);
                        if (bus.getChannelGroup(out var cg) == FMOD.RESULT.OK && cg.hasHandle())
                        {
                            ModBehaviour.MusicGroup = cg;
                            ModBehaviour.MusicGroupIsFallback = false;
                            if (_lastResolvedBus != path)
                            {
                                BGMLogger.Info($"在播放时解析到 Music bus: {path}");
                                _lastResolvedBus = path;
                            }
                            return cg;
                        }
                    }
                    catch { }
                }
            }
            if (ModBehaviour.MusicGroup.hasHandle() && !ModBehaviour.MusicGroupIsFallback)
            {
                if (_lastResolvedBus != "bus:/Master/Music") _lastResolvedBus = "bus:/Master/Music"; // 通用
                return ModBehaviour.MusicGroup;
            }

            // 2) 确保 SFX 句柄有效
            if (!ModBehaviour.SfxGroup.hasHandle())
            {
                try
                {
                    var sfx = FMODUnity.RuntimeManager.GetBus("bus:/Master/SFX");
                    if (sfx.getChannelGroup(out var sfxCg) == FMOD.RESULT.OK && sfxCg.hasHandle())
                    {
                        ModBehaviour.SfxGroup = sfxCg;
                    }
                }
                catch { }
            }
            if (ModBehaviour.SfxGroup.hasHandle())
            {
                if (_lastResolvedBus != "bus:/Master/SFX")
                {
                    BGMLogger.Info("使用 SFX bus（music bus 未就绪）。");
                    _lastResolvedBus = "bus:/Master/SFX";
                }
                return ModBehaviour.SfxGroup;
            }

            // 3) 回退到 Master bus，确保至少可以通过 Master 控制滑块
            try
            {
                var master = FMODUnity.RuntimeManager.GetBus("bus:/Master");
                if (master.getChannelGroup(out var mCg) == FMOD.RESULT.OK && mCg.hasHandle())
                {
                    if (_lastResolvedBus != "bus:/Master")
                    {
                        BGMLogger.Info("回退到 Master bus 通道组。");
                        _lastResolvedBus = "bus:/Master";
                    }
                    return mCg;
                }
            }
            catch { }

            // 最后的选择：返回我们拥有的任何内容（可能无效）
            return ModBehaviour.MusicGroup.hasHandle() ? ModBehaviour.MusicGroup : ModBehaviour.SfxGroup;
        }


        private static bool _rebindScheduled;
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
            bool requireMusic = !isTitle; // Home BGM 必须等到 Music 可用
            float deadline = Time.realtimeSinceStartup + (isTitle ? 2.0f : 30.0f);
            bool musicOk = false;

            // 第一阶段：在合理时间窗内轮询 Music 总线
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
                    else if (res == FMOD.RESULT.ERR_STUDIO_NOT_LOADED)
                    {
                        // 继续等
                    }
                }
                catch { }
                yield return new UnityEngine.WaitForSeconds(0.1f);
            }

            // 第二阶段：Home BGM 不落到 SFX，继续等待直至 Music 可用为止
            if (requireMusic && !musicOk)
            {
                BGMLogger.Info("Home BGM 将等待 Music bus 就绪（不使用 SFX 回退）。");
                while (!musicOk)
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
                    yield return new UnityEngine.WaitForSeconds(0.2f);
                }
            }

            // 安全播放
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
