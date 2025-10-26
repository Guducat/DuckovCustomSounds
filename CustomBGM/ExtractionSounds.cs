using System;
using System.Collections;
using System.IO;
using DuckovCustomSounds.Logging;
using FMOD;
using FMODUnity;
using UnityEngine;

namespace DuckovCustomSounds.CustomBGM
{
    /// <summary>
    /// 撤离音效（SFX 层，2D）控制器：
    /// - 在倒计时剩余 <= 5s 时开始播放（仅一次）
    /// - 中止/取消撤离时立刻停止
    /// - 成功撤离时允许播放至结束
    /// - 当 overrideExtractionBGM = true 时，拦截游戏原始撤离 BGM（由 Patch 处理），并在场景切换/StopBGM 时中止本音效
    /// </summary>
    internal static class ExtractionSounds
    {
        private static readonly ILog Log = LogManager.GetLogger("ExtractionSounds");

        // 当前绑定的倒计时区域（只跟踪一个活动实例）
        private static WeakReference _currentAreaRef;
        private static bool _startedThisRound = false;

        // FMOD 状态
        private static Sound _sound;
        private static Channel _channel;

        private static readonly string[] kCandidateFiles = new[] { "countdown.mp3", "countdown.wav", "extraction.mp3", "extraction.wav" };
        private static string AudioFilePath
        {
            get
            {
                // DuckovCustomSounds/Extraction/<file>
                var baseDir = Path.Combine(ModBehaviour.ModFolderName, "Extraction");
                foreach (var f in kCandidateFiles)
                {
                    var p = Path.Combine(baseDir, f);
                    if (File.Exists(p)) return p;
                }
                return Path.Combine(baseDir, "countdown.mp3"); // 优先用于日志提示
            }
        }

        public static void OnCountDownStarted(object countDownArea)
        {
            try
            {
                _currentAreaRef = new WeakReference(countDownArea);
                _startedThisRound = false;
                Log.Debug("撤离倒计时开始：等待剩余<=5s触发音效...");
            }
            catch (Exception ex)
            {
                Log.Warning($"OnCountDownStarted 异常：{ex.Message}");
            }
        }

        public static void OnCountDownStopped(object countDownArea)
        {
            try
            {
                if (!_startedThisRound) return;
                StopActiveImmediate();
                Log.Debug("撤离倒计时中止：已停止撤离音效。");
            }
            catch (Exception ex)
            {
                Log.Warning($"OnCountDownStopped 异常：{ex.Message}");
            }
        }

        public static void OnCountDownSucceeded(object countDownArea)
        {
            try
            {
                // 成功时不做处理，让音效自然播放完成
                Log.Debug("撤离成功：保留撤离音效直至自然结束。");
            }
            catch (Exception ex)
            {
                Log.Warning($"OnCountDownSucceeded 异常：{ex.Message}");
            }
        }

        public static void OnTick(object countDownArea, float remainingSeconds)
        {
            try
            {
                if (_startedThisRound) return;
                if (!ReferenceEqualsFromWeak(_currentAreaRef, countDownArea)) return;
                if (remainingSeconds > 5.0f) return;
                TryStart2DSFX();
            }
            catch (Exception ex)
            {
                Log.Warning($"OnTick 异常：{ex.Message}");
            }
        }

        public static void StopOnSceneChangeIfNeeded()
        {
            try
            {
                if (!_startedThisRound) return;
                // 在场景切换/StopBGM时一律停止，避免跨场景残留
                StopActiveImmediate();
                Log.Debug("场景切换/StopBGM：撤离音效已停止。");
            }
            catch (Exception ex)
            {
                Log.Warning($"StopOnSceneChangeIfNeeded 异常：{ex.Message}");
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

        private static void TryStart2DSFX()
        {
            try
            {
                if (_startedThisRound) return;
                if (!RuntimeManager.IsInitialized) { Log.Info("FMOD 未初始化，无法播放撤离音效。"); return; }

                var path = AudioFilePath;
                if (!File.Exists(path))
                {
                    Log.Info($"未找到撤离音效文件：{path}");
                    return;
                }

                var mode = MODE.CREATESTREAM | MODE._2D | MODE.LOOP_OFF;
                var res = RuntimeManager.CoreSystem.createSound(path, mode, out _sound);
                if (res != RESULT.OK || !_sound.hasHandle())
                {
                    Log.Warning($"撤离音效 createSound 失败：{res}");
                    return;
                }

                var group = ResolveSfxGroupSafe();
                var playRes = RuntimeManager.CoreSystem.playSound(_sound, group, false, out _channel);
                if (playRes != RESULT.OK || !_channel.hasHandle())
                {
                    Log.Warning($"撤离音效 playSound 失败：{playRes}");
                    try { if (_sound.hasHandle()) _sound.release(); } catch { }
                    return;
                }

                _startedThisRound = true;
                Log.Info($"撤离音效已触发（<=5s）：{Path.GetFileName(path)}");

                // 协程监控播放完成后清理
                try
                {
                    if (ModBehaviour.Instance != null)
                        ModBehaviour.Instance.StartCoroutine(WaitAndCleanup());
                }
                catch { }
            }
            catch (Exception ex)
            {
                Log.Warning($"TryStart2DSFX 异常：{ex.Message}");
            }
        }

        private static IEnumerator WaitAndCleanup()
        {
            var wait = new WaitForSeconds(0.1f);
            try
            {
                while (true)
                {
                    bool has = false, playing = false;
                    try { has = _channel.hasHandle(); } catch { has = false; }
                    if (!has) break;
                    try { _channel.isPlaying(out playing); } catch { playing = false; }
                    if (!playing) break;
                    yield return wait;
                }
            }
            finally
            {
                StopActiveImmediate();
            }
        }

        private static void StopActiveImmediate()
        {
            try { if (_channel.hasHandle()) _channel.stop(); } catch { }
            try { if (_sound.hasHandle()) _sound.release(); } catch { }
            _channel.clearHandle();
            _sound.clearHandle();
            _startedThisRound = false;
            _currentAreaRef = null;
        }

        private static ChannelGroup ResolveSfxGroupSafe()
        {
            try
            {
                // 优先尝试 SFX bus
                try
                {
                    var sfxBus = RuntimeManager.GetBus("bus:/Master/SFX");
                    if (sfxBus.getChannelGroup(out var cg2) == RESULT.OK && cg2.hasHandle())
                        return cg2;
                }
                catch { }

                // 回退 Master
                if (RuntimeManager.CoreSystem.getMasterChannelGroup(out var master) == RESULT.OK && master.hasHandle())
                    return master;
            }
            catch { }
            return default;
        }
    }
}

