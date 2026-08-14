---
title: Boss BGM
---

# Boss BGM

为特定 Boss 播放专属 BGM。进入触发距离内自动淡入，离开或死亡时平滑淡出。多 Boss 并存时选择触发距离内最近的 Boss（切换有防抖与距离优势阈值）。

## 目录与命名

```
BossBGM/
├─ default_boss.mp3    # 默认回退
├─ BALeader.mp3        # Cname_BALeader → BALeader.mp3
├─ Boss_Sniper.ogg     # Cname_Boss_Sniper → Boss_Sniper.ogg
└─ ServerGuardian.flac # Cname_ServerGuardian → ServerGuardian.flac
```

- 文件名 = 敌人 NameKey 去掉 `Cname_` 前缀。
- 找不到专属文件时用 `default_boss.*`；再没有就不播。
- 支持 `.mp3`/`.wav`/`.ogg`/`.flac` 及所有 `AudioFileExtensions` 支持的格式。

## ModConfig 设置

| 设置 | 默认 | 范围 |
|------|------|------|
| 启用首领音乐 | 开 | 开/关 |
| 触发距离 | 40 米 | 10-200 米 |
| 音量 | 70% | 0-100% |

## 高级配置（config.json）

文件位置：`BossBGM/config.json`。首次运行自动生成。

```json
{
  "volume": 0.7,
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

| 参数 | 说明 |
|------|------|
| `volume` | 音量（0~1） |
| `fadeDuration` | 淡入/淡出时间（秒） |
| `updateInterval` | 单体更新频率（秒） |
| `managerUpdateInterval` | 管理器更新频率（秒） |
| `minSwitchIntervalSeconds` | 切换防抖间隔（秒） |
| `minDistanceDeltaToSwitch` | 切换最小距离变化（米） |
| `resumePlaybackEnabled` | 是否从暂停处恢复播放 |
| `delayedStopEnabled` | 预留参数，当前代码未使用 |
| `delayedStopSeconds` | 预留参数，当前代码未使用 |
| `bossDeathFadeOutSeconds` | Boss 死亡后淡出时间（秒） |

## 优先级

Boss 进入触发距离内时，Boss BGM 优先级高于场景 BGM 和标题/主页 BGM；Boss 离开触发距离、被注销后，场景 BGM 自动恢复——若场景循环 BGM 在 Boss→Boss 交叉淡出期间被意外停止，解除压制时也会自动重建（自愈）。多 Boss 时只播一个，切换有防抖和淡入淡出处理。

## 附录：BOSS NameKey → 文件名对照表

文件名 = NameKey 去掉 `Cname_` 前缀，支持 `AudioFileExtensions` 全部格式（`.mp3`/`.wav`/`.ogg`/`.oga`/`.flac`/`.aif`/`.aiff`/`.mp2`/`.m4a`/`.mp4`/`.wma`/`.asf`/`.fsb`/`.it`/`.mid`/`.midi`/`.mod`/`.s3m`/`.xm`）。

| NameKey | 中文名 | 文件名 |
|---------|--------|--------|
| `Cname_BALeader` | BA队长 | `BALeader.{ext}` |
| `Cname_Boss_Sniper` | 劳登 | `Boss_Sniper.{ext}` |
| `Cname_Boss_Shot` | 喷子 | `Boss_Shot.{ext}` |
| `Cname_ServerGuardian` | 矿长 | `ServerGuardian.{ext}` |
| `Cname_Speedy` | 急速团长 | `Speedy.{ext}` |
| `Cname_Boss_Fly` | 蝇蝇队长 | `Boss_Fly.{ext}` |
| `Cname_Boss_Arcade` | 暴走街机 | `Boss_Arcade.{ext}` |
| `Cname_Boss_3Shot` | 三枪哥 | `Boss_3Shot.{ext}` |
| `Cname_Prison_Boss` | 典狱长 | `Prison_Boss.{ext}` |
| `Cname_StormBoss1` | 噗咙噗咙 | `StormBoss1.{ext}` |
| `Cname_StormBoss2` | 咕噜咕噜 | `StormBoss2.{ext}` |
| `Cname_StormBoss3` | 啪啦啪啦 | `StormBoss3.{ext}` |
| `Cname_StormBoss4` | 比利比利 | `StormBoss4.{ext}` |
| `Cname_StormBoss5` | 口口口口 | `StormBoss5.{ext}` |
| `Cname_ShortEagle` | 矮鸭 | `ShortEagle.{ext}` |
| `Cname_UltraMan` | 光之男 | `UltraMan.{ext}` |
| `Cname_CrazyRob` | 失控机械蜘蛛 | `CrazyRob.{ext}` |
| `Cname_Vida` | 维达 | `Vida.{ext}` |

未列出的 BOSS 或找不到专属文件时，回退到 `default_boss.{ext}`。
