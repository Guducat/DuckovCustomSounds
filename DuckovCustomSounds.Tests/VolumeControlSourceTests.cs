using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DuckovCustomSounds;
using DuckovCustomSounds.CustomBGM.BossBGM;
using DuckovCustomSounds.CustomBGM.Core;
using DuckovCustomSounds.CustomBGM.SceneBGM;

var root = FindRepositoryRoot();
var tests = new (string Name, Action Body)[]
{
    ("Footstep playback uses FootstepConfig.Volume", FootstepPlaybackUsesFootstepConfigVolume),
    ("Enemy voice exposes configurable volume and applies it", EnemyVoiceExposesConfigurableVolumeAndAppliesIt),
    ("BossBGM target volume comes from configuration", BossBgmTargetVolumeComesFromConfiguration),
    ("BossBGM trigger distance stays consistent after config reload", BossBgmTriggerDistanceStaysConsistentAfterConfigReload),
    ("Out-of-range boss does not suppress scene BGM", OutOfRangeBossDoesNotSuppressSceneBgm),
    ("Unregistering current boss restores scene BGM", UnregisteringCurrentBossRestoresSceneBgm),
    ("Unregistering current boss with only far bosses restores scene BGM", UnregisteringCurrentBossWithOnlyFarBossesRestoresSceneBgm),
    ("Clearing boss manager restores scene BGM", ClearingBossManagerRestoresSceneBgm),
    ("Disabling boss BGM restores scene BGM", DisablingBossBgmRestoresSceneBgm),
    ("Switching bosses deactivates old controller without active instance", SwitchingBossesDeactivatesOldControllerWithoutActiveInstance),
    ("Out-of-range current boss bypasses switch cooldown", OutOfRangeCurrentBossBypassesSwitchCooldown),
    ("Boss range transitions update scene suppression", BossRangeTransitionsUpdateSceneSuppression),
    ("Disabled current boss restores scene BGM", DisabledCurrentBossRestoresSceneBgm),
    ("Unregistering current boss keeps suppression for next candidate", UnregisteringCurrentBossKeepsSuppressionForNextCandidate),
    ("Re-enabling boss BGM reuses registered controllers", ReEnablingBossBgmReusesRegisteredControllers),
    ("Boss manager queries each distance once per update", BossManagerQueriesEachDistanceOncePerUpdate),
    ("Boss fader completes at zero volume and releases the instance", BossFaderCompletesAtZeroVolumeAndReleasesInstance),
    ("Boss fader stops only instances owned by the activated controller", BossFaderStopsOnlyInstancesOwnedByActivatedController),
    ("Boss controller transfers playback to the persistent fader on deactivation", BossControllerTransfersPlaybackToPersistentFaderOnDeactivation),
    ("ExtractionBGM exposes configurable volume and applies it", ExtractionBgmExposesConfigurableVolumeAndAppliesIt),
    ("Extraction countdown cancellation fades out before release", ExtractionCountdownCancellationFadesOutBeforeRelease),
    ("TitleBGM stingers apply HomeBGM volume", TitleBgmStingersApplyHomeBgmVolume),
    ("HomeBGM Set_Prefix entry trace is Verbose", HomeBgmSetPrefixEntryTraceIsVerbose),
    ("SceneBGM uses official scene events", SceneBgmUsesOfficialSceneEvents),
    ("SceneBGM reads SceneLoadingContext fields", SceneBgmReadsSceneLoadingContextFields),
    ("SceneBGM bridges loading scenes through MapDetector", SceneBgmBridgesLoadingScenesThroughMapDetector),
    ("SceneBGM missing music logs candidate names", SceneBgmMissingMusicLogsCandidateNames),
    ("SceneBGM resolves numeric scene variants to main scene music", SceneBgmResolvesNumericSceneVariantsToMainSceneMusic),
    ("SceneBGM resolves farm 01 scene variants to main scene music", SceneBgmResolvesFarm01SceneVariantsToMainSceneMusic),
    ("SceneBGM resolves hidden warehouse aliases", SceneBgmResolvesHiddenWarehouseAliases),
    ("ItemUse StopSound logs only when tracked action sound stopped", ItemUseStopSoundLogsOnlyWhenTrackedActionSoundStopped),
    ("ItemUse records cancellation reason before StopSound", ItemUseRecordsCancellationReasonBeforeStopSound),
    ("Project references official SceneReference assembly", ProjectReferencesOfficialSceneReferenceAssembly),
    ("Release CI runs source regression tests", ReleaseCiRunsSourceRegressionTests),
    ("Scene and Boss BGM use non-stopping custom playback", SceneAndBossBgmUseNonStoppingCustomPlayback),
    ("Stopped retained BGM instances are treated as inactive", StoppedRetainedBgmInstancesAreTreatedAsInactive),
    ("Home stinger intercepts PlayStringer", HomeStingerInterceptsPlayStringer),
    ("Logging levels reload while the mod is running", LoggingLevelsReloadWhileModIsRunning),
    ("Logging levels are editable through ModConfig", LoggingLevelsAreEditableThroughModConfig),
    ("Logging ModConfig changes avoid recursive option saves", LoggingModConfigChangesAvoidRecursiveOptionSaves),
    ("Logging accepts Warn as Warning", LoggingAcceptsWarnAsWarning),
    ("Config handlers ignore unrelated ModConfig keys", ConfigHandlersIgnoreUnrelatedModConfigKeys),
    ("Sound pack ModConfig handles prefixed key", SoundPackModConfigHandlesPrefixedKey),
    ("Footstep rule engine logs through Footstep logger", FootstepRuleEngineLogsThroughFootstepLogger),
    ("Footstep path attempts use Verbose while outcomes stay Debug", FootstepPathAttemptsUseVerboseWhileOutcomesStayDebug),
    ("Enemy context registration logs only once at Debug", EnemyContextRegistrationLogsOnlyOnceAtDebug),
    ("Footstep tracker separates footstep and dash slots", FootstepTrackerSeparatesFootstepAndDashSlots),
    ("Dash cooldown falls back to original sound", DashCooldownFallsBackToOriginalSound),
    ("Grenade sounds keep legacy lookup while adding source and TypeID routing", GrenadeSoundsKeepLegacyLookupWhileAddingSourceAndTypeIdRouting),
    ("Grenade sounds support no-event injection and strict variants", GrenadeSoundsSupportNoEventInjectionAndStrictVariants),
    ("Grenade docs describe source directories and legacy fallback", GrenadeDocsDescribeSourceDirectoriesAndLegacyFallback),
    ("Logger emits structured module scopes", LoggerEmitsStructuredModuleScopes),
    ("Diagnostic loggers use structured scopes", DiagnosticLoggersUseStructuredScopes),
    ("Map detector classifies loading scenes separately", MapDetectorClassifiesLoadingScenesSeparately),
    ("Extraction coverage accepts every supported source scene", ExtractionCoverageAcceptsEverySupportedSourceScene),
    ("Extraction coverage rejects base and unknown scenes", ExtractionCoverageRejectsBaseAndUnknownScenes),
    ("Extraction transition suppresses only recent map stingers", ExtractionTransitionSuppressesOnlyRecentMapStingers),
    ("Extraction completion ignores duplicate notifications", ExtractionCompletionIgnoresDuplicateNotifications),
    ("Extraction stinger supports contextual map stingers", ExtractionStingerSupportsContextualMapStingers),
    ("BGM audio file resolver supports cached FLAC lookup", BgmAudioFileResolverSupportsCachedFlacLookup),
    ("Ambient intercept is editable through ModConfig", AmbientInterceptIsEditableThroughModConfig),
    ("Ambient intercept docs describe game source behavior", AmbientInterceptDocsDescribeGameSourceBehavior),
    ("Hit and kill sounds are wired through module registration", HitAndKillSoundsAreWiredThroughModuleRegistration),
    ("Hit and kill event mapping covers marker and simple health cases", HitAndKillEventMappingCoversMarkerAndSimpleHealthCases),
    ("Hit and kill docs describe reflection backed coverage", HitAndKillDocsDescribeReflectionBackedCoverage),
};

var failures = new List<string>();

foreach (var test in tests)
{
    try
    {
        test.Body();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{test.Name}: {ex.Message}");
        Console.WriteLine($"FAIL {test.Name}");
        Console.WriteLine($"  {ex.Message}");
    }
}

if (failures.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine($"{failures.Count} test(s) failed.");
    Environment.Exit(1);
}

Console.WriteLine();
Console.WriteLine("All source regression tests passed.");

void FootstepPlaybackUsesFootstepConfigVolume()
{
    var patches = Read("CustomFootStepSounds/CustomFootStepSounds_Patches.cs");

    AssertContains(patches, "setVolume(FootstepConfig.Volume)", "脚步和 dash 播放应使用 ModConfig 对应的 FootstepConfig.Volume。");
    AssertDoesNotContain(patches, "setVolume(ModSettings.FootstepVolumeScale)", "播放点仍读取 settings.json 的旧脚步音量字段。");
}

void EnemyVoiceExposesConfigurableVolumeAndAppliesIt()
{
    var options = Read("CustomEnemySounds/Config/EnemyVoiceOptions.cs");
    var patches = Read("CustomEnemySounds/CustomEnemySounds_Patches.cs");

    AssertContains(options, "public static float Volume", "敌人语音配置类应暴露音量属性。");
    AssertContains(options, "SafeAddInputWithSlider(Scope, \"volume\"", "敌人语音 ModConfig UI 应包含音量滑块。");
    AssertContains(patches, "setVolume(EnemyVoiceOptions.Volume)", "敌人语音播放实例应应用配置音量。");
}

void BossBgmTargetVolumeComesFromConfiguration()
{
    var config = Read("CustomBGM/BossBGM/BossBGMConfig.cs");
    var controller = Read("CustomBGM/BossBGM/BossBGMController.cs");

    AssertContains(config, "public static float Volume", "BossBGM 配置类应暴露音量属性。");
    AssertContains(config, "SafeAddInputWithSlider(Scope, \"volume\"", "BossBGM ModConfig UI 应包含音量滑块。");
    AssertContains(controller, "targetVolume = BossBGMConfig.Volume", "BossBGM 淡入目标音量应来自配置。");
    AssertDoesNotContain(controller, "targetVolume = 0.7f", "BossBGM 目标音量仍为硬编码。");
}

void BossBgmTriggerDistanceStaysConsistentAfterConfigReload()
{
    var controller = Read("CustomBGM/BossBGM/BossBGMController.cs");

    AssertDoesNotContain(controller, "private float triggerDistance;",
        "BossBGMController 不应缓存初始化时的触发距离。");
    AssertDoesNotContain(controller, "private float triggerDistanceSqr;",
        "BossBGMController 不应缓存初始化时的触发距离平方。");
    AssertContains(controller, "float triggerDistance = Mathf.Max(0f, BossBGMConfig.TriggerDistance);",
        "BossBGMController 距离判断应读取当前 BossBGMConfig.TriggerDistance。");
}

