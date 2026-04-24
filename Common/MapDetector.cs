using System;
using DuckovCustomSounds.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MapDetection
{
    public enum MapSceneKind
    {
        Unknown = 0,
        Base = 1,
        Loading = 2,
        Combat = 3
    }

    /// <summary>
    /// 玩家所在地图检测系统 - 专门检测基地/主菜单等非战斗场景
    /// </summary>
    public class MapDetectionBehaviour : MonoBehaviour
    {
        private static readonly ILog Log = LogManager.GetLogger("Core").ForScope("MapDetection");

        #region 基础设置
        private string currentSceneName = "";
        private MapSceneKind currentSceneKind = MapSceneKind.Unknown;
        private bool isInBaseScene = false;

        // 事件：当场景状态改变时触发 (sceneName, isBase)
        public static event Action<string, bool>? OnSceneStatusChanged;
        #endregion

        void Awake()
        {
            Log.Info("地图检测系统初始化");

            // 订阅场景事件
            SceneManager.sceneLoaded += OnSceneLoaded;

            // 初始化当前场景状态
            currentSceneName = SceneManager.GetActiveScene().name;
            currentSceneKind = ClassifyScene(currentSceneName);
            isInBaseScene = currentSceneKind == MapSceneKind.Base;

            Log.Info($"初始场景: {currentSceneName}, 类型: {currentSceneKind}, 基地: {isInBaseScene}");
        }

        #region 场景检测实现
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            string previousScene = currentSceneName;
            MapSceneKind previousSceneKind = currentSceneKind;
            bool previousBaseStatus = isInBaseScene;

            currentSceneName = scene.name;
            currentSceneKind = ClassifyScene(currentSceneName);
            isInBaseScene = currentSceneKind == MapSceneKind.Base;

            Log.Info($"场景加载: {currentSceneName}, 类型: {currentSceneKind}, 基地: {isInBaseScene}");

            // 如果场景状态发生变化，触发事件
            if (previousScene != currentSceneName || previousSceneKind != currentSceneKind || previousBaseStatus != isInBaseScene)
            {
                var evt = OnSceneStatusChanged;
                if (evt != null)
                {
                    try { evt.Invoke(currentSceneName, isInBaseScene); } catch (Exception ex) { Log.Warning($"OnSceneStatusChanged 触发异常: {ex.Message}"); }
                }
            }

            // 显示当前状态通知（尽量不影响游戏体验）
            ShowSceneNotification();
        }

        // 检测是否为基地/主菜单等非战斗场景
        public bool IsBaseScene(string sceneName)
        {
            return ClassifyScene(sceneName) == MapSceneKind.Base;
        }

        public MapSceneKind ClassifyScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return MapSceneKind.Unknown;

            if (IsLoadingSceneName(sceneName))
                return MapSceneKind.Loading;

            if (IsBaseSceneName(sceneName))
                return MapSceneKind.Base;

            return MapSceneKind.Combat;
        }

        private static bool IsLoadingSceneName(string sceneName)
        {
            string lowerSceneName = sceneName.ToLower();
            return lowerSceneName.Contains("loading");
        }

        private static bool IsBaseSceneName(string sceneName)
        {
            string lowerSceneName = sceneName.ToLower();
            return lowerSceneName == "base_scenev2" ||
                   lowerSceneName.Contains("base") ||
                   lowerSceneName.Contains("home") ||
                   lowerSceneName.Contains("mainmenu") ||
                   lowerSceneName.Contains("lobby") ||
                   IsSpecialBaseSceneName(sceneName);
        }

        private static bool IsSpecialBaseSceneName(string sceneName)
        {
            string[] specialBaseScenes = { "PlayerBase", "SafeHouse", "Hub", "Menu", "Start" };
            foreach (string baseScene in specialBaseScenes)
            {
                if (sceneName.IndexOf(baseScene, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        // 显示场景状态通知（可选）
        private void ShowSceneNotification()
        {
            string statusMessage;
            switch (currentSceneKind)
            {
                case MapSceneKind.Base:
                    statusMessage = "当前在基地中";
                    break;
                case MapSceneKind.Loading:
                    statusMessage = "当前在加载界面";
                    break;
                case MapSceneKind.Combat:
                    statusMessage = "当前在战斗地图中";
                    break;
                default:
                    statusMessage = "当前在未知场景";
                    break;
            }
            Log.Info($"地图检测: {statusMessage}");
            // 移除了 PopText 调用，改为仅输出 DebugLog
        }
        #endregion

        #region 公共API
        public string GetCurrentSceneName() => currentSceneName;
        public MapSceneKind GetCurrentSceneKind() => currentSceneKind;
        public bool IsCurrentlyInBase() => isInBaseScene;
        public bool IsCurrentlyLoading() => currentSceneKind == MapSceneKind.Loading;
        public bool CheckIfBaseScene(string sceneName) => IsBaseScene(sceneName);
        public bool CheckIfLoadingScene(string sceneName) => ClassifyScene(sceneName) == MapSceneKind.Loading;
        public string GetSceneStatusSummary() => $"场景: {currentSceneName} | 类型: {currentSceneKind} | 基地: {isInBaseScene}";
        #endregion

        #region 生命周期
        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Log.Info("地图检测系统已卸载");
        }
        #endregion

        #region 调试GUI（可选）
        void OnGUI()
        {
            // 可按需启用
        }
        #endregion
    }

    /// <summary>
    /// 简化的静态地图检测工具类
    /// </summary>
    public static class MapDetector
    {
        private static MapDetectionBehaviour? instance;

        private static MapDetectionBehaviour Instance
        {
            get
            {
                if (instance == null) Initialize();
                return instance!;
            }
        }

        public static void Initialize()
        {
            if (instance == null)
            {
                GameObject detectorObject = new GameObject("MapDetectionSystem");
                instance = detectorObject.AddComponent<MapDetectionBehaviour>();
                UnityEngine.Object.DontDestroyOnLoad(detectorObject);
            }
        }

        public static bool IsInBase()
        {
            return Instance.IsCurrentlyInBase();
        }

        public static bool IsInLoading()
        {
            return Instance.IsCurrentlyLoading();
        }

        public static string GetCurrentScene()
        {
            return Instance.GetCurrentSceneName();
        }

        public static MapSceneKind GetCurrentSceneKind()
        {
            return Instance.GetCurrentSceneKind();
        }

        public static bool CheckSceneIsBase(string sceneName)
        {
            return Instance.CheckIfBaseScene(sceneName);
        }

        public static bool CheckSceneIsLoading(string sceneName)
        {
            return Instance.CheckIfLoadingScene(sceneName);
        }

        public static void SubscribeToSceneChanges(Action<string, bool> callback)
        {
            if (instance == null) Initialize();
            MapDetectionBehaviour.OnSceneStatusChanged += callback;
        }

        public static void UnsubscribeFromSceneChanges(Action<string, bool> callback)
        {
            if (instance != null && callback != null)
            {
                MapDetectionBehaviour.OnSceneStatusChanged -= callback;
            }
        }
    }
}
