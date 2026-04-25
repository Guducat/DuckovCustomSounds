using System;
using UnityEngine;
using Duckov; // AudioManager

namespace DuckovCustomSounds.CustomBGM.SceneBGM
{
    /// <summary>
    /// 场景 BGM 控制器 - MonoBehaviour
    /// 负责单个场景音乐的播放控制：
    /// - 持有 FMOD EventInstance
    /// - 支持循环播放（loop=true）和单次播放（loop=false）
    /// - 音量淡入淡出控制
    /// - 单次播放完成回调
    /// - OnDestroy() 自动清理资源
    /// </summary>
    internal class SceneBGMController : MonoBehaviour
    {
        // FMOD EventInstance
        private FMOD.Studio.EventInstance? bgmInstance;

        // 场景信息
        private string sceneName;
        private string musicType; // "Enter" or "Loop"

        // 播放模式
        private bool isLoop;

        // 音量控制
        private float currentVolume = 0f;
        private float targetVolume = 1f; // 目标音量（场景音乐通常全音量）
        private float baseVolume = 1f; // 基础音量（来自配置）
        private float fadeSpeed; // 淡入淡出速度

        // 状态
        private bool isPrioritySuppressed = false; // 是否被优先级抑制（BOSS BGM 激活时）
        private bool isPlaying = false;
        private bool isFadingOut = false;

        // 播放完成回调（仅用于单次播放）
        public event Action OnPlaybackFinished;

        // 定时器（用于检测播放结束）
        private float playbackCheckTimer = 0f;
        private const float PLAYBACK_CHECK_INTERVAL = 0.5f;

        /// <summary>
        /// 初始化控制器
        /// </summary>
        public void Initialize(string musicPath, string sceneName, string musicType, bool isLoop, float baseVolume, float fadeDuration)
        {
            try
            {
                this.sceneName = sceneName;
                this.musicType = musicType;
                this.isLoop = isLoop;
                this.baseVolume = baseVolume;
                this.fadeSpeed = 1f / fadeDuration;

                SceneBGMLogger.Debug($"初始化 {musicType} BGM Controller: {sceneName}, loop={isLoop}, volume={baseVolume}");

                // 播放 BGM
                SceneBGMLogger.Debug($"加载音频路径: {musicPath}");
                bgmInstance = AudioManager.PlayCustomBGM(musicPath, loop: isLoop);

                if (!bgmInstance.HasValue || !bgmInstance.Value.isValid())
                {
                    SceneBGMLogger.Error($"播放场景 {musicType} BGM 失败: {sceneName}");
                    Destroy(this);
                    return;
                }

                // 设置初始音量为 0（淡入效果）
                bgmInstance.Value.setVolume(0f);
                currentVolume = 0f;
                targetVolume = baseVolume;
                isPlaying = true;

                SceneBGMLogger.Info($"场景 {musicType} BGM 已启动: {sceneName} (loop={isLoop}, fadeDuration={fadeDuration}s)");
            }
            catch (Exception ex)
            {
                SceneBGMLogger.Error($"初始化 SceneBGMController 失败: {sceneName}", ex);
                Destroy(this);
            }
        }

        /// <summary>
        /// Update - 音量淡入淡出 + 播放结束检测
        /// </summary>
        void Update()
        {
            if (!isPlaying || !bgmInstance.HasValue || !bgmInstance.Value.isValid())
                return;

            try
            {
                // 1. 音量淡入淡出
                float effectiveTargetVolume = isPrioritySuppressed ? 0f : targetVolume;

                if (!Mathf.Approximately(currentVolume, effectiveTargetVolume))
                {
                    // 平滑淡入淡出
                    currentVolume = Mathf.MoveTowards(currentVolume, effectiveTargetVolume, fadeSpeed * Time.deltaTime);
                    bgmInstance.Value.setVolume(currentVolume);

                    // 淡出完成后停止
                    if (isFadingOut && Mathf.Approximately(currentVolume, 0f))
                    {
                        StopPlayback();
                        return;
                    }
                }

                // 2. 检测单次播放是否结束
                if (!isLoop && isPlaying)
                {
                    playbackCheckTimer += Time.deltaTime;
                    if (playbackCheckTimer >= PLAYBACK_CHECK_INTERVAL)
                    {
                        playbackCheckTimer = 0f;
                        CheckPlaybackFinished();
                    }
                }
            }
            catch (Exception ex)
            {
                SceneBGMLogger.Error($"Update 异常: {sceneName}", ex);
            }
        }

