using System.Collections.Concurrent;
using UnityEngine;
using Duckov;
using DuckovCustomSounds.CustomEnemySounds.Rules;

namespace DuckovCustomSounds.CustomEnemySounds.Context
{
    /// <summary>
    /// 线程安全的运行时注册表，将GameObject实例ID映射到EnemyContext。
    /// </summary>
    internal static class EnemyContextRegistry
    {
        private static readonly ConcurrentDictionary<int, EnemyContext> Map = new ConcurrentDictionary<int, EnemyContext>();

        public static EnemyContext? Register(CharacterMainControl? cmc, AudioManager.VoiceType voiceType, AudioManager.FootStepMaterialType foot)
        {
            if (cmc == null) return null;
            var ctx = EnemyContext.FromCharacter(cmc, voiceType, foot);
            if (ctx.GameObject == null)
            {
                CESLogger.Debug("Register called with null GameObject");
                return ctx;
            }
            Map[ctx.InstanceId] = ctx;
            CESLogger.Debug($"登记敌人上下文: {ctx}");
            return ctx;
        }

        public static bool TryGet(GameObject? go, out EnemyContext? ctx)
        {
            ctx = null;
            if (go == null) return false;
            return Map.TryGetValue(go.GetInstanceID(), out ctx);
        }

        public static void UpdateVoiceType(GameObject go, AudioManager.VoiceType voiceType)
        {
            if (go == null) return;
            if (Map.TryGetValue(go.GetInstanceID(), out var ctx))
            {
                ctx.VoiceType = voiceType;
                CESLogger.Debug($"更新敌人语音类型: go={ctx.InstanceId} -> {voiceType}");
            }
        }

        public static void Remove(GameObject go)
        {
            if (go == null) return;
            int id = go.GetInstanceID();
            Map.TryRemove(id, out _);
            // 同步清理变体索引绑定
            try { VariantIndexBinder.Remove(id); } catch { }
        }

        public static void Clear()
        {
            Map.Clear();
            try { VariantIndexBinder.Clear(); } catch { }
        }

		public static void ProcessBossesBatch()
		{
			try
			{
				if (!DuckovCustomSounds.CustomBGM.BossBGM.BossBGMConfig.Enabled ||
				    !DuckovCustomSounds.CustomBGM.BossBGM.BossMusicResolver.HasAnyMusic)
				{
					CESLogger.Debug("BatchBoss: 配置未启用或无音乐，跳过批量检测");
					return;
				}

				int total = 0, bossFound = 0, added = 0, existed = 0;
				
				// 首先处理已在注册表中的敌人
				foreach (var kvp in Map)
				{
					total++;
					var ctx = kvp.Value;
					if (ctx == null) continue;
					if (ctx.GetRank() != "boss") continue;
					bossFound++;

					var go = ctx.GameObject;
					if (go == null) continue;

					var existing = go.GetComponent<DuckovCustomSounds.CustomBGM.BossBGM.BossBGMController>();
					if (existing == null)
					{
						try
						{
							var controller = go.AddComponent<DuckovCustomSounds.CustomBGM.BossBGM.BossBGMController>();
							controller.Initialize(ctx);
							added++;
							DuckovCustomSounds.CustomBGM.BossBGM.BossBGMLogger.Debug($"[Batch] 已为 BOSS 添加 Controller: {ctx.NameKey}");
						}
						catch (System.Exception ex)
						{
							DuckovCustomSounds.CustomBGM.BossBGM.BossBGMLogger.Warning($"[Batch] 添加 Controller 失败: {ctx.NameKey} - {ex.Message}");
						}
					}
					else
					{
						existed++;
					}
				}
				
				// 主动查找场景中所有可能的BOSS（包括尚未注册的）
				// 使用与 BossLiveMapMod 相同的方法：通过 CharacterSpawnerRoot 获取敌人
				try
				{
					var spawnerRoots = Resources.FindObjectsOfTypeAll<CharacterSpawnerRoot>();
					CESLogger.Debug($"[Batch] 找到 {spawnerRoots.Length} 个 CharacterSpawnerRoot");
					
					foreach (var spawnerRoot in spawnerRoots)
					{
						if (spawnerRoot == null) continue;
						
						// 通过反射获取 createdCharacters 字段
						try
						{
							var createdCharactersField = typeof(CharacterSpawnerRoot).GetField("createdCharacters",
								System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
							if (createdCharactersField == null)
							{
								CESLogger.Warning("[Batch] 无法找到 createdCharacters 字段");
								continue;
							}
							
							var createdCharacters = createdCharactersField.GetValue(spawnerRoot) as System.Collections.Generic.List<CharacterMainControl>;
							if (createdCharacters == null) continue;
							
							CESLogger.Debug($"[Batch] CharacterSpawnerRoot 有 {createdCharacters.Count} 个角色");
							
							foreach (var character in createdCharacters)
							{
								if (character == null || character.gameObject == null) continue;
								
								// 跳过已经在注册表中的
								if (Map.ContainsKey(character.gameObject.GetInstanceID())) continue;
								
								total++;
								
								// 尝试创建临时上下文来检测是否为BOSS
								try
								{
									var tempCtx = EnemyContext.FromCharacter(character, character.AudioVoiceType, character.FootStepMaterialType);
									if (tempCtx != null && tempCtx.GetRank() == "boss")
									{
										bossFound++;
										
										// 注册到注册表
										var registeredCtx = EnemyContextRegistry.Register(character, character.AudioVoiceType, character.FootStepMaterialType);
										if (registeredCtx == null)
										{
											CESLogger.Debug("[Batch] Register 返回空上下文，跳过 Controller 注入");
											continue;
										}
										
										var go = character.gameObject;
										var existing = go.GetComponent<DuckovCustomSounds.CustomBGM.BossBGM.BossBGMController>();
										if (existing == null)
										{
											try
											{
												var controller = go.AddComponent<DuckovCustomSounds.CustomBGM.BossBGM.BossBGMController>();
												controller.Initialize(registeredCtx);
												added++;
												DuckovCustomSounds.CustomBGM.BossBGM.BossBGMLogger.Info($"[Batch] 发现并注册新 BOSS: {registeredCtx.NameKey}");
												}
												catch (System.Exception ex)
												{
													DuckovCustomSounds.CustomBGM.BossBGM.BossBGMLogger.Warning($"[Batch] 为新 BOSS 添加 Controller 失败: {registeredCtx.NameKey} - {ex.Message}");
												}
											}
										else
										{
											existed++;
										}
									}
								}
								catch (System.Exception ex)
								{
									CESLogger.Debug($"[Batch] 处理角色时出错: {character.name} - {ex.Message}");
								}
							}
						}
						catch (System.Exception ex)
						{
							CESLogger.Warning($"[Batch] 处理 CharacterSpawnerRoot 时出错: {ex.Message}");
						}
					}
				}
				catch (System.Exception ex)
				{
					CESLogger.Warning($"[Batch] 主动查找场景中的 BOSS 时出错: {ex.Message}");
				}

				DuckovCustomSounds.CustomBGM.BossBGM.BossBGMLogger.Info($"[Batch] BOSS 批量检测完成: 总={total}, boss={bossFound}, 新增={added}, 已有={existed}");
			}
			catch (System.Exception ex)
			{
				DuckovCustomSounds.CustomBGM.BossBGM.BossBGMLogger.Error("[Batch] 处理 BOSS 失败", ex);
			}
		}

    }
}
