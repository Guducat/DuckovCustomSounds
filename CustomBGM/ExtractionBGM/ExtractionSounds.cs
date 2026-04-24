using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using DuckovCustomSounds.CustomBGM.Core;

namespace DuckovCustomSounds.CustomBGM.ExtractionBGM
{
    /// <summary>
    /// 撤离音效控制器：
    /// 支持两种互斥模式：
    /// - 倒计时模式：在倒计时 ≤5s 时播放音效，持续到成功并屏蔽成功Stinger
    /// - 成功替换模式：仅替换撤离成功的Stinger音效
    /// </summary>
    internal static class ExtractionSounds
    {
        // 撤离成功Stinger事件字典（确保只拦截真正的撤离事件）
        public static readonly HashSet<string> ExtractionStingerKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "stg_map_zero",     // 主要撤离成功Stinger
            "stg_map_farm"      // 农场地图撤离成功Stinger
        };

        // 当前绑定的倒计时区域（只跟踪一个活动实例）
        private static WeakReference? _currentAreaRef;
        private static bool _countdownActive = false;
        private static bool _startedThisRound = false;
        private static float _lastCountdownSuccessTime = -1f;
        private const float CountdownSuccessStingerGraceSeconds = 3.0f;
        private const float CountdownCancelFadeOutSeconds = 0.35f;


        // 当前倒计时音效的播放实例（用于在离开时强制停止）
        private static FMOD.Studio.EventInstance? _currentCountdownInstance = null;
        
        // 旧逻辑：当前播放的extraction.mp3实例（用于场景切换时停止）
        private static FMOD.Studio.EventInstance? _currentLegacyInstance = null;

        /// <summary>
        /// 倒计时音效文件路径
        /// </summary>
        private static string CountdownAudioPath
        {
            get
            {
                var extractionDir = Path.Combine(ModBehaviour.ModFolderName, "Extraction");
                return AudioFileExtensions.FindFirstMusicFile(extractionDir, "countdown", "extraction")
                       ?? Path.Combine(extractionDir, "countdown.mp3");
            }
        }

        /// <summary>
        /// 成功音效文件路径
        /// </summary>
        private static string SuccessAudioPath
        {
            get
            {
                var extractionDir = Path.Combine(ModBehaviour.ModFolderName, "Extraction");
                var successPath = AudioFileExtensions.FindFirstMusicFile(extractionDir, "success");
                if (!string.IsNullOrEmpty(successPath)) return successPath;

                var titleDir = Path.Combine(ModBehaviour.ModFolderName, "TitleBGM");
                var titleBgmPath = AudioFileExtensions.FindMusicFile(titleDir, "extraction");
                if (!string.IsNullOrEmpty(titleBgmPath)) return titleBgmPath;

                return Path.Combine(extractionDir, "success.mp3"); // 默认路径（用于日志）
            }
        }

        public static void OnCountDownStarted(object countDownArea)
        {
            try
            {
                // 仅在倒计时模式下启用
                if (ExtractionBGMConfig.Mode != ExtractionBGMMode.CountdownMode)
                    return;

                _currentAreaRef = new WeakReference(countDownArea);
                _countdownActive = true;
                _startedThisRound = false;
                _lastCountdownSuccessTime = -1f;
                ExtractionBGMLogger.Debug("撤离倒计时开始：等待剩余<=5s触发音效...");
            }
            catch (Exception ex)
            {
                ExtractionBGMLogger.Warning($"OnCountDownStarted 异常：{ex.Message}");
            }
        }

        public static void OnCountDownStopped(object countDownArea)
        {
            try
            {
                if (!ReferenceEqualsFromWeak(_currentAreaRef, countDownArea)) return;

                _countdownActive = false;
                _lastCountdownSuccessTime = -1f;

                if (!_startedThisRound)
                {
                    _currentAreaRef = null;
                    return;
                }

                StopActive(fadeCountdown: true);
                ExtractionBGMLogger.Debug("撤离倒计时中止：撤离音效已开始淡出停止。");
            }
            catch (Exception ex)
            {
                ExtractionBGMLogger.Warning($"OnCountDownStopped 异常：{ex.Message}");
            }
        }

