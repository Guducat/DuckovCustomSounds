using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using Duckov; // AudioManager, AICharacterController, CharacterMainControl, AISound, SoundTypes

namespace DuckovCustomSounds.CustomEnemySounds
{
    /// <summary>
    /// 敌人发现手雷的语音提示（遵循自定义敌人语音系统的架构）。
    /// - 通过 Harmony Postfix 挂到 AICharacterController.OnSound，不改变原生命周期
    /// - 在听到 grenadeDropSound 且在可听范围内时，调用 AudioManager.PostQuak("grenade", ...)
    /// - 播放由 CustomEnemySounds 的 PostQuak Postfix 统一路由/替换为自定义 3D 声音
    /// - 简单去抖：同一 AI 对同一颗手雷只播一次，并有最小冷却
    /// </summary>
    [HarmonyPatch]
    internal static class EnemyGrenadeSounds
    {
        private sealed class LastPlay
        {
            public int LastGrenadeObjId;
            public float LastTime;
        }

        // aiInstanceID -> last play
        private static readonly Dictionary<int, LastPlay> _last = new Dictionary<int, LastPlay>();

	        // Global rate limit for NPC grenade surprised voice
	        private static float _lastGrenadeSurprisedGlobalTime = -999f;


        // 同一 AI 同一手雷的最小触发间隔
        private const float MinCooldown = 6.0f;

        [HarmonyPatch(typeof(AICharacterController))]
        [HarmonyPatch("OnSound", new Type[] { typeof(AISound) })]
        [HarmonyPostfix]
        private static void AI_OnSound_Postfix(AICharacterController __instance, AISound sound)
        {
            try
            {
                if (__instance == null) return;
                if (sound.soundType != SoundTypes.grenadeDropSound) return;
                if (!__instance.canTalk) return;
                if (!((UnityEngine.Object)sound.fromObject)) return;

                // 距离判定（与原逻辑一致，使用半径 * 听力）
                var pos = sound.pos; pos.y = 0f;
                var my = __instance.transform.position; my.y = 0f;
                var hearRange = sound.radius * __instance.hearingAbility;
                if (Vector3.Distance(my, pos) >= hearRange) return;

                // 去抖：同一 AI、同一手雷，短时间内不重复播报
                int aiId = __instance.GetInstanceID();
                int grenadeId = sound.fromObject.GetInstanceID();
                if (_last.TryGetValue(aiId, out var lp))
                {
                    if (lp.LastGrenadeObjId == grenadeId && (Time.realtimeSinceStartup - lp.LastTime) < MinCooldown)
                        return;
                }

                var cmc = __instance.CharacterMainControl;
                if (cmc == null) return;
                var go = cmc.gameObject;

                // 确保自定义语音系统加载（路由/优先级/跟踪）
                CustomEnemySounds.EnsureLoaded();
                try
                {
                    // 确保上下文存在（通常在 Init 已注册，容错处理）
                    if (!EnemyContextRegistry.TryGet(go, out var ctx) || ctx == null)
                        EnemyContextRegistry.Register(cmc, cmc.AudioVoiceType, cmc.FootStepMaterialType);
                }
                catch { }

                // 设置开关/频率限制（全局）
                if (!DuckovCustomSounds.ModSettings.NPCGrenadeSurprisedEnabled) return;
                float __now = Time.realtimeSinceStartup;
                float __min = DuckovCustomSounds.ModSettings.NPCGrenadeSurprisedMinInterval;
                if (__min > 0f && (__now - _lastGrenadeSurprisedGlobalTime) < __min) return;

                // 通过标准入口触发语音：soundKey = "grenade"
                // 后续由 AudioObject.PostQuak 的 Postfix 进行自定义替换与 3D 路由
                // 使用 AudioObject 以避免跨程序集的 internal 访问限制
                var ao = go.GetComponent<AudioObject>();
                if (ao == null) ao = go.AddComponent<AudioObject>();
                ao.VoiceType = cmc.AudioVoiceType;
                ao.PostQuak("grenade");

                _lastGrenadeSurprisedGlobalTime = Time.realtimeSinceStartup;
                _last[aiId] = new LastPlay { LastGrenadeObjId = grenadeId, LastTime = Time.realtimeSinceStartup };
                CESLogger.Debug($"[GrenadeVoice] AI#{aiId} 播放 grenade 提示 (obj={grenadeId})");
            }
            catch (Exception ex)
            {
                CESLogger.Error("[GrenadeVoice] AI_OnSound_Postfix 异常", ex);
            }
        }
    }
}

