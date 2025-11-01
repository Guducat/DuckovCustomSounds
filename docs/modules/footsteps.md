---
title: 脚步声
---

# 脚步声

本模块用于替换角色的脚步声与冲刺音效，支持按角色类型、材质类型、移动方式等条件进行精准匹配。模块复用了敌人语音系统的规则引擎，提供灵活的配置方式，同时保持与原版 FMOD 3D 音效的距离衰减一致性。

## 功能概述

- 替换所有角色（玩家、敌人、NPC）的脚步声与冲刺音效
- 支持按移动方式（行走/奔跑）、强度（轻/重）、材质类型（有机/机械/危险）区分
- 自动 3D 定位与距离衰减，与原版音效保持一致
- 支持同名变体随机播放
- 独立的音量控制与冷却时间设置
- 与敌人语音系统共享规则引擎，配置方式一致

## 快速开始

### 最小可用示例

假设你想为 Scav 敌人替换脚步声，只需：

1. 创建目录结构：
```
DuckovCustomSounds/
└─ CustomFootStepSounds/
   └─ Scav/
      ├─ normal_scav_footstep_walk_light.mp3
      ├─ normal_scav_footstep_run_heavy.mp3
      └─ normal_scav_dash.mp3
```

2. 首次运行游戏后，会自动生成配置文件 `DuckovCustomSounds/CustomFootStepSounds/footstep_voice_rule.json`，默认已包含 Scav 的简化规则。

3. 进入游戏测试，靠近 Scav 敌人观察脚步声是否替换成功。

### 为玩家角色添加脚步声

玩家角色的 NameKey 为空，需要通过 Team 匹配：

```
DuckovCustomSounds/
└─ CustomFootStepSounds/
   └─ Player/
      ├─ normal_duck_footstep_walk_light.mp3
      ├─ normal_duck_footstep_run_heavy.mp3
      └─ normal_duck_dash.mp3
```

默认配置已包含 `Team: "player"` 的规则，无需额外配置。

## soundKey 说明

模块会根据角色的动作与材质类型自动生成 soundKey，并按优先级依次尝试匹配：

### 脚步声 soundKey

- **优先匹配**：`footstep_{move}_{strength}_{material}`
- **回退匹配**：`footstep_{move}_{strength}`

其中：
- `{move}`：移动方式
  - `walk`：行走
  - `run`：奔跑
- `{strength}`：强度
  - `light`：轻（对应游戏中的 walkLight / runLight）
  - `heavy`：重（对应游戏中的 walkHeavy / runHeavy）
- `{material}`：材质类型
  - `organic`：有机材质（大部分人形角色）
  - `mech`：机械材质（机器人等）
  - `danger`：危险材质（特殊敌人）
  - `nosound`：无声材质（不会触发自定义）

**示例**：
- `footstep_walk_light_organic`：有机材质的轻型行走
- `footstep_run_heavy_mech`：机械材质的重型奔跑
- `footstep_walk_light`：通用轻型行走（未指定材质时的回退）

### 冲刺音效 soundKey

- **优先匹配**：`dash_{material}`
- **回退匹配**：`dash`

**示例**：
- `dash_organic`：有机材质的冲刺
- `dash_mech`：机械材质的冲刺
- `dash`：通用冲刺（未指定材质时的回退）

## 文件命名规则

### 简化规则模式（推荐）

默认启用简化规则模式（`UseSimpleRules: true`），文件命名遵循：

```
{FilePattern}/{iconPrefix}_{voiceType}_{soundKey}{ext}
```

- `{FilePattern}`：在 SimpleRules 中为每个角色指定的目录路径
- `{iconPrefix}`：角色等级前缀
  - `normal`：普通敌人（默认）
  - `elite`：精英敌人
  - `boss`：Boss 敌人
- `{voiceType}`：语音类型
  - 对于有 NameKey 的敌人（如 `Cname_Scav`），会优先使用从 NameKey 提取的类型名（如 `scav`）
  - 对于玩家或无 NameKey 的角色，使用原始 VoiceType（如 `duck`、`robot`）
- `{soundKey}`：上文所述的 soundKey
- `{ext}`：扩展名（`.mp3` 或 `.wav`，按配置中的 PreferredExtensions 顺序尝试）