        public static void OnCountDownSucceeded(object countDownArea)
        {
            try
            {
                // 倒计时模式下：成功时不做处理，让音效自然播放完成
                if (ExtractionBGMConfig.Mode == ExtractionBGMMode.CountdownMode)
                {
                    if (!ReferenceEqualsFromWeak(_currentAreaRef, countDownArea)) return;

                    _countdownActive = false;
                    _lastCountdownSuccessTime = Time.realtimeSinceStartup;
                    ExtractionBGMLogger.Debug("撤离成功：保留撤离音效直至自然结束。");
                }
            }
            catch (Exception ex)
            {
                ExtractionBGMLogger.Warning($"OnCountDownSucceeded 异常：{ex.Message}");
            }
        }

        public static void OnTick(object countDownArea, float remainingSeconds)
        {
            try
            {
                // 仅在倒计时模式下生效
                if (ExtractionBGMConfig.Mode != ExtractionBGMMode.CountdownMode) return;
                if (_startedThisRound) return;
                if (!ReferenceEqualsFromWeak(_currentAreaRef, countDownArea)) return;
                if (remainingSeconds > 5.0f) return;
                TryStartCountdownSFX();
            }
            catch (Exception ex)
            {
                ExtractionBGMLogger.Warning($"OnTick 异常：{ex.Message}");
            }
        }

        public static void StopOnSceneChangeIfNeeded()
        {
            try
            {
                bool hasActiveSound = false;
                
                // 检查倒计时模式是否有活动音效
                if (_startedThisRound)
                {
                    hasActiveSound = true;
                }
                
                // 检查旧逻辑是否有活动音效
                if (_currentLegacyInstance.HasValue && _currentLegacyInstance.Value.isValid())
                {
                    hasActiveSound = true;
                }
                
                if (!hasActiveSound) return;
                
                // 在场景切换/StopBGM时一律停止，避免跨场景残留
                StopActive(fadeCountdown: false);
                ExtractionBGMLogger.Debug("场景切换/StopBGM：撤离音效已停止。");
            }
            catch (Exception ex)
            {
                ExtractionBGMLogger.Warning($"StopOnSceneChangeIfNeeded 异常：{ex.Message}");
            }
        }

        public static void ApplyVolumeToCurrentSounds()
        {
            ApplyConfiguredVolume(_currentCountdownInstance);
            ApplyConfiguredVolume(_currentLegacyInstance);
        }

        private static void ApplyConfiguredVolume(FMOD.Studio.EventInstance? instance)
        {
            try
            {
                if (instance.HasValue && instance.Value.isValid())
                {
                    instance.Value.setVolume(ExtractionBGMConfig.Volume);
                }
            }
            catch { }
        }

        private static bool ReferenceEqualsFromWeak(WeakReference? wr, object target)
        {
            try
            {
                if (wr == null) return false;
                var o = wr.Target;
                return o != null && ReferenceEquals(o, target);
            }
            catch { return false; }
        }

        public static bool IsExtractionStingerKey(string key)
        {
            try
            {
                if (string.IsNullOrEmpty(key)) return false;
                if (ExtractionStingerKeys.Contains(key)) return true;
                if (!key.StartsWith("stg_map_", StringComparison.OrdinalIgnoreCase)) return false;
                if (string.Equals(key, "stg_map_base", StringComparison.OrdinalIgnoreCase)) return false;

                return HasActiveExtractionContext();
            }
            catch (Exception ex)
            {
                ExtractionBGMLogger.Warning($"IsExtractionStingerKey 异常：{ex.Message}");
                return false;
            }
        }

