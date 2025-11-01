---
title: Boss BGM
---

# Boss BGM

为特定 Boss 提供专属 BGM。进入触发距离内自动淡入，离开或死亡时平滑淡出；多 Boss 并存时按“最近/更高优先级”规则选择。

## 目录与命名

```
DuckovCustomSounds/
└─ BossBGM/
   ├─ default_boss.mp3      # 兜底
   ├─ BALeader.mp3          # Cname_BALeader → BALeader.mp3
   ├─ Boss_Sniper.ogg       # Cname_Boss_Sniper → Boss_Sniper.ogg
   └─ ServerGuardian.flac   # Cname_ServerGuardian → ServerGuardian.flac
```

- 将敌人 `NameKey` 去掉前缀 `Cname_` 作为文件名；未命中时退回 `default_boss.*`。
- 支持 `.mp3/.wav/.ogg/.flac`。

## 配置（ModConfig）

- 启用 Boss BGM
- 触发距离（米）
- （其余高级参数见下）

## 高级（config.json）

位置：`DuckovCustomSounds/BossBGM/config.json`。首次运行会自动生成。

```json
{
  "fadeDuration": 2.0,
  "updateInterval": 0.1,
  "managerUpdateInterval": 0.5,
  "minSwitchIntervalSeconds": 2.0,
  "minDistanceDeltaToSwitch": 5.0,
  "resumePlaybackEnabled": true,
  "delayedStopEnabled": true,
  "delayedStopSeconds": 1.0,
  "bossDeathFadeOutSeconds": 3.0
}
```

- `fadeDuration`：淡入/淡出时间（秒）
- `updateInterval`：单体更新频率（秒）
- `managerUpdateInterval`：管理器更新频率（秒）
- `minSwitchIntervalSeconds`：切换抖动保护（秒）
- `minDistanceDeltaToSwitch`：切换所需最小距离变化量（米）
- `resumePlaybackEnabled`：从暂停处恢复而非重头播放
- `delayedStopEnabled`/`delayedStopSeconds`：离开后延时停止（避免边界反复进出）
- `bossDeathFadeOutSeconds`：目标死亡后的淡出时间

## 优先级与协同

- Boss BGM 的优先级高于场景与标题/主页 BGM。
- 多 Boss 并存时，仅保留优先目标的 BGM；切换时应用防抖与淡入淡出。

## 原理与实现

- 基于敌人 `NameKey` 与距离监控的管理器模型，周期性评估优先目标并进行切换。
- 进入范围触发淡入；离开、死亡或场景切换时淡出并释放实例。

