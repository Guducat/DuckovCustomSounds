using System;
using UnityEngine;
using DuckovCustomSounds.CustomEnemySounds.Context;
using Duckov; // AudioManager, LevelManager
using DuckovCustomSounds.CustomBGM.Core;

namespace DuckovCustomSounds.CustomBGM.BossBGM
{
    /// <summary>
    /// BOSS BGM Controller - MonoBehaviour
    /// 附加到每个 BOSS GameObject，负责：
    /// - 持有 FMOD EventInstance
    /// - 检测玩家距离
    /// - 根据距离和优先级控制音量淡入淡出（2秒）
    /// - OnDestroy() 自动清理资源
    /// </summary>
    internal class BossBGMController : MonoBehaviour
    {
        // FMOD EventInstance
        private FMOD.Studio.EventInstance? bgmInstance;

        // BOSS 上下文
        private EnemyContext? bossContext;
        private string bossName = "Unknown";
        private string? musicPath;

        // 音量控制
        private float currentVolume = 0f;
        private float targetVolume = 0f;
        private float fadeSpeed; // 淡入淡出速度（根据 FadeDuration 计算）

        // 配置参数
        private float triggerDistance;
        private float triggerDistanceSqr; // 优化：避免开方

        // 优先级控制
        private bool isActive = false;

        // 优化：降低 Update 频率
        private float updateTimer = 0f;
        private float updateInterval;

        // 进度恢复
        private int lastTimelineMs = -1;

        // 玩家引用缓存
        private Transform? playerTransform;

        /// <summary>
        /// 初始化 Controller
        /// </summary>
        public void Initialize(EnemyContext ctx)
        {
            try
            {
                bossContext = ctx;
                bossName = ctx?.NameKey ?? "Unknown";

                // 加载配置
                triggerDistance = BossBGMConfig.TriggerDistance;
                triggerDistanceSqr = triggerDistance * triggerDistance;
                updateInterval = BossBGMConfig.UpdateInterval;

                // 计算淡入淡出速度（2秒从 0 到 1 = 0.5/秒）
                fadeSpeed = 1f / BossBGMConfig.FadeDuration;

                // 解析音乐文件路径（延迟播放）
                musicPath = BossMusicResolver.ResolveMusicPath(ctx);
                if (string.IsNullOrEmpty(musicPath))
                {
                    BossBGMLogger.Warning($"未找到 BOSS 音乐，Controller 不会播放: {bossName}");
                    Destroy(this); // 没有音乐，销毁 Controller
                    return;
                }

                // 延迟播放：不在 Initialize 即创建/切断全局BGM，等待被设为活跃且进入范围后再启动
                bgmInstance = null;
                currentVolume = 0f;
                targetVolume = 0f;

                BossBGMLogger.Info($"BOSS BGM Controller 已启动: {bossName} (距离阈值: {triggerDistance}m, 淡入淡出: {BossBGMConfig.FadeDuration}s)");

                // 注册到 BossBGMManager
                BossBGMManager.RegisterBoss(this);
            }
            catch (Exception ex)
            {
                BossBGMLogger.Error($"初始化 BossBGMController 失败: {bossName}", ex);
                Destroy(this);
            }
        }

        /// <summary>
        /// Update - 检测距离并控制音量
        /// </summary>
        void Update()
        {
            // 优化：降低更新频率
            updateTimer += Time.deltaTime;
            if (updateTimer < updateInterval)
                return;
            updateTimer = 0f;

            // 如果实例当前无效，先不销毁，允许后续在“活跃且入距”时自动重建
            // （见下方活跃分支中的重建逻辑）

            // 更新目标音量（根据优先级和距离）
            UpdateTargetVolume();

            // 平滑淡入淡出
            if (Mathf.Abs(currentVolume - targetVolume) > 0.001f)
            {
                // 淡出时使用更快的速度，淡入时保持正常速度
                float actualFadeSpeed = fadeSpeed;
                if (currentVolume > targetVolume) // 正在淡出
                {
                    actualFadeSpeed = fadeSpeed * 3f; // 淡出速度提高3倍
                }

                float fadeAmount = actualFadeSpeed * Time.deltaTime;
                currentVolume = Mathf.MoveTowards(currentVolume, targetVolume, fadeAmount);
                if (bgmInstance.HasValue && CustomBGMPlayer.IsEventInstanceActive(bgmInstance))
                {
                    var instance = bgmInstance.Value;
                    instance.setVolume(currentVolume);
                }

                // 调试输出淡入淡出状态
                if (Time.frameCount % 60 == 0) // 每秒输出一次
                {
                    string fadeDirection = currentVolume > targetVolume ? "淡出" : "淡入";
                    BossBGMLogger.Debug($"BOSS BGM {fadeDirection}: {bossName}, 当前音量: {currentVolume:F3}, 目标音量: {targetVolume:F3}, 活跃: {isActive}, 速度倍数: {(actualFadeSpeed/fadeSpeed):F1}x");
                }
            }
        }