        /// <summary>
        /// 检测播放是否结束
        /// </summary>
        private void CheckPlaybackFinished()
        {
            if (!bgmInstance.HasValue)
                return;

            try
            {
                bgmInstance.Value.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE state);

                if (state == FMOD.Studio.PLAYBACK_STATE.STOPPED || state == FMOD.Studio.PLAYBACK_STATE.STOPPING)
                {
                    SceneBGMLogger.Info($"场景 {musicType} BGM 播放完成: {sceneName}");
                    isPlaying = false;

                    // 触发播放完成回调
                    OnPlaybackFinished?.Invoke();

                    // 自动销毁控制器
                    Destroy(this);
                }
            }
            catch (Exception ex)
            {
                SceneBGMLogger.Warning($"检测播放状态失败: {sceneName}, {ex.Message}");
            }
        }

        /// <summary>
        /// 设置优先级抑制（BOSS BGM 激活时调用）
        /// </summary>
        public void SetPrioritySuppressed(bool suppressed)
        {
            if (isPrioritySuppressed == suppressed)
                return;

            isPrioritySuppressed = suppressed;
            SceneBGMLogger.Debug($"{musicType} BGM 优先级抑制: {sceneName} -> {suppressed}");
        }

        /// <summary>
        /// 设置音量（用于实时配置调节）
        /// </summary>
        public void SetVolume(float volume)
        {
            baseVolume = Mathf.Clamp01(volume);
            targetVolume = baseVolume;
            SceneBGMLogger.Debug($"{musicType} BGM 音量调节: {sceneName} -> {baseVolume}");
        }

        /// <summary>
        /// 淡出并停止
        /// </summary>
        public void FadeOutAndStop()
        {
            if (!isPlaying)
                return;

            isFadingOut = true;
            SceneBGMLogger.Debug($"淡出 {musicType} BGM: {sceneName}");
        }

        /// <summary>
        /// 立即停止播放
        /// </summary>
        private void StopPlayback()
        {
            if (bgmInstance.HasValue && bgmInstance.Value.isValid())
            {
                try
                {
                    bgmInstance.Value.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    bgmInstance.Value.release();
                    SceneBGMLogger.Debug($"停止 {musicType} BGM: {sceneName}");
                }
                catch (Exception ex)
                {
                    SceneBGMLogger.Warning($"停止 BGM 失败: {sceneName}, {ex.Message}");
                }
            }

            bgmInstance = null;
            isPlaying = false;
        }

        /// <summary>
        /// 检查是否有效
        /// </summary>
        public bool IsValid()
        {
            return bgmInstance.HasValue && bgmInstance.Value.isValid() && isPlaying;
        }

        /// <summary>
        /// 获取场景名称
        /// </summary>
        public string GetSceneName()
        {
            return sceneName;
        }

        /// <summary>
        /// 获取音乐类型
        /// </summary>
        public string GetMusicType()
        {
            return musicType;
        }

        /// <summary>
        /// OnDestroy - 自动清理资源
        /// </summary>
        void OnDestroy()
        {
            if (bgmInstance.HasValue && bgmInstance.Value.isValid())
            {
                try
                {
                    bgmInstance.Value.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    bgmInstance.Value.release();
                    SceneBGMLogger.Debug($"清理 {musicType} BGM 资源: {sceneName}");
                }
                catch (Exception ex)
                {
                    SceneBGMLogger.Warning($"清理 BGM 资源失败: {sceneName}, {ex.Message}");
                }
            }

            bgmInstance = null;
        }
    }
}
