using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Duckov;

namespace DuckovCustomSounds.CustomBGM.HomeBGM
{
    /// <summary>
    /// 自定义 HomeBGM 管理器（主菜单/大厅 BGM）
    /// 负责加载音频文件元数据，实际播放由 AudioManager.PostCustomSound() 处理
    /// </summary>
    public static class HomeBGMManager
    {
        // 音乐信息结构
        private struct MusicInfo
        {
            public string Name;
            public string Author;
            public string FilePath;
        }

        private static string TitleBGMPath;
        private static List<MusicInfo> HomeBGMList = new List<MusicInfo>();

        public static bool HasHomeSongs => HomeBGMList != null && HomeBGMList.Count > 0;

        // 当前播放索引（用于自动切歌）
        private static int currentHomeBGMIndex = 0;
        private static bool s_AutoAdvanceEnabled = false;

        // 当检测到当前曲目真正开始播放后才武装自动切歌
        private static bool s_AutoAdvanceArmed = false;

        // 当前播放的 BGM EventInstance（用于手动管理 BGM 停止）
        private static FMOD.Studio.EventInstance? _currentBGMInstance = null;

        // 递归保护标志：防止 StopCurrentBGM -> AudioManager.StopBGM -> StopBGM_Postfix -> StopCurrentBGM 无限递归
        private static bool _isStoppingBGM = false;

        // --- 加载逻辑 (由 ModBehaviour.cs 调用) ---
        public static void Load()
        {
            // 加载主菜单 BGM
            TitleBGMPath = Path.Combine(ModBehaviour.ModFolderName, "TitleBGM", "title.mp3");
            if (File.Exists(TitleBGMPath))
            {
                HomeBGMLogger.Info($"找到主菜单音乐: {TitleBGMPath}");
            }
            else
            {
                HomeBGMLogger.Info($"未找到主菜单音乐文件（可选）: {TitleBGMPath}");
                TitleBGMPath = null;
            }

            // 加载 HomeBGM 文件夹
            string homeBGMPath = Path.Combine(ModBehaviour.ModFolderName, "HomeBGM");
            if (!Directory.Exists(homeBGMPath))
            {
                HomeBGMLogger.Info($"未找到大厅音乐文件夹（可选）: {homeBGMPath}");
                return;
            }

            // 获取所有 .mp3 文件
            string[] musicFiles = Directory.GetFiles(homeBGMPath, "*.mp3");
            HomeBGMLogger.Info($"在 HomeBGM 中找到 {musicFiles.Length} 首歌曲");

            foreach (string filePath in musicFiles)
            {
                var info = ParseMusicInfo(filePath);
                info.FilePath = filePath;
                HomeBGMList.Add(info);
                HomeBGMLogger.Info($"加载 BGM 元数据: {info.Name} - {info.Author}");
            }
        }

        // --- 卸载逻辑 (由 ModBehaviour.cs 调用) ---
        public static void Unload()
        {
            // 停止当前 BGM
            try { AudioManager.StopBGM(); } catch { }

            HomeBGMList.Clear();
            TitleBGMPath = null;
        }

        // --- 文件名解析 ---
        private static MusicInfo ParseMusicInfo(string filePath)
        {
            try
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath)?.Trim() ?? string.Empty;
                string name = fileName;
                string author = "群星";

                int dash = fileName.IndexOf('-');
                if (dash >= 0)
                {
                    string left = fileName.Substring(0, dash).Trim();
                    string right = fileName.Substring(dash + 1).Trim();
                    if (!string.IsNullOrEmpty(left)) name = left;
                    if (!string.IsNullOrEmpty(right)) author = right;
                }