**推荐命名示例**（Scav）：
```
CustomFootStepSounds/Scav/normal_scav_footstep_walk_light.mp3
CustomFootStepSounds/Scav/normal_scav_footstep_run_heavy.mp3
CustomFootStepSounds/Scav/normal_scav_dash.mp3
```

**不推荐命名**（会导致匹配失败）：
```
CustomFootStepSounds/Scav/normal_duck_footstep_walk_light.mp3  ❌
```

原因：Scav 的 NameKey 为 `Cname_Scav`，系统会优先尝试 `voiceType=scav` 的文件。

### 复杂规则模式

关闭简化规则（`UseSimpleRules: false`）后，使用默认模板：

```
CustomFootStepSounds/{team}/{rank}_{voiceType}_{soundKey}{ext}
```

- `{team}`：队伍归一化名称（`scav`、`pmc`、`player`、`unknown`）
- `{rank}`：等级（`boss`、`elite`、`normal`）
- `{voiceType}`：语音类型（`duck`、`robot`、`wolf` 等）
- `{soundKey}`：soundKey
- `{ext}`：扩展名

**示例**：
```
CustomFootStepSounds/scav/normal_duck_footstep_walk_light.mp3
CustomFootStepSounds/pmc/elite_robot_footstep_run_heavy.wav
```

## 配置文件说明

配置文件位于：`DuckovCustomSounds/CustomFootStepSounds/footstep_voice_rule.json`

### 完整配置示例

```json
{
  "_comment": "自定义脚步声规则配置",
  "Debug": {
    "_comment": "日志配置",
    "Enabled": true,
    "Level": "Debug",
    "ValidateFileExists": true
  },
  "Fallback": {
    "_comment": "回退策略",
    "UseOriginalWhenMissing": true,
    "PreferredExtensions": [".mp3", ".wav"]
  },
  "DefaultPattern": "CustomFootStepSounds/{team}/{rank}_{voiceType}_{soundKey}{ext}",
  "MinCooldownSeconds": 0.3,
  "UseSimpleRules": true,
  "SimpleRules": [
    {
      "_comment": "玩家角色（nameKey为空）按 team 匹配",
      "Team": "player",
      "IconType": "",
      "FilePattern": "CustomFootStepSounds/Player"
    },
    {
      "_comment": "Scav 敌人",
      "NameKey": "Cname_Scav",
      "IconType": "",
      "FilePattern": "CustomFootStepSounds/Scav"
    },
    {
      "_comment": "USEC 敌人",
      "NameKey": "Cname_Usec",
      "IconType": "",
      "FilePattern": "CustomFootStepSounds/Usec"
    }
  ],
  "PriorityInterruptEnabled": false,
  "BindVariantIndexPerEnemy": false,
  "Rules": []
}
```

### 配置项说明

#### Debug（日志配置）

- `Enabled`（布尔值）：是否启用日志输出
- `Level`（字符串）：日志级别
  - `Error`：仅错误
  - `Warning`：警告及以上
  - `Info`：信息及以上（推荐）
  - `Debug`：调试及以上（排错时使用）
  - `Verbose`：详细输出（包含所有路径尝试）
- `ValidateFileExists`（布尔值）：路由前是否检查文件存在（建议 `true`）

**注意**：详细的路由路径日志（`[CFS:Path]`）仅在 Footstep 和 Enemy 模块同时设置为 Debug 级别时才会输出。

#### Fallback（回退策略）

- `UseOriginalWhenMissing`（布尔值）：未找到自定义文件时是否使用原版音效（建议 `true`）
- `PreferredExtensions`（字符串数组）：扩展名优先级，按顺序尝试（如 `[".mp3", ".wav"]`）

#### MinCooldownSeconds（最小冷却时间）

- 类型：浮点数
- 默认值：`0.3`
- 范围：`0.05` - `2.0`
- 作用：防止同一角色在短时间内重复触发脚步声，避免音效播放不完整或叠加
- 建议值：`0.1` - `0.5`（过大可能导致脚步声缺失）

#### UseSimpleRules（简化规则模式）

- 类型：布尔值
- 默认值：`true`
- 作用：启用简化规则模式，按 NameKey 或 Team 快速匹配目录

#### SimpleRules（简化规则列表）

数组，每个规则包含：
- `Team`（字符串，可选）：队伍名称（如 `player`、`scav`、`pmc`），用于 NameKey 为空的角色
- `NameKey`（字符串，可选）：敌人唯一标识（如 `Cname_Scav`、`Cname_Usec`）
- `IconType`（字符串，可选）：限定图标类型（空字符串表示匹配所有）
- `FilePattern`（字符串）：目录路径前缀（如 `CustomFootStepSounds/Scav`）

