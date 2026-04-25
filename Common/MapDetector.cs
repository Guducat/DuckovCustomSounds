using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MapDetection
{
    /// <summary>
    /// 玩家所在地图检测系统 - 专门检测基地/主菜单等非战斗场景
    /// </summary>
    public class MapDetectionBehaviour : MonoBehaviour
    {
        #region 基础设置
        private string currentSceneName = "";
        private bool isInBaseScene = false;

        // 事件：当场景状态改变时触发 (sceneName, isBase)
        public static event Action<string, bool> OnSceneStatusChanged;
        #endregion

        void Awake()
        {
            Debug.Log("[MapDetection] 地图检测系统初始化");

            // 订阅场景事件
            SceneManager.sceneLoaded += OnSceneLoaded;

            // 初始化当前场景状态
            currentSceneName = SceneManager.GetActiveScene().name;
            isInBaseScene = IsBaseScene(currentSceneName);

            Debug.Log($"[MapDetection] 初始场景: {currentSceneName}, 基地: {isInBaseScene}");
        }

        #region 场景检测实现
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            string previousScene = currentSceneName;
            bool previousBaseStatus = isInBaseScene;

            currentSceneName = scene.name;
            isInBaseScene = IsBaseScene(currentSceneName);

            Debug.Log($"[MapDetection] 场景加载: {currentSceneName}, 基地: {isInBaseScene}");

            // 如果场景状态发生变化，触发事件
            if (previousScene != currentSceneName || previousBaseStatus != isInBaseScene)
            {
                var evt = OnSceneStatusChanged;
                if (evt != null)
                {
                    try { evt.Invoke(currentSceneName, isInBaseScene); } catch (Exception ex) { Debug.LogWarning($"[MapDetection] OnSceneStatusChanged 触发异常: {ex.Message}"); }
                }
            }

            // 显示当前状态通知（尽量不影响游戏体验）
            ShowSceneNotification();
        }

        // 检测是否为基地/主菜单等非战斗场景
        public bool IsBaseScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return false;

            string lowerSceneName = sceneName.ToLower();
            return lowerSceneName == "base_scenev2" ||
                   lowerSceneName.Contains("base") ||
                   lowerSceneName.Contains("home") ||
                   lowerSceneName.Contains("mainmenu") ||
                   lowerSceneName.Contains("lobby") ||
                   IsSpecialBaseScene(sceneName);
        }

        // 检测特殊基地场景
        private bool IsSpecialBaseScene(string sceneName)
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
            string statusMessage = isInBaseScene ? "当前在基地中" : "当前在战斗地图中";
            Debug.Log($"[MapDetection] 地图检测: {statusMessage}");
            // 移除了 PopText 调用，改为仅输出 DebugLog
        }
        #endregion

        #region 公共API
        public string GetCurrentSceneName() => currentSceneName;
        public bool IsCurrentlyInBase() => isInBaseScene;
        public bool CheckIfBaseScene(string sceneName) => IsBaseScene(sceneName);
        public string GetSceneStatusSummary() => $"场景: {currentSceneName} | 基地: {isInBaseScene}";
        #endregion

        #region 生命周期
        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Debug.Log("[MapDetection] 地图检测系统已卸载");
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
        private static MapDetectionBehaviour instance;

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
            if (instance == null) Initialize();
            return instance.IsCurrentlyInBase();
        }

        public static string GetCurrentScene()
        {
            if (instance == null) Initialize();
            return instance.GetCurrentSceneName();
        }

        public static bool CheckSceneIsBase(string sceneName)
        {
            if (instance == null) Initialize();
            return instance.CheckIfBaseScene(sceneName);
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

