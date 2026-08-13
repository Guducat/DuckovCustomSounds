using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace DuckovCustomSounds.CustomBGM.SceneBGM
{
    /// <summary>
    /// 场景 BGM 静态管理器
    /// 管理两个独立通道：EnterBGM（进入音乐）和 LoopBGM（循环音乐）
    /// 处理：
    /// - 场景切换时的音乐播放
    /// - 进入 → 循环音乐的序列控制
    /// - 热配置响应
    /// - 优先级控制（BOSS BGM 激活时降音量）
    /// </summary>
    internal static class SceneBGMManager
    {
        // 双通道控制器
        private static SceneBGMController? currentEnterBGM = null;
        private static SceneBGMController? currentLoopBGM = null;

        // 当前场景信息
        private static string currentSceneId = "";
        private static string currentDisplayName = "";

        // 状态标志
        private static bool isInitialized = false;
        private static bool isBossBGMActive = false;
        private static bool isExtractionActive = false;


        // 跨调用请求排队（Enter 播放期间）
        private static bool s_HasPendingRequest = false;
        private static string s_PendingSceneId = "";
        private static string s_PendingDisplayName = "";

        /// <summary>
        /// 初始化管理器
        /// </summary>
        public static void Initialize()
        {
            if (isInitialized) return;

            try
            {
                // 订阅配置变更事件
                SceneBGMConfig.OnEnterBGMConfigChanged += OnEnterBGMConfigChanged;
                SceneBGMConfig.OnLoopBGMConfigChanged += OnLoopBGMConfigChanged;

                isInitialized = true;
                SceneBGMLogger.Info("SceneBGMManager 初始化完成");
            }
            catch (Exception ex)
            {
                SceneBGMLogger.Error("SceneBGMManager 初始化失败", ex);
            }
        }

        /// <summary>
        /// 播放场景音乐（场景加载完成时调用）
        /// </summary>
        public static void PlaySceneMusic(string? sceneId, string? displayName)
        {
            try
            {
                sceneId ??= string.Empty;
                displayName ??= string.Empty;

                // 检查总开关
                if (!SceneBGMConfig.Enabled)
                {
                    SceneBGMLogger.Debug("场景音乐系统已禁用");
                    return;
                }

                // 若当前 Enter BGM 正在播放，则不打断；根据策略将新请求排队
                if (currentEnterBGM != null && currentEnterBGM.IsValid())
                {
                    // 与当前场景完全一致则忽略重复触发
                    if ((!string.IsNullOrEmpty(currentSceneId) &&
                         (string.Equals(sceneId, currentSceneId, StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(displayName, currentDisplayName, StringComparison.OrdinalIgnoreCase))))
                    {
                        SceneBGMLogger.Debug("Enter BGM 正在播放且请求与当前场景一致，忽略重复触发");
                        return;
                    }

                    s_HasPendingRequest = true;
                    s_PendingSceneId = sceneId;
                    s_PendingDisplayName = displayName;
                    SceneBGMLogger.Debug($"Enter BGM 正在播放，已排队新场景请求: {displayName ?? sceneId}");
                    return;
                }

                SceneBGMLogger.Info($"开始播放场景音乐: sceneId={sceneId}, displayName={displayName}");

                // 清理旧场景音乐
                StopAll();

                // 更新当前场景信息
                currentSceneId = sceneId;
                currentDisplayName = displayName;

                // 解析音乐路径
                string? enterMusicPath = SceneMusicResolver.ResolveEnterMusicPath(sceneId, displayName);
                string? loopMusicPath = SceneMusicResolver.ResolveLoopMusicPath(sceneId, displayName);

                SceneBGMLogger.Info($"BGM启用结果 -> Enter: {(string.IsNullOrEmpty(enterMusicPath) ? "<none>" : enterMusicPath)}, Loop: {(string.IsNullOrEmpty(loopMusicPath) ? "<none>" : loopMusicPath)}");

                // 播放进入音乐（如果启用且有文件）
                if (SceneBGMConfig.EnterBGMEnabled && !string.IsNullOrEmpty(enterMusicPath))
                {
                    PlayEnterBGM(enterMusicPath, sceneId, displayName);
                }
                else
                {
                    SceneBGMLogger.Debug("进入 BGM 未启用或未找到文件");
                }

                // 播放循环音乐（如果启用且有文件）
                if (SceneBGMConfig.LoopBGMEnabled && !string.IsNullOrEmpty(loopMusicPath))
                {
                    // 如果有进入音乐，等待其播放完成后再启动循环音乐
                    if (currentEnterBGM != null && currentEnterBGM.IsValid())
                    {
                        SceneBGMLogger.Debug("等待进入音乐播放完成后启动循环音乐");
                        // 订阅进入音乐播放完成事件：为避免底层清理与新建实例同帧冲突，延迟极短时间再启动 Loop
                        currentEnterBGM.OnPlaybackFinished += () =>
                        {
                            try
                            {
                                var runner = ModBehaviour.Instance;
                                if (runner != null)
                                {
                                    runner.StartCoroutine(HandleEnterFinished(sceneId, displayName, loopMusicPath, 0.05f));
                                }
                                else
                                {
                                    HandleEnterFinishedImmediate(sceneId, displayName, loopMusicPath);
                                }
                            }
                            catch { HandleEnterFinishedImmediate(sceneId, displayName, loopMusicPath); }
                        };
                    }
                    else
                    {
                        // 直接播放循环音乐
                        PlayLoopBGM(loopMusicPath, sceneId, displayName);
                    }
                }
                else
                {
                    SceneBGMLogger.Debug("循环 BGM 未启用或未找到文件");
                }
            }
            catch (Exception ex)
            {
                SceneBGMLogger.Error($"播放场景音乐失败: {sceneId}", ex);
            }
        }

        /// <summary>
        /// 播放进入音乐
        /// </summary>
        private static void PlayEnterBGM(string musicPath, string sceneId, string displayName)
        {
            try
            {
                // 创建 GameObject 并附加控制器
                GameObject enterObj = new GameObject($"SceneEnterBGM_{sceneId}");
                UnityEngine.Object.DontDestroyOnLoad(enterObj);

                currentEnterBGM = enterObj.AddComponent<SceneBGMController>();
                currentEnterBGM.Initialize(
                    musicPath,
                    displayName ?? sceneId,
                    "Enter",
                    isLoop: false,
                    baseVolume: SceneBGMConfig.EnterBGMVolume,
                    fadeDuration: SceneBGMConfig.EnterFadeDuration
                );

                SceneBGMLogger.Info($"进入音乐已播放: {displayName}");
            }
            catch (Exception ex)
            {
                SceneBGMLogger.Error($"播放进入音乐失败: {sceneId}", ex);
            }
        }

        /// <summary>
        /// 播放循环音乐
        /// </summary>
        private static void PlayLoopBGM(string musicPath, string sceneId, string displayName)
        {
            try
            {
                // 创建 GameObject 并附加控制器
                GameObject loopObj = new GameObject($"SceneLoopBGM_{sceneId}");
                UnityEngine.Object.DontDestroyOnLoad(loopObj);

                currentLoopBGM = loopObj.AddComponent<SceneBGMController>();
                currentLoopBGM.Initialize(
                    musicPath,
                    displayName ?? sceneId,
                    "Loop",
                    isLoop: true,
                    baseVolume: SceneBGMConfig.LoopBGMVolume,
                    fadeDuration: SceneBGMConfig.LoopFadeDuration
                );

                // 若 BOSS / 撤离压制激活，立即抑制循环音乐
                ApplySuppressionToCurrentBgm();

                SceneBGMLogger.Info($"循环音乐已播放: {displayName}");
            }
            catch (Exception ex)
            {
                SceneBGMLogger.Error($"播放循环音乐失败: {sceneId}", ex);
            }
        }

        /// <summary>
        /// 停止所有场景音乐
        /// </summary>
        public static void StopAll()
        {
            SceneBGMLogger.Debug("停止所有场景音乐");

            // 停止进入音乐
            if (currentEnterBGM != null)
            {
                if (currentEnterBGM.gameObject != null)
                {
                    UnityEngine.Object.Destroy(currentEnterBGM.gameObject);
                }
                currentEnterBGM = null;
            }

            // 停止循环音乐
            if (currentLoopBGM != null)
            {
                if (currentLoopBGM.gameObject != null)
                {
                    UnityEngine.Object.Destroy(currentLoopBGM.gameObject);
                }
                currentLoopBGM = null;
            }
        }

        /// <summary>
        /// 设置 BOSS BGM 激活状态（由 BOSS BGM 系统调用）
        /// allowRebuild：解除压制时是否允许自愈重建意外停止的循环 BGM（场景切换清理应传 false）。
        /// </summary>
        public static void SetBossBGMActive(bool active, bool allowRebuild = true)
        {
            if (isBossBGMActive == active)
                return;

            isBossBGMActive = active;
            SceneBGMLogger.Debug($"BOSS BGM 状态变更: {active}");

            // Boss 解除压制时，若循环 BGM 已意外死亡则自愈重建（场景切换清理路径除外）
            if (!active && allowRebuild && currentLoopBGM != null && !currentLoopBGM.IsValid())
            {
                RestoreLoopBgmIfExpected();
            }

            ApplySuppressionToCurrentBgm();
        }

        /// <summary>
        /// 设置撤离 BGM 激活状态（由撤离 BGM 系统调用）
        /// 倒计时期间鸭子场景音乐（只降音量、不停止），取消/结束时平滑恢复。
        /// </summary>
        public static void SetExtractionActive(bool active)
        {
            if (isExtractionActive == active)
                return;

            isExtractionActive = active;
            SceneBGMLogger.Debug($"撤离 BGM 状态变更: {active}");
            ApplySuppressionToCurrentBgm();
        }

        /// <summary>
        /// 合并 BOSS 与撤离的压制状态并应用到当前场景音乐。
        /// 快速淡出速度由当前合并状态推导（撤离压制激活期间始终快速），
        /// 与 BOSS/撤离事件的调用顺序无关。压制只做音量渐变、不停止实例；
        /// 实例停止仅由场景切换或播放完成负责。
        /// </summary>
        private static void ApplySuppressionToCurrentBgm()
        {
            bool suppressed = isBossBGMActive || isExtractionActive;
            bool fastFade = isExtractionActive;

            if (currentLoopBGM != null && currentLoopBGM.IsValid())
            {
                currentLoopBGM.SetPrioritySuppressed(suppressed, fastFade);
            }

            if (currentEnterBGM != null && currentEnterBGM.IsValid())
            {
                currentEnterBGM.SetPrioritySuppressed(suppressed, fastFade);
            }
        }

        /// <summary>
        /// Boss 解除压制时自愈：循环 BGM 意外死亡且当前场景仍应有循环音乐时重建。
        /// </summary>
        private static void RestoreLoopBgmIfExpected()
        {
            if (!SceneBGMConfig.LoopBGMEnabled || string.IsNullOrEmpty(currentSceneId))
                return;

            string? loopMusicPath = SceneMusicResolver.ResolveLoopMusicPath(currentSceneId, currentDisplayName);
            if (string.IsNullOrEmpty(loopMusicPath))
                return;

            SceneBGMLogger.Info("BOSS BGM 解除压制后重建意外停止的场景循环 BGM（自愈）");

            // 清理已死亡的旧循环控制器，避免僵尸 GameObject 累积
            if (currentLoopBGM != null && currentLoopBGM.gameObject != null)
            {
                UnityEngine.Object.Destroy(currentLoopBGM.gameObject);
                currentLoopBGM = null;
            }

            PlayLoopBGM(loopMusicPath, currentSceneId, currentDisplayName);
        }

        /// <summary>
        /// 进入 BGM 配置变更回调
        /// </summary>
        private static void OnEnterBGMConfigChanged()
        {
            SceneBGMLogger.Debug("进入 BGM 配置已变更");

            // 如果禁用进入 BGM，淡出停止当前进入音乐
            if (!SceneBGMConfig.EnterBGMEnabled && currentEnterBGM != null && currentEnterBGM.IsValid())
            {
                currentEnterBGM.FadeOutAndStop();
            }

            // 如果启用进入 BGM 且当前在场景中，重新播放
            if (SceneBGMConfig.EnterBGMEnabled && currentEnterBGM == null && !string.IsNullOrEmpty(currentSceneId))
            {
                string? enterMusicPath = SceneMusicResolver.ResolveEnterMusicPath(currentSceneId, currentDisplayName);
                if (!string.IsNullOrEmpty(enterMusicPath))
                {
                    PlayEnterBGM(enterMusicPath, currentSceneId, currentDisplayName);
                }
            }

            // 音量调节
            if (currentEnterBGM != null && currentEnterBGM.IsValid())
            {
                currentEnterBGM.SetVolume(SceneBGMConfig.EnterBGMVolume);
            }
        }

        /// <summary>
        /// 循环 BGM 配置变更回调
        /// </summary>
        private static void OnLoopBGMConfigChanged()
        {
            SceneBGMLogger.Debug("循环 BGM 配置已变更");

            // 如果禁用循环 BGM，淡出停止当前循环音乐
            if (!SceneBGMConfig.LoopBGMEnabled && currentLoopBGM != null && currentLoopBGM.IsValid())
            {
                currentLoopBGM.FadeOutAndStop();
            }


            // 如果启用循环 BGM 且当前在场景中，重新播放
            if (SceneBGMConfig.LoopBGMEnabled && currentLoopBGM == null && !string.IsNullOrEmpty(currentSceneId))
            {
                string? loopMusicPath = SceneMusicResolver.ResolveLoopMusicPath(currentSceneId, currentDisplayName);
                if (!string.IsNullOrEmpty(loopMusicPath))
                {
                    PlayLoopBGM(loopMusicPath, currentSceneId, currentDisplayName);
                }
            }

            // 音量调节
            if (currentLoopBGM != null && currentLoopBGM.IsValid())
            {
                currentLoopBGM.SetVolume(SceneBGMConfig.LoopBGMVolume);
            }
        }

        /// <summary>
        /// 获取当前场景名称（用于调试）
        /// </summary>
        public static string GetCurrentSceneName()
        {
            return currentDisplayName ?? currentSceneId ?? "None";
        }


        /// <summary>
        /// 延迟启动循环 BGM（避免同帧 Stop/Start 竞争）
        /// </summary>
        private static IEnumerator DelayedStartLoop(string musicPath, string sceneId, string displayName, float delay)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, delay));
            PlayLoopBGM(musicPath, sceneId, displayName);
        }


        // 在 Enter 播放完成后，根据是否有排队请求决定后续行为
        private static IEnumerator HandleEnterFinished(string sceneId, string displayName, string loopMusicPath, float delay)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, delay));
            HandleEnterFinishedImmediate(sceneId, displayName, loopMusicPath);
        }

        private static void HandleEnterFinishedImmediate(string sceneId, string displayName, string loopMusicPath)
        {
            if (s_HasPendingRequest)
            {
                var targetScene = s_PendingSceneId;
                var targetName = s_PendingDisplayName;
                ClearPendingRequest();
                SceneBGMLogger.Debug($"Enter BGM 已播放完成，处理排队场景: {targetName ?? targetScene}");
                PlaySceneMusic(targetScene, targetName);
                return;
            }

            if (SceneBGMConfig.LoopBGMEnabled && !string.IsNullOrEmpty(loopMusicPath))
            {
                SceneBGMLogger.Debug("Enter BGM 已播放完成，开始播放循环 BGM");
                PlayLoopBGM(loopMusicPath, sceneId, displayName);
            }
            else
            {
                SceneBGMLogger.Debug("Enter BGM 已播放完成，无排队且无循环 BGM");
            }
        }

        private static void ClearPendingRequest()
        {
            s_HasPendingRequest = false;
            s_PendingSceneId = "";
            s_PendingDisplayName = "";
        }

        /// <summary>
        /// 获取当前播放状态（用于调试）
        /// </summary>
        public static string GetPlaybackStatus()
        {
            bool enterPlaying = currentEnterBGM != null && currentEnterBGM.IsValid();
            bool loopPlaying = currentLoopBGM != null && currentLoopBGM.IsValid();
            return $"Enter: {enterPlaying}, Loop: {loopPlaying}, BossBGM: {isBossBGMActive}, Extraction: {isExtractionActive}";
        }
    }
}