        private static bool HasActiveExtractionContext()
        {
            try
            {
                if (_countdownActive) return true;
                if (_lastCountdownSuccessTime < 0f) return false;

                return Time.realtimeSinceStartup - _lastCountdownSuccessTime <= CountdownSuccessStingerGraceSeconds;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 尝试播放倒计时音效（倒计时模式）
        /// </summary>
        private static void TryStartCountdownSFX()
        {
            try
            {
                if (_startedThisRound) return;

                var path = CountdownAudioPath;
                if (!File.Exists(path))
                {
                    ExtractionBGMLogger.Info($"未找到倒计时音效文件：{path}");
                    return;
                }

                // 使用 2D 音效接口播放倒计时音效（单次播放）
                try
                {
                    _currentCountdownInstance = Duckov.AudioManager.PostCustomSFX(path, loop: false);
                    ApplyConfiguredVolume(_currentCountdownInstance);
                    _startedThisRound = true;
                    ExtractionBGMLogger.Info($"倒计时音效已触发（<=5s）：{Path.GetFileName(path)}");
                }
                catch (Exception ex)
                {
                    ExtractionBGMLogger.Warning($"播放倒计时音效失败：{ex.Message}");
                }
            }
            catch (Exception ex)
            {
                ExtractionBGMLogger.Warning($"TryStartCountdownSFX 异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 处理撤离成功Stinger请求（成功替换模式）
        /// </summary>
        /// <param name="stingerKey">Stinger事件键名</param>
        /// <returns>是否已处理（true=拦截原版Stinger，false=放行）</returns>
        public static bool OnSuccessStingerRequested(string stingerKey)
        {
            try
            {
                var mode = ExtractionBGMConfig.Mode;

                // 倒计时模式：屏蔽成功Stinger（倒计时音效会持续）
                if (mode == ExtractionBGMMode.CountdownMode)
                {
                    ExtractionBGMLogger.Debug("倒计时模式：屏蔽撤离成功Stinger（倒计时音效持续）");
                    return true; // 拦截
                }

                // 成功替换模式：播放自定义成功音效
                if (mode == ExtractionBGMMode.SuccessStingerMode)
                {
                    var path = SuccessAudioPath;
                    if (!File.Exists(path))
                    {
                        ExtractionBGMLogger.Info($"未找到成功音效文件：{path}");
                        return false; // 文件不存在，放行原版
                    }

                    try
                    {
                        _currentLegacyInstance = Duckov.AudioManager.PlayCustomBGM(path, loop: false);
                        ApplyConfiguredVolume(_currentLegacyInstance);
                        ExtractionBGMLogger.Info($"已播放自定义撤离成功音效：{Path.GetFileName(path)}");
                        return true; // 拦截原版
                    }
                    catch (Exception ex)
                    {
                        ExtractionBGMLogger.Warning($"播放成功音效失败：{ex.Message}");
                        return false; // 播放失败，放行原版
                    }
                }

                // 禁用模式：使用旧逻辑（兼容1.0.0行为）
                // 当 overrideExtractionBGM=false 时，播放自定义的 TitleBGM/extraction.mp3
                var titleDir = Path.Combine(ModBehaviour.ModFolderName, "TitleBGM");
                var legacyExtractionPath = AudioFileExtensions.FindMusicFile(titleDir, "extraction");
                if (!string.IsNullOrEmpty(legacyExtractionPath))
                {
                    try
                    {
                        // 保存播放实例以便后续停止
                        _currentLegacyInstance = Duckov.AudioManager.PlayCustomBGM(legacyExtractionPath, loop: false);
                        ApplyConfiguredVolume(_currentLegacyInstance);
                        ExtractionBGMLogger.Info($"已播放自定义撤离音效（旧逻辑）：{Path.GetFileName(legacyExtractionPath)} (事件: {stingerKey})");
                        return true; // 拦截原版
                    }
                    catch (Exception ex)
                    {
                        ExtractionBGMLogger.Warning($"播放自定义撤离音效失败：{ex.Message}");
                        return false; // 播放失败，放行原版
                    }
                }
                else
                {
                    ExtractionBGMLogger.Info($"未找到自定义撤离音效文件：{legacyExtractionPath}，放行原版 (事件: {stingerKey})");
                    return false; // 文件不存在，放行原版
                }
            }
            catch (Exception ex)
            {
                ExtractionBGMLogger.Warning($"OnSuccessStingerRequested 异常：{ex.Message}");
                return false; // 异常时放行原版
            }
        }

        /// <summary>
        /// 停止所有撤离音效（用于热重载配置切换）
        /// </summary>
        public static void StopAllExtractionSounds()
        {
            try
            {
                StopActive(fadeCountdown: false);
                ExtractionBGMLogger.Debug("已停止所有撤离音效（热重载）");
            }
            catch (Exception ex)
            {
                ExtractionBGMLogger.Warning($"StopAllExtractionSounds 异常：{ex.Message}");
            }
        }

        private static void StopActive(bool fadeCountdown)
        {
            try
            {
                // 停止当前播放的倒计时音效；倒计时取消使用短淡出，场景切换仍立即清理。
                if (_currentCountdownInstance.HasValue && _currentCountdownInstance.Value.isValid())
                {
                    try
                    {
                        var countdownInstance = _currentCountdownInstance.Value;
                        if (fadeCountdown)
                        {
                            FadeOutAndReleaseCountdown(countdownInstance, CountdownCancelFadeOutSeconds);
                            ExtractionBGMLogger.Debug($"倒计时音效淡出停止已启动: {CountdownCancelFadeOutSeconds:F2}s");
                        }
                        else
                        {
                            countdownInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                            countdownInstance.release();
                            ExtractionBGMLogger.Debug("已立即停止倒计时音效实例");
                        }
                    }
                    catch (Exception ex)
                    {
                        ExtractionBGMLogger.Warning($"停止倒计时音效实例失败：{ex.Message}");
                    }
                }

                // 停止旧逻辑的extraction.mp3实例
                if (_currentLegacyInstance.HasValue && _currentLegacyInstance.Value.isValid())
                {
                    try
                    {
                        _currentLegacyInstance.Value.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                        _currentLegacyInstance.Value.release();
                        ExtractionBGMLogger.Debug("已强制停止旧逻辑撤离音效实例");
                    }
                    catch (Exception ex)
                    {
                        ExtractionBGMLogger.Warning($"停止旧逻辑撤离音效实例失败：{ex.Message}");
                    }
                }

                // 重置状态标志
                _countdownActive = false;
                _startedThisRound = false;
                _lastCountdownSuccessTime = -1f;
                _currentAreaRef = null;
                _currentCountdownInstance = null;
                _currentLegacyInstance = null;
                ExtractionBGMLogger.Debug("撤离音效状态已重置");
            }
            catch (Exception ex)
            {
                ExtractionBGMLogger.Warning($"重置撤离音效状态失败：{ex.Message}");
            }
        }

        private static void FadeOutAndReleaseCountdown(FMOD.Studio.EventInstance instance, float seconds)
        {
            try
            {
                var runner = ModBehaviour.Instance;
                if (runner == null)
                {
                    StopCountdownWithAllowFadeout(instance);
                    return;
                }

                runner.StartCoroutine(FadeOutCountdownCoroutine(instance, Mathf.Max(0.01f, seconds)));
            }
            catch (Exception ex)
            {
                ExtractionBGMLogger.Warning($"启动倒计时淡出失败：{ex.Message}");
                StopCountdownWithAllowFadeout(instance);
            }
        }

        private static IEnumerator FadeOutCountdownCoroutine(FMOD.Studio.EventInstance instance, float seconds)
        {
            float startVolume = ExtractionBGMConfig.Volume;

            try
            {
                if (!instance.isValid()) yield break;
                instance.getVolume(out var actual, out _);
                if (!float.IsNaN(actual) && !float.IsInfinity(actual))
                {
                    startVolume = actual;
                }
            }
            catch { }

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                if (!instance.isValid()) yield break;

                float t = Mathf.Clamp01(elapsed / seconds);
                try
                {
                    instance.setVolume(Mathf.Lerp(startVolume, 0f, t));
                }
                catch { }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            StopCountdownWithAllowFadeout(instance);
        }

        private static void StopCountdownWithAllowFadeout(FMOD.Studio.EventInstance instance)
        {
            try
            {
                if (!instance.isValid()) return;
                try { instance.setVolume(0f); } catch { }
                instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                instance.release();
                ExtractionBGMLogger.Debug("已淡出停止倒计时音效实例");
            }
            catch (Exception ex)
            {
                ExtractionBGMLogger.Warning($"淡出停止倒计时音效实例失败：{ex.Message}");
            }
        }
    }
}
