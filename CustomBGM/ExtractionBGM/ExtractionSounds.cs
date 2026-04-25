using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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
        private static WeakReference _currentAreaRef;
        private static bool _startedThisRound = false;


        // 当前倒计时音效的播放实例（用于在离开时强制停止）
        private static FMOD.Studio.EventInstance? _currentCountdownInstance = null;
        
        // 旧逻辑：当前播放的extraction.mp3实例（用于场景切换时停止）
        private static FMOD.Studio.EventInstance? _currentLegacyInstance = null;

        // 倒计时音效文件候选
        private static readonly string[] kCountdownCandidates = new[] { "countdown.mp3", "countdown.wav", "extraction.mp3", "extraction.wav" };

        // 成功音效文件候选（优先 Extraction/success.mp3，回退到 TitleBGM/extraction.mp3）
        private static readonly string[] kSuccessCandidates = new[] { "success.mp3", "success.wav" };

        /// <summary>
        /// 倒计时音效文件路径
        /// </summary>
        private static string CountdownAudioPath
        {
            get
            {
                var baseDir = Path.Combine(ModBehaviour.ModFolderName, "Extraction");
                foreach (var f in kCountdownCandidates)
                {
                    var p = Path.Combine(baseDir, f);
                    if (File.Exists(p)) return p;
                }
                return Path.Combine(baseDir, "countdown.mp3"); // 优先用于日志提示
            }
        }

        /// <summary>
        /// 成功音效文件路径
        /// </summary>
        private static string SuccessAudioPath
        {
            get
            {
                // 优先使用 Extraction/success.mp3
                var extractionDir = Path.Combine(ModBehaviour.ModFolderName, "Extraction");
                foreach (var f in kSuccessCandidates)
                {
                    var p = Path.Combine(extractionDir, f);
                    if (File.Exists(p)) return p;
                }

                // 回退到 TitleBGM/extraction.mp3（兼容旧配置）
                var titleBgmPath = Path.Combine(ModBehaviour.ModFolderName, "TitleBGM", "extraction.mp3");
                if (File.Exists(titleBgmPath)) return titleBgmPath;

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
                _startedThisRound = false;
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
                if (!_startedThisRound) return;
                StopActiveImmediate();
                ExtractionBGMLogger.Debug("撤离倒计时中止：已停止撤离音效。");
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
                StopActiveImmediate();
                ExtractionBGMLogger.Debug("场景切换/StopBGM：撤离音效已停止。");
            }
            catch (Exception ex)
            {
                ExtractionBGMLogger.Warning($"StopOnSceneChangeIfNeeded 异常：{ex.Message}");
            }
        }

        private static bool ReferenceEqualsFromWeak(WeakReference wr, object target)
        {
            try
            {
                if (wr == null) return false;
                var o = wr.Target;
                return o != null && ReferenceEquals(o, target);
            }
            catch { return false; }
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
                        Duckov.AudioManager.PlayCustomBGM(path, loop: false);
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
                var legacyExtractionPath = Path.Combine(ModBehaviour.ModFolderName, "TitleBGM", "extraction.mp3");
                if (File.Exists(legacyExtractionPath))
                {
                    try
                    {
                        // 保存播放实例以便后续停止
                        _currentLegacyInstance = Duckov.AudioManager.PlayCustomBGM(legacyExtractionPath, loop: false);
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
                StopActiveImmediate();
                ExtractionBGMLogger.Debug("已停止所有撤离音效（热重载）");
            }
            catch (Exception ex)
            {
                ExtractionBGMLogger.Warning($"StopAllExtractionSounds 异常：{ex.Message}");
            }
        }

        private static void StopActiveImmediate()
        {
            try
            {
                // 真正停止当前播放的倒计时音效
                if (_currentCountdownInstance.HasValue && _currentCountdownInstance.Value.isValid())
                {
                    try
                    {
                        _currentCountdownInstance.Value.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                        _currentCountdownInstance.Value.release();
                        ExtractionBGMLogger.Debug("已强制停止倒计时音效实例");
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
                _startedThisRound = false;
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
    }
}