                if (string.IsNullOrEmpty(name)) name = Path.GetFileNameWithoutExtension(filePath) ?? "未知曲目";
                return new MusicInfo { Name = name, Author = author };
            }
            catch (System.Exception ex)
            {
                HomeBGMLogger.Error($"解析文件名失败: {filePath}", ex);
                return new MusicInfo { Name = Path.GetFileNameWithoutExtension(filePath) ?? "未知曲目", Author = "群星" };
            }
        }

        // --- 公共接口：获取音乐信息 ---
        public static bool TryGetCurrentMusicInfo(out string name, out string author)
        {
            name = null;
            author = null;
            if (!HasHomeSongs) return false;
            int idx = currentHomeBGMIndex;
            if (idx < 0 || idx >= HomeBGMList.Count) return false;
            var info = HomeBGMList[idx];
            name = info.Name;
            author = info.Author;
            return true;
        }

        public static bool TryGetHomeMusicInfo(int index, out string name, out string author, out string filePath)
        {
            name = null;
            author = null;
            filePath = null;
            if (!HasHomeSongs) return false;
            if (index < 0 || index >= HomeBGMList.Count) return false;
            var info = HomeBGMList[index];
            name = info.Name;
            author = info.Author;
            filePath = info.FilePath;
            return true;
        }

        public static bool TryGetCurrentHomeIndex(out int index)
        {
            if (!HasHomeSongs)
            {
                index = -1;
                return false;
            }
            index = currentHomeBGMIndex;
            return true;
        }

        public static int GetHomeCount() => HasHomeSongs ? HomeBGMList.Count : 0;

        // 获取 TitleBGM 路径（用于补丁检查）
        public static string GetTitleBGMPath() => TitleBGMPath;

        // --- 播放控制 (使用新接口) ---
        public static void PlayTitleBGM()
        {
            if (!HomeBGMConfig.Enabled) return; // 模块未启用
            if (string.IsNullOrEmpty(TitleBGMPath)) return;

            try
            {
                // 1. 停止当前 BGM（如果有）
                if (_currentBGMInstance.HasValue && _currentBGMInstance.Value.isValid())
                {
                    _currentBGMInstance.Value.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    _currentBGMInstance.Value.release();
                }

                // 2. 播放新 BGM（循环播放，走 Music 总线）
                HomeBGMLogger.Info($"使用 Music 总线播放主菜单 BGM: {TitleBGMPath}");
                _currentBGMInstance = AudioManager.PlayCustomBGM(TitleBGMPath, loop: true);

                // 3. 更新当前 BGM 名称
                SetCurrentBGMName("Title BGM");
            }
            catch (System.Exception ex)
            {
                HomeBGMLogger.Warning($"播放主菜单 BGM 失败: {ex.Message}");
            }
        }

        public static void PlayHomeBGM(int index)
        {
            if (!HomeBGMConfig.Enabled) return; // 模块未启用
            if (!HasHomeSongs) return;

            // 使用数学取模确保索引有效
            int count = HomeBGMList.Count;
            int safeIndex = ((index % count) + count) % count;
            currentHomeBGMIndex = safeIndex;

            var info = HomeBGMList[currentHomeBGMIndex];
            try
            {
                // 1. 停止当前 BGM（如果有）
                if (_currentBGMInstance.HasValue && _currentBGMInstance.Value.isValid())
                {
                    _currentBGMInstance.Value.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    _currentBGMInstance.Value.release();
                    _currentBGMInstance = null;
                }
                else if (_currentBGMInstance.HasValue)
                {
                    // 实例存在但已失效，清理引用
                    _currentBGMInstance = null;
                    s_AutoAdvanceArmed = false;
                }

                // 2. 播放新 BGM（根据配置选择总线）
                bool loopSingleTrack = !HomeBGMConfig.AutoPlayNext; // 自动切歌时不循环单曲
                if (HomeBGMConfig.UseSFXBus)
                {
                    // 使用 SFX 总线播放（实验性）
                    HomeBGMLogger.Info($"使用 SFX 总线播放 Home BGM: {info.Name} - {info.Author}");
                    _currentBGMInstance = PlayBGMOnSFXBus(info.FilePath, loopSingleTrack);
                }
                else
                {
                    // 使用 Music 总线播放（默认）
                    HomeBGMLogger.Info($"使用 Music 总线播放 Home BGM: {info.Name} - {info.Author}");
                    _currentBGMInstance = AudioManager.PlayCustomBGM(info.FilePath, loop: loopSingleTrack);
                }

                // 2.5. 应用音量设置
                ApplyVolumeToCurrentBGM();

                // 3. 更新当前 BGM 名称
                SetCurrentBGMName(info.Name);

                // 4. 重新武装自动切歌检测
                s_AutoAdvanceArmed = false;
            }
            catch (System.Exception ex)
            {
                HomeBGMLogger.Warning($"播放 Home BGM 失败: {ex.Message}");
            }
        }

        public static void PlayNextHomeBGM()
        {
            if (!HomeBGMConfig.Enabled) return; // 模块未启用
            if (!HasHomeSongs) return;
            int target = currentHomeBGMIndex + 1;

            try
            {
                // 随机模式
                if (HomeBGMConfig.RandomEnabled)
                {
                    target = GetRandomHomeIndex(HomeBGMConfig.AvoidImmediateRepeat);
                }

                var selector = UnityEngine.Object.FindObjectOfType<BaseBGMSelector>();
                if (selector != null)
                {
                    selector.Set(target, false, true);
                    HomeBGMLogger.Debug($"自动切歌：通过 BaseBGMSelector.Set 触发 → index={target}");
                    return;
                }
                else
                {
                    HomeBGMLogger.Debug("自动切歌：未找到 BaseBGMSelector，疑似处于场景过渡/主菜单，跳过切歌");
                    return;
                }
            }
            catch (System.Exception ex)
            {
                HomeBGMLogger.Debug($"自动切歌：切换下一首时异常，跳过。ex={ex.Message}");
                return;
            }
        }

        public static void PlayPreviousHomeBGM()
        {
            if (!HomeBGMConfig.Enabled) return; // 模块未启用
            if (!HasHomeSongs) return;
            int target = currentHomeBGMIndex - 1;
            try
            {
                var selector = UnityEngine.Object.FindObjectOfType<BaseBGMSelector>();
                if (selector != null)
                {
                    selector.Set(target, false, true);
                    HomeBGMLogger.Debug($"自动切歌：通过 BaseBGMSelector.Set 触发上一首 → index={target}");
                    return;
                }
                else
                {
                    HomeBGMLogger.Debug("自动切歌：未找到 BaseBGMSelector（上一首），疑似过渡/主菜单，跳过");
                    return;
                }
            }
            catch (System.Exception ex)
            {
                HomeBGMLogger.Debug($"自动切歌：切换上一首时异常，跳过。ex={ex.Message}");
                return;
            }
        }

        // 通过文件路径播放 BGM（用于留声机拦截）
        public static void PlayHomeBGMByFilePath(string filePath, string bgmName)
        {
            if (!HomeBGMConfig.Enabled) return; // 模块未启用

            try
            {
                // 1. 停止当前 BGM（如果有）
                if (_currentBGMInstance.HasValue && _currentBGMInstance.Value.isValid())
                {
                    _currentBGMInstance.Value.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    _currentBGMInstance.Value.release();
                    _currentBGMInstance = null;
                }
                else if (_currentBGMInstance.HasValue)
                {
                    // 实例存在但已失效，清理引用
                    _currentBGMInstance = null;
                    s_AutoAdvanceArmed = false;
                }

                // 2. 播放新 BGM（根据配置选择总线）
                bool loopSingleTrack = !HomeBGMConfig.AutoPlayNext; // 自动切歌时不循环单曲
                if (HomeBGMConfig.UseSFXBus)
                {
                    // 使用 SFX 总线播放（实验性）
                    HomeBGMLogger.Info($"使用 SFX 总线播放自定义 BGM: {bgmName} ({filePath})");
                    _currentBGMInstance = PlayBGMOnSFXBus(filePath, loopSingleTrack);
                }
                else
                {
                    // 使用 Music 总线播放（默认，doRelease=false，保证实例有效以供自动切歌检测）
                    HomeBGMLogger.Info($"使用 Music 总线播放自定义 BGM: {bgmName} ({filePath})");
                    _currentBGMInstance = PlayBGMOnMusicBus(filePath, loopSingleTrack);
                }

                // 2.5. 应用音量设置
                ApplyVolumeToCurrentBGM();

                // 2.6. 打印实例有效性，便于诊断
                bool instValid = _currentBGMInstance.HasValue && _currentBGMInstance.Value.isValid();
                HomeBGMLogger.Debug($"[Play] bus={(HomeBGMConfig.UseSFXBus ? "SFX" : "Music")}, loop={loopSingleTrack}, instValid={instValid}");

                // 3. 更新当前 BGM 名称
                SetCurrentBGMName(bgmName);

                // 4. 重新武装自动切歌检测
                s_AutoAdvanceArmed = false;
            }
            catch (System.Exception ex)
            {
                HomeBGMLogger.Warning($"播放自定义 BGM 失败: {ex.Message}");
            }
        }

        public static int GetRandomHomeIndex(bool avoidImmediateRepeat)
        {
            int count = GetHomeCount();
            if (count <= 0) return 0;
            if (!avoidImmediateRepeat || count == 1)
            {
                return UnityEngine.Random.Range(0, count);
            }

            // 排除当前索引的等概率采样
            int current = currentHomeBGMIndex;
            int r = UnityEngine.Random.Range(0, count - 1);
            if (r >= current) r++;
            return r;
        }

        public static void StopCurrentBGM(bool fade)
        {
            // 递归保护：如果已经在停止过程中，直接返回
            if (_isStoppingBGM)
            {
                HomeBGMLogger.Debug("StopCurrentBGM: 递归保护触发，跳过重复调用");
                return;
            }

            _isStoppingBGM = true;
            try
            {
                s_AutoAdvanceEnabled = false;

                // 停止自定义 BGM 实例（如果有）
                if (_currentBGMInstance.HasValue && _currentBGMInstance.Value.isValid())
                {
                    _currentBGMInstance.Value.stop(fade ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT : FMOD.Studio.STOP_MODE.IMMEDIATE);
                    _currentBGMInstance.Value.release();
                    _currentBGMInstance = null;
                    HomeBGMLogger.Debug("已停止自定义 BGM 实例");
                }

                // 注意：不再调用 AudioManager.StopBGM()，避免无限递归
                // 原版 BGM 由游戏本体的 AudioManager.StopBGM() 处理
            }
            catch (System.Exception ex)
            {
                HomeBGMLogger.Warning($"StopCurrentBGM 错误: {ex.Message}");
            }
            finally
            {
                _isStoppingBGM = false;
            }
        }

        // 通过反射安全更新 AudioManager 的私有字段 currentBGMName（避免修改游戏本体代码）
        private static void SetCurrentBGMName(string name)
        {
            try
            {
                var fi = typeof(AudioManager).GetField("currentBGMName", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                if (fi != null)
                {
                    fi.SetValue(null, name);
                }
            }
            catch { }
        }

        /// <summary>
        /// 使用 Music 总线播放自定义 BGM（避免默认 PostFile 的 doRelease=true 导致实例立刻失效）
        /// </summary>
        private static FMOD.Studio.EventInstance? PlayBGMOnMusicBus(string filePath, bool loopSingleTrack)
        {
            try
            {
                // 停止当前 BGM
                AudioManager.StopBGM();

                var audioManagerInstance = AudioManager.Instance;
                if (audioManagerInstance == null)
                {
                    HomeBGMLogger.Warning("AudioManager.Instance 为空，无法使用 Music 总线");
                    return null;
                }

                // 反射拿到私有字段 bgmSource
                var bgmSourceField = typeof(AudioManager).GetField("bgmSource", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (bgmSourceField == null)
                {
                    HomeBGMLogger.Warning("无法获取 bgmSource 字段，回退到 AudioManager.PlayCustomBGM (doRelease=true)");
                    return AudioManager.PlayCustomBGM(filePath, loop: loopSingleTrack);
                }

                var bgmSource = bgmSourceField.GetValue(audioManagerInstance) as Duckov.AudioObject;
                if (bgmSource == null)
                {
                    HomeBGMLogger.Warning("bgmSource 为空，回退到 AudioManager.PlayCustomBGM (doRelease=true)");
                    return AudioManager.PlayCustomBGM(filePath, loop: loopSingleTrack);
                }

                // 关键：doRelease=false，确保实例在播放期间有效，便于自动切歌状态检测
                string eventPath = loopSingleTrack ? "Music/custom_loop" : "Music/custom";
                var instance = bgmSource.PostFile(eventPath, filePath, doRelease: false);
                if (instance.HasValue && instance.Value.isValid())
                {
                    HomeBGMLogger.Debug("Music 总线播放成功 (doRelease=false)");
                }
                else
                {
                    HomeBGMLogger.Warning("Music 总线播放返回无效实例 (doRelease=false)");
                }
                return instance;
            }
            catch (System.Exception ex)
            {
                HomeBGMLogger.Warning($"Music 总线播放异常: {ex.Message}，回退到 AudioManager.PlayCustomBGM (doRelease=true)");
                return AudioManager.PlayCustomBGM(filePath, loop: loopSingleTrack);
            }
        }

        /// <summary>
        /// 使用 SFX 总线播放 BGM（实验性功能）
        /// </summary>
        private static FMOD.Studio.EventInstance? PlayBGMOnSFXBus(string filePath, bool loopSingleTrack)
        {
            try
            {
                // 停止当前 BGM
                AudioManager.StopBGM();

                // 获取 AudioManager 实例的 stingerSource（SFX 总线）
                var audioManagerInstance = AudioManager.Instance;
                if (audioManagerInstance == null)
                {
                    HomeBGMLogger.Warning("AudioManager.Instance 为空，无法使用 SFX 总线");
                    return null;
                }

                // 使用反射获取 stingerSource 字段
                var stingerSourceField = typeof(AudioManager).GetField("stingerSource", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (stingerSourceField == null)
                {
                    HomeBGMLogger.Warning("无法获取 stingerSource 字段，回退到 Music 总线");
                    return AudioManager.PlayCustomBGM(filePath, loop: loopSingleTrack);
                }

                var stingerSource = stingerSourceField.GetValue(audioManagerInstance) as Duckov.AudioObject;
                if (stingerSource == null)
                {
                    HomeBGMLogger.Warning("stingerSource 为空，回退到 Music 总线");
                    return AudioManager.PlayCustomBGM(filePath, loop: loopSingleTrack);
                }

                // 使用 SFX 总线播放（根据自动切歌配置决定是否循环，不自动释放）
                var instance = stingerSource.PostCustomSFX(filePath, doRelease: false, loop: loopSingleTrack);

                if (instance.HasValue && instance.Value.isValid())
                {
                    HomeBGMLogger.Debug("SFX 总线播放成功");
                    return instance;
                }
                else
                {
                    HomeBGMLogger.Warning("SFX 总线播放失败，回退到 Music 总线");
                    return PlayBGMOnMusicBus(filePath, loopSingleTrack);
                }
            }
            catch (System.Exception ex)
            {
                HomeBGMLogger.Warning($"SFX 总线播放异常: {ex.Message}，回退到 Music 总线");
                return PlayBGMOnMusicBus(filePath, loopSingleTrack);
            }
        }

        /// <summary>
        /// 应用音量设置到当前播放的 BGM
        /// </summary>
        public static void ApplyVolumeToCurrentBGM()
        {
            try
            {
                if (_currentBGMInstance.HasValue && _currentBGMInstance.Value.isValid())
                {
                    float volume = HomeBGMConfig.Volume;
                    int result = (int)_currentBGMInstance.Value.setVolume(volume);
                    if (result == 0) // FMOD.RESULT.OK = 0
                    {
                        HomeBGMLogger.Debug($"已应用音量设置: {volume:F2} ({volume * 100:F0}%)");
                        LogVolumeDiagnostics("ApplyVolume");
                    }
                    else
                    {
                        HomeBGMLogger.Warning($"设置音量失败，FMOD 错误码: {result}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                HomeBGMLogger.Warning($"应用音量设置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 音量诊断：记录实例音量、总线音量以及配置音量，帮助定位“音量过小”问题
        /// </summary>
        private static void LogVolumeDiagnostics(string source)
        {
            try
            {
                if (!_currentBGMInstance.HasValue)
                {
                    HomeBGMLogger.Debug($"[VolumeDiag] src={source} no-instance");
                    return;
                }
                var inst = _currentBGMInstance.Value;
                float v = -1f, vf = -1f;
                if (inst.isValid())
                {
                    try { inst.getVolume(out v, out vf); } catch {}
                }
                float master = -1f, mix = -1f;
                try { var b0 = FMODUnity.RuntimeManager.GetBus("bus:/Master"); b0.getVolume(out master); } catch {}
                try { var b1 = FMODUnity.RuntimeManager.GetBus(HomeBGMConfig.UseSFXBus ? "bus:/Master/SFX" : "bus:/Master/Music"); b1.getVolume(out mix); } catch {}
                HomeBGMLogger.Debug($"[VolumeDiag] src={source} inst=({v:F2}/{vf:F2}) bus Master={master:F2} {(HomeBGMConfig.UseSFXBus ? "SFX" : "Music")}={mix:F2} cfg={HomeBGMConfig.Volume:F2}");
            }
            catch (System.Exception ex)
            {
                HomeBGMLogger.Debug($"[VolumeDiag] err: {ex.Message}");
            }
        }

        /// <summary>
        /// 重新播放当前 BGM（用于热重载配置）
        /// </summary>
        public static void ReplayCurrentBGM()
        {
            try
            {
                if (currentHomeBGMIndex >= 0 && currentHomeBGMIndex < HomeBGMList.Count)
                {
                    HomeBGMLogger.Info("配置变更，重新播放当前 BGM");
                    PlayHomeBGM(currentHomeBGMIndex);
                }
            }
            catch (System.Exception ex)
            {
                HomeBGMLogger.Warning($"重新播放 BGM 失败: {ex.Message}");
            }
        }

        // --- 每帧更新：检测曲目结束并自动切换 ---
        public static void Update()
        {
            try
            {
                // 每 30 帧打一条心跳日志，便于确认 Update() 正常被调用
                bool logThisFrame = (UnityEngine.Time.frameCount % 30) == 0;
                // 注释掉AutoAdvance心跳日志以减少日志输出
                // if (logThisFrame)
                //     HomeBGMLogger.Debug($"[AutoAdvance] enabled={s_AutoAdvanceEnabled}, hasInst={_currentBGMInstance.HasValue}");

                if (!s_AutoAdvanceEnabled) return;

                if (!_currentBGMInstance.HasValue)
                {
                    // 注释掉AutoAdvance调试日志以减少日志输出
                    // if (logThisFrame) HomeBGMLogger.Debug("[AutoAdvance] 无有效实例（_currentBGMInstance.HasValue=false）");
                    return;
                }

                var inst = _currentBGMInstance.Value;
                if (!inst.isValid())
                {
                    // 注释掉AutoAdvance调试日志以减少日志输出
                    // if (logThisFrame) HomeBGMLogger.Debug("[AutoAdvance] 实例无效（isValid=false）");
                    _currentBGMInstance = null;
                    s_AutoAdvanceArmed = false;
                    // 注释掉AutoAdvance调试日志以减少日志输出
                    // HomeBGMLogger.Debug("[AutoAdvance] 检测到实例失效，已清理状态");
                    return;
                }

                FMOD.Studio.PLAYBACK_STATE state;
                var res = inst.getPlaybackState(out state);
                if (res != 0)
                {
                    if (logThisFrame) HomeBGMLogger.Debug($"[AutoAdvance] getPlaybackState 非 OK，FMOD={res}");
                    return; // 非 OK 忽略
                }

                if (logThisFrame){
                    //HomeBGMLogger.Debug($"[AutoAdvance] state={state}, armed={s_AutoAdvanceArmed}");
                }
                if (state == FMOD.Studio.PLAYBACK_STATE.PLAYING)
                {
                    if (!s_AutoAdvanceArmed)
                    {
                        s_AutoAdvanceArmed = true;
                        //HomeBGMLogger.Debug("[AutoAdvance] 已进入 PLAYING，武装成功");
                    }
                    return;
                }

                if (s_AutoAdvanceArmed && state == FMOD.Studio.PLAYBACK_STATE.STOPPED)
                {
                    s_AutoAdvanceArmed = false;
                    //HomeBGMLogger.Debug("[AutoAdvance] 检测到曲目结束（STOPPED），自动切换下一首");
                    PlayNextHomeBGM();
                }
            }
            catch (System.Exception ex)
            {
                //HomeBGMLogger.Debug($"自动切歌轮询异常: {ex.Message}");
            }
        }

        // --- 自动切歌控制 ---
        public static void EnableAutoAdvance(int currentIndex, int totalCount)
        {
            currentHomeBGMIndex = currentIndex;
            s_AutoAdvanceEnabled = HomeBGMConfig.AutoPlayNext;
            HomeBGMLogger.Debug($"启用自动切歌: index={currentIndex}, total={totalCount}, enabled={s_AutoAdvanceEnabled}");
        }
    }
}