void OutOfRangeBossDoesNotSuppressSceneBgm()
{
    BossBGMTestEnvironment.Reset();

    try
    {
        var boss = new BossBGMController("far-boss", distance: 100f);

        BossBGMManager.RegisterBoss(boss);

        if (CustomSceneBGM.IsBossBGMActive)
            throw new InvalidOperationException("触发范围外 Boss 注册后不应抑制场景音乐。");
    }
    finally
    {
        BossBGMTestEnvironment.Reset();
    }
}

void UnregisteringCurrentBossRestoresSceneBgm()
{
    BossBGMTestEnvironment.Reset();

    try
    {
        var boss = new BossBGMController("near-boss", distance: 20f);
        BossBGMManager.RegisterBoss(boss);

        if (!CustomSceneBGM.IsBossBGMActive)
            throw new InvalidOperationException("范围内 Boss 注册后应抑制场景音乐。测试前置状态错误。");

        BossBGMManager.UnregisterBoss(boss);

        if (CustomSceneBGM.IsBossBGMActive)
            throw new InvalidOperationException("当前 Boss 注销后应恢复场景音乐。");
        if (boss.PriorityActive)
            throw new InvalidOperationException("当前 Boss 注销后应撤销自身优先级。");
    }
    finally
    {
        BossBGMTestEnvironment.Reset();
    }
}

void UnregisteringCurrentBossWithOnlyFarBossesRestoresSceneBgm()
{
    BossBGMTestEnvironment.Reset();

    try
    {
        var currentBoss = new BossBGMController("current-boss", distance: 10f);
        var farBoss = new BossBGMController("far-boss", distance: 100f);
        BossBGMManager.RegisterBoss(currentBoss);
        BossBGMManager.RegisterBoss(farBoss);

        BossBGMManager.UnregisterBoss(currentBoss);

        if (CustomSceneBGM.IsBossBGMActive)
            throw new InvalidOperationException("当前 Boss 注销且仅剩范围外 Boss 时应恢复场景音乐。");
        if (farBoss.PriorityActive)
            throw new InvalidOperationException("范围外 Boss 不应获得播放优先级。");
    }
    finally
    {
        BossBGMTestEnvironment.Reset();
    }
}

void ClearingBossManagerRestoresSceneBgm()
{
    BossBGMTestEnvironment.Reset();

    try
    {
        var boss = new BossBGMController("near-boss", distance: 20f);
        BossBGMManager.RegisterBoss(boss);

        if (!CustomSceneBGM.IsBossBGMActive)
            throw new InvalidOperationException("范围内 Boss 注册后应抑制场景音乐。测试前置状态错误。");

        BossBGMManager.Clear();

        if (CustomSceneBGM.IsBossBGMActive)
            throw new InvalidOperationException("Boss 管理器清理后应恢复场景音乐。");
    }
    finally
    {
        BossBGMTestEnvironment.Reset();
    }
}

void DisablingBossBgmRestoresSceneBgm()
{
    BossBGMTestEnvironment.Reset();

    try
    {
        var boss = new BossBGMController("near-boss", distance: 20f);
        BossBGMManager.RegisterBoss(boss);

        if (!CustomSceneBGM.IsBossBGMActive)
            throw new InvalidOperationException("范围内 Boss 注册后应抑制场景音乐。测试前置状态错误。");

        BossBGMConfig.Enabled = false;
        BossBGMManager.UpdateActiveBGM();

        if (CustomSceneBGM.IsBossBGMActive)
            throw new InvalidOperationException("运行时关闭 Boss BGM 后应恢复场景音乐。");
        if (boss.PriorityActive)
            throw new InvalidOperationException("运行时关闭 Boss BGM 后应撤销当前 Boss 优先级。");
    }
    finally
    {
        BossBGMTestEnvironment.Reset();
    }
}

void SwitchingBossesDeactivatesOldControllerWithoutActiveInstance()
{
    BossBGMTestEnvironment.Reset();

    try
    {
        var oldBoss = new BossBGMController("old-boss", distance: 20f);
        BossBGMManager.RegisterBoss(oldBoss);
        oldBoss.Valid = false;

        UnityEngine.Time.time += BossBGMConfig.MinSwitchIntervalSeconds + 1f;
        var newBoss = new BossBGMController("new-boss", distance: 10f);
        BossBGMManager.RegisterBoss(newBoss);

        if (oldBoss.PriorityActive)
            throw new InvalidOperationException("切换 Boss 时应撤销无有效播放实例的旧 Controller 优先级。");
        if (!newBoss.PriorityActive)
            throw new InvalidOperationException("切换 Boss 后新 Controller 应获得优先级。");
    }
    finally
    {
        BossBGMTestEnvironment.Reset();
    }
}

void OutOfRangeCurrentBossBypassesSwitchCooldown()
{
    BossBGMTestEnvironment.Reset();

    try
    {
        var currentBoss = new BossBGMController("current-boss", distance: 10f);
        var waitingBoss = new BossBGMController("waiting-boss", distance: 20f);
        BossBGMManager.RegisterBoss(currentBoss);
        BossBGMManager.RegisterBoss(waitingBoss);

        currentBoss.Distance = 100f;
        BossBGMManager.UpdateActiveBGM();

        if (currentBoss.PriorityActive)
            throw new InvalidOperationException("当前 Boss 离开范围后应立即撤销优先级。");
        if (!waitingBoss.PriorityActive)
            throw new InvalidOperationException("当前 Boss 离开范围后应跳过切换冷却并激活范围内候选。");
        if (!CustomSceneBGM.IsBossBGMActive)
            throw new InvalidOperationException("切换到另一范围内 Boss 时场景音乐应保持抑制。");
    }
    finally
    {
        BossBGMTestEnvironment.Reset();
    }
}

void BossRangeTransitionsUpdateSceneSuppression()
{
    BossBGMTestEnvironment.Reset();

    try
    {
        var boss = new BossBGMController("moving-boss", distance: 20f);
        BossBGMManager.RegisterBoss(boss);

        boss.Distance = 100f;
        BossBGMManager.UpdateActiveBGM();
        if (CustomSceneBGM.IsBossBGMActive)
            throw new InvalidOperationException("Boss 离开触发范围后应恢复场景音乐。");

        boss.Distance = 20f;
        BossBGMManager.UpdateActiveBGM();
        if (!CustomSceneBGM.IsBossBGMActive)
            throw new InvalidOperationException("Boss 重新进入触发范围后应再次抑制场景音乐。");
    }
    finally
    {
        BossBGMTestEnvironment.Reset();
    }
}

void DisabledCurrentBossRestoresSceneBgm()
{
    BossBGMTestEnvironment.Reset();

    try
    {
        var boss = new BossBGMController("disabled-boss", distance: 10f);
        BossBGMManager.RegisterBoss(boss);

        boss.isActiveAndEnabled = false;
        BossBGMManager.UpdateActiveBGM();

        if (boss.PriorityActive)
            throw new InvalidOperationException("停用的当前 Boss 应失去播放优先级。");
        if (CustomSceneBGM.IsBossBGMActive)
            throw new InvalidOperationException("当前 Boss 停用后应恢复场景音乐。");

        boss.isActiveAndEnabled = true;
        BossBGMManager.UpdateActiveBGM();

        if (!boss.PriorityActive)
            throw new InvalidOperationException("Boss 重新启用后应恢复参与候选选择。");
        if (!CustomSceneBGM.IsBossBGMActive)
            throw new InvalidOperationException("重新启用且位于范围内的 Boss 应再次抑制场景音乐。");
    }
    finally
    {
        BossBGMTestEnvironment.Reset();
    }
}

void UnregisteringCurrentBossKeepsSuppressionForNextCandidate()
{
    BossBGMTestEnvironment.Reset();

    try
    {
        var currentBoss = new BossBGMController("current-boss", distance: 10f);
        var nextBoss = new BossBGMController("next-boss", distance: 20f);
        BossBGMManager.RegisterBoss(currentBoss);
        BossBGMManager.RegisterBoss(nextBoss);

        BossBGMManager.UnregisterBoss(currentBoss);

        if (!nextBoss.PriorityActive)
            throw new InvalidOperationException("当前 Boss 注销后应激活另一范围内候选。");
        if (!CustomSceneBGM.IsBossBGMActive)
            throw new InvalidOperationException("切换到另一范围内 Boss 时场景音乐应保持抑制。");
        if (CustomSceneBGM.StateChanges.Count != 1)
            throw new InvalidOperationException("范围内 Boss 连续切换不应产生恢复后再次抑制的状态抖动。");
    }
    finally
    {
        BossBGMTestEnvironment.Reset();
    }
}

void ReEnablingBossBgmReusesRegisteredControllers()
{
    BossBGMTestEnvironment.Reset();

    try
    {
        var boss = new BossBGMController("near-boss", distance: 20f);
        BossBGMManager.RegisterBoss(boss);

        BossBGMConfig.Enabled = false;
        BossBGMManager.UpdateActiveBGM();
        BossBGMConfig.Enabled = true;
        BossBGMManager.UpdateActiveBGM();

        if (!boss.PriorityActive)
            throw new InvalidOperationException("重新启用 Boss BGM 后应复用已注册 Controller。");
        if (!CustomSceneBGM.IsBossBGMActive)
            throw new InvalidOperationException("重新启用并激活范围内 Boss 后应抑制场景音乐。");
    }
    finally
    {
        BossBGMTestEnvironment.Reset();
    }
}

void BossManagerQueriesEachDistanceOncePerUpdate()
{
    BossBGMTestEnvironment.Reset();

    try
    {
        var firstBoss = new BossBGMController("first-boss", distance: 10f);
        var secondBoss = new BossBGMController("second-boss", distance: 20f);
        BossBGMManager.RegisterBoss(firstBoss);
        BossBGMManager.RegisterBoss(secondBoss);
        firstBoss.ResetDistanceQueryCount();
        secondBoss.ResetDistanceQueryCount();

        BossBGMManager.UpdateActiveBGM();

        if (firstBoss.DistanceQueryCount != 1 || secondBoss.DistanceQueryCount != 1)
        {
            throw new InvalidOperationException(
                $"每轮更新应只查询每个 Boss 一次距离。first={firstBoss.DistanceQueryCount}, second={secondBoss.DistanceQueryCount}");
        }
    }
    finally
    {
        BossBGMTestEnvironment.Reset();
    }
}