**匹配优先级**：
1. 优先匹配 NameKey（如果提供）
2. 如果 NameKey 为空，则匹配 Team
3. 如果提供了 IconType，则必须同时匹配

#### PriorityInterruptEnabled（优先级打断）

- 类型：布尔值
- 默认值：`false`
- 作用：是否允许高优先级音效打断低优先级音效（脚步声通常不需要）

#### BindVariantIndexPerEnemy（变体索引绑定）

- 类型：布尔值
- 默认值：`false`
- 作用：
  - `false`：每次触发随机选择变体
  - `true`：同一敌人实例固定使用同一变体索引

#### Rules（复杂规则列表）

仅在 `UseSimpleRules: false` 时生效。支持更复杂的条件匹配，详见敌人语音模块文档。

## 变体机制

支持为同一 soundKey 提供多个变体文件，系统会自动识别并随机选择：

### 变体命名规则

基础文件名后添加 `_1`、`_2`、`_3` 等后缀（必须连续）：

```
CustomFootStepSounds/Scav/
├─ normal_scav_footstep_walk_light.mp3      # 基础文件（变体 0）
├─ normal_scav_footstep_walk_light_1.mp3    # 变体 1
├─ normal_scav_footstep_walk_light_2.mp3    # 变体 2
└─ normal_scav_footstep_walk_light_3.mp3    # 变体 3
```

系统会在 `[0, 3]` 范围内随机选择一个变体播放。

### 变体绑定

通过配置 `BindVariantIndexPerEnemy` 控制：
- `false`（默认）：每次触发随机选择
- `true`：同一敌人实例始终使用同一变体索引

## ModConfig UI 配置

在游戏内 ModConfig 菜单中可以调整以下选项：

### 启用自定义脚步音效

- 类型：开关
- 默认值：启用
- 作用：总开关，关闭后使用原版脚步声

### 音量 (0~2)

- 类型：滑块
- 默认值：`1.0`（100%）
- 范围：`0.0` - `2.0`（0% - 200%）
- 作用：调整自定义脚步声的音量

**调整建议**：
- 脚步声偏大：设置为 `0.5` - `0.8`
- 脚步声偏小：设置为 `1.2` - `1.5`
- 静音测试：设置为 `0.0`

**注意**：此音量设置仅影响脚步声模块，不影响其他模块（如敌人语音、BGM 等）。

## settings.json 配置

除了 ModConfig UI，也可以直接编辑 `DuckovCustomSounds/settings.json`：

```json
{
  "enableCustomFootStepSounds": true,
  "footstepVolumeScale": 1.0
}
```

- `enableCustomFootStepSounds`：总开关（与 ModConfig UI 同步）
- `footstepVolumeScale`：音量缩放（与 ModConfig UI 同步）

## 目录结构示例

### 完整示例（多角色）

```
DuckovCustomSounds/
└─ CustomFootStepSounds/
   ├─ footstep_voice_rule.json          # 配置文件
   ├─ Player/                           # 玩家角色
   │  ├─ normal_duck_footstep_walk_light.mp3
   │  ├─ normal_duck_footstep_run_heavy.mp3
   │  └─ normal_duck_dash.mp3
   ├─ Scav/                             # Scav 敌人
   │  ├─ normal_scav_footstep_walk_light.mp3
   │  ├─ normal_scav_footstep_walk_light_1.mp3  # 变体
   │  ├─ normal_scav_footstep_run_heavy.mp3
   │  ├─ normal_scav_dash.mp3
   │  └─ elite_scav_footstep_run_heavy.mp3      # 精英 Scav
   └─ Usec/                             # USEC 敌人
      ├─ normal_usec_footstep_walk_light.mp3
      ├─ normal_usec_footstep_run_heavy.mp3
      └─ normal_usec_dash.mp3
```

## 常见问题

### 放置了文件但仍然播放原声？

1. **检查配置文件是否命中**
   - 打开 `footstep_voice_rule.json`，设置 `Debug.Enabled: true` 和 `Debug.Level: "Debug"`
   - 查看游戏日志，搜索 `[CFS:Route]` 关键字
   - 确认是否输出了匹配信息

