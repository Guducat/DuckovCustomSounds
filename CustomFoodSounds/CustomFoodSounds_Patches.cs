using HarmonyLib;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

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
        private static readonly string[] Exts = new[] { ".mp3", ".wav", ".ogg", ".oga" };
        private static IEnumerable<string> ExpandCandidates(string dir, params string[] namesNoExt)
        {
            foreach (var name in namesNoExt)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                foreach (var ext in Exts)
                {
                    yield return Path.Combine(dir, name + ext);
                }
            }
        }

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

                // 仅按 item.TypeID 匹配，失败则放弃
                string typeIdStr = string.Empty;
                try { typeIdStr = item.TypeID.ToString(); } catch { }

                if (string.IsNullOrWhiteSpace(typeIdStr))
                {
                    GunLogger.Debug("[FoodUse] item.TypeID 为空，跳过播放");
                    return;
                }

                var attempts = new List<string>(ExpandCandidates(dir, typeIdStr));
                var chain = string.Join(" → ", attempts.Select(p => $"[{p}]"));
                string filePath = attempts.FirstOrDefault(File.Exists);
                if (filePath == null)
                {
                    GunLogger.Debug($"[FoodUse] TypeID={typeIdStr}, 查找顺序: {chain}, 最终使用: 未找到自定义文件");
                    return;
                }
                else
                {
                    GunLogger.Debug($"[FoodUse] TypeID={typeIdStr}, 查找顺序: {chain}, 最终使用: {filePath}");
                }

                // FMOD 就绪性
                try { if (!RuntimeManager.IsInitialized) { GunLogger.Info("[FoodUse] FMOD 未初始化，跳过自定义音效"); return; } } catch { }

                // 创建 3D 声音并路由至 SFX（按扩展名选择 STREAM/SAMPLE）
                string fullPath = filePath;
                try { fullPath = System.IO.Path.GetFullPath(filePath); } catch { }
                string ext = null; try { ext = Path.GetExtension(fullPath)?.ToLowerInvariant(); } catch { }
                var mode = ((ext == ".mp3" || ext == ".ogg" || ext == ".oga") ? MODE.CREATESTREAM : MODE.CREATESAMPLE) | MODE._3D | MODE.LOOP_OFF;
                var r1 = RuntimeManager.CoreSystem.createSound(fullPath, mode, out Sound sound);
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
                try { channel.set3DMinMaxDistance(1f, 25f); } catch { }
                try { channel.setMode(MODE._3D | MODE._3D_LINEARROLLOFF | MODE.LOOP_OFF); } catch { }
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

