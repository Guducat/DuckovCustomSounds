using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DuckovCustomSounds.CustomBGM.BossBGM
{
    /// <summary>
    /// BOSS BGM 静态管理器
    /// 管理所有活跃的 BOSS BGM Controller，处理多 BOSS 优先级（距离最近优先）
    /// </summary>
    internal static class BossBGMManager
    {
        private static List<BossBGMController> activeBosses = new List<BossBGMController>();
        private static BossBGMController currentActiveBoss = null;

        private static float _lastSwitchTime = -999f;

        /// <summary>
        /// 注册 BOSS
        /// </summary>
        public static void RegisterBoss(BossBGMController boss)
        {
            if (boss == null) return;

            if (!activeBosses.Contains(boss))
            {
                activeBosses.Add(boss);
                BossBGMLogger.Debug($"BOSS 已注册: {boss.GetBossName()}, 当前活跃 BOSS 数量: {activeBosses.Count}");
            }

            // 立即更新活跃 BGM
            UpdateActiveBGM();
        }

        /// <summary>
        /// 注销 BOSS
        /// </summary>
        public static void UnregisterBoss(BossBGMController boss)
        {
            if (boss == null) return;

            if (activeBosses.Remove(boss))
            {
                BossBGMLogger.Debug($"BOSS 已注销: {boss.GetBossName()}, 当前活跃 BOSS 数量: {activeBosses.Count}");

                // 如果注销的是当前活跃 BOSS，清除引用
                if (currentActiveBoss == boss)
                {
                    currentActiveBoss = null;
                }

                // 立即更新活跃 BGM
                UpdateActiveBGM();
            }
        }

        /// <summary>
        /// 更新活跃 BGM（由 ModBehaviour 定期调用）
        /// 策略：距离最近的 BOSS 优先
        /// </summary>
        public static void UpdateActiveBGM()
        {
            // 清理无效的 BOSS（GameObject 已销毁）
            activeBosses.RemoveAll(b => b == null || b.gameObject == null);

            bool hadActiveBoss = currentActiveBoss != null;

            if (activeBosses.Count == 0)
            {
                if (currentActiveBoss != null)
                {
                    currentActiveBoss = null;
                    BossBGMLogger.Debug("所有 BOSS 已清除，无活跃 BGM");

                    // 通知场景 BGM 系统：BOSS BGM 已停用
                    NotifySceneBGM(false);
                }
                return;
            }

            // 找到距离最近的 BOSS
            BossBGMController closest = null;
            float minDistance = float.MaxValue;

            foreach (var boss in activeBosses)
            {
                if (boss == null)
                    continue;

                float distance = boss.GetDistanceToPlayer();
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = boss;
                }
            }

            // 计算当前活跃者距离（如有）
            float currentDistance = float.MaxValue;
            if (currentActiveBoss != null)
            {
                currentDistance = currentActiveBoss.GetDistanceToPlayer();
            }

            // 切换活跃 BOSS（带防抖/粘滞）
            if (closest != currentActiveBoss)
            {
                bool wasActive = currentActiveBoss != null;
                bool willBeActive = closest != null;

                // 如果已有活跃者且候选存在，则应用防抖与距离优势判断
                if (wasActive && willBeActive)
                {
                    // 1) 切换冷却
                    float elapsed = Time.time - _lastSwitchTime;
                    float minInterval = BossBGMConfig.MinSwitchIntervalSeconds;
                    if (elapsed < minInterval)
                    {
                        BossBGMLogger.Debug($"[BossBGM] 切换冷却中：已过 {elapsed:F2}s / 冷却 {minInterval:F2}s，保持当前 {currentActiveBoss.GetBossName()}");
                        return;
                    }

                    // 2) 距离优势阈值
                    float delta = currentDistance - minDistance; // 需要大于等于阈值才允许切换
                    float required = BossBGMConfig.MinDistanceDeltaToSwitch;
                    if (delta < required)
                    {
                        BossBGMLogger.Debug($"[BossBGM] 距离优势不足：当前 {currentActiveBoss.GetBossName()}={currentDistance:F1}m, 新最近 {closest.GetBossName()}={minDistance:F1}m, 阈值={required:F1}m");
                        return;
                    }
                }

                // 旧 BOSS 设置为非活跃（静音）
                if (currentActiveBoss != null && currentActiveBoss.IsValid())
                {
                    currentActiveBoss.SetPriority(false);
                    BossBGMLogger.Debug($"BOSS BGM 静音: {currentActiveBoss.GetBossName()}");
                }

                // 新 BOSS 设置为活跃（淡入）
                if (closest != null)
                {
                    closest.SetPriority(true);
                    BossBGMLogger.Info($"切换活跃 BOSS BGM: {closest.GetBossName()} (新距离 {minDistance:F1}m / 原 {currentDistance:F1}m / 优势 {Mathf.Max(0f, currentDistance - minDistance):F1}m)");
                }

                currentActiveBoss = closest;
                _lastSwitchTime = Time.time;

                // 通知场景 BGM 系统状态变化
                if (!wasActive && willBeActive)
                {
                    // BOSS BGM 从无到有
                    NotifySceneBGM(true);
                }
                else if (wasActive && !willBeActive)
                {
                    // BOSS BGM 从有到无
                    NotifySceneBGM(false);
                }
            }
        }

        /// <summary>
        /// 通知场景 BGM 系统 BOSS BGM 状态变化
        /// </summary>
        private static void NotifySceneBGM(bool isActive)
        {
            try
            {
                SceneBGM.CustomSceneBGM.SetBossBGMActive(isActive);
                BossBGMLogger.Debug($"已通知场景 BGM 系统: BOSS BGM Active = {isActive}");
            }
            catch (System.Exception ex)
            {
                BossBGMLogger.Debug($"通知场景 BGM 系统失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理所有 BOSS（场景切换时调用）
        /// </summary>
        public static void Clear()
        {
            BossBGMLogger.Debug($"清理所有 BOSS BGM，共 {activeBosses.Count} 个");

            // 销毁所有 Controller（会触发 OnDestroy 自动注销）
            foreach (var boss in activeBosses.ToList())
            {
                if (boss != null && boss.gameObject != null)
                {
                    Object.Destroy(boss);
                }
            }

            activeBosses.Clear();
            currentActiveBoss = null;

            //
            //
            //
            //
            //
            //
            //
            //
            //
            //
            //
            //
            //
            //
            //
            //
            //
            //
            //
            //
            //
            //
            //
            //
            //
            //
            
            //
            //
            //
            //
            //
            //
            //
            //
            // 
            //
            //
            // 
            // 
            //
            //
            //

            //
            //
            //

            //
            //
            
            //
            //
            
            //
            //

            //
            //
            // 
            // 
            //
            //

            // 
            //
            //
            // 
            // 
            // 
            // 
            // 
            // 
            //
            // 
            // 
            //
            // 
            //
            
            //
            // 
            // 
            // 
            //
            //
            // 
            // 
            // 
            //
            
            //
            // 
            //
            // 
            // 
            // 
            //
            
            //
            // 
            // 
            // 
            //
            //

            //
            // 
            // 
            //
            // 
            // 
            
            //
            // 
            
            //
            // 
            // 
            //
            // 
            // 
            // 
            // 
            // 
            
            //
            // 
            // 
            // 
            //
            // 
            
            // 
            // 
            //
            // 
            // 
            //
            // 
            // 
            // 
            // 
            // 
            
            //
            // 
            // 
            //
            // 
            // 
            // 
            // 
            // 
            
            //
            // 
            // 
            // 
            //
            
            //
            //
            
            //
            
            //
            //
            
            
            //
            // 
            
            //
            // 
            
            //
            // 
            
            //
            //
            // 
            // 
            BossBGMFader.ForceStopAll("SceneSwitch/Clear");
            BossBGMLogger.Debug("BOSS    : Fader ForceStopAll");
        }

        /// <summary>
        /// 获取当前活跃 BOSS 数量（用于调试）
        /// </summary>
        public static int GetActiveBossCount()
        {
            activeBosses.RemoveAll(b => b == null || b.gameObject == null);
            return activeBosses.Count;
        }

        /// <summary>
        /// 获取当前活跃 BOSS 名称（用于调试）
        /// </summary>
        public static string GetCurrentActiveBossName()
        {
            return currentActiveBoss != null ? currentActiveBoss.GetBossName() : "None";
        }
    }
}
