---
title: 脚步声
---

# 脚步声

替换角色（玩家、敌人、NPC）的脚步声和冲刺音效。共用敌人语音的规则引擎。

## 快速开始

```
CustomFootStepSounds/
├── footstep_voice_rule.json    # 首次启动自动生成
├── Scav/
│   ├── normal_scav_footstep_walk_light.mp3
│   ├── normal_scav_footstep_run_heavy.mp3
│   └── normal_scav_dash.mp3
└── Player/
    ├── normal_duck_footstep_walk_light.mp3
    ├── normal_duck_footstep_run_heavy.mp3
    └── normal_duck_dash.mp3
```

首次运行自动生成 `footstep_voice_rule.json`（默认含 Player、Scav、Usec 的简化规则）。**注意：配置文件名必须是 `footstep_voice_rule.json`，不是 `footsteps.json`。**

## soundKey 说明

系统根据角色动作自动生成 soundKey，按优先级依次尝试：

**脚步声**：优先 `footstep_{move}_{strength}_{material}` → 回退 `footstep_{move}_{strength}`

**冲刺**：优先 `dash_{material}` → 回退 `dash`

| 变量 | 取值 |
|------|------|
| `{move}` | `walk`（行走）/ `run`（奔跑） |
| `{strength}` | `light`（轻）/ `heavy`（重） |
| `{material}` | 游戏 `FootStepMaterialType` 枚举：`organic`（有机/人形）、`mech`（机械）、`danger`（危险）、`nosound`（无声，不触发自定义）、`horse`（马） |

示例：`footstep_walk_light_organic`、`footstep_run_heavy_mech`、`dash_organic`

## 文件命名（简化规则，默认）

格式：`{FilePattern}/{iconPrefix}_{voiceType}_{soundKey}{ext}`

| 变量 | 说明 |
|------|------|
| `{iconPrefix}` | 规则中指定的 IconType（默认规则为空 → 恒为 `normal`；要 `elite`/`boss` 需在 SimpleRules 显式指定 IconType） |
| `{voiceType}` | 有 NameKey 时从 NameKey 提取（如 `Cname_Scav` → `Scav`，大小写按 NameKey 原样）。没有时用游戏 `VoiceType` 枚举：`Duck`、`Robot`、`Wolf`、`Chicken`、`Crow`、`Eagle`、`coalball` |
| `{soundKey}` | 上文所述 |
| `{ext}` | `.mp3`、`.wav` 等（按 `PreferredExtensions` 顺序） |

推荐：`Scav/normal_scav_footstep_walk_light.mp3`

**注意**：Scav 的 voiceType 是 `Scav` 不是 `Duck`（VoiceType 枚举是 `Duck`）。系统优先使用从 NameKey 提取的值；Windows 大小写不敏感，用小写文件名也能命中。

## footstep_voice_rule.json

> 示例为简化版；首次生成的文件还含 `_comment` 注释字段与空的 `Rules` 数组。

```json
{
  "Debug": { "Enabled": true, "Level": "Debug", "ValidateFileExists": true },
  "Fallback": { "UseOriginalWhenMissing": true, "PreferredExtensions": [".mp3", ".wav"] },
  "DefaultPattern": "CustomFootStepSounds/{team}/{rank}_{voiceType}_{soundKey}{ext}",
  "MinCooldownSeconds": 0.3,
  "UseSimpleRules": true,
  "SimpleRules": [
    { "Team": "player", "FilePattern": "CustomFootStepSounds/Player" },
    { "NameKey": "Cname_Scav", "FilePattern": "CustomFootStepSounds/Scav" },
    { "NameKey": "Cname_Usec", "FilePattern": "CustomFootStepSounds/Usec" }
  ],
  "PriorityInterruptEnabled": false,
  "BindVariantIndexPerEnemy": false
}
```

### 配置项

| 字段 | 说明 | 默认 |
|------|------|------|
| `Debug.Level` | `Error` / `Warning` / `Info` / `Debug` / `Verbose` | Debug（首次生成文件） |
| `Fallback.UseOriginalWhenMissing` | 没找到文件时用原版（预留项：当前未命中时始终保留原声，改此值不影响行为） | true |
| `Fallback.PreferredExtensions` | 扩展名顺序，可加 `.ogg`、`.flac` | [".mp3",".wav"] |
| `MinCooldownSeconds` | 同一角色最短触发间隔，避免音效重叠 | 0.3（0.05~2.0） |
| `UseSimpleRules` | 用简化规则 | true |
| `SimpleRules[].NameKey` | 敌人标识，如 `Cname_Scav` | - |
| `SimpleRules[].Team` | NameKey 为空时按队伍匹配（`player`/`scav`/`pmc`） | - |
| `SimpleRules[].FilePattern` | 目录路径 | - |
| `BindVariantIndexPerEnemy` | 同一敌人固定用同一变体 | false |

### 变体

加 `_1`、`_2` 后缀（必须连续）：
```
Scav/normal_scav_footstep_walk_light.mp3     # 变体 0
Scav/normal_scav_footstep_walk_light_1.mp3   # 变体 1
```

## ModConfig 设置

| 设置 | 默认 | 范围 |
|------|------|------|
| 启用自定义脚步音效 | 开 | 开/关 |
| 音量倍率 | 1.0 | 0~2 |

## settings.json

```json
{
  "enableCustomFootStepSounds": true,
  "footstepVolumeScale": 1.0
}
```

## 常见问题

**放了文件还播原声**：开 Debug 日志，在 player.log 搜 `[CFS:Route]`。确认文件名中 voiceType 正确（Scav → `Scav`，不是 `Duck`）。查看 `[CFS:Path]` 路径日志需将 Footstep 模块日志级别设为 Verbose（在 `settings.json` 的 `logging.modules.Footstep.level` 或 `footstep_voice_rule.json` 的 `Debug.Level` 设置）；搜 `[CFS:Route]` 只需 Debug 级。

**脚步声频繁被打断/缺失**：调 `MinCooldownSeconds`（被打断就调大，缺失就调小）。

**不同材质不同音效**：文件名中加 `_organic`、`_mech`、`_danger`、`_horse`。系统优先匹配含材质的文件。

**禁用某角色**：从 SimpleRules 删对应规则。

**日志关键字**：player.log 搜 `[CFS]`、`[CFS:Route]`、`[CFS:Path]`、`[CFS:Cooldown]`。