2. **检查文件路径与命名**
   - 确认文件路径与 SimpleRules 中的 FilePattern 一致
   - 确认文件名中的 voiceType 正确（Scav 应使用 `scav`，不是 `duck`）
   - 确认扩展名在 PreferredExtensions 列表中

3. **检查 NameKey 是否正确**
   - 如果使用 SimpleRules，确认 NameKey 与游戏中的一致（如 `Cname_Scav`）
   - 可以在日志中查看 `[CFS:Route]` 输出的 nameKey 值

### 如何查看详细的路径尝试日志？

需要同时满足以下条件：
1. `footstep_voice_rule.json` 中 `Debug.Level` 设置为 `Debug`
2. `DuckovCustomSounds/settings.json` 中 `logging.modules.CustomEnemySounds.level` 也设置为 `Debug`

然后在日志中搜索 `[CFS:Path]` 关键字，可以看到所有尝试的路径。

### 脚步声播放不完整或被打断？

调整 `MinCooldownSeconds` 参数：
- 如果脚步声经常被打断，增大此值（如 `0.5`）
- 如果脚步声缺失，减小此值（如 `0.2`）

### 如何为不同材质类型设置不同音效？

提供包含材质后缀的文件即可：

```
normal_scav_footstep_walk_light_organic.mp3   # 有机材质
normal_scav_footstep_walk_light_mech.mp3      # 机械材质
normal_scav_footstep_walk_light.mp3           # 通用回退
```

系统会优先匹配包含材质的文件，未找到时回退到通用文件。

### 如何禁用某个角色的自定义脚步声？

从 SimpleRules 中移除对应的规则即可。例如，要禁用 Scav 的自定义脚步声，删除或注释掉：

```json
{
  "NameKey": "Cname_Scav",
  "IconType": "",
  "FilePattern": "CustomFootStepSounds/Scav"
}
```

### 音效格式支持哪些？

推荐使用 `.mp3` 或 `.wav` 格式。在 `Fallback.PreferredExtensions` 中可以调整优先级：

```json
"PreferredExtensions": [".mp3", ".wav"]
```

系统会按顺序尝试这些扩展名。

## 技术细节

### 3D 音效与距离衰减

- 自定义脚步声使用 FMOD Studio EventInstance 播放
- 自动继承原版事件的 3D 最小/最大距离设置
- 自动跟随角色 GameObject 移动，无需手动更新位置
- 与原版音效的距离衰减曲线保持一致

### 音效生命周期管理

- 使用独立的 FootstepSoundTracker 跟踪脚步声实例
- 同一角色同时只保留一个脚步声实例，新音效会自动停止旧音效
- 音效播放完毕后自动释放资源
- 与敌人语音系统的 CoreSoundTracker 独立，互不干扰

### 冷却机制

- 每个角色独立计时
- 脚步声与冲刺音效分别计时
- 超过 120 秒未触发的角色会自动清理冷却记录
- 每 30 秒自动清理一次过期记录

## 排错指引

### 启用详细日志

1. 编辑 `footstep_voice_rule.json`：
```json
{
  "Debug": {
    "Enabled": true,
    "Level": "Debug",
    "ValidateFileExists": true
  }
}
```

2. 编辑 `settings.json`（可选，用于查看详细路径）：
```json
{
  "logging": {
    "modules": {
      "CustomFootStepSounds": {
        "enabled": true,
        "level": "Debug"
      },
      "CustomEnemySounds": {
        "enabled": true,
        "level": "Debug"
      }
    }
  }
}
```

3. 重启游戏，查看日志文件（通常在游戏根目录或 BepInEx/LogOutput.log）

### 日志关键字

- `[CFS]`：脚步声模块通用日志
- `[CFS:Route]`：规则匹配日志
- `[CFS:Path]`：路径尝试日志（需要同时启用 Footstep 和 Enemy 的 Debug 级别）
- `[CFS:Cooldown]`：冷却机制日志
- `[CFS:Core]`：音效跟踪器日志

### 快速禁用日志

在 `DuckovCustomSounds/` 目录下创建以下任一文件：
- `debug_off`：禁用所有 Debug 级别日志
- `.nolog`：完全禁用日志输出

## 下一步

- 如需了解敌人语音系统的详细规则引擎，请参阅"敌人语音"模块文档
- 如需了解声音包系统，请参阅"声音包系统"文档
- 如需了解其他音效模块（枪械、近战、手雷等），请参阅对应模块文档

