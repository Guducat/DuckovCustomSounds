using System;
using System.Collections;
using UnityEngine;
using System.Reflection;
using FMOD;
using Debug = UnityEngine.Debug;
using Duckov; // AudioManager, AICharacterController, AudioObject, CharacterMainControl
using DuckovCustomSounds.CustomEnemySounds;


namespace DuckovCustomSounds
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private static object? harmony;

        // 1. 将这两个变量设为 public static 让其他模块访问
        public static string ErrorMessage = "";
        public static ChannelGroup SfxGroup;
        public static ChannelGroup MusicGroup;
        public static bool MusicGroupIsFallback; // 当 MusicGroup 当前指向 SFX 回退时为 true
        public static ModBehaviour Instance;

        // 2. Mod 根文件夹 (方便复用)
        public const string ModFolderName = "DuckovCustomSounds";

        // 游戏加载 Mod 时调用
        public void OnEnable()
        {
            Instance = this;
            try
            {
                // 3. 获取 FMOD 总线 (仅一次轻量探测；不设置回退，不写入错误信息)
                try
                {
                    var sfxBus = FMODUnity.RuntimeManager.GetBus("bus:/Master/SFX");
                    var sfxRes = sfxBus.getChannelGroup(out SfxGroup);
                    if (sfxRes != RESULT.OK || !SfxGroup.hasHandle())
                        Debug.Log($"[CustomSounds] SFX 总线初始化时未就绪: {sfxRes}");
                }
                catch (Exception ex)
                {
                    Debug.Log($"[CustomSounds] SFX 总线初始化探测异常: {ex.Message}");
                }

                // 不在初始化阶段获取 Music，总线会在实际播放/预热协程中再解析，避免误判与冗余日志
                CESLogger.ApplyFileSwitches(ModFolderName);


                // 4. 指挥其他模块加载它们自己的资源
                CustomBGM.CustomBGM.Load();
                CustomEnemySounds.CustomEnemySounds.Load();


                // 5. 应用所有补丁（动态加载 Harmony，避免在缺失 0Harmony.dll 时类型加载失败）
                var harmonyType = Type.GetType("HarmonyLib.Harmony, 0Harmony");
                if (harmonyType == null)
                {
                    ErrorMessage += "未找到 0Harmony.dll，已跳过打补丁（功能受限）。\n";
                }
                else
                {
                    var ctor = harmonyType.GetConstructor(new[] { typeof(string) });
                    harmony = ctor?.Invoke(new object[] { "com.guducat.duckovcustomsounds" });
                    var patchAll = harmonyType.GetMethod("PatchAll", new[] { typeof(Assembly) });
                    patchAll?.Invoke(harmony, new object[] { Assembly.GetExecutingAssembly() });
                    Debug.Log("[CustomSounds] Mod 已加载并应用所有补丁。");

                    // 额外：诊断性输出 - 验证关键补丁的目标方法是否已被 Harmony 标记
                    try
                    {
                        var amPostQuak = typeof(AudioManager).GetMethod("PostQuak", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, null, new Type[] { typeof(string), typeof(AudioManager.VoiceType), typeof(GameObject) }, null);
                        var aiInit = typeof(AICharacterController).GetMethod("Init", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public, null, new Type[] { typeof(CharacterMainControl), typeof(Vector3), typeof(AudioManager.VoiceType), typeof(AudioManager.FootStepMaterialType) }, null);
                        var aoPostQuak = typeof(AudioObject).GetMethod("PostQuak", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, new Type[] { typeof(string) }, null);

                        var getPatchInfo = harmonyType.GetMethod("GetPatchInfo", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (getPatchInfo != null)
                        {
                            void logStatus(string name, System.Reflection.MethodBase m)
                            {
                                try
                                {
                                    var info = m != null ? getPatchInfo.Invoke(null, new object[] { m }) : null;
                                    Debug.Log($"[CustomSounds] PatchStatus {name}: methodFound={(m!=null)}, patched={(info!=null)}");
                                }
                                catch (Exception ex)
                                {
                                    Debug.Log($"[CustomSounds] PatchStatus {name}: exception {ex.Message}");
                                }
                            }

                            logStatus("AudioManager.PostQuak", amPostQuak);
                            logStatus("AICharacterController.Init", aiInit);
                            logStatus("AudioObject.PostQuak", aoPostQuak);
                        }
                    }
                    catch { }

                }
            }
            catch (Exception e)
            {
                ErrorMessage += "Mod 加载时发生致命错误: " + e.ToString() + "\n";
            }
        }

        // 游戏卸载 Mod 时调用
        public void OnDisable()
        {
	        Instance = null;
		        CustomEnemySounds.CustomEnemySounds.Unload();

	        // 6. 指挥其他模块卸载它们自己的资源
	        CustomBGM.CustomBGM.Unload();

	        // 7. 卸载所有补丁（反射卸载，避免缺失 Harmony 时抛异常）
	        if (harmony != null)
	        {
		        try
		        {
			        var hType = harmony.GetType();
			        var unpatchAll = hType.GetMethod("UnpatchAll", Type.EmptyTypes);
			        unpatchAll?.Invoke(harmony, null);
		        }
		        catch (Exception ex)
		        {
			        Debug.LogWarning($"[CustomSounds] 卸载补丁失败: {ex}");
		        }
	        }

	        Debug.Log("[CustomSounds] Mod 已卸载。");
        }

        // 手动/事件触发：在进入标题后或进入大厅时预热总线句柄，避免首次播放不受滑块控制
	        public static Coroutine BusWarmupRoutine;
	        public static void KickBusWarmup(float timeoutSeconds = 2f, float pollInterval = 0.1f)
	        {
	            if (Instance == null) return;
	            if (BusWarmupRoutine != null) return; // 已在运行
	            BusWarmupRoutine = Instance.StartCoroutine(Instance.WarmUpBusesCoroutine(timeoutSeconds, pollInterval));
	        }

	        private IEnumerator WarmUpBusesCoroutine(float timeoutSeconds, float pollInterval)
	        {
	            float deadline = Time.realtimeSinceStartup + Mathf.Max(0.1f, timeoutSeconds);

	            bool sfxLogged = false, musicLogged = false;
	            while (Time.realtimeSinceStartup < deadline)
	            {
		            if (!FMODUnity.RuntimeManager.IsInitialized)
		            {
		                yield return new WaitForSeconds(Mathf.Max(0.02f, pollInterval));
		                continue;
		            }

	                // 预热 SFX
	                if (!SfxGroup.hasHandle())
	                {
	                    try
	                    {
	                        var sfx = FMODUnity.RuntimeManager.GetBus("bus:/Master/SFX");

	                        var sfxRes = sfx.getChannelGroup(out var sfxCg);
	                        if (sfxRes == RESULT.OK && sfxCg.hasHandle())
	                        {
	                            SfxGroup = sfxCg;
	                            if (!sfxLogged) { CESLogger.Info("已预热 SFX 总线通道组。"); sfxLogged = true; }
	                        }
	                        else if (!sfxLogged)
	                        {
	                            CESLogger.Info($"SFX 总线 getChannelGroup 失败: {sfxRes}");
	                            sfxLogged = true;
	                        }
	                    }
	                    catch (Exception ex) { CESLogger.Info($"SFX 总线预热异常: {ex.Message}"); }
	                }

	                // 预热 Music（优先多候选路径）
	                if (!MusicGroup.hasHandle() || MusicGroupIsFallback)
	                {
	                    var candidates = new string[] { "bus:/Master/Music" };
	                    foreach (var path in candidates)
	                    {
	                        try
	                        {
	                            var bus = FMODUnity.RuntimeManager.GetBus(path);
	                            var cgRes = bus.getChannelGroup(out var cg);
	                            if (cgRes == RESULT.OK && cg.hasHandle())
	                            {
	                                MusicGroup = cg;
	                                MusicGroupIsFallback = false;
	                                if (!musicLogged) { CESLogger.Info($"已预热 Music 总线: {path}"); musicLogged = true; }
	                                break;
	                            }
	                            else if (!musicLogged)
	                            {
	                                CESLogger.Info($"Music 总线 getChannelGroup 失败 {path}: {cgRes}");
	                                // 不置 musicLogged=true，允许后续候选成功时再打一次成功日志
	                            }
	                        }
	                        catch (Exception ex) { CESLogger.Info($"Music 总线预热异常 {path}: {ex.Message}"); }
	                    }
	                }

	                if (SfxGroup.hasHandle() && MusicGroup.hasHandle() && !MusicGroupIsFallback) break;
	                yield return new WaitForSeconds(Mathf.Max(0.02f, pollInterval));
	            }
	            BusWarmupRoutine = null;

	            // 预热结束日志
	            if (!SfxGroup.hasHandle() || !MusicGroup.hasHandle() || MusicGroupIsFallback)
	            {
	                CESLogger.Info($"总线预热结束。SFX:{SfxGroup.hasHandle()} Music:{MusicGroup.hasHandle()} 回退:{MusicGroupIsFallback}");
	            }
	        }

	       // 8. 在屏幕上显示错误

        private void OnGUI()
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                var errorStyle = new GUIStyle(GUI.skin.label);
                errorStyle.normal.textColor = Color.red;
                GUI.Label(new Rect(10, 10, Screen.width - 10, Screen.height - 10), "[CustomSounds] 错误: \n" + ErrorMessage, errorStyle);
            }
        }
    }
}
