using System;
using UnityEngine;
using MapDetection;
using Duckov.Scenes;
using UnityEngine.SceneManagement;

namespace DuckovCustomSounds.CustomBGM.SceneBGM
{
    /// <summary>
    /// 场景 BGM 模块主入口
    /// 功能：
    /// - 进入场景 BGM：场景加载完成时播放一次的迎接音乐
    /// - 场景循环 BGM：持续循环播放的背景音乐
    /// - 支持不同场景（零号区、农场镇等）配置不同音乐
    /// - 与 BOSS BGM 优先级协调，不冲突
    /// </summary>
    public static class CustomSceneBGM
    {
        private static bool _initialized = false;
        private static bool _mapSubscribed = false;

        private static bool _sceneLoaderSubscribed = false;

        // MainMenu 场景 UI/game_start 首次触发控制标志（进入 MainMenu 时重置）
        private static bool s_MenuGameStartHandledOnce = false;


        // MainMenu 开场序列进行中标志：startFX 播放期间为 true，用于抑制 mus_title 冲突
        private static bool s_MenuStartSequenceActive = false;
        public static bool IsMenuStartSequenceActive() => s_MenuStartSequenceActive;
        public static void SetMenuStartSequenceActive(bool v) { s_MenuStartSequenceActive = v; }

