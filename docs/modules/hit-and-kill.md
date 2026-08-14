---
title: 命中与击杀音效
---

# 命中与击杀音效

替换命中提示、击杀提示，并补充玩家受击与 NPC 受击音效。

## 快速开始

放置默认文件：

```text
CustomHitAndKillSounds/
├── hitmarker.mp3
├── hitmarker_head.mp3
├── killmarker.mp3
├── killmarker_head.mp3
├── player_hurt.mp3
└── npc_hurt.mp3
```

## 文件名

| 文件 | 触发场景 |
|------|----------|
| `hitmarker.*` | 玩家命中目标 |
| `hitmarker_head.*` | 玩家暴击命中目标 |
| `killmarker.*` | 玩家击杀目标 |
| `killmarker_head.*` | 玩家暴击击杀目标 |
| `player_hurt.*` | 玩家受击 |
| `player_hurt_crit.*` | 玩家被暴击 |
| `npc_hurt.*` | 玩家命中 NPC |
| `npc_hurt_crit.*` | 玩家暴击命中 NPC |

支持格式：`.mp3`、`.wav`、`.ogg`、`.oga`。

## 变体

同一个文件可以添加 `_1`、`_2` 等任意正整数后缀随机选择：

```text
CustomHitAndKillSounds/
├── hitmarker.mp3
├── hitmarker_1.mp3
├── hitmarker_2.mp3
└── player_hurt_1.mp3
```

## 覆盖机制

命中和击杀提示音优先拦截原版事件：

```text
SFX/Combat/Marker/hitmarker
SFX/Combat/Marker/hitmarker_head
SFX/Combat/Marker/killmarker
SFX/Combat/Marker/killmarker_head
```

受击音效通过运行时事件补充。模块订阅 `Health.OnHurt`、`Health.OnDead`、`HealthSimpleBase.OnSimpleHealthHit` 和 `HealthSimpleBase.OnSimpleHealthDead`，再根据 `DamageInfo` 判断玩家受击、NPC 受击、暴击和击杀。

游戏中的部分音频事件来自 Unity 序列化资源，反编译源码只能看到字段名，无法完整看到字段值。模块提供反射诊断能力，用于记录 `critSfx`、`nonCritSfx`、`hurtSfx`、`soundKey` 等常见字段，后续可依据日志补充覆盖范围。

## ModConfig

| 设置 | 默认 |
|------|------|
| 启用命中与击杀音效 | 开 |
| 替换命中/击杀提示音 | 开 |
| 播放受击音效 | 开 |
| 启用反射诊断日志 | 关 |
| 音量倍率 | 1.0（0~2） |
| 提示音冷却毫秒 | 30（0~500） |
| 受击音效冷却毫秒 | 120（0~1000） |

## 日志

在 `settings.json` 中设置：

```json
{
  "logging": {
    "modules": {
      "HitAndKill": { "level": "Verbose" }
    }
  }
}
```

> `enableAudioPostLogger` 的跟踪日志是 Verbose 级，设 `Debug` 会被过滤，排查时请用 `Verbose`。

排查未知受击事件时，可同时开启 `enableAudioPostLogger`（settings.json 顶层键，默认关闭）和 `reflectionDiagnostics`（ModConfig 中的"启用反射诊断日志"）。
