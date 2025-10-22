using HarmonyLib;
using UnityEngine;
using System.IO;
using FMOD;
using Duckov;
using Debug = UnityEngine.Debug;

namespace DuckovCustomSounds.CUstomKillSounds
{
    // 补丁目标是 AudioManager
    [HarmonyPatch(typeof(AudioManager))]
    public static class CustomKillSounds
    {
        // 1. 资源：只存放击杀音效
        private static Sound HeadshotKillSound;
        private static Sound NormalKillSound;

        // 2. 加载逻辑：由 ModBehaviour.cs 在 OnEnable 时调用
        public static void Load()
        {
            string headshotPath = Path.Combine(ModBehaviour.ModFolderName, "KillSound", "headshot.mp3");
            if (File.Exists(headshotPath))
            {
                var result = FMODUnity.RuntimeManager.CoreSystem.createSound(headshotPath, MODE.LOOP_OFF, out HeadshotKillSound);
                if (result != RESULT.OK) ModBehaviour.ErrorMessage += $"加载 {headshotPath} 失败: {result}\n";
                else Debug.Log($"[CustomSounds] 成功加载 {headshotPath}");
            }
            else
            {
                Debug.Log($"[CustomSounds] 未找到爆头音效文件: {headshotPath}");
            }

            string normalKillPath = Path.Combine(ModBehaviour.ModFolderName, "KillSound", "normalkill.mp3");
            if (File.Exists(normalKillPath))
            {
                var result = FMODUnity.RuntimeManager.CoreSystem.createSound(normalKillPath, MODE.LOOP_OFF, out NormalKillSound);
                if (result != RESULT.OK) ModBehaviour.ErrorMessage += $"加载 {normalKillPath} 失败: {result}\n";
                else Debug.Log($"[CustomSounds] 成功加载 {normalKillPath}");
            }
            else
            {
                Debug.Log($"[CustomSounds] 未找到普通击杀音效文件: {normalKillPath}");
            }
        }

        // 3. 卸载逻辑：由 ModBehaviour.cs 在 OnDisable 时调用
        public static void Unload()
        {
            if (HeadshotKillSound.hasHandle()) HeadshotKillSound.release();
            if (NormalKillSound.hasHandle()) NormalKillSound.release();
        }

        // 4. 补丁
        [HarmonyPatch("PostKillMarker")] // 目标方法
        [HarmonyPrefix]                  // 前置补丁
        public static bool PostKillMarkerPrefix(bool crit) // 拿到 crit 参数
        {
            if (crit && HeadshotKillSound.hasHandle())
            {
                // 播放爆头音效
                FMODUnity.RuntimeManager.CoreSystem.playSound(HeadshotKillSound, ModBehaviour.SfxGroup, false, out var channel);
            }
            else if (!crit && NormalKillSound.hasHandle())
            {
                // 播放普通击杀音效
                FMODUnity.RuntimeManager.CoreSystem.playSound(NormalKillSound, ModBehaviour.SfxGroup, false, out var channel);
            }
            
            // 返回 false 会阻止游戏本体的 PostKillMarker 方法执行，
            // 如果想叠加音效就返回 true
            return false; 
        }
    }
}