        /// <summary>
        /// 判定是否命中“MainMenu 场景 + UI/game_start + 首次触发”条件。
        /// 命中后会将标志位置为 true（一次性）。
        /// 仅做首次业务判定，不涉及配置与资源存在性检查。
        /// </summary>
        public static bool ShouldInterceptMainMenuGameStart(string eventName)
        {
            try
            {
                if (string.IsNullOrEmpty(eventName)) return false;
                if (!eventName.Equals("UI/game_start", StringComparison.OrdinalIgnoreCase)) return false;

                string scene = MapDetector.GetCurrentScene() ?? string.Empty;
                if (string.IsNullOrEmpty(scene)) return false;

                // 精确到 MainMenu（大小写不敏感）
                if (scene.IndexOf("mainmenu", StringComparison.OrdinalIgnoreCase) < 0) return false;

                if (s_MenuGameStartHandledOnce) return false;

                // 命中后置位，确保仅首次生效
                s_MenuGameStartHandledOnce = true;
                SceneBGMLogger.Debug("[MenuStart] 首次命中：MainMenu + UI/game_start");
                return true;
            }
            catch (Exception ex)
            {
                SceneBGMLogger.Debug($"[MenuStart] 判定异常：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 供外部（补丁/MapDetector 桥接）调用的重置方法：进入 MainMenu 时重置首次触发标志。
        /// </summary>
        public static void OnSceneChangedForMenuReset(string sceneName, bool _isBase)
        {
            try
            {
                string name = sceneName ?? string.Empty;
                if (name.IndexOf("mainmenu", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    s_MenuGameStartHandledOnce = false;
                    s_MenuStartSequenceActive = false;
                    SceneBGMLogger.Debug("[MenuStart] 进入 MainMenu：已重置首次触发标志与序列状态");
                }
            }
            catch (Exception ex)
            {
                SceneBGMLogger.Debug($"[MenuStart] 重置标志失败：{ex.Message}");
            }
        }

        // 通过 MapDetector 触发时的去抖动
        private static string _lastQueuedScene = string.Empty;
        private static float _lastQueueTime = -999f;
        private const float MIN_REQUEUE_INTERVAL = 0.75f; // 秒

        /// <summary>
        /// 初始化场景 BGM 系统
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
            {
                SceneBGMLogger.Warning("场景 BGM 系统已初始化，跳过");
                return;
            }

            try
            {
                SceneBGMLogger.Info("初始化场景 BGM 系统...");

                // 1. 加载配置
                SceneBGMConfig.Load();

                // 检查总开关
                if (!SceneBGMConfig.Enabled)
                {
                    SceneBGMLogger.Info("场景 BGM 系统已禁用（配置）");
                    _initialized = true;
                    return;
                }

                // 2. 初始化音乐文件解析器
                SceneMusicResolver.Initialize();

                // 检查是否有音乐文件
                if (!SceneMusicResolver.HasAnyEnterMusic && !SceneMusicResolver.HasAnyLoopMusic)
                {
                    SceneBGMLogger.Warning("未找到任何场景音乐文件，系统将不会播放音乐");
                }

                // 3. 初始化管理器
                SceneBGMManager.Initialize();

                // 4. 订阅场景加载事件（优先使用游戏自带 SceneLoader 事件）
                _sceneLoaderSubscribed = SubscribeToSceneEvents();

                // 5. MapDetector 始终作为加载界面桥接；普通场景仍以官方事件为准
                TrySubscribeMapDetectorBridge();

                _initialized = true;
                SceneBGMLogger.Info("场景 BGM 系统初始化完成");
            }
            catch (Exception ex)
            {
                SceneBGMLogger.Error("场景 BGM 系统初始化失败", ex);
            }
        }

        /// <summary>
        /// 订阅场景加载事件
        /// </summary>
        private static bool SubscribeToSceneEvents()
        {
            try
            {
                SceneLoader.onAfterSceneInitialize += OnSceneInitialized;
                _sceneLoaderSubscribed = true;

                MultiSceneCore.OnSubSceneLoaded += OnSubSceneLoaded;

                SceneBGMLogger.Info("已订阅场景加载事件 (SceneLoader.onAfterSceneInitialize, MultiSceneCore.OnSubSceneLoaded)");
                return true;
            }
            catch (Exception ex)
            {
                SceneBGMLogger.Error("订阅场景加载事件失败", ex);
                return false;
            }
        }

        /// <summary>
        /// 场景初始化完成回调
        /// </summary>
        private static void OnSceneInitialized(SceneLoadingContext context)
        {
            try
            {
                if (!_initialized || !SceneBGMConfig.Enabled)
                    return;

                ResolveSceneInfo(context, out string sceneId, out string displayName);

                if (string.IsNullOrEmpty(sceneId) && string.IsNullOrEmpty(displayName))
                {
                    SceneBGMLogger.Warning("未能获取场景信息，跳过播放");
                    return;
                }

                SceneBGMLogger.Info($"场景加载完成: sceneId={sceneId}, displayName={displayName}");

                // 延迟播放（避免与场景加载音效冲突）
                float delay = SceneBGMConfig.SceneLoadDelay;
                if (delay > 0)
                {
                    SceneBGMLogger.Debug($"延迟 {delay} 秒后播放场景音乐");
                    UnityEngine.MonoBehaviour coroutineRunner = GetCoroutineRunner();
                    if (coroutineRunner != null)
                    {
                        coroutineRunner.StartCoroutine(DelayedPlaySceneMusic(sceneId, displayName, delay));
                    }
                    else
                    {
                        // 无法获取协程运行器，直接播放
                        SceneBGMManager.PlaySceneMusic(sceneId, displayName);
                    }
                }
                else
                {
                    SceneBGMManager.PlaySceneMusic(sceneId, displayName);
                }
            }
            catch (Exception ex)
            {
                SceneBGMLogger.Error("处理场景初始化事件失败", ex);
            }
        }

        private static void OnSubSceneLoaded(MultiSceneCore core, UnityEngine.SceneManagement.Scene scene)
        {
            try
            {
                if (!_initialized || !SceneBGMConfig.Enabled)
                    return;

                string sceneId = string.Empty;
                string displayName = string.Empty;

                try
                {
                    var entry = core?.GetSubSceneInfo();
                    if (entry != null)
                    {
                        sceneId = entry.sceneID ?? string.Empty;
                        displayName = entry.DisplayName ?? string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    SceneBGMLogger.Debug($"获取子场景信息失败: {ex.Message}");
                }

                if (string.IsNullOrEmpty(sceneId))
                {
                    sceneId = ResolveSceneIdFromUnityScene(scene);
                }

                if (string.IsNullOrEmpty(displayName))
                {
                    displayName = ResolveDisplayName(sceneId, scene.name, string.Empty);
                }

                if (string.IsNullOrEmpty(sceneId) && string.IsNullOrEmpty(displayName))
                    return;

                ScheduleSceneMusic(sceneId, displayName, Mathf.Max(0f, SceneBGMConfig.SceneLoadDelay));
            }
            catch (Exception ex)
            {
                SceneBGMLogger.Error("处理子场景加载事件失败", ex);
            }
        }

        /// <summary>
        /// MapDetector 桥接：补充加载界面的 SceneManager 场景变化事件
        /// </summary>
        private static void TrySubscribeMapDetectorBridge()
        {
            if (_mapSubscribed) return;
            try
            {
                MapDetector.Initialize();
                MapDetector.SubscribeToSceneChanges(OnMapSceneChanged);
                _mapSubscribed = true;
                SceneBGMLogger.Info("已通过 MapDetector 订阅场景变化 (loading bridge)");
            }
            catch (Exception ex)
            {
                SceneBGMLogger.Debug($"MapDetector 订阅失败（可忽略）: {ex.Message}");
            }
        }

        private static void OnMapSceneChanged(string sceneName, bool isBase)
        {
            try
            {
                if (!_initialized || !SceneBGMConfig.Enabled)
                    return;

                // 基地/菜单由 HomeBGM 等模块处理，这里不介入
                if (isBase)
                {
                    SceneBGMManager.StopAll();
                    return;
                }

                if (string.IsNullOrEmpty(sceneName)) return;

                string lower = sceneName.ToLowerInvariant();
                bool isLoading = lower.Contains("loading");

                if (_sceneLoaderSubscribed && !isLoading)
                {
                    return;
                }

                // 频繁场景切换（加载→主关卡→子关卡）去抖动
                float now = Time.time;
                if (sceneName == _lastQueuedScene && (now - _lastQueueTime) < MIN_REQUEUE_INTERVAL)
                {
                    return;
                }

                _lastQueuedScene = sceneName;
                _lastQueueTime = now;

                // 加载界面尽快播放；其他场景按配置延迟
                float delay = isLoading ? 0.05f : Mathf.Max(0f, SceneBGMConfig.SceneLoadDelay);

                SceneBGMLogger.Debug($"[MapBridge] 计划{(isLoading ? "立即" : $"延迟 {delay}s")}播放: {sceneName}");
                ScheduleSceneMusic(sceneName, sceneName, delay);
            }
            catch (Exception ex)
            {
                SceneBGMLogger.Debug($"MapBridge 处理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从 SceneLoadingContext 获取场景信息
        /// </summary>
        private static void ResolveSceneInfo(SceneLoadingContext context, out string sceneId, out string displayName)
        {
            sceneId = string.Empty;
            displayName = string.Empty;

            try
            {
                string sceneName = context.sceneName ?? string.Empty;

                if (context.useLocation && !string.IsNullOrEmpty(context.location.SceneID))
                {
                    sceneId = context.location.SceneID;
                    displayName = GetLocationDisplayName(context);
                }

                if (string.IsNullOrEmpty(sceneId) && !string.IsNullOrEmpty(sceneName))
                {
                    var targetScene = SceneManager.GetSceneByName(sceneName);
                    sceneId = ResolveSceneIdFromUnityScene(targetScene);
                }

                if (string.IsNullOrEmpty(sceneId))
                {
                    sceneId = ResolveSceneIdFromUnityScene(SceneManager.GetActiveScene());
                }

                if (string.IsNullOrEmpty(sceneId))
                    sceneId = sceneName;

                displayName = ResolveDisplayName(sceneId, sceneName, displayName);
            }
            catch (Exception ex)
            {
                SceneBGMLogger.Debug($"获取场景信息失败: {ex.Message}");
            }
        }

        private static string ResolveSceneIdFromUnityScene(UnityEngine.SceneManagement.Scene scene)
        {
            try
            {
                if (scene.IsValid() && scene.buildIndex >= 0)
                {
                    return SceneInfoCollection.GetSceneID(scene.buildIndex) ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                SceneBGMLogger.Debug($"通过 SceneInfoCollection 获取场景 ID 失败: {ex.Message}");
            }

            return string.Empty;
        }

        private static string ResolveDisplayName(string sceneId, string sceneName, string fallback)
        {
            try
            {
                if (!string.IsNullOrEmpty(sceneId))
                {
                    var info = SceneInfoCollection.GetSceneInfo(sceneId);
                    if (info != null && !string.IsNullOrEmpty(info.DisplayName))
                        return info.DisplayName;
                }
            }
            catch (Exception ex)
            {
                SceneBGMLogger.Debug($"获取场景显示名失败: {ex.Message}");
            }

            if (!string.IsNullOrEmpty(fallback))
                return fallback;

            return !string.IsNullOrEmpty(sceneName) ? sceneName : sceneId;
        }

        private static string GetLocationDisplayName(SceneLoadingContext context)
        {
            try
            {
                return context.location.DisplayName ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void ScheduleSceneMusic(string sceneId, string displayName, float delay)
        {
            var runner = GetCoroutineRunner();
            if (runner != null && delay > 0f)
            {
                runner.StartCoroutine(DelayedPlaySceneMusic(sceneId, displayName, delay));
                return;
            }

            SceneBGMManager.PlaySceneMusic(sceneId, displayName);
        }

        /// <summary>
        /// 延迟播放场景音乐协程
        /// </summary>
        private static System.Collections.IEnumerator DelayedPlaySceneMusic(string sceneId, string displayName, float delay)
        {
            yield return new UnityEngine.WaitForSeconds(delay);
            SceneBGMManager.PlaySceneMusic(sceneId, displayName);
        }

        /// <summary>
        /// 获取协程运行器（使用 ModBehaviour）
        /// </summary>
        private static UnityEngine.MonoBehaviour GetCoroutineRunner()
        {
            try
            {
                return ModBehaviour.Instance!;
            }
            catch
            {
                SceneBGMLogger.Debug("无法获取协程运行器");
                return null!;
            }
        }

        /// <summary>
        /// 设置 BOSS BGM 激活状态（由 BOSS BGM 系统调用）
        /// </summary>
        public static void SetBossBGMActive(bool active, bool allowRebuild = true)
        {
            if (!_initialized)
                return;

            SceneBGMManager.SetBossBGMActive(active, allowRebuild);
        }

        /// <summary>
        /// 设置撤离 BGM 激活状态（由撤离 BGM 系统调用）
        /// 倒计时期间鸭子场景音乐，取消/结束时恢复。
        /// </summary>
        public static void SetExtractionActive(bool active)
        {
            if (!_initialized)
                return;

            SceneBGMManager.SetExtractionActive(active);
        }

        /// <summary>
        /// 停止所有场景音乐（供外部调用）
        /// </summary>
        public static void StopAll()
        {
            if (!_initialized)
                return;

            SceneBGMManager.StopAll();
        }

        /// <summary>
        /// 获取当前播放状态（调试用）
        /// </summary>
        public static string GetPlaybackStatus()
        {
            if (!_initialized)
                return "Not initialized";

            return SceneBGMManager.GetPlaybackStatus();
        }
    }
}
