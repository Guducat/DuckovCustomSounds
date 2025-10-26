using HarmonyLib;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using FMOD;
using FMODUnity;
using Duckov; // CharacterMainControl
using Duckov.ItemUsage; // FoodDrink
using ItemStatsSystem; // Item
using DuckovCustomSounds.CustomGunSounds; // 复用 GunLogger 与 ModBehaviour

namespace DuckovCustomSounds.CustomFoodSounds
{
    // 食物使用：在 FoodDrink.OnUse 时播放自定义 3D 音效（参考枪械/近战实现模式）
    [HarmonyPatch(typeof(FoodDrink))]
    public static class FoodDrink_OnUse_PlayCustomSfx
    {
        [HarmonyPatch("OnUse", new Type[] { typeof(Item), typeof(object) })]
        [HarmonyPostfix]
        public static void Postfix(FoodDrink __instance, Item item, object user)
        {
            try
            {
                // 仅当由角色实际使用时播放
                CharacterMainControl character = null;
                try { character = user as CharacterMainControl; } catch { }
                if (character == null) return;
                if (item == null) return;

                string dir = Path.Combine(ModBehaviour.ModFolderName, "CustomFoodSounds");

                // 仅按 item.TypeID 匹配，失败则放弃（不再使用 default.mp3 回退）
                string typeIdStr = string.Empty;
                try { typeIdStr = item.TypeID.ToString(); } catch { }

                if (string.IsNullOrWhiteSpace(typeIdStr))
                {
                    GunLogger.Debug("[FoodUse] item.TypeID 为空，跳过播放");
                    return;
                }

                string filePath = Path.Combine(dir, typeIdStr + ".mp3");
                if (!File.Exists(filePath))
                {
                    GunLogger.Debug($"[FoodUse] 未找到文件: {filePath}，跳过播放");
                    return;
                }

                // FMOD 就绪性
                try { if (!RuntimeManager.IsInitialized) { GunLogger.Info("[FoodUse] FMOD 未初始化，跳过自定义音效"); return; } } catch { }

                // 创建 3D 声音并路由至 SFX
                var mode = MODE.CREATESAMPLE | MODE._3D | MODE.LOOP_OFF;
                var r1 = RuntimeManager.CoreSystem.createSound(filePath, mode, out Sound sound);
                if (r1 != RESULT.OK || !sound.hasHandle())
                {
                    GunLogger.Info($"[FoodUse] createSound 失败({r1})，跳过自定义音效");
                    return;
                }
                try { sound.set3DMinMaxDistance(1f, 25f); } catch { }

                ChannelGroup group = default;
                try { if (ModBehaviour.SfxGroup.hasHandle()) group = ModBehaviour.SfxGroup; } catch { }
                if (!group.hasHandle())
                {
                    try { var sfxBus = RuntimeManager.GetBus("bus:/Master/SFX"); if (sfxBus.getChannelGroup(out var cg) == RESULT.OK && cg.hasHandle()) group = cg; } catch { }
                    if (!group.hasHandle()) { try { var sfxBusAlt = RuntimeManager.GetBus("bus:/SFX"); if (sfxBusAlt.getChannelGroup(out var cg2) == RESULT.OK && cg2.hasHandle()) group = cg2; } catch { } }
                }
                if (!group.hasHandle())
                {
                    GunLogger.Info("[FoodUse] 未找到 SFX 通道组，跳过自定义音效");
                    try { if (sound.hasHandle()) sound.release(); } catch { }
                    return;
                }

                var r2 = RuntimeManager.CoreSystem.playSound(sound, group, true, out Channel channel);
                if (r2 != RESULT.OK || !channel.hasHandle())
                {
                    GunLogger.Info($"[FoodUse] playSound 失败({r2})，跳过自定义音效");
                    try { if (sound.hasHandle()) sound.release(); } catch { }
                    return;
                }

                Vector3 pos = Vector3.zero;
                try { pos = character?.transform?.position ?? Vector3.zero; } catch { }
                var fpos = new FMOD.VECTOR { x = pos.x, y = pos.y, z = pos.z };
                var fvel = new FMOD.VECTOR { x = 0, y = 0, z = 0 };
                try { channel.set3DAttributes(ref fpos, ref fvel); } catch { }
                try { channel.setPaused(false); } catch { }

                GunLogger.Debug($"[FoodUse] 播放 {Path.GetFileName(filePath)} @ ({pos.x:F1},{pos.y:F1},{pos.z:F1})");

                // 延后释放
                try { ModBehaviour.Instance?.StartCoroutine(FollowAndCleanup(character.transform, sound, channel, 6f)); } catch { }
            }
            catch (Exception ex)
            {
                GunLogger.Warn($"[FoodUse] Postfix 异常: {ex.Message}");
            }
        }

        private static System.Collections.IEnumerator FollowAndCleanup(Transform follow, Sound sound, Channel channel, float maxSec)
        {
            float end = Time.realtimeSinceStartup + Mathf.Max(1f, maxSec);
            try
            {
                while (Time.realtimeSinceStartup < end)
                {
                    bool playing = false;
                    try { if (channel.hasHandle()) channel.isPlaying(out playing); } catch { }
                    if (!playing) break;

                    // 跟踪玩家位置
                    Vector3 pos = Vector3.zero;
                    try { if (follow != null) pos = follow.position; } catch { }
                    var fpos = new FMOD.VECTOR { x = pos.x, y = pos.y, z = pos.z };
                    var fvel = new FMOD.VECTOR { x = 0, y = 0, z = 0 };
                    try { channel.set3DAttributes(ref fpos, ref fvel); } catch { }

                    yield return null;
                }
            }
            finally
            {
                try { if (channel.hasHandle()) channel.stop(); } catch { }
                try { if (sound.hasHandle()) sound.release(); } catch { }
            }
        }
    }
}

