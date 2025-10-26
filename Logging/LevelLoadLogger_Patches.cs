using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DuckovCustomSounds.Logging
{
    // Harmony Patch: 在 SceneLoader.LoadScene 的 "onAfterSceneInitialize" 调用点插入日志
    // 目标方法通过反射定位，避免对 Eflatun.SceneReference 的编译期依赖
    [HarmonyPatch]
    internal static class LevelLoadLogger_Patches
    {
        // 选择所有名为 LoadScene 的方法，由 Transpiler 自行识别包含 "Getting ready..." 的那个真正的加载方法
        static IEnumerable<MethodBase> TargetMethods()
        {
            var t = AccessTools.TypeByName("SceneLoader"); // 全局命名空间类型，位于 TeamSoda.Duckov.Core
            if (t == null)
                yield break;

            foreach (var m in AccessTools.GetDeclaredMethods(t))
            {
                if (m.Name == "LoadScene")
                    yield return m;
            }
        }

        // 在 IL 中找到 ldstr "Getting ready..." 的位置，在其之前插入一次调用 Hook 方法
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var list = new List<CodeInstruction>(instructions);
            var hook = AccessTools.Method(typeof(LevelLoadLogger_Patches), nameof(AfterSceneInitializeHook));
            if (hook == null)
                return list;

            var inserted = false;
            for (int i = 0; i < list.Count; i++)
            {
                var ci = list[i];
                if (ci.opcode == OpCodes.Ldstr && ci.operand is string s && s == "Getting ready...")
                {
                    // 在出现 "Getting ready..." 之前插入一次调用，这个位置紧随 onAfterSceneInitialize(context) 之后
                    list.Insert(i, new CodeInstruction(OpCodes.Call, hook));
                    inserted = true;
                    break;
                }
            }

            if (!inserted)
            {
                // 兜底：在方法末尾插入（比理想时机稍晚，但仍在场景完全就绪之后）
                list.Add(new CodeInstruction(OpCodes.Call, hook));
            }

            return list;
        }

        // 插入的钩子：在场景完成初始化后输出信息（受 ModSettings.enableLevelLoadLogger 控制）
        public static void AfterSceneInitializeHook()
        {
            try
            {
                if (!ModSettings.LevelLoadLoggerEnabled)
                    return;

                var active = SceneManager.GetActiveScene();
                var id = TryGetSceneId(active.buildIndex);
                var display = TryGetDisplayNameBySceneId(id);
                Debug.Log($"[LevelLoadLogger] AfterSceneInit: {active.name}({active.buildIndex}), ID={id}, Display={display}");
            }
            catch (Exception ex)
            {
                Debug.Log($"[LevelLoadLogger] Hook error: {ex.Message}");
            }
        }

        // 运行时反射访问，避免编译期依赖 Eflatun.SceneReference
        private static string TryGetSceneId(int buildIndex)
        {
            try
            {
                var t = Type.GetType("Duckov.Scenes.SceneInfoCollection, TeamSoda.Duckov.Core");
                var m = t?.GetMethod("GetSceneID", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null);
                return m != null ? (string)m.Invoke(null, new object[] { buildIndex }) : null;
            }
            catch { return null; }
        }

        private static string TryGetDisplayNameBySceneId(string sceneId)
        {
            if (string.IsNullOrEmpty(sceneId)) return null;
            try
            {
                var t = Type.GetType("Duckov.Scenes.SceneInfoCollection, TeamSoda.Duckov.Core");
                var m = t?.GetMethod("GetSceneInfo", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                var info = m?.Invoke(null, new object[] { sceneId });
                if (info == null) return null;
                var p = info.GetType().GetProperty("DisplayName", BindingFlags.Public | BindingFlags.Instance);
                return p?.GetValue(info) as string;
            }
            catch { return null; }
        }
    }
}

