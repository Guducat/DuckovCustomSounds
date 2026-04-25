using System;
using HarmonyLib;
using UnityEngine;
using Duckov; // AICharacterController, CharacterMainControl, AudioManager
using Duckov.Scenes; // MultiSceneCore
using DuckovCustomSounds.CustomEnemySounds.Context;

namespace DuckovCustomSounds.CustomBGM.BossBGM
{
    /// <summary>
    /// BOSS BGM Harmony Patches
    /// - Hook AICharacterController.Init 检测 BOSS 生成
    /// - 添加 BossBGMController 组件
    /// - Hook 场景卸载清理资源
    /// </summary>
    internal static class BossBGM_Patches
    {
        /// <summary>
        /// Patch: AICharacterController.Init
        /// 在 AI 初始化时检测 BOSS 并添加 BossBGMController
        /// </summary>
#if false // Disabled duplicate AICharacterController.Init patch (moved to CustomEnemySounds)

        [HarmonyPatch(typeof(AICharacterController))]
        [HarmonyPatch("Init", new Type[]
        {
            typeof(CharacterMainControl),
            typeof(Vector3),
            typeof(AudioManager.VoiceType),
            typeof(AudioManager.FootStepMaterialType)
        })]
        [HarmonyPostfix]
        private static void AICharacterController_Init_Postfix(
            CharacterMainControl _characterMainControl,
            Vector3 patrolCenter,
            AudioManager.VoiceType voiceType,
            AudioManager.FootStepMaterialType footStepMatType)
        {
            try
            {
                // 检查配置是否启用
                if (!BossBGMConfig.Enabled)
                    return;

                // 检查是否有音乐文件（性能优化：没有音乐文件时直接跳过）
                if (!BossMusicResolver.HasAnyMusic)
                    return;

                // 检查 CharacterMainControl 有效性
                if (_characterMainControl == null || _characterMainControl.gameObject == null)
                    return;

                // 获取或注册 EnemyContext
                EnemyContext ctx = null;
                if (!EnemyContextRegistry.TryGet(_characterMainControl.gameObject, out ctx) || ctx == null)
                {
                    // 如果没有注册，手动注册一次
                    ctx = EnemyContextRegistry.Register(_characterMainControl, voiceType, footStepMatType);
                }

                if (ctx == null)
                {
                    BossBGMLogger.Debug("[Patch] EnemyContext 为 null，跳过");
                    return;
                }

                // 检查是否是 BOSS
                string rank = ctx.GetRank();
                if (rank != "boss")
                {
                    // 不是 BOSS，跳过
                    return;
                }

                BossBGMLogger.Debug($"[Patch] 检测到 BOSS 生成: {ctx.NameKey}, rank={rank}, iconType={ctx.IconType}");

                // 检查是否已经有 BossBGMController 组件（避免重复添加）
                var existing = _characterMainControl.gameObject.GetComponent<BossBGMController>();
                if (existing != null)
                {
                    BossBGMLogger.Warning($"[Patch] BOSS 已有 BossBGMController，跳过: {ctx.NameKey}");
                    return;
                }

                // 添加 BossBGMController 组件
                var controller = _characterMainControl.gameObject.AddComponent<BossBGMController>();
                controller.Initialize(ctx);

                BossBGMLogger.Info($"[Patch] BOSS BGM Controller 已添加: {ctx.NameKey}");
            }
            catch (Exception ex)
            {
                BossBGMLogger.Error("[Patch] AICharacterController.Init Postfix 失败", ex);
            }
        }

#endif // end disabled AICharacterController.Init patch

        /// <summary>
        /// Patch: MultiSceneCore.LocalOnSubSceneWillBeUnloaded
        /// 场景卸载时清理所有 BOSS BGM
        /// </summary>
        [HarmonyPatch(typeof(MultiSceneCore), "LocalOnSubSceneWillBeUnloaded")]
        [HarmonyPostfix]
        private static void MultiSceneCore_OnSubSceneWillBeUnloaded_Postfix(UnityEngine.SceneManagement.Scene scene)
        {
            try
            {
                BossBGMLogger.Debug($"[Patch] 场景即将卸载，清理 BOSS BGM: {scene.name}");
                BossBGMManager.Clear();
                try { DuckovCustomSounds.ModBehaviour.Instance?.ResetBossBatchProcessing(); } catch {}

            }
            catch (Exception ex)
            {
                BossBGMLogger.Error("[Patch] 场景卸载清理失败", ex);
            }
        }

        /// <summary>
        /// 可选 Patch: LevelManager.OnDestroy
        /// 关卡销毁时清理（额外保险）
        /// </summary>
        [HarmonyPatch(typeof(LevelManager), "OnDestroy")]
        [HarmonyPostfix]
        private static void LevelManager_OnDestroy_Postfix()
        {
            try
            {
                BossBGMLogger.Debug("[Patch] LevelManager 销毁，清理 BOSS BGM");
                BossBGMManager.Clear();
            }
            catch (Exception ex)
            {
                BossBGMLogger.Error("[Patch] LevelManager 销毁清理失败", ex);
            }
        }
    }
}
