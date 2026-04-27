using System;
using System.Collections;
using System.IO;
using UnityEngine;
using System.Reflection;
using FMOD;
using Duckov; // AudioManager, AICharacterController, AudioObject, CharacterMainControl
using DuckovCustomSounds.CustomEnemySounds;
using DuckovCustomSounds.Logging;
using Duckov.ItemUsage; // FoodDrink
using ItemStatsSystem; // Item

using MapDetection;




namespace DuckovCustomSounds
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private static object? harmony;

        public static string ErrorMessage = "";
        private static ILog CoreLog => LogManager.GetLogger("Core");
        private Action<string, bool>? _mapSceneChangedHandler;

        public static ModBehaviour? Instance;

        // 初始化完成标志（防止 Update() 在初始化完成前执行）
        private static bool _fullyInitialized = false;
        private const float LoggingHotReloadInterval = 1.0f;
        private float loggingHotReloadTimer = 0f;
        private DateTime loggingSettingsLastWriteUtc = DateTime.MinValue;
        private bool loggingDebugOffExists;
        private bool loggingNoLogExists;

        // 2. Mod 根文件夹 (永远指向根目录，用于 settings.json 等全局配置)
        public const string RootFolderName = "DuckovCustomSounds";

        // 3. 动态的模块文件夹路径 (根据当前声音包变化)
        // 访问此属性会自动返回当前激活的声音包路径
        public static string ModFolderName => SoundPack.SoundPackManager.CurrentPackPath;

        // 游戏加载 Mod 时调用
        public void OnEnable()
        {
            Instance = this;
            try
            {
                // 0. 初始化声音包系统（必须最先执行，因为其他模块依赖 ModFolderName）
                SoundPack.SoundPackManager.Initialize();

                // 1. 加载/生成统一日志配置（settings.json）- 使用根目录
                LogManager.Initialize(RootFolderName);
                LoggingConfig.Initialize();


                // 2. 读取并应用模块设置（例如 overrideExtractionBGM）
                ModSettings.Initialize();

                // 2.1 初始化环境音拦截配置（支持 ModConfig UI）
                DuckovCustomSounds.CustomBGM.AmbientIntercept.AmbientInterceptConfig.Initialize();

                // 3. 验证声音包有效性
                if (!SoundPack.SoundPackManager.ValidateCurrentPack())
                {
                    ErrorMessage += $"声音包 '{SoundPack.SoundPackManager.CurrentPackId}' 无效，已回退到 Default\n";
                }

                // 4. 初始化声音包 ModConfig UI（在其他配置之前）
                SoundPack.SoundPackConfig.Initialize();

                // 5. 初始化 HomeBGM 配置（支持热配置）
                DuckovCustomSounds.CustomBGM.HomeBGM.HomeBGMConfig.Initialize();


                // 6. 指挥其他模块加载它们自己的资源（此时 ModFolderName 已指向正确的声音包路径）
                CustomBGM.CustomBGM.Load();

                // 启用 CustomEnemySounds
                CustomEnemySounds.CustomEnemySounds.Load();

                // 启用 BOSS BGM
                DuckovCustomSounds.CustomBGM.BossBGM.BossBGMConfig.Load();
                DuckovCustomSounds.CustomBGM.BossBGM.BossMusicResolver.Initialize();

                // 启用场景 BGM
                DuckovCustomSounds.CustomBGM.SceneBGM.CustomSceneBGM.Initialize();

                // 启用撤离 BGM
                DuckovCustomSounds.CustomBGM.ExtractionBGM.ExtractionBGMConfig.Initialize();

                // 初始化手雷音效系统
                DuckovCustomSounds.CustomGrenadeSounds.CustomGrenadeSounds.Initialize();

                // 初始化枪械音效系统
                DuckovCustomSounds.CustomGunSounds.CustomGunSounds.Initialize();

                // 初始化近战音效系统
                DuckovCustomSounds.CustomMeleeSounds.CustomMeleeSounds.Initialize();

                // 初始化命中与击杀音效系统
                DuckovCustomSounds.CustomHitAndKillSounds.CustomHitAndKillSounds.Initialize();

                // 初始化物品音效系统
                DuckovCustomSounds.CustomItemSounds.CustomItemSounds.Initialize();

                // 初始化脚步音效系统
                DuckovCustomSounds.CustomFootStepSounds.CustomFootStepSounds.Initialize();

                // 初始化 DuckovCustomPlayerQuak 系统（若模块存在）
                try
                {
                    var t = Type.GetType("DuckovCustomSounds.DuckovCustomPlayerQuak.DuckovCustomPlayerQuak, DuckovCustomSounds");
                    var init = t?.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static);
                    init?.Invoke(null, null);
                }
                catch (Exception ex)
                {
                    CoreLog.Info($"DuckovCustomPlayerQuak 未加载或初始化失败：{ex.Message}");
                }

                // 应用基于文件的快速开关（仅在 settings.json 未显式指定时生效）
                LogManager.ApplyFileSwitches(RootFolderName);
                CaptureLoggingHotReloadState();

                // 6.5. 运行 DuckovCustomPlayerQuak 模块验证（仅在 Debug 模式下）
                #if DEBUG
                try
                {
                    var vt = Type.GetType("DuckovCustomSounds.DuckovCustomPlayerQuak.DuckovCustomPlayerQuakValidator, DuckovCustomSounds");
                    var run = vt?.GetMethod("RunValidation", BindingFlags.Public | BindingFlags.Static);
                    run?.Invoke(null, null);
                }
                catch (Exception ex)
                {
                    CoreLog.Warning($"DuckovCustomPlayerQuak 验证失败: {ex.Message}");
                }
                #endif

                // 7. 应用所有补丁（动态加载 Harmony，避免在缺失 0Harmony.dll 时类型加载失败）
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
                    CoreLog.Info("Mod 已加载并应用所有补丁。");

                    // 额外：诊断性输出 - 验证关键补丁的目标方法是否已被 Harmony 标记
                    try
                    {
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

                                    // 获取详细的补丁信息
                                    if (info != null)
                                    {
                                        var prefixes = info.GetType().GetProperty("Prefixes")?.GetValue(info);
                                        var postfixes = info.GetType().GetProperty("Postfixes")?.GetValue(info);
                                        var prefixCount = prefixes != null ? ((System.Collections.ICollection)prefixes).Count : 0;
                                        var postfixCount = postfixes != null ? ((System.Collections.ICollection)postfixes).Count : 0;
                                        CoreLog.Info($"PatchStatus {name}: methodFound={(m!=null)}, patched=True, prefixes={prefixCount}, postfixes={postfixCount}");
                                    }
                                    else
                                    {
                                        CoreLog.Info($"PatchStatus {name}: methodFound={(m!=null)}, patched=False");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    CoreLog.Info($"PatchStatus {name}: exception {ex.Message}");
                                }
                            }

                            logStatus("AICharacterController.Init", aiInit);
                            logStatus("AudioObject.PostQuak", aoPostQuak);
                        }

	                // 初始化并订阅场景状态变化（用于双重门控的第一重：是否在基地/菜单）
	                try
	                {
	                    MapDetector.Initialize();
	                    _mapSceneChangedHandler = (scene, isBase) =>
	                    {
	                        try { CoreLog.Info($"[Map] SceneChanged: {scene}, isBase={isBase}"); } catch {}
	                        try { ResetBossBatchProcessing(); } catch {}
	                    };
	                    MapDetector.SubscribeToSceneChanges(_mapSceneChangedHandler);
	                }
	                catch (Exception ex)
	                {
	                    try { CoreLog.Warning($"MapDetector 初始化/订阅失败: {ex.Message}"); } catch {}
	                }

                    }
                    catch { }

                }

                // 标记初始化完成（允许 Update() 方法开始执行）
                _fullyInitialized = true;
                CoreLog.Info("Mod 初始化完成，已标记为可更新状态");
            }
            catch (Exception e)
            {
                ErrorMessage += "Mod 加载时发生致命错误: " + e.ToString() + "\n";
                _fullyInitialized = false; // 初始化失败，禁止 Update() 执行
            }
        }

        // 游戏卸载 Mod 时调用
        public void OnDisable()
        {
	        // 立即停止 Update() 执行（防止卸载过程中访问已销毁的对象）
	        _fullyInitialized = false;

	        try
	        {
		        Instance = null;


			        // 取消订阅场景切换事件
			        if (_mapSceneChangedHandler != null)
			        {
			            try { MapDetector.UnsubscribeFromSceneChanges(_mapSceneChangedHandler); } catch {}
			        }

		        // 6. 指挥其他模块卸载它们自己的资源（每个模块独立 try-catch，避免连锁失败）
		        try { CustomEnemySounds.CustomEnemySounds.Unload(); } catch (Exception ex) { CoreLog.Warning($"卸载敌人音效模块失败: {ex.Message}"); }

		        // 启用 BGM 模块卸载
		        try { CustomBGM.CustomBGM.Unload(); } catch (Exception ex) { CoreLog.Warning($"卸载 BGM 模块失败: {ex.Message}"); }
		        try { DuckovCustomSounds.CustomBGM.BossBGM.BossBGMManager.Clear(); } catch (Exception ex) { CoreLog.Warning($"清理 BOSS BGM 系统失败: {ex.Message}"); }
		        try { DuckovCustomSounds.CustomBGM.SceneBGM.CustomSceneBGM.StopAll(); } catch (Exception ex) { CoreLog.Warning($"清理场景 BGM 系统失败: {ex.Message}"); }

		        // 启用其他模块卸载
		        //没有Unload try { DuckovCustomSounds.CustomGrenadeSounds.CustomGrenadeSounds.Unload(); } catch (Exception ex) { CoreLog.Warning($"卸载手雷音效模块失败: {ex.Message}"); }
		        //没有Unload try { DuckovCustomSounds.CustomGunSounds.CustomGunSounds.Unload(); } catch (Exception ex) { CoreLog.Warning($"卸载枪械音效模块失败: {ex.Message}"); }
		        //没有Unload try { DuckovCustomSounds.CustomMeleeSounds.CustomMeleeSounds.Unload(); } catch (Exception ex) { CoreLog.Warning($"卸载近战音效模块失败: {ex.Message}"); }
		        try { DuckovCustomSounds.CustomHitAndKillSounds.CustomHitAndKillSounds.Unload(); } catch (Exception ex) { CoreLog.Warning($"卸载命中与击杀音效模块失败: {ex.Message}"); }
		        //没有Unload try { DuckovCustomSounds.CustomItemSounds.CustomItemSounds.Unload(); } catch (Exception ex) { CoreLog.Warning($"卸载物品音效模块失败: {ex.Message}"); }
		        try { DuckovCustomSounds.CustomFootStepSounds.CustomFootStepSounds.Unload(); } catch (Exception ex) { CoreLog.Warning($"卸载脚步音效模块失败: {ex.Message}"); }

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
				        try { CoreLog.Warning($"卸载补丁失败: {ex}"); } catch { }
			        }
		        }

		        try { CoreLog.Info("Mod 已卸载。"); } catch { }
	        }
	        catch (Exception ex)
	        {
		        // 卸载过程发生致命错误，静默失败（避免影响游戏退出）
		        try { CoreLog.Error("Mod 卸载时发生严重错误", ex); } catch { }
	        }
        }

        // Update - 定期更新 BOSS BGM 优先级
        // BOSS 批量检测（仅一次，延迟触发以覆盖地图初次生成）
        private float bossBatchCheckTimer = 0f;
        private bool bossBatchProcessed = false;

        private float bossBGMUpdateTimer = 0f;
        
        // 添加场景状态缓存，减少每帧检测
        private string lastCheckedSceneName = "";
        private bool lastInLevel = false;
        private bool lastIsBase = false;
        private MapSceneKind lastSceneKind = MapSceneKind.Unknown;
        private float sceneCheckTimer = 0f;
        private const float SCENE_CHECK_INTERVAL = 0.5f; // 每0.5秒检查一次场景状态

        void Update()
        {
            // ⚠️ 重要：等待初始化完成后再执行更新逻辑
            if (!_fullyInitialized)
                return;

            CheckLoggingHotReload();

            // 减少每帧检测频率，使用计时器控制
            sceneCheckTimer += Time.deltaTime;
            // 定期检查场景状态（而不是每帧都检查）
            if (sceneCheckTimer >= SCENE_CHECK_INTERVAL)
            {
                sceneCheckTimer = 0f;
                
                var __active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                bool __inLevel = false;
                try { __inLevel = LevelManager.LevelInited; } catch { __inLevel = false; }
                bool __isBase = false;
                try { __isBase = MapDetector.IsInBase(); } catch { __isBase = false; }
                MapSceneKind __sceneKind = MapSceneKind.Unknown;
                try { __sceneKind = MapDetector.GetCurrentSceneKind(); } catch { __sceneKind = MapSceneKind.Unknown; }
                
                // 只有当场景状态发生变化时才处理
                if (__active.name != lastCheckedSceneName || __inLevel != lastInLevel || __isBase != lastIsBase || __sceneKind != lastSceneKind)
                {
                    lastCheckedSceneName = __active.name;
                    lastInLevel = __inLevel;
                    lastIsBase = __isBase;
                    lastSceneKind = __sceneKind;
                    
                    if (__sceneKind != MapSceneKind.Combat || !__inLevel)
                    {
                        // 首次遇到非战斗条件时输出一次日志，并重置计时器
                        if (!bossBatchProcessed && bossBatchCheckTimer == 0f)
                        {
                            try { CoreLog.Info($"ModBehaviour.Update: 当前不是战斗关卡，跳过批量检测: active={__active.name}({__active.buildIndex}), sceneKind={__sceneKind}, isBase={__isBase}, LevelInited={__inLevel}"); } catch {}
                        }
                        bossBatchCheckTimer = 0f;
                    }
                    else
                    {
                        // 延迟批量处理BOSS（关卡已初始化，仅一次）
                        if (!bossBatchProcessed)
                        {
                            // 第一次进入关卡时提示开始倒计时
                            if (bossBatchCheckTimer == 0f)
                            {
                                try { CoreLog.Info("ModBehaviour.Update: 开始批量检测倒计时（15s）..."); } catch {}
                            }
                        }
                    }
                }
            }
            
            // 只有在需要时才处理BOSS批量检测逻辑
            if (!bossBatchProcessed && lastSceneKind == MapSceneKind.Combat && !lastIsBase && lastInLevel)
            {
                bossBatchCheckTimer += Time.deltaTime;
                float startDelay = 15.0f; // 增大检查间距，解决大地图上BOSS登记不出来的问题
                if (bossBatchCheckTimer >= startDelay)
                {
                    bossBatchProcessed = true;
                    try
                    {
                        DuckovCustomSounds.CustomEnemySounds.Context.EnemyContextRegistry.ProcessBossesBatch();
                        CoreLog.Info($"[BossBGM] [Batch] 触发于关卡: {lastCheckedSceneName}");
                    }
                    catch (Exception ex)
                    {
                        CoreLog.Warning($"BOSS BGM 批量检测失败: {ex}");
                    }
                }
            }

            // Home BGM 自动切歌轮询
            try { DuckovCustomSounds.CustomBGM.HomeBGM.HomeBGMManager.Update(); } catch {}

            // 定期更新 BOSS BGM 优先级（距离最近优先）
            bossBGMUpdateTimer += Time.deltaTime;
            float interval = DuckovCustomSounds.CustomBGM.BossBGM.BossBGMConfig.ManagerUpdateInterval;
            if (bossBGMUpdateTimer >= interval)
            {
                bossBGMUpdateTimer = 0f;
                try
                {
                    DuckovCustomSounds.CustomBGM.BossBGM.BossBGMManager.UpdateActiveBGM();
                }
                catch (Exception ex)
                {
                    CoreLog.Warning($"更新 BOSS BGM 失败: {ex}");
                }
            }
        }

        private void CheckLoggingHotReload()
        {
            loggingHotReloadTimer += Time.deltaTime;
            if (loggingHotReloadTimer < LoggingHotReloadInterval)
                return;

            loggingHotReloadTimer = 0f;
            ReadLoggingHotReloadState(out var settingsLastWriteUtc, out var debugOffExists, out var noLogExists);

            if (settingsLastWriteUtc == loggingSettingsLastWriteUtc &&
                debugOffExists == loggingDebugOffExists &&
                noLogExists == loggingNoLogExists)
            {
                return;
            }

            loggingSettingsLastWriteUtc = settingsLastWriteUtc;
            loggingDebugOffExists = debugOffExists;
            loggingNoLogExists = noLogExists;

            try
            {
                if (LogManager.ReloadSettings(RootFolderName))
                {
                    CoreLog.Info("日志配置已热重载");
                }
            }
            catch (Exception ex)
            {
                try { CoreLog.Warning($"日志配置热重载失败: {ex.Message}"); } catch { }
            }
        }

        private void CaptureLoggingHotReloadState()
        {
            loggingHotReloadTimer = 0f;
            ReadLoggingHotReloadState(out loggingSettingsLastWriteUtc, out loggingDebugOffExists, out loggingNoLogExists);
        }

        private static void ReadLoggingHotReloadState(out DateTime settingsLastWriteUtc, out bool debugOffExists, out bool noLogExists)
        {
            settingsLastWriteUtc = DateTime.MinValue;
            debugOffExists = false;
            noLogExists = false;

            try
            {
                var settingsPath = Path.Combine(RootFolderName, "settings.json");
                if (File.Exists(settingsPath))
                {
                    settingsLastWriteUtc = File.GetLastWriteTimeUtc(settingsPath);
                }

                debugOffExists = File.Exists(Path.Combine(RootFolderName, "debug_off"));
                noLogExists = File.Exists(Path.Combine(RootFolderName, ".nolog"));
            }
            catch
            {
                settingsLastWriteUtc = DateTime.MinValue;
                debugOffExists = false;
                noLogExists = false;
            }
        }


	        public void ResetBossBatchProcessing()
	        {
	            bossBatchCheckTimer = 0f;
	            bossBatchProcessed = false;
	            
		            // 重置场景状态缓存变量
		            lastCheckedSceneName = "";
		            lastInLevel = false;
		            lastIsBase = false;
		            lastSceneKind = MapSceneKind.Unknown;
		            sceneCheckTimer = 0f;
	           
	            // BOSS 批量检测状态重置日志（已修复乱码问题）
	            try { CoreLog.Info("BOSS 批量检测已重置，准备重新开始检测"); } catch {}
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