        /// <summary>
        /// 更新目标音量
        /// </summary>
        private void UpdateTargetVolume()
        {
            // 获取玩家位置
            if (playerTransform == null)
            {
                var levelManager = LevelManager.Instance;
                if (levelManager == null || levelManager.MainCharacter == null)
                {
                    targetVolume = 0f;
                    return;
                }
                playerTransform = levelManager.MainCharacter.transform;
            }

            // 计算距离（使用 sqrMagnitude 优化）
            Vector3 delta = playerTransform.position - transform.position;
            float distanceSqr = delta.sqrMagnitude;
            float distance = Mathf.Sqrt(distanceSqr);

            // 根据距离和活跃状态设置目标音量
            if (!isActive)
            {
                // 非活跃BOSS：仅静音，保留实例（若存在），避免与其他控制器相互
                // 抢占/销毁导致的无效化；由活跃分支在需要时负责重建
                targetVolume = 0f;
                return;
            }
            else if (distanceSqr < triggerDistanceSqr)
            {
                targetVolume = BossBGMConfig.Volume; // 活跃且在范围内，淡入

                // 重新启动FMOD事件实例（如果需要）
                if (!CustomBGMPlayer.IsEventInstanceActive(bgmInstance))
                {
                    try
                    {
                        string? musicPath = BossMusicResolver.ResolveMusicPath(bossContext);
                        if (!string.IsNullOrEmpty(musicPath))
                        {
                            bgmInstance = CustomBGMPlayer.PlayMusicFile(musicPath, loop: true, stopExistingBGM: false);
                            if (bgmInstance.HasValue && CustomBGMPlayer.IsEventInstanceActive(bgmInstance))
                            {
                                var instance = bgmInstance.Value;
                                if (BossBGMConfig.ResumePlaybackEnabled && lastTimelineMs > 0)
                                {
                                    try
                                    {
                                        instance.setTimelinePosition(lastTimelineMs);
                                        BossBGMLogger.Debug($"[BossBGM] 恢复进度: {bossName} -> setTimelinePosition({lastTimelineMs})");
                                    }
                                    catch (Exception e)
                                    {
                                        BossBGMLogger.Debug($"[BossBGM] 恢复进度失败（自动回退从头）: {bossName} - {e.Message}");
                                    }
                                }

                                instance.setVolume(currentVolume);
                                BossBGMLogger.Debug($"BOSS BGM 已重新启动: {bossName}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        BossBGMLogger.Warning($"重新启动 BOSS BGM 失败: {bossName} - {ex.Message}");
                    }
                }
            }
            else
            {
                targetVolume = 0f; // 活跃但超出范围，淡出
            }

            // 调试输出（每秒一次）
            if (Time.frameCount % 60 == 0)
            {
                string instanceStatus = CustomBGMPlayer.IsEventInstanceActive(bgmInstance) ? "有效" : "无效/已停止";
                bool isOutOfRange = distanceSqr >= triggerDistanceSqr;
                BossBGMLogger.Debug($"BOSS BGM 距离检测: {bossName}, 距离: {distance:F1}m, 范围: {triggerDistance}m, 超出范围: {isOutOfRange}, 活跃: {isActive}, 目标音量: {targetVolume:F1}, 当前音量: {currentVolume:F3}, 实例: {instanceStatus}");

                // 额外调试：检查FMOD实例的实际音量
                if (CustomBGMPlayer.IsEventInstanceActive(bgmInstance))
                {
                    try
                    {
                        float actualVolume = 0f;
                        float finalVolume = 0f;
                        if (bgmInstance.HasValue)
                        {
                            var instance = bgmInstance.Value;
                            instance.getVolume(out actualVolume, out finalVolume);
                        }
                        BossBGMLogger.Debug($"BOSS BGM FMOD音量: {bossName}, 实际: {actualVolume:F3}, 最终: {finalVolume:F3}");
                    }
                    catch (Exception ex)
                    {
                        BossBGMLogger.Warning($"获取 FMOD 音量失败: {bossName} - {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 设置优先级（由 BossBGMManager 调用）
        /// </summary>
        public void SetPriority(bool active)
        {
                if (isActive != active)
                {
                    isActive = active;
                    BossBGMLogger.Debug($"BOSS BGM 优先级变更: {bossName} -> {(active ? "激活" : "静音")}");
                }
        }

        /// <summary>
        /// 获取到玩家的距离（供 BossBGMManager 调用）
        /// </summary>
        public float GetDistanceToPlayer()
        {
            if (playerTransform == null)
            {
                var levelManager = LevelManager.Instance;
                if (levelManager == null || levelManager.MainCharacter == null)
                    return float.MaxValue;
                playerTransform = levelManager.MainCharacter.transform;
            }

            return Vector3.Distance(playerTransform.position, transform.position);
        }

        /// <summary>
        /// 获取 BOSS 名称（用于调试）
        /// </summary>
        public string GetBossName()
        {
            return bossName;
        }

        /// <summary>
        /// 检查 Controller 是否有效
        /// </summary>
        public bool IsValid()
        {
            return CustomBGMPlayer.IsEventInstanceActive(bgmInstance);
        }

        /// <summary>
        /// OnDestroy - 自动清理资源
        /// </summary>
        void OnDestroy()
        {
            try
            {
                // BOSS 死亡：启动“死亡淡出”，由宿主在指定时间后停止并释放
                if (bgmInstance.HasValue && bgmInstance.Value.isValid())
                {
                    float deathFade = Mathf.Max(0f, BossBGMConfig.BossDeathFadeOutSeconds);
                    try
                    {
                        BossBGMFader.FadeOutAndRelease(bgmInstance.Value, bossName, deathFade);
                    }
                    catch (System.Exception ex)
                    {
                        // 兜底：如果宿主不可用，直接淡出停止
                        try
                        {
                            bgmInstance.Value.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                            bgmInstance.Value.release();
                        }
                        catch { }
                        BossBGMLogger.Warning($"[BossBGM] 死亡淡出宿主不可用，已直接停止: {bossName} - {ex.Message}");
                    }
                    finally
                    {
                        bgmInstance = null;
                    }
                }

                // 从 BossBGMManager 注销
                BossBGMManager.UnregisterBoss(this);
            }
            catch (Exception ex)
            {
                BossBGMLogger.Error($"清理 BossBGMController 失败: {bossName}", ex);
            }
        }
    }
}
