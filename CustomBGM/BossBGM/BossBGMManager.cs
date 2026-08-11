using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DuckovCustomSounds.CustomBGM.BossBGM
{
    /// <summary>
    /// 管理所有 Boss BGM Controller，并按玩家距离选择当前活动 Boss。
    /// </summary>
    internal static class BossBGMManager
    {
        private static readonly List<BossBGMController> ActiveBosses = new List<BossBGMController>();

        private static BossBGMController? currentActiveBoss;
        private static bool sceneBgmSuppressed;
        private static float lastSwitchTime = -999f;

        public static void RegisterBoss(BossBGMController boss)
        {
            if (boss == null)
                return;

            if (!ActiveBosses.Contains(boss))
            {
                ActiveBosses.Add(boss);
                BossBGMLogger.Debug($"BOSS 已注册: {boss.GetBossName()}, 当前活跃 BOSS 数量: {ActiveBosses.Count}");
            }

            UpdateActiveBGM();
        }

        public static void UnregisterBoss(BossBGMController boss)
        {
            if (boss == null)
                return;

            if (!ActiveBosses.Remove(boss))
                return;

            BossBGMLogger.Debug($"BOSS 已注销: {boss.GetBossName()}, 当前活跃 BOSS 数量: {ActiveBosses.Count}");

            if (currentActiveBoss == boss)
            {
                boss.SetPriority(false);
                currentActiveBoss = null;
            }

            UpdateActiveBGM();
        }

        /// <summary>
        /// 选择触发范围内最近的 Boss。由 ModBehaviour 定期调用。
        /// </summary>
        public static void UpdateActiveBGM()
        {
            ActiveBosses.RemoveAll(boss => boss == null || boss.gameObject == null);

            if (!BossBGMConfig.Enabled)
            {
                DeactivateCurrentBoss();
                NotifySceneBGM(false);
                return;
            }

            if (ActiveBosses.Count == 0)
            {
                if (currentActiveBoss != null)
                    BossBGMLogger.Debug("所有 BOSS 已清除，无活跃 BGM");

                currentActiveBoss = null;
                NotifySceneBGM(false);
                return;
            }

            float triggerDistance = Mathf.Max(0f, BossBGMConfig.TriggerDistance);
            float triggerDistanceSquared = triggerDistance * triggerDistance;
            BossBGMController? closest = null;
            float closestDistanceSquared = float.MaxValue;
            float currentDistanceSquared = float.MaxValue;
            bool currentBossInRange = false;

            foreach (BossBGMController boss in ActiveBosses)
            {
                if (!boss.TryGetDistanceSquaredToPlayer(out float distanceSquared))
                    continue;

                bool inRange = distanceSquared < triggerDistanceSquared;
                if (boss == currentActiveBoss)
                {
                    currentDistanceSquared = distanceSquared;
                    currentBossInRange = inRange;
                }

                if (inRange && distanceSquared < closestDistanceSquared)
                {
                    closest = boss;
                    closestDistanceSquared = distanceSquared;
                }
            }

            if (closest == currentActiveBoss)
                return;

            BossBGMController? previousBoss = currentActiveBoss;
            bool hadActiveBoss = previousBoss != null;
            bool willHaveActiveBoss = closest != null;

            if (previousBoss != null && closest != null && currentBossInRange)
            {
                float elapsed = Time.time - lastSwitchTime;
                float minInterval = BossBGMConfig.MinSwitchIntervalSeconds;
                if (elapsed < minInterval)
                {
                    BossBGMLogger.Debug(
                        $"[BossBGM] 切换冷却中：已过 {elapsed:F2}s / 冷却 {minInterval:F2}s，保持当前 {previousBoss.GetBossName()}");
                    return;
                }

                float currentDistance = Mathf.Sqrt(currentDistanceSquared);
                float closestDistance = Mathf.Sqrt(closestDistanceSquared);
                float distanceAdvantage = currentDistance - closestDistance;
                float requiredAdvantage = BossBGMConfig.MinDistanceDeltaToSwitch;
                if (distanceAdvantage < requiredAdvantage)
                {
                    BossBGMLogger.Debug(
                        $"[BossBGM] 距离优势不足：当前 {previousBoss.GetBossName()}={currentDistance:F1}m, " +
                        $"新最近 {closest.GetBossName()}={closestDistance:F1}m, 阈值={requiredAdvantage:F1}m");
                    return;
                }
            }

            if (previousBoss != null)
            {
                previousBoss.SetPriority(false);
                BossBGMLogger.Debug($"BOSS BGM 静音: {previousBoss.GetBossName()}");
            }

            if (closest != null)
            {
                closest.SetPriority(true);
                float closestDistance = Mathf.Sqrt(closestDistanceSquared);
                BossBGMLogger.Info($"切换活跃 BOSS BGM: {closest.GetBossName()} (距离 {closestDistance:F1}m)");
            }

            currentActiveBoss = closest;
            lastSwitchTime = Time.time;

            if (!hadActiveBoss && willHaveActiveBoss)
                NotifySceneBGM(true);
            else if (hadActiveBoss && !willHaveActiveBoss)
                NotifySceneBGM(false);
        }

        private static void DeactivateCurrentBoss()
        {
            if (currentActiveBoss == null)
                return;

            currentActiveBoss.SetPriority(false);
            currentActiveBoss = null;
        }

        private static void NotifySceneBGM(bool isActive)
        {
            if (sceneBgmSuppressed == isActive)
                return;

            try
            {
                SceneBGM.CustomSceneBGM.SetBossBGMActive(isActive);
                sceneBgmSuppressed = isActive;
                BossBGMLogger.Debug($"已通知场景 BGM 系统: BOSS BGM Active = {isActive}");
            }
            catch (System.Exception ex)
            {
                BossBGMLogger.Debug($"通知场景 BGM 系统失败: {ex.Message}");
            }
        }

        public static void Clear()
        {
            BossBGMLogger.Debug($"清理所有 BOSS BGM，共 {ActiveBosses.Count} 个");

            foreach (BossBGMController boss in ActiveBosses.ToList())
            {
                if (boss != null && boss.gameObject != null)
                    Object.Destroy(boss);
            }

            ActiveBosses.Clear();
            currentActiveBoss = null;
            NotifySceneBGM(false);
            BossBGMFader.ForceStopAll("SceneSwitch/Clear");
            BossBGMLogger.Debug("Boss BGM 已在场景清理时全部停止");
        }

        public static int GetActiveBossCount()
        {
            ActiveBosses.RemoveAll(boss => boss == null || boss.gameObject == null);
            return ActiveBosses.Count;
        }

        public static string GetCurrentActiveBossName()
        {
            return currentActiveBoss != null ? currentActiveBoss.GetBossName() : "None";
        }
    }
}