void BossFaderCompletesAtZeroVolumeAndReleasesInstance()
{
    var owner = new BossBGMController("fading-boss", distance: 20f);
    var state = new FMOD.Studio.EventInstanceState { Volume = 0.7f };
    var instance = new FMOD.Studio.EventInstance(state);
    var host = new BossBGMFaderHost();

    var startFade = typeof(BossBGMFaderHost).GetMethod(
        "StartFade",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
        binder: null,
        new[]
        {
            typeof(FMOD.Studio.EventInstance),
            typeof(BossBGMController),
            typeof(string),
            typeof(float)
        },
        modifiers: null);
    if (startFade == null)
        throw new InvalidOperationException("BossBGMFaderHost 缺少带 Controller 所有者的 StartFade 入口。");

    var advanceFades = typeof(BossBGMFaderHost).GetMethod(
        "AdvanceFades",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    if (advanceFades == null)
        throw new InvalidOperationException("BossBGMFaderHost 缺少可测试的确定性淡出推进入口。");

    startFade.Invoke(host, new object[] { instance, owner, "fading-boss", 1f });
    advanceFades.Invoke(host, new object[] { 0.4f });
    advanceFades.Invoke(host, new object[] { 0.6f });

    if (state.Volume != 0f)
        throw new InvalidOperationException($"淡出完成音量应为 0，当前为 {state.Volume}。");
    if (!state.Stopped)
        throw new InvalidOperationException("淡出完成后应停止 FMOD 实例。");
    if (!state.Released)
        throw new InvalidOperationException("淡出完成后应释放 FMOD 实例。");
}

void BossFaderStopsOnlyInstancesOwnedByActivatedController()
{
    var firstOwner = new BossBGMController("first-owner", distance: 20f);
    var secondOwner = new BossBGMController("second-owner", distance: 20f);
    var firstState = new FMOD.Studio.EventInstanceState { Volume = 0.6f };
    var secondState = new FMOD.Studio.EventInstanceState { Volume = 0.8f };

    BossBGMFader.ForceStopAll("TestReset");
    try
    {
        BossBGMFader.FadeOutAndRelease(
            new FMOD.Studio.EventInstance(firstState),
            firstOwner,
            "first-owner",
            1f);
        BossBGMFader.FadeOutAndRelease(
            new FMOD.Studio.EventInstance(secondState),
            secondOwner,
            "second-owner",
            1f);

        var stopPending = typeof(BossBGMFader).GetMethod(
            "StopPending",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (stopPending == null)
            throw new InvalidOperationException("BossBGMFader 缺少按 Controller 所有者停止待淡出实例的入口。");

        stopPending.Invoke(null, new object[] { firstOwner, "PriorityActivated" });

        if (!firstState.Stopped || !firstState.Released)
            throw new InvalidOperationException("重新激活 Controller 时应停止并释放其旧实例。");
        if (secondState.Stopped || secondState.Released)
            throw new InvalidOperationException("清理已激活 Controller 的旧实例时应保留其他 Controller 的淡出实例。");
    }
    finally
    {
        BossBGMFader.ForceStopAll("TestCleanup");
    }
}

void BossControllerTransfersPlaybackToPersistentFaderOnDeactivation()
{
    var controller = Read("CustomBGM/BossBGM/BossBGMController.cs");

    AssertContains(
        controller,
        "BossBGMFader.StopPending(this, \"PriorityActivated\");",
        "Controller 重新激活时应清理自身尚未完成的旧实例淡出。");
    AssertContains(
        controller,
        "TransferPlaybackToFader(Mathf.Max(0f, BossBGMConfig.FadeDuration), \"PriorityDeactivated\");",
        "Controller 失去优先级时应立即将播放实例移交给持久淡出宿主。");
    AssertContains(
        controller,
        "private void TransferPlaybackToFader(float fadeSeconds, string reason)",
        "Controller 应统一处理离开范围与销毁时的实例移交。");
    AssertContains(
        controller,
        "instance.getTimelinePosition(out lastTimelineMs);",
        "实例移交前应保存播放进度，以便再次进入范围时恢复。");
    AssertContains(
        controller,
        "BossBGMFader.FadeOutAndRelease(instance, this, bossName, fadeSeconds);",
        "实例移交应记录 Controller 所有者，避免重新激活时影响其他 Boss。");
    AssertContains(
        controller,
        "bgmInstance = null;\n            currentVolume = 0f;\n            targetVolume = 0f;",
        "实例移交后应同步清除本地句柄与音量状态。");
    AssertContains(
        controller,
        "TransferPlaybackToFader(\n                    Mathf.Max(0f, BossBGMConfig.BossDeathFadeOutSeconds),\n                    \"Destroyed\");",
        "Controller 销毁时应复用统一移交逻辑并采用死亡淡出时长。");
}

void ExtractionBgmExposesConfigurableVolumeAndAppliesIt()
{
    var config = Read("CustomBGM/ExtractionBGM/ExtractionBGMConfig.cs");
    var sounds = Read("CustomBGM/ExtractionBGM/ExtractionSounds.cs");

    AssertContains(config, "public static float Volume", "ExtractionBGM 配置类应暴露音量属性。");
    AssertContains(config, "SafeAddInputWithSlider(Scope, \"volume\"", "ExtractionBGM ModConfig UI 应包含音量滑块。");
    AssertContains(sounds, "setVolume(ExtractionBGMConfig.Volume)", "撤离音量应用方法应使用配置值。");
    AssertAtLeast(sounds, "ApplyConfiguredVolume(", 3, "撤离倒计时、成功替换和旧逻辑播放都应应用配置音量。");
}

void ExtractionCountdownCancellationFadesOutBeforeRelease()
{
    var sounds = Read("CustomBGM/ExtractionBGM/ExtractionSounds.cs");

    AssertContains(sounds, "private const float CountdownCancelFadeOutSeconds = 0.35f",
        "倒计时取消应使用 0.x 秒淡出时长，避免立即切断音频。");
    AssertContains(sounds, "StopActive(fadeCountdown: true, clearTransitionState: true);",
        "倒计时中止分支应进入淡出停止入口。");
    AssertContains(sounds, "FadeOutAndReleaseCountdown",
        "倒计时实例应通过独立淡出协程降低音量后释放。");
    AssertContains(sounds, "FMOD.Studio.STOP_MODE.ALLOWFADEOUT",
        "淡出完成后的 FMOD 停止应允许事件自身释放尾音。");
    AssertDoesNotContain(sounds, "已强制停止倒计时音效实例",
        "倒计时取消日志不应继续描述为强制停止。");
}

void TitleBgmStingersApplyHomeBgmVolume()
{
    var patches = Read("CustomBGM/CustomBGM_Patches.cs");

    AssertContains(patches, "setVolume(HomeBGMConfig.Volume)", "TitleBGM Stinger 音量应用方法应使用 HomeBGM 配置值。");
    AssertAtLeast(patches, "ApplyHomeBgmVolume(", 2, "start.mp3 和 death.mp3 替换播放都应应用 HomeBGM 音量。");
}

void HomeBgmSetPrefixEntryTraceIsVerbose()
{
    var logger = Read("CustomBGM/HomeBGM/HomeBGMLogger.cs");
    var patches = Read("CustomBGM/HomeBGM/HomeBGM_Patches.cs");

    AssertContains(logger, "public static void Verbose(string message) => _log.Verbose(message);",
        "HomeBGMLogger 应暴露 Verbose 入口。");
    AssertContains(patches, "HomeBGMLogger.Verbose($\"Set_Prefix ENTER: index={index}, play={play}, showInfo={showInfo}, manualFlag={manualFlagAtEnter}\")",
        "Set_Prefix 高频入口日志应下调到 Verbose。");
    AssertDoesNotContain(patches, "HomeBGMLogger.Debug($\"Set_Prefix ENTER:",
        "Set_Prefix 高频入口日志不应继续占用 Debug。");
}

void SceneBgmUsesOfficialSceneEvents()
{
    var sceneBgm = Read("CustomBGM/SceneBGM/CustomSceneBGM.cs");

    AssertContains(sceneBgm, "SceneLoader.onAfterSceneInitialize += OnSceneInitialized", "场景 BGM 应订阅官方 SceneLoader.onAfterSceneInitialize 事件。");
    AssertContains(sceneBgm, "MultiSceneCore.OnSubSceneLoaded += OnSubSceneLoaded", "场景 BGM 应订阅官方子场景加载事件。");
    AssertDoesNotContain(sceneBgm, "Assets.BGM.SceneLoader, Assembly-CSharp", "场景 BGM 仍在查找错误的 SceneLoader 反射类型。");
}

void SceneBgmReadsSceneLoadingContextFields()
{
    var sceneBgm = Read("CustomBGM/SceneBGM/CustomSceneBGM.cs");

    AssertContains(sceneBgm, "OnSceneInitialized(SceneLoadingContext context)", "场景初始化回调应使用官方 SceneLoadingContext 类型。");
    AssertContains(sceneBgm, "context.sceneName", "SceneLoadingContext 应读取 sceneName 字段。");
    AssertContains(sceneBgm, "context.useLocation", "SceneLoadingContext 应读取 useLocation 字段。");
    AssertContains(sceneBgm, "context.location.SceneID", "SceneLoadingContext 应从 location.SceneID 获取目标场景 ID。");
    AssertContains(sceneBgm, "SceneInfoCollection.GetSceneID", "场景 BGM 应通过 SceneInfoCollection 获取官方场景 ID。");
    AssertDoesNotContain(sceneBgm, "GetSceneInfoFromContext(context, \"sceneId\")", "场景 BGM 仍在读取不存在的 sceneId 属性。");
    AssertDoesNotContain(sceneBgm, "GetSceneInfoFromContext(context, \"displayName\")", "场景 BGM 仍在读取不存在的 displayName 属性。");
}

void SceneBgmBridgesLoadingScenesThroughMapDetector()
{
    var sceneBgm = Read("CustomBGM/SceneBGM/CustomSceneBGM.cs");

    AssertContains(sceneBgm, "TrySubscribeMapDetectorBridge();",
        "SceneLoader 订阅成功时，加载界面仍应通过 MapDetector 桥接触发 SceneBGM。");
    AssertContains(sceneBgm, "if (_sceneLoaderSubscribed && !isLoading)",
        "官方场景事件可用时，MapDetector 桥接应只处理加载界面，避免普通场景重复调度。");
    AssertContains(sceneBgm, "ScheduleSceneMusic(sceneName, sceneName, delay);",
        "LoadingScreen_Getout 应由 MapDetector 场景名进入 SceneBGM 播放调度。");
}

void SceneBgmMissingMusicLogsCandidateNames()
{
    var resolver = Read("CustomBGM/SceneBGM/SceneMusicResolver.cs");

    AssertContains(resolver, "BuildMissingMusicNames(candidates)",
        "场景 BGM 未命中日志应生成候选音乐名，便于确认需要放置的文件。");
    AssertContains(resolver, "$\"未找到场景{label}音乐: {cleanName}({missingMusicNames})\"",
        "场景 BGM 未命中日志应按“场景名(音乐名)”格式输出。");
}

void SceneBgmResolvesNumericSceneVariantsToMainSceneMusic()
{
    using var fixture = new SceneBgmFixture();
    var expected = fixture.AddEnterMusic("level_groundzero_main_enter.mp3");

    var actual = SceneMusicResolver.ResolveEnterMusicPath("Level_GroundZero_1", "零号区");

    AssertPathEquals(expected, actual, "Level_GroundZero_1 应兼容匹配 level_groundzero_main_enter。");
}

void SceneBgmResolvesFarm01SceneVariantsToMainSceneMusic()
{
    using var fixture = new SceneBgmFixture();
    var expected = fixture.AddEnterMusic("level_farm_main_enter.mp3");

    var actual = SceneMusicResolver.ResolveEnterMusicPath("Level_Farm_01", "农场镇");

    AssertPathEquals(expected, actual, "Level_Farm_01 应兼容匹配 level_farm_main_enter。");
}

void SceneBgmResolvesHiddenWarehouseAliases()
{
    using var fixture = new SceneBgmFixture();
    var expected = fixture.AddEnterMusic("level_warehouse_main_enter.mp3");

    var actual = SceneMusicResolver.ResolveEnterMusicPath("Level_HiddenWarehouse_Main", "仓库区");

    AssertPathEquals(expected, actual, "Level_HiddenWarehouse_Main 应兼容匹配 level_warehouse_main_enter。");
}

void ItemUseStopSoundLogsOnlyWhenTrackedActionSoundStopped()
{
    var patches = Read("CustomItemSounds/CustomItemSounds_Patches.cs");

    AssertContains(patches, "public static bool StopByPhase(GameObject",
        "ItemUse 阶段停止方法应返回是否实际停止过已追踪音效。");
    AssertContains(patches, "bool stoppedActionSound = ItemUseSoundRegistry.StopByPhase(go, ItemUsePhase.Action, FMOD.Studio.STOP_MODE.ALLOWFADEOUT);",
        "StopSound 前缀应基于阶段停止结果决定是否记录停止日志。");
    AssertContains(patches, "if (stoppedActionSound)",
        "ItemUse 停止日志应受实际停止结果保护。");
}

void ItemUseRecordsCancellationReasonBeforeStopSound()
{
    var patches = Read("CustomItemSounds/CustomItemSounds_Patches.cs");

    AssertContains(patches, "internal enum ItemUseStopReason",
        "ItemUse 应显式记录停止原因，避免 StopSound 反推正常完成与取消。");
    AssertContains(patches, "MarkCancelled(__instance?.gameObject, ItemUseStopReason.ItemMissing)",
        "OnUpdateAction 应在物品缺失时标记取消原因。");
    AssertContains(patches, "MarkCancelled(__instance?.gameObject, ItemUseStopReason.HoldItemMismatch)",
        "OnUpdateAction 应在手持物品不一致时标记取消原因。");
    AssertContains(patches, "bool isAbnormalStop = ItemUseCycle.IsAbnormalStop(go)",
        "StopSound 应先读取异常停止标记，再决定停止策略。");
    AssertContains(patches, "if (!isAbnormalStop)",
        "最短可听时间保护只应作用于正常停止场景。");
    AssertContains(patches, "ItemLogger.Debug($\"[ItemUse] 取消使用: reason={reason}\")",
        "取消分支应输出原因日志，便于区分开始、取消和停止。");
    AssertContains(patches, "ItemLogger.Debug($\"[ItemUse] 停止使用: reason={cyc.StopReason}, normal={cyc.StopReason == ItemUseStopReason.Completed}\")",
        "停止分支应输出最终原因与是否正常结束。");
}

void ProjectReferencesOfficialSceneReferenceAssembly()
{
    var project = Read("DuckovCustomSounds.csproj");

    AssertContains(project, "Eflatun.SceneReference", "使用官方场景 API 后，项目应显式引用 Eflatun.SceneReference.dll。");
}

void ReleaseCiRunsSourceRegressionTests()
{
    var workflow = Read(".github/workflows/release-package.yml");

    AssertContains(workflow,
        "dotnet run --project DuckovCustomSounds.Tests/DuckovCustomSounds.Tests.csproj",
        "发布任务应在打包前执行源码回归测试。");
}

void SceneAndBossBgmUseNonStoppingCustomPlayback()
{
    var player = Read("CustomBGM/Core/CustomBGMPlayer.cs");
    var sceneController = Read("CustomBGM/SceneBGM/SceneBGMController.cs");
    var bossController = Read("CustomBGM/BossBGM/BossBGMController.cs");

    AssertContains(player, "stopExistingBGM", "自定义 BGM 播放器应显式暴露是否停止现有 BGM 的参数。");
    AssertContains(player, "if (stopExistingBGM)", "自定义 BGM 播放器应只在调用方要求时停止现有 BGM。");
    AssertContains(player, "PostFile(eventPath, filePath, doRelease: false)", "自定义 BGM 播放器应保留实例，便于 controller 持有并释放。");
    AssertContains(sceneController, "CustomBGMPlayer.PlayMusicFile(musicPath, isLoop, stopExistingBGM: false)", "SceneBGM 不应通过会停止全局 BGM 的 PlayCustomBGM 播放。");
    AssertContains(bossController, "CustomBGMPlayer.PlayMusicFile(musicPath, loop: true, stopExistingBGM: false)", "BossBGM 不应通过会停止全局 BGM 的 PlayCustomBGM 播放。");
    AssertDoesNotContain(sceneController, "AudioManager.PlayCustomBGM(musicPath", "SceneBGM 仍在调用全局停止式 PlayCustomBGM。");
    AssertDoesNotContain(bossController, "AudioManager.PlayCustomBGM(musicPath", "BossBGM 仍在调用全局停止式 PlayCustomBGM。");
}

void StoppedRetainedBgmInstancesAreTreatedAsInactive()
{
    var player = Read("CustomBGM/Core/CustomBGMPlayer.cs");
    var sceneController = Read("CustomBGM/SceneBGM/SceneBGMController.cs");
    var bossController = Read("CustomBGM/BossBGM/BossBGMController.cs");

    AssertContains(player, "IsEventInstanceActive", "自定义 BGM 播放器应提供基于播放状态的活跃实例判断。");
    AssertContains(player, "PLAYBACK_STATE.STOPPED", "活跃实例判断应排除已停止的 FMOD 实例。");
    AssertContains(player, "PLAYBACK_STATE.STOPPING", "活跃实例判断应排除正在停止的 FMOD 实例。");
    AssertContains(sceneController, "CustomBGMPlayer.IsEventInstanceActive(bgmInstance)", "SceneBGM 应使用播放状态判断实例是否仍可播放。");
    AssertContains(bossController, "CustomBGMPlayer.IsEventInstanceActive(bgmInstance)", "BossBGM 应使用播放状态判断实例是否仍可播放。");
}

void HomeStingerInterceptsPlayStringer()
{
    var patches = Read("CustomBGM/CustomBGM_Patches.cs");

    AssertContains(patches, "HarmonyPatch(\"PlayStringer\", new Type[] { typeof(string) })", "Home Stinger 应覆盖 AudioManager.PlayStringer(string) 入口。");
    AssertContains(patches, "TryPlayHomeStinger(", "Post 与 PlayStringer 两个入口应共用同一段 start.mp3 替换逻辑。");
}

void LoggingLevelsReloadWhileModIsRunning()
{
    var logManager = Read("Logging/LogManager.cs");
    var modBehaviour = Read("ModBehaviour.cs");

    AssertContains(logManager, "public static bool ReloadSettings(string modRoot)", "LogManager 应暴露运行时重新读取 settings.json 的入口。");
    AssertContains(logManager, "ApplyFileSwitchesLocked", "日志重载应在读取 settings.json 后重新应用 debug_off 和 .nolog 文件开关。");
    AssertContains(logManager, "[\"CustomBGM\"] = \"BGM\"", "文档中的 CustomBGM 模块名应映射到实际 BGM 日志模块。");
    AssertContains(modBehaviour, "CheckLoggingHotReload()", "ModBehaviour.Update 应低频检查日志配置变化。");
    AssertContains(modBehaviour, "LogManager.ReloadSettings(RootFolderName)", "检测到日志配置变化后应重新应用 LogManager 设置。");
}

void LoggingLevelsAreEditableThroughModConfig()
{
    var loggingConfig = Read("Logging/LoggingConfig.cs");
    var modBehaviour = Read("ModBehaviour.cs");

    AssertContains(modBehaviour, "LoggingConfig.Initialize()", "ModBehaviour.OnEnable 应初始化日志 ModConfig UI。");
    AssertContains(loggingConfig, "SafeAddBoolDropdownList(Scope, \"enabled\"", "日志配置 UI 应包含日志总开关。");
    AssertContains(loggingConfig, "SafeAddDropdownList(Scope, ModuleKey(module)", "日志配置 UI 应为模块注册等级下拉列表。");
    AssertContains(loggingConfig, "SafeAddOnOptionsChangedDelegate(_onChangedHandler)", "日志配置 UI 应注册变更回调。");
    AssertContains(loggingConfig, "SafeLoad(Scope, \"enabled\"", "日志配置变更时应从 ModConfig 读取总开关。");
    AssertContains(loggingConfig, "JObject.Parse", "写回 logging 节时应保留 settings.json 中的其他字段。");
    AssertContains(loggingConfig, "root[\"logging\"] = logging", "写回时应只替换 logging 节。");
    AssertContains(loggingConfig, "LogManager.ReloadSettings(ModBehaviour.RootFolderName)", "ModConfig 修改后应立即重新加载日志设置。");

    foreach (var module in new[]
    {
        "Core",
        "SoundPack",
        "Enemy",
        "Footstep",
        "BGM",
        "HomeBGM",
        "SceneBGM",
        "ExtractionBGM",
        "Gun",
        "Grenade",
        "Item",
        "Melee",
    })
    {
        AssertContains(loggingConfig, $"\"{module}\"", $"日志 ModConfig UI 应覆盖模块 {module}。");
    }
}

void LoggingModConfigChangesAvoidRecursiveOptionSaves()
{
    var loggingConfig = Read("Logging/LoggingConfig.cs");

    AssertDoesNotContain(loggingConfig, "SafeSave(ModName,",
        "日志配置变更回调不应把刚读取的 ModConfig 值再次写回，避免触发嵌套变更事件。");
}

void LoggingAcceptsWarnAsWarning()
{
    var logManager = Read("Logging/LogManager.cs");
    var loggingConfig = Read("Logging/LoggingConfig.cs");

    AssertContains(logManager, "NormalizeLevelName(name)", "LogManager 应在解析日志等级前规范化兼容别名。");
    AssertContains(logManager, "value.Equals(\"Warn\", StringComparison.OrdinalIgnoreCase)", "LogManager 应兼容 settings.json 中的 Warn。");
    AssertContains(loggingConfig, "NormalizeLevelName(value)", "LoggingConfig 应在读取 settings.json 时兼容等级别名。");
    AssertContains(loggingConfig, "value.Equals(\"Warn\", StringComparison.OrdinalIgnoreCase)", "LoggingConfig 应把 Warn 映射为 Warning。");
    AssertContains(loggingConfig, "[\"CustomFootStepSounds\"] = \"Footstep\"", "CustomFootstepSounds 应通过大小写无关匹配映射到 Footstep。");
}

void ConfigHandlersIgnoreUnrelatedModConfigKeys()
{
    var api = Read("ModConfig/ModConfigApi.cs");
    AssertContains(api, "public static bool IsKeyForMod", "ModConfigAPI 应提供模块键名判断，避免各配置类重复拼接前缀。");
    AssertContains(api, "public static bool IsKeyForOption", "ModConfigAPI 应提供单项键名判断，兼容带前缀与不带前缀的回调键名。");

    foreach (var file in new[]
    {
        "CustomFootStepSounds/FootstepConfig.cs",
        "CustomGunSounds/GunConfig.cs",
        "CustomGrenadeSounds/GrenadeConfig.cs",
        "CustomMeleeSounds/MeleeConfig.cs",
        "CustomBGM/HomeBGM/HomeBGMConfig.cs",
        "CustomBGM/BossBGM/BossBGMConfig.cs",
        "CustomBGM/ExtractionBGM/ExtractionBGMConfig.cs",
        "CustomBGM/SceneBGM/SceneBGMConfig.cs",
        "CustomItemSounds/ItemConfig.cs",
        "CustomEnemySounds/Config/EnemyVoiceOptions.cs",
    })
    {
        var text = Read(file);
        AssertContains(text, "ModConfigAPI.IsKeyForMod(key, Scope)",
            $"{file} 的变更回调应通过 Scope 过滤其他模块的 ModConfig 键。");
    }
}

void SoundPackModConfigHandlesPrefixedKey()
{
    var soundPackConfig = Read("SoundPack/SoundPackConfig.cs");

    AssertContains(soundPackConfig, "ModConfigAPI.IsKeyForOption(key, Scope, \"soundPack\")",
        "声音包选择应通过 Scope 兼容 ModConfig 回调传入的完整键名 DuckovCustomSounds_soundPack。");
    AssertDoesNotContain(soundPackConfig, "key != \"soundPack\"",
        "声音包变更回调不应只接受未加前缀的 soundPack。");
}

void FootstepRuleEngineLogsThroughFootstepLogger()
{
    var engine = Read("CustomEnemySounds/Rules/VoiceRuleEngine.cs");
    var pathBuilder = Read("CustomEnemySounds/Rules/PathBuilder.cs");
    var footstep = Read("CustomFootStepSounds/CustomFootStepSounds.cs");
    var footstepLogger = Read("CustomFootStepSounds/FootstepLogger.cs");
    var footstepPatches = Read("CustomFootStepSounds/CustomFootStepSounds_Patches.cs");
    var enemy = Read("CustomEnemySounds/CustomEnemySounds.cs");

    AssertContains(engine, "VoiceRuleEngine(",
        "规则引擎应暴露日志注入入口，避免内部固定写入 Enemy 日志模块。");
    AssertDoesNotContain(engine, "\"[CES:Rule]",
        "规则引擎日志前缀不应硬编码为 CES:Rule。");
    AssertContains(footstep, "new VoiceRuleEngine(FootstepLogger.Info, FootstepLogger.Debug, FootstepLogger.Verbose, \"CFS:Rule\")",
        "脚步模块应使用 FootstepLogger 和 CFS:Rule 前缀创建规则引擎。");
    AssertContains(enemy, "new VoiceRuleEngine(CESLogger.Info, CESLogger.Debug, CESLogger.Verbose, \"CES:Rule\")",
        "敌人语音模块应继续使用 CESLogger 和 CES:Rule 前缀创建规则引擎。");
    AssertContains(pathBuilder, "Action<string>? verboseLog",
        "路径构建日志应由调用方传入，避免脚步路径日志进入 Enemy 模块。");
    AssertContains(pathBuilder, "bindVariantIndexPerOwner",
        "变体绑定开关应由调用方配置决定，避免脚步模块读取敌人配置。");
    AssertDoesNotContain(footstepLogger, "ShouldLog(\"Enemy\"",
        "脚步详细日志开关不应再依赖 Enemy 模块等级。");
    AssertDoesNotContain(pathBuilder, "CESLogger.Verbose($\"[CES:Path]",
        "PathBuilder 不应硬编码 CES:Path 的 Verbose 输出。");
    AssertDoesNotContain(pathBuilder, "CESLogger.Debug($\"[CES:Path]",
        "PathBuilder 不应硬编码 CES:Path 的 Debug 输出。");
    AssertDoesNotContain(footstepPatches, "CustomEnemySounds=Debug",
        "脚步未命中排查提示不应再要求开启敌人日志。");
}

void FootstepPathAttemptsUseVerboseWhileOutcomesStayDebug()
{
    var engine = Read("CustomEnemySounds/Rules/VoiceRuleEngine.cs");
    var footstepLogger = Read("CustomFootStepSounds/FootstepLogger.cs");
    var footstepPatches = Read("CustomFootStepSounds/CustomFootStepSounds_Patches.cs");

    AssertContains(footstepLogger, "public static void VerboseDetail(string msg)",
        "脚步模块应提供 VerboseDetail 入口，用于逐次匹配探测日志。");
    AssertContains(footstepPatches, "FootstepLogger.VerboseDetail($\"[CFS:Route] 开始匹配: sk={skSpecific}, engine={(CustomFootStepSounds.Engine != null ? \"已加载\" : \"未加载\")}\")",
        "首次匹配探测应下调到 Verbose。");
    AssertContains(footstepPatches, "FootstepLogger.VerboseDetail($\"[CFS:Route] 首次匹配结果: matched={matched}, route={(route != null ? $\"UseCustom={route.UseCustom}, File={route.FileFullPath}\" : \"null\")}\")",
        "首次匹配结果应下调到 Verbose。");
    AssertContains(footstepPatches, "FootstepLogger.VerboseDetail($\"[CFS:Path] tried[{i}]: {route.TriedPaths[i]}\")",
        "逐条候选文件探测应下调到 Verbose。");
    AssertDoesNotContain(footstepPatches, "FootstepLogger.DebugDetail($\"[CFS:Path] tried[{i}]: {route.TriedPaths[i]}\")",
        "逐条候选文件探测不应继续占用 Debug。");
    AssertContains(footstepPatches, "FootstepLogger.DebugDetail($\"[CFS:Route] 命中: rule={route.MatchRule}, file={route.FileFullPath}\")",
        "命中结果应继续保留在 Debug，并携带最终文件位置。");
    AssertContains(footstepPatches, "FootstepLogger.DebugDetail(\"[CFS:Route] 未命中（UseSimpleRules=false）：复杂规则/默认模板未命中；可开启 Footstep=Verbose 查看 [CFS:Path] cand/exists\")",
        "未命中提示应继续保留在 Debug，并引导到 Verbose 查看逐次探测。");
    AssertContains(engine, "Verbose($\"Simple(Team) 尝试路径[{i}]: {tried[i]}\")",
        "Simple Team 逐条候选文件探测应下调到 Verbose。");
    AssertContains(engine, "Verbose($\"Simple 尝试路径[{i}]: {tried[i]}\")",
        "Simple 逐条候选文件探测应下调到 Verbose。");
    AssertDoesNotContain(engine, "Debug($\"Simple(Team) 尝试路径[{i}]: {tried[i]}\")",
        "Simple Team 逐条候选文件探测不应继续占用 Debug。");
    AssertDoesNotContain(engine, "Debug($\"Simple 尝试路径[{i}]: {tried[i]}\")",
        "Simple 逐条候选文件探测不应继续占用 Debug。");
    AssertContains(engine, "Debug($\"Simple(Team) 未命中，尝试的路径数: {tried?.Count ?? 0}\")",
        "Simple Team 未命中摘要应继续保留在 Debug。");
    AssertContains(engine, "Debug($\"Simple 未命中，尝试的路径数: {tried?.Count ?? 0}\")",
        "Simple 未命中摘要应继续保留在 Debug。");
}

void EnemyContextRegistrationLogsOnlyOnceAtDebug()
{
    var registry = Read("CustomEnemySounds/Context/EnemyContextRegistry.cs");

    AssertContains(registry, "CESLogger.Debug($\"登记敌人上下文: {ctx}\")",
        "敌人上下文登记应保留在 Debug，便于排查注册过程。");
    AssertDoesNotContain(registry, "CESLogger.Info($\"登记敌人上下文: {ctx}\")",
        "敌人上下文登记不应同时写入 Info，避免同一次注册出现两条不同级别的重复日志。");
}

void FootstepTrackerSeparatesFootstepAndDashSlots()
{
    var tracker = Read("CustomFootStepSounds/FootstepSoundTracker.cs");
    var patches = Read("CustomFootStepSounds/CustomFootStepSounds_Patches.cs");

    AssertContains(tracker, "internal enum FootstepSoundKind",
        "脚步追踪器应显式区分脚步与 dash 的声音类型。");
    AssertContains(tracker, "private readonly struct PlaybackSlot",
        "追踪键应包含 owner 和声音类型，避免同一角色的脚步替换 dash。");
    AssertContains(tracker, "Dictionary<PlaybackSlot, Entry>",
        "追踪表应按 owner 与声音类型分槽保存 EventInstance。");
    AssertContains(tracker, "var slot = new PlaybackSlot(ownerId, kind)",
        "Track 应只替换同一 owner 下同一声音类型的旧实例。");
    AssertDoesNotContain(tracker, "Dictionary<int, Entry>",
        "追踪表不应继续只按 owner 保存单个 EventInstance。");
    AssertContains(patches, "FootstepSoundKind.Footstep",
        "脚步播放应写入 Footstep 类型槽位。");
    AssertContains(patches, "FootstepSoundKind.Dash",
        "dash 播放应写入 Dash 类型槽位。");
}

void DashCooldownFallsBackToOriginalSound()
{
    var patches = Read("CustomFootStepSounds/CustomFootStepSounds_Patches.cs");

    AssertContains(patches, "return true; // dash cooldown fallback to original",
        "dash 自定义播放被冷却跳过时应保留原版 dash 声音。");
    AssertDoesNotContain(patches, "FootstepLogger.DebugDetail($\"[CFS:Cooldown] dash SKIP id={did} remain={dRemain:F2}s (min={dMinCd:F2}s)\");\n                            return false;",
        "dash 冷却分支不应继续静音原版 dash 声音。");
}

void GrenadeSoundsKeepLegacyLookupWhileAddingSourceAndTypeIdRouting()
{
    var context = Read("CustomGrenadeSounds/ExplosionSoundContext.cs");
    var map = Read("CustomGrenadeSounds/GrenadeSoundMap.cs");
    var resolver = Read("CustomGrenadeSounds/GrenadeSoundResolver.cs");
    var patches = Read("CustomGrenadeSounds/CustomGrenadeSounds_Patches.cs");
    var module = Read("CustomGrenadeSounds/CustomGrenadeSounds.cs");

    AssertContains(context, "internal enum ExplosionSoundSource", "手雷模块应显式记录爆炸来源。");
    AssertContains(context, "Grenade", "爆炸来源应包含手雷。");
    AssertContains(context, "Breakable", "爆炸来源应包含可破坏物。");
    AssertContains(context, "Proxy", "爆炸来源应包含 ExplosionProxy。");
    AssertContains(context, "[ThreadStatic]", "爆炸上下文应使用线程静态存储，避免跨线程污染。");
    AssertContains(context, "Restore(previous)", "爆炸上下文应支持恢复旧状态，保证嵌套爆炸正确。");

    AssertContains(map, "grenade_sound_map.json", "手雷模块应加载独立映射文件。");
    AssertContains(map, "ResolveForReplace", "手雷映射应支持替换路径的 soundKey 覆盖。");
    AssertContains(map, "ResolveForInjection", "手雷映射应支持无原版事件注入。");
    AssertContains(map, "ResolveFileBase", "手雷映射应支持 fileBase 共享文件。");
    AssertContains(module, "GrenadeSoundMap.Initialize()", "手雷模块初始化应加载 grenade_sound_map.json。");

    AssertContains(resolver, "CustomGrenadeSounds/grenade/<TypeID>.*", "手雷查找顺序应在代码中保留可读说明。");
    AssertContains(resolver, "Path.Combine(baseDir, \"grenade\")", "手雷来源应优先查找 grenade 子目录。");
    AssertContains(resolver, "Path.Combine(baseDir, \"breakable\")", "可破坏物来源应优先查找 breakable 子目录。");
    AssertContains(resolver, "Path.Combine(baseDir, \"proxy\")", "ExplosionProxy 来源应优先查找 proxy 子目录。");
    AssertContains(resolver, "ExpandCandidates(baseDir, typeIdStr)", "手雷根目录应保留 TypeID 回退。");
    AssertContains(resolver, "ExpandCandidates(baseDir, soundKey)", "根目录旧 soundKey 结构必须保留。");
    AssertContains(resolver, "ExpandCandidates(baseDir, \"default\")", "根目录旧 default 回退必须保留。");

    AssertContains(patches, "HarmonyPatch(typeof(Grenade), \"Explode\")", "应在 Grenade.Explode 上建立手雷上下文。");
    AssertContains(patches, "HarmonyPatch(typeof(Breakable), \"OnDead\")", "应在 Breakable.OnDead 上建立可破坏物上下文。");
    AssertContains(patches, "HarmonyPatch(typeof(ExplosionProxy), \"DoExplode\")", "应在 ExplosionProxy.DoExplode 上建立代理爆炸上下文。");
    AssertContains(patches, "ExplosionSoundContext.MarkObserved(originalKey)", "观察到原版爆炸事件时应标记上下文，避免重复注入。");
    AssertContains(patches, "GrenadeSoundResolver.TryResolve", "AudioManager.Post 补丁应通过统一解析器查找文件。");
    AssertDoesNotContain(patches, "attempts.AddRange(ExpandCandidates(dir, soundKey));\n\n                string filePath = attempts.FirstOrDefault(File.Exists);",
        "手雷补丁不应继续只按根目录 soundKey 查找。");
}

void GrenadeSoundsSupportNoEventInjectionAndStrictVariants()
{
    var resolver = Read("CustomGrenadeSounds/GrenadeSoundResolver.cs");
    var patches = Read("CustomGrenadeSounds/CustomGrenadeSounds_Patches.cs");

    AssertContains(resolver, "TryPickVariantStrict", "手雷模块应支持严格 _1/_2 变体选择。");
    AssertContains(resolver, "TryPickVariantStrictByBase", "未命中基准文件时也应能按基名选择变体。");
    AssertContains(resolver, "char.IsDigit", "变体匹配应限制为数字后缀，避免误选其他下划线文件。");

    AssertContains(patches, "InjectIfMissing", "Grenade.Explode 结束时应检查是否需要补声。");
    AssertContains(patches, "GrenadeSoundMap.ResolveForInjection", "无原版事件补声应由映射配置驱动。");
    AssertContains(patches, "!current.ObservedOriginalEvent", "已有原版事件时不应重复注入。");
    AssertContains(patches, "AudioManager.PostCustomSFX(filePath, sourceObject, loop: false)", "注入应使用游戏本体自定义 SFX 接口。");
}

void GrenadeDocsDescribeSourceDirectoriesAndLegacyFallback()
{
    var zh = Read("docs/modules/grenade.md");
    var en = Read("docs/en/modules/grenade.md");

    AssertContains(zh, "CustomGrenadeSounds/grenade/", "中文文档应说明 grenade 来源目录。");
    AssertContains(zh, "CustomGrenadeSounds/breakable/", "中文文档应说明 breakable 来源目录。");
    AssertContains(zh, "grenade_sound_map.json", "中文文档应说明映射文件。");
    AssertContains(zh, "旧结构", "中文文档应明确旧结构兼容。");
    AssertContains(zh, "_1", "中文文档应说明变体命名。");

    AssertContains(en, "CustomGrenadeSounds/grenade/", "英文文档应说明 grenade 来源目录。");
    AssertContains(en, "CustomGrenadeSounds/breakable/", "英文文档应说明 breakable 来源目录。");
    AssertContains(en, "grenade_sound_map.json", "英文文档应说明映射文件。");
    AssertContains(en, "legacy", "英文文档应明确旧结构兼容。");
    AssertContains(en, "_1", "英文文档应说明变体命名。");
}

void LoggerEmitsStructuredModuleScopes()
{
    var logManager = Read("Logging/LogManager.cs");

    AssertContains(logManager, "private const string ModName = \"DuckovCustomSounds\"",
        "日志输出应包含统一 Mod 主标签，避免与其他 Mod 日志混淆。");
    AssertContains(logManager, "ILog ForScope(params string[] scopes)",
        "统一日志接口应支持结构化子模块作用域。");
    AssertContains(logManager, "BuildPrefix(LogLevel level, IEnumerable<string> scopes)",
        "日志前缀应由统一格式化方法生成。");
    AssertContains(logManager, "ExtractLeadingScopes",
        "日志器应兼容旧式正文前缀并转换为结构化作用域。");
    AssertContains(logManager, "AppendSegment(prefix, ModName)",
        "统一日志格式应输出主标签。");
    AssertDoesNotContain(logManager, "NormalizeModuleName(scope), module",
        "作用域过滤只能跳过同名模块与显式短别名，BossBGM 等子模块不应被模块配置别名吞掉。");
    AssertDoesNotContain(logManager, $"[{{Module}}:Debug]",
        "Debug 等级不应继续混入模块标签。");
    AssertDoesNotContain(logManager, $"[{{Module}}:Verbose]",
        "Verbose 等级不应继续混入模块标签。");
}

void DiagnosticLoggersUseStructuredScopes()
{
    var audioPost = Read("Logging/AudioPostLogger_Patches.cs");
    var levelLoad = Read("Logging/LevelLoadLogger_Patches.cs");
    var mapDetector = Read("Common/MapDetector.cs");

    AssertContains(audioPost, ".ForScope(\"AudioPostLogger\")",
        "AudioPostLogger 应使用结构化作用域输出。");
    AssertDoesNotContain(audioPost, "\"[AudioPostLogger]",
        "AudioPostLogger 不应继续把子模块写入正文前缀。");

    AssertContains(levelLoad, ".ForScope(\"LevelLoadLogger\")",
        "LevelLoadLogger 应接入统一日志器并使用结构化作用域。");
    AssertDoesNotContain(levelLoad, "Debug.Log($\"[LevelLoadLogger]",
        "LevelLoadLogger 不应绕过统一日志器。");

    AssertContains(mapDetector, ".ForScope(\"MapDetection\")",
        "MapDetection 应接入统一日志器并使用结构化作用域。");
    AssertDoesNotContain(mapDetector, "Debug.Log(\"[MapDetection]",
        "MapDetection 不应绕过统一日志器。");
}

void MapDetectorClassifiesLoadingScenesSeparately()
{
    var mapDetector = Read("Common/MapDetector.cs");
    var modBehaviour = Read("ModBehaviour.cs");

    AssertContains(mapDetector, "public enum MapSceneKind", "地图检测应提供基地、加载、战斗等明确场景分类。");
    AssertContains(mapDetector, "MapSceneKind.Loading", "LoadingScreen 系列场景应归类为加载界面。");
    AssertContains(mapDetector, "IsLoadingSceneName", "加载界面判断应集中封装在 MapDetector 中。");
    AssertContains(mapDetector, "public bool IsCurrentlyLoading()", "MapDetector 应暴露当前是否处于加载界面的实例 API。");
    AssertContains(mapDetector, "public static bool IsInLoading()", "MapDetector 应暴露当前是否处于加载界面的静态 API。");
    AssertContains(mapDetector, "当前在加载界面", "加载界面日志不应继续显示为战斗地图。");
    AssertDoesNotContain(mapDetector, "isInBaseScene ? \"当前在基地中\" : \"当前在战斗地图中\"",
        "非基地场景不应统一显示为战斗地图。");
    AssertContains(modBehaviour, "MapSceneKind", "批量检测日志应带上明确场景分类，方便排查加载界面。");
}

void ExtractionStingerSupportsContextualMapStingers()
{
    var sounds = Read("CustomBGM/ExtractionBGM/ExtractionSounds.cs");
    var patches = Read("CustomBGM/ExtractionBGM/ExtractionSounds_Patches.cs");

    AssertContains(patches, "HarmonyPatch(typeof(LevelManager))", "撤离替换应挂接游戏确认撤离的 LevelManager 入口。");
    AssertContains(patches, "HarmonyPatch(\"NotifyEvacuated\")", "撤离替换应覆盖 NotifyEvacuated，而非猜测少数地图键。");
    AssertContains(patches, "ExtractionSounds.OnEvacuationCompleted()", "NotifyEvacuated 补丁应转发到撤离音乐控制器。");
    AssertContains(sounds, "public static void OnEvacuationCompleted()", "撤离音乐控制器应提供确认撤离入口。");
    AssertContains(sounds, "MultiSceneCore.MainSceneID", "多场景地图应使用权威主场景 ID 判断撤离来源。");
    AssertContains(sounds, "MapDetector.GetCurrentScene()", "主场景实例缺失时应回退到统一地图检测结果。");
    AssertContains(sounds, "ExtractionCoveragePolicy.IsSupportedSourceScene(sceneName)", "撤离确认应限定明确支持的来源场景。");
    AssertContains(sounds, "ExtractionCoveragePolicy.ShouldSuppressMapStinger(", "撤离转场应通过统一规则覆盖未知地图 Stinger。");
    AssertContains(patches, "ExtractionSounds.ShouldSuppressEvacuationStinger(key)", "PlayStringer 补丁应只负责抑制已替换的撤离转场音乐。");
    AssertContains(sounds, "ExtractionBGMConfig.Mode == ExtractionBGMMode.Disabled", "禁用模式应放行游戏撤离音乐。");
    AssertContains(sounds, "return _startedThisRound;", "倒数模式只应在倒数音效实际启动后抑制游戏撤离音乐。");
    AssertContains(sounds, "_lastEvacuationCompletedTime = -1f;", "停止撤离音效时应清除转场抑制窗口。");
    AssertContains(sounds, "StopActive(fadeCountdown: false, clearTransitionState: false)",
        "场景切换 StopBGM 应保留撤离转场窗口，并允许后续撤离确认重新播放音乐。");
    AssertContains(sounds, "_lastHandledEvacuationTime = -1f;", "停止播放后应解除撤离通知去重状态。");
    AssertAtLeast(sounds, "ExtractionBGMConfig.Mode != ExtractionBGMMode.CountdownMode", 2,
        "本次修复应保留倒数开始与逐帧播放的原有模式判断。");
    AssertContains(sounds, "TryStartCountdownSFX();", "本次修复应保留原有五秒倒数音效入口。");
}

void ExtractionCoverageAcceptsEverySupportedSourceScene()
{
    string[] supportedScenes =
    {
        "Level_Farm_Main",
        "Level_GroundZero_Main",
        "Prologue_Main",
        "Level_HiddenWarehouse_Main",
        "Level_Guide_Main",
        "Level_JLab_Main",
        "Level_DemoChallenge_Main",
        "Level_StormZone_Main",
        "Level_ChallengeSnow_Main",
        "Level_SnowMilitaryBase_Main",
        "Level_SnowMilitaryBase_ColdStorage_Main",
        "Level_SurivalChallenge_Main"
    };

    foreach (string sceneName in supportedScenes)
    {
        if (!DuckovCustomSounds.CustomBGM.ExtractionBGM.ExtractionCoveragePolicy.IsSupportedSourceScene(sceneName))
            throw new InvalidOperationException($"撤离覆盖应接受来源场景: {sceneName}");
    }

    if (!DuckovCustomSounds.CustomBGM.ExtractionBGM.ExtractionCoveragePolicy.IsSupportedSourceScene("level_hiddenwarehouse_main"))
        throw new InvalidOperationException("撤离来源场景匹配应忽略大小写。");
}

void ExtractionCoverageRejectsBaseAndUnknownScenes()
{
    string?[] rejectedScenes = { null, "", "Base", "LoadingScreen_Getout", "Level_Future_Main" };

    foreach (string? sceneName in rejectedScenes)
    {
        if (DuckovCustomSounds.CustomBGM.ExtractionBGM.ExtractionCoveragePolicy.IsSupportedSourceScene(sceneName))
            throw new InvalidOperationException($"撤离覆盖应拒绝来源场景: {sceneName ?? "<null>"}");
    }
}

void ExtractionTransitionSuppressesOnlyRecentMapStingers()
{
    const float completedAt = 100f;
    const float windowSeconds = 15f;

    if (!DuckovCustomSounds.CustomBGM.ExtractionBGM.ExtractionCoveragePolicy.ShouldSuppressMapStinger(
            "stg_map_hiddenwarehouse", completedAt, now: 110f, windowSeconds))
        throw new InvalidOperationException("撤离完成后的短期窗口应覆盖未知地图 Stinger。");

    string?[] allowedKeys = { null, "", "stg_map_base", "stg_storm_1", "Music/Stinger/stg_storm_1" };
    foreach (string? key in allowedKeys)
    {
        if (DuckovCustomSounds.CustomBGM.ExtractionBGM.ExtractionCoveragePolicy.ShouldSuppressMapStinger(
                key, completedAt, now: 110f, windowSeconds))
            throw new InvalidOperationException($"撤离转场不应拦截事件: {key ?? "<null>"}");
    }

    if (DuckovCustomSounds.CustomBGM.ExtractionBGM.ExtractionCoveragePolicy.ShouldSuppressMapStinger(
            "stg_map_hiddenwarehouse", completedAt, now: 116f, windowSeconds))
        throw new InvalidOperationException("撤离转场窗口结束后应放行地图 Stinger。");

    if (DuckovCustomSounds.CustomBGM.ExtractionBGM.ExtractionCoveragePolicy.ShouldSuppressMapStinger(
            "stg_map_hiddenwarehouse", completedAt: -1f, now: 1f, windowSeconds))
        throw new InvalidOperationException("尚未撤离时应放行地图 Stinger。");
}

void ExtractionCompletionIgnoresDuplicateNotifications()
{
    const float handledAt = 100f;
    const float windowSeconds = 15f;

    if (!DuckovCustomSounds.CustomBGM.ExtractionBGM.ExtractionCoveragePolicy.IsDuplicateCompletion(
            "Level_HiddenWarehouse_Main", "Level_HiddenWarehouse_Main", handledAt, now: 110f, windowSeconds))
        throw new InvalidOperationException("同一来源场景的短期重复撤离通知应被忽略。");

    if (DuckovCustomSounds.CustomBGM.ExtractionBGM.ExtractionCoveragePolicy.IsDuplicateCompletion(
            "Level_Farm_Main", "Level_HiddenWarehouse_Main", handledAt, now: 110f, windowSeconds))
        throw new InvalidOperationException("不同来源场景的撤离通知应正常处理。");

    if (DuckovCustomSounds.CustomBGM.ExtractionBGM.ExtractionCoveragePolicy.IsDuplicateCompletion(
            "Level_HiddenWarehouse_Main", "Level_HiddenWarehouse_Main", handledAt, now: 116f, windowSeconds))
        throw new InvalidOperationException("去重窗口结束后应允许新的撤离通知。");
}

void BgmAudioFileResolverSupportsCachedFlacLookup()
{
    var helper = Read("CustomBGM/Core/AudioFileExtensions.cs");
    var home = Read("CustomBGM/HomeBGM/HomeBGMManager.cs");
    var patches = Read("CustomBGM/CustomBGM_Patches.cs");
    var extraction = Read("CustomBGM/ExtractionBGM/ExtractionSounds.cs");

    AssertContains(helper, "AudioFileExtensions", "BGM 应提供统一音频扩展名工具。");
    AssertContains(helper, "GetMusicFiles", "音乐列表扫描应由统一工具负责。");
    AssertContains(helper, "FindMusicFile", "按基名查找音乐文件应由统一工具负责。");
    AssertContains(helper, "DirectoryIndex", "统一工具应缓存目录索引，避免每次按扩展名重复访问磁盘。");
    AssertContains(home, "AudioFileExtensions.FindMusicFile(titleDir, \"title\")",
        "TitleBGM 应通过统一工具查找 title.*。");
    AssertContains(home, "AudioFileExtensions.GetMusicFiles(homeBGMPath)",
        "HomeBGM 应通过统一工具扫描所有支持格式。");
    AssertContains(home, "AudioFileExtensions.FindMusicFile(titleDir, \"startFX\")",
        "startFX 应通过统一工具查找所有支持格式。");
    AssertContains(patches, "AudioFileExtensions.FindMusicFile(titleDir, \"start\")",
        "start stinger 应通过统一工具查找所有支持格式。");
    AssertContains(patches, "AudioFileExtensions.FindMusicFile(titleDir, \"death\")",
        "death stinger 应通过统一工具查找所有支持格式。");
    AssertContains(extraction, "AudioFileExtensions.FindFirstMusicFile(extractionDir, \"countdown\", \"extraction\")",
        "撤离倒计时应通过统一工具按基名查找所有支持格式。");
    AssertContains(extraction, "AudioFileExtensions.FindFirstMusicFile(extractionDir, \"success\")",
        "撤离成功提示应通过统一工具按基名查找所有支持格式。");

    var tempRoot = Path.Combine(Path.GetTempPath(), "DCS_AudioFileExtensions_" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(tempRoot);
    try
    {
        File.WriteAllText(Path.Combine(tempRoot, "title.flac"), string.Empty);
        File.WriteAllText(Path.Combine(tempRoot, "title.mp3"), string.Empty);
        File.WriteAllText(Path.Combine(tempRoot, "ambient.aiff"), string.Empty);

        var titlePath = AudioFileExtensions.FindMusicFile(tempRoot, "title");
        var missingPath = AudioFileExtensions.FindMusicFile(tempRoot, "missing");
        var files = AudioFileExtensions.GetMusicFiles(tempRoot).Select(Path.GetFileName).OrderBy(x => x).ToArray();

        if (!string.Equals(Path.GetFileName(titlePath), "title.mp3", StringComparison.Ordinal))
            throw new InvalidOperationException("统一工具应按扩展名优先级返回 title.mp3。");

        if (missingPath != null)
            throw new InvalidOperationException("缺失基名应返回 null。");

        if (!files.SequenceEqual(new[] { "ambient.aiff", "title.flac", "title.mp3" }))
            throw new InvalidOperationException("统一工具应扫描所有支持音乐格式。");

        File.WriteAllText(Path.Combine(tempRoot, "late.wav"), string.Empty);
        if (AudioFileExtensions.FindMusicFile(tempRoot, "late") != null)
            throw new InvalidOperationException("目录索引应被复用，运行时新增文件需显式刷新后才参与匹配。");

        AudioFileExtensions.ClearCache(tempRoot);
        if (!string.Equals(Path.GetFileName(AudioFileExtensions.FindMusicFile(tempRoot, "late")), "late.wav", StringComparison.Ordinal))
            throw new InvalidOperationException("清理目录索引后应能读取新增文件。");
    }
    finally
    {
        AudioFileExtensions.ClearCache(tempRoot);
        if (Directory.Exists(tempRoot))
            Directory.Delete(tempRoot, recursive: true);
    }
}

void AmbientInterceptIsEditableThroughModConfig()
{
    var config = Read("CustomBGM/AmbientIntercept/AmbientInterceptConfig.cs");
    var patches = Read("CustomBGM/AmbientIntercept/AmbientIntercept_Patches.cs");
    var scopes = Read("ModConfig/ModConfigScopes.cs");
    var modBehaviour = Read("ModBehaviour.cs");

    AssertContains(scopes, "AmbientIntercept", "环境音拦截应拥有独立 ModConfig 范围。");
    AssertContains(config, "private static readonly ModConfigScope Scope = ModConfigScopes.AmbientIntercept",
        "环境音拦截配置应使用独立 Scope。");
    AssertContains(config, "public static bool Enabled", "环境音拦截配置应暴露运行时开关。");
    AssertContains(config, "public static bool InterceptStormStingers", "环境音拦截配置应暴露风暴阶段提示音独立开关。");
    AssertContains(config, "SafeAddBoolDropdownList(Scope, \"enabled\"", "环境音拦截 ModConfig UI 应包含启用开关。");
    AssertContains(config, "SafeAddBoolDropdownList(Scope, \"interceptStormStingers\"", "环境音拦截 ModConfig UI 应包含风暴阶段提示音开关。");
    AssertContains(config, "SafeLoad(Scope, \"enabled\"", "环境音拦截应从 ModConfig 读取启用状态。");
    AssertContains(config, "SafeLoad(Scope, \"interceptStormStingers\"", "环境音拦截应从 ModConfig 读取风暴阶段提示音启用状态。");
    AssertContains(config, "ModConfigAPI.IsKeyForMod(key, Scope)", "环境音拦截变更回调应过滤其他模块键。");
    AssertContains(modBehaviour, "AmbientInterceptConfig.Initialize()", "ModBehaviour.OnEnable 应初始化环境音拦截 ModConfig UI。");
    AssertContains(patches, "AmbientInterceptConfig.Enabled", "环境音拦截补丁应读取支持 ModConfig 热更新的配置类。");
    AssertContains(patches, "AmbientInterceptConfig.InterceptStormStingers", "环境音拦截补丁应读取风暴阶段提示音独立开关。");
    AssertContains(patches, "IsStormStinger(eventName)", "风暴阶段提示音判断应集中封装。");
    AssertContains(patches, "Music/Stinger/stg_storm_1", "风暴第一阶段提示音应在独立开关控制范围内。");
    AssertContains(patches, "Music/Stinger/stg_storm_2", "风暴第二阶段提示音应在独立开关控制范围内。");
    AssertDoesNotContain(patches, "ModSettings.EnableAmbientIntercept", "环境音拦截补丁不应只读取 settings.json 静态值。");
}

void AmbientInterceptDocsDescribeGameSourceBehavior()
{
    var zh = Read("docs/advanced/ambient-intercept.md");
    var en = Read("docs/en/advanced/ambient-intercept.md");
    var zhOverview = Read("docs/modules/bgm/overview.md");
    var enOverview = Read("docs/en/modules/bgm/overview.md");

    AssertContains(zh, "ModConfig", "中文高级文档应说明 ModConfig 入口。");
    AssertContains(zh, "settings.json", "中文高级文档应保留 settings.json 回退说明。");
    AssertContains(zh, "AudioManager", "中文高级文档应说明依据游戏源码核对的播放入口。");
    AssertContains(zh, "WeatherFxControl", "中文高级文档应说明雨声来源。");
    AssertContains(zh, "TimeOfDayController", "中文高级文档应说明风暴阶段提示音不属于 Amb/amb_*。");
    AssertContains(zh, "拦截风暴阶段提示音（实验性）", "中文高级文档应说明风暴阶段提示音独立开关。");
    AssertDoesNotContain(zh, "不提供 ModConfig UI 入口", "中文高级文档不应继续声称没有 ModConfig UI。");

    AssertContains(en, "ModConfig", "英文高级文档应说明 ModConfig 入口。");
    AssertContains(en, "settings.json", "英文高级文档应保留 settings.json fallback。");
    AssertContains(en, "AudioManager", "英文高级文档应说明依据游戏源码核对的播放入口。");
    AssertContains(en, "WeatherFxControl", "英文高级文档应说明雨声来源。");
    AssertContains(en, "TimeOfDayController", "英文高级文档应说明 storm phase stingers are outside Amb/amb_*。");
    AssertContains(en, "Intercept Storm Phase Stingers (Experimental)", "英文高级文档应说明风暴阶段提示音独立开关。");
    AssertDoesNotContain(en, "has no ModConfig UI entry", "英文高级文档不应继续声称没有 ModConfig UI。");

    AssertDoesNotContain(zhOverview, "不支持 ModConfig UI 控制", "中文 BGM 总览不应保留旧的 ModConfig 说明。");
    AssertContains(zhOverview, "拦截风暴阶段提示音", "中文 BGM 总览应说明风暴阶段提示音独立开关。");
    AssertDoesNotContain(enOverview, "not exposed in ModConfig UI", "英文 BGM 总览不应保留旧的 ModConfig 说明。");
    AssertContains(enOverview, "Intercept Storm Phase Stingers", "英文 BGM 总览应说明风暴阶段提示音独立开关。");

    const string audioManagerPath = "DuckovAPIs-Mod引用库/TeamSoda.Duckov.Core/Duckov/AudioManager.cs";
    const string weatherFxPath = "DuckovAPIs-Mod引用库/TeamSoda.Duckov.Core/WeatherFxControl.cs";
    const string timeOfDayPath = "DuckovAPIs-Mod引用库/TeamSoda.Duckov.Core/TimeOfDayController.cs";
    bool hasLocalOfficialSource = SourceFileExists(audioManagerPath)
        || SourceFileExists(weatherFxPath)
        || SourceFileExists(timeOfDayPath);

    if (!hasLocalOfficialSource)
        return;

    var audioManager = Read(audioManagerPath);
    var weatherFx = Read(weatherFxPath);
    var timeOfDay = Read(timeOfDayPath);

    AssertContains(audioManager, "private const string path_ambient_fmt_soundkey = \"Amb/amb_{soundkey}\"",
        "游戏源码应继续通过 Amb/amb_{soundkey} 生成场景环境音事件。");
    AssertContains(audioManager, "this.ambientSource.Post(\"Amb/amb_{soundkey}\".Format",
        "游戏源码应继续通过 ambientSource.Post 播放场景环境音。");
    AssertContains(weatherFx, "public string rainSoundKey = \"Amb/amb_rain\"",
        "游戏源码中的天气雨声应继续使用 Amb/amb_rain。");
    AssertContains(timeOfDay, "private string stormPhaseISoundKey = \"Music/Stinger/stg_storm_1\"",
        "游戏源码中的风暴阶段提示应继续使用 Music/Stinger。");
    AssertContains(timeOfDay, "private string stormPhaseIISoundKey = \"Music/Stinger/stg_storm_2\"",
        "游戏源码中的风暴第二阶段提示应继续使用 Music/Stinger。");
}

void HitAndKillSoundsAreWiredThroughModuleRegistration()
{
    var modBehaviour = Read("ModBehaviour.cs");
    var scopes = Read("ModConfig/ModConfigScopes.cs");
    var logManager = Read("Logging/LogManager.cs");
    var audioPostLogger = Read("Logging/AudioPostLogger_Patches.cs");
    var project = Read("DuckovCustomSounds.csproj");

    AssertContains(modBehaviour, "CustomHitAndKillSounds.CustomHitAndKillSounds.Initialize()",
        "ModBehaviour.OnEnable 应初始化命中与击杀音效模块。");
    AssertContains(modBehaviour, "CustomHitAndKillSounds.CustomHitAndKillSounds.Unload()",
        "ModBehaviour.OnDisable 应卸载命中与击杀音效模块。");
    AssertContains(scopes, "DCSHitAndKill | 命中与击杀音效",
        "命中与击杀音效应拥有独立 ModConfig 范围。");
    AssertContains(logManager, "[\"CustomHitAndKillSounds\"] = \"HitAndKill\"",
        "日志系统应将新模块名映射到 HitAndKill 日志模块。");
    AssertContains(audioPostLogger, "SFX/Combat/Marker/",
        "AudioPostLogger 应将原版命中与击杀提示音归到 HitAndKill 模块。");
    AssertContains(project, "<Compile Remove=\"DuckovCustomSounds.Tests/**/*.cs\" />",
        "主项目应继续排除本地测试目录。");
    AssertDoesNotContain(project, "<Compile Remove=\"Tests/**/*.cs\" />",
        "主项目不应保留临时 Tests 目录排除项。");
}

void HitAndKillEventMappingCoversMarkerAndSimpleHealthCases()
{
    var classifier = Read("CustomHitAndKillSounds/HitAndKillEventClassifier.cs");
    var runtimeHooks = Read("CustomHitAndKillSounds/HitAndKillRuntimeHooks.cs");
    var audioPatch = Read("CustomHitAndKillSounds/HitAndKillAudioPost_Patches.cs");
    var resolver = Read("CustomHitAndKillSounds/HitAndKillSoundResolver.cs");

    AssertContains(classifier, "SFX/Combat/Marker/", "命中与击杀分类器应识别原版 Marker 事件前缀。");
    AssertContains(classifier, "case \"hitmarker_head\":", "命中与击杀分类器应识别暴击命中提示音。");
    AssertContains(classifier, "return damageInfo.crit > 0 ? \"hitmarker_head\" : \"hitmarker\";",
        "简单生命值目标命中补齐应能区分普通命中和暴击命中。");
    AssertContains(classifier, "return damageInfo.crit > 0 ? \"killmarker_head\" : \"killmarker\";",
        "击杀语义应能区分普通击杀和暴击击杀。");
    AssertContains(runtimeHooks, "HealthSimpleBase.OnSimpleHealthHit += OnSimpleHealthHit",
        "简单生命值目标命中事件应被订阅。");
    AssertContains(runtimeHooks, "PlaySemantic(HitAndKillEventClassifier.GetHitKey(damageInfo), 0, null, \"hitmarker\", HitAndKillConfig.MarkerCooldownMs)",
        "简单生命值目标应补齐原版不会播放的命中提示音。");
    AssertContains(runtimeHooks, "PlaySemantic(HitAndKillEventClassifier.GetNpcHurtKey(damageInfo), ownerId, go, \"default_hit\", HitAndKillConfig.HurtCooldownMs)",
        "玩家命中 NPC 时应支持目标受击音效。");
    AssertContains(audioPatch, "HitAndKillSoundPlayer.SuppressOriginal(__result);",
        "Marker 自定义文件存在时应抑制原版提示音。");
    AssertContains(resolver, "\".mp3\", \".wav\", \".ogg\", \".oga\"",
        "命中与击杀音效解析器应覆盖现有 SFX 模块支持的格式。");
    AssertContains(resolver, "TryPickVariantStrict",
        "命中与击杀音效解析器应支持严格编号变体。");
}

void HitAndKillDocsDescribeReflectionBackedCoverage()
{
    var zh = Read("docs/modules/hit-and-kill.md");
    var en = Read("docs/en/modules/hit-and-kill.md");
    var moduleReadme = Read("CustomHitAndKillSounds/README.md");

    AssertContains(zh, "Health.OnHurt", "中文文档应说明受击音效的运行时事件来源。");
    AssertContains(zh, "反射诊断", "中文文档应说明反射诊断用于补充覆盖。");
    AssertContains(en, "Reflection diagnostics", "英文文档应说明反射诊断。");
    AssertContains(moduleReadme, "Unity 序列化资源中的部分音频字段无法仅靠反编译 C# 全量确认",
        "模块 README 应明确说明需要依赖反射补充覆盖。");
}

string Read(string relativePath)
{
    var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
    if (!File.Exists(path))
        throw new FileNotFoundException($"文件不存在: {relativePath}");

    return File.ReadAllText(path);
}

bool SourceFileExists(string relativePath)
{
    var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
    return File.Exists(path);
}

void AssertContains(string text, string expected, string message)
{
    if (!text.Contains(expected, StringComparison.Ordinal))
        throw new InvalidOperationException(message);
}

void AssertDoesNotContain(string text, string unexpected, string message)
{
    if (text.Contains(unexpected, StringComparison.Ordinal))
        throw new InvalidOperationException(message);
}

void AssertAtLeast(string text, string pattern, int minimum, string message)
{
    var count = Regex.Matches(text, Regex.Escape(pattern)).Count;
    if (count < minimum)
        throw new InvalidOperationException($"{message} 当前数量: {count}, 期望至少: {minimum}。");
}

void AssertPathEquals(string expected, string? actual, string message)
{
    if (!string.Equals(Path.GetFullPath(expected), actual == null ? null : Path.GetFullPath(actual), StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException($"{message} 当前路径: {actual ?? "<null>"}，期望路径: {expected}。");
}

string FindRepositoryRoot()
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());

    while (dir != null)
    {
        if (File.Exists(Path.Combine(dir.FullName, "DuckovCustomSounds.csproj")))
            return dir.FullName;

        dir = dir.Parent;
    }

    throw new DirectoryNotFoundException("无法定位仓库根目录。");
}

sealed class SceneBgmFixture : IDisposable
{
    private readonly string previousModFolderName;
    private readonly string root;
    private readonly string enterFolder;

    public SceneBgmFixture()
    {
        previousModFolderName = ModBehaviour.ModFolderName;
        root = Path.Combine(Path.GetTempPath(), "DCS_SceneBGM_" + Guid.NewGuid().ToString("N"));
        var packFolder = Path.Combine(root, "Pack");
        enterFolder = Path.Combine(packFolder, "SceneBGM", "Enter");
        Directory.CreateDirectory(enterFolder);
        Directory.CreateDirectory(Path.Combine(packFolder, "SceneBGM", "Loop"));

        ModBehaviour.ModFolderName = packFolder;
        AudioFileExtensions.ClearCache();
        SceneMusicResolver.ClearCache();
        SceneMusicResolver.Initialize();
    }

    public string AddEnterMusic(string fileName)
    {
        var path = Path.Combine(enterFolder, fileName);
        File.WriteAllText(path, string.Empty);
        AudioFileExtensions.ClearCache(enterFolder);
        SceneMusicResolver.ClearCache();
        SceneMusicResolver.Initialize();
        return path;
    }

    public void Dispose()
    {
        SceneMusicResolver.ClearCache();
        AudioFileExtensions.ClearCache();
        ModBehaviour.ModFolderName = previousModFolderName;

        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
}
