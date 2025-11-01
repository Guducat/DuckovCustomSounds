---
title: 敌人语音
---

# 敌人语音

本模块用于替换游戏内敌人的语音（叫喊、惊呼、手雷提示、死亡等），支持按阵营、等级、具体角色类型进行精准匹配。模块提供强大的规则引擎，支持变体随机播放、优先级打断、触发模式控制等高级功能，同时保持与原版 FMOD 3D 音效的一致性。

> [!NOTE]
> 目前 ModConfig UI 支持尚未完善，你看到的属于前瞻性内容。

## 功能概述

- 替换所有敌人语音事件（normal、surprise、grenade、death 等）
- 支持按阵营（Scav/PMC/Player）、等级（普通/精英/Boss）、具体 NameKey 精确匹配
- 支持同名变体随机播放或固定绑定
- 优先级打断机制（高优先级语音可中断低优先级语音）
- 触发模式控制（原版模式/仅玩家相关/混合模式）
- 自动 3D 定位与距离衰减，与原版音效保持一致
- 与脚步声模块共享规则引擎，配置方式一致

## 快速开始

### 最小可用示例

假设你想为 Scav 敌人替换语音，只需：

1. 创建目录结构：
```
DuckovCustomSounds/
└─ CustomEnemySounds/
   └─ Scav/
      ├─ normal_scav_normal.mp3
      ├─ normal_scav_surprise.mp3
      ├─ normal_scav_grenade.mp3
      └─ normal_scav_death.mp3
```

2. 首次运行游戏后，会自动生成配置文件 `DuckovCustomSounds/CustomEnemySounds/voice_rules.json`，默认已包含 Scav 和 USEC 的简化规则。

3. 进入游戏测试，靠近 Scav 敌人或与其战斗，观察语音是否替换成功。

### 为多个角色添加语音

```
DuckovCustomSounds/
└─ CustomEnemySounds/
   ├─ voice_rules.json              # 配置文件
   ├─ Scav/
   │  ├─ normal_scav_normal.mp3
   │  ├─ normal_scav_surprise.mp3
   │  ├─ normal_scav_grenade.mp3
   │  └─ normal_scav_death.mp3
   └─ Usec/
      ├─ normal_usec_normal.mp3
      ├─ normal_usec_surprise.mp3
      └─ normal_usec_death.mp3
```

## soundKey 说明

模块会拦截游戏中的语音事件，并根据 soundKey 进行匹配：

### 内置 soundKey

- `normal`：一般语音（站桩、搜索、注意等日常行为）
- `surprise`：遭遇、受惊、发现威胁（带距离与视野判定，过远时会抑制）
- `grenade`：听到手雷落地并在一定距离内（默认 10 米，可在 settings.json 中调整）
- `death`：死亡时触发

### 优先级

内置优先级从高到低：`death` (100) > `surprise` (50) > `grenade` (40) > `normal` (10)

未知的 soundKey 会按普通优先级（10）处理，并尝试匹配同名文件。

### 优先级打断

当同一敌人有新语音到来时：
- 如果新语音优先级更高，会中断当前正在播放的低优先级语音
- 如果新语音优先级更低或相等，会被忽略，保留当前语音

可在配置文件中通过 `PriorityInterruptEnabled` 关闭此功能。

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
  - 对于无 NameKey 的角色，使用原始 VoiceType（如 `duck`、`robot`）
- `{soundKey}`：上文所述的 soundKey
- `{ext}`：扩展名（`.mp3` 或 `.wav`，按配置中的 PreferredExtensions 顺序尝试）

**推荐命名示例**（Scav）：
```
CustomEnemySounds/Scav/normal_scav_normal.mp3
CustomEnemySounds/Scav/normal_scav_surprise.mp3
CustomEnemySounds/Scav/normal_scav_grenade.mp3
CustomEnemySounds/Scav/normal_scav_death.mp3
CustomEnemySounds/Scav/elite_scav_normal.mp3      # 精英 Scav
```

**不推荐命名**（会导致匹配失败）：
```
CustomEnemySounds/Scav/normal_duck_normal.mp3  ❌
```

原因：Scav 的 NameKey 为 `Cname_Scav`，系统会优先尝试 `voiceType=scav` 的文件。

### 复杂规则模式

关闭简化规则（`UseSimpleRules: false`）后，使用默认模板：

```
CustomEnemySounds/{team}/{rank}_{voiceType}_{soundKey}{ext}
```

- `{team}`：队伍归一化名称（`scav`、`pmc`、`player`、`unknown`）
- `{rank}`：等级（`boss`、`elite`、`normal`）
- `{voiceType}`：语音类型（`duck`、`robot`、`wolf` 等）
- `{soundKey}`：soundKey
- `{ext}`：扩展名

**示例**：
```
CustomEnemySounds/scav/normal_duck_normal.mp3
CustomEnemySounds/pmc/elite_robot_surprise.wav
```

### 通配符回退

如果模板中包含 `_{soundKey}`，系统会在找不到对应文件时自动尝试去掉 soundKey 的同名文件：

- 首先尝试：`normal_scav_surprise.mp3`
- 找不到则尝试：`normal_scav.mp3`（通配所有 soundKey）

这样可以用一个文件覆盖多个事件。

## 配置文件说明

配置文件位于：`DuckovCustomSounds/CustomEnemySounds/voice_rules.json`

### 完整配置示例

```json
{
  "_comment": "自定义敌人语音规则配置",
  "Debug": {
    "_comment": "日志配置",
    "Enabled": true,
    "Level": "Info",
    "ValidateFileExists": true
  },
  "Fallback": {
    "_comment": "回退策略",
    "UseOriginalWhenMissing": true,
    "PreferredExtensions": [".mp3", ".wav"]
  },
  "DefaultPattern": "CustomEnemySounds/{team}/{rank}_{voiceType}_{soundKey}{ext}",
  "UseSimpleRules": true,
  "SimpleRules": [
    {
      "_comment": "Scav 敌人",
      "NameKey": "Cname_Scav",
      "IconType": "",
      "FilePattern": "CustomEnemySounds/Scav"
    },
    {
      "_comment": "USEC 敌人",
      "NameKey": "Cname_Usec",
      "IconType": "",
      "FilePattern": "CustomEnemySounds/Usec"
    }
  ],
  "PriorityInterruptEnabled": true,
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

#### Fallback（回退策略）

- `UseOriginalWhenMissing`（布尔值）：未找到自定义文件时是否使用原版音效（建议 `true`）
- `PreferredExtensions`（字符串数组）：扩展名优先级，按顺序尝试（如 `[".mp3", ".wav"]`）
  - 可添加 `.ogg`、`.flac` 等格式

#### DefaultPattern（默认模板）

默认值：`CustomEnemySounds/{team}/{rank}_{voiceType}_{soundKey}{ext}`

令牌说明：
- `{team}`：队伍归一化名称（scav/pmc/player/unknown）
- `{rank}`：等级（normal/elite/boss，由图标或血量估算）
- `{voiceType}`：语音类型（duck/robot/scav/usec 等）
- `{soundKey}`：soundKey（normal/surprise/grenade/death 等）
- `{ext}`：扩展名（按 PreferredExtensions 依次尝试）

#### UseSimpleRules（简化规则模式）

- 类型：布尔值
- 默认值：`true`
- 作用：启用简化规则模式，按 NameKey 或 Team 快速匹配目录

#### SimpleRules（简化规则列表）

数组，每个规则包含：
- `NameKey`（字符串，可选）：敌人唯一标识（如 `Cname_Scav`、`Cname_Usec`）
- `Team`（字符串，可选）：队伍名称（如 `scav`、`pmc`），用于 NameKey 为空的角色
- `IconType`（字符串，可选）：限定图标类型（空字符串表示匹配所有）
- `FilePattern`（字符串）：目录路径前缀（如 `CustomEnemySounds/Scav`）

**匹配优先级**：
1. 优先匹配 NameKey（如果提供）
2. 如果 NameKey 为空，则匹配 Team
3. 如果提供了 IconType，则必须同时匹配

**示例**：
```json
{
  "NameKey": "Cname_Scav",
  "IconType": "",
  "FilePattern": "CustomEnemySounds/Scav"
}
```

#### PriorityInterruptEnabled（优先级打断）

- 类型：布尔值
- 默认值：`true`
- 作用：是否允许高优先级音效打断低优先级音效

#### BindVariantIndexPerEnemy（变体索引绑定）

- 类型：布尔值
- 默认值：`false`
- 作用：
  - `false`：每次触发随机选择变体
  - `true`：同一敌人实例固定使用同一变体索引，保证一致性

#### Rules（复杂规则列表）

仅在 `UseSimpleRules: false` 时生效。每个规则可包含：
- `Team`（字符串）：队伍名称（如 `scav`、`pmc`）
- `IconType`（字符串）：图标类型（如 `elite`、`boss`、`merchant`、`pet`、`none`）
- `MinHealth`（浮点数）：最小生命值
- `MaxHealth`（浮点数）：最大生命值
- `NameKeyContains`（字符串）：NameKey 部分匹配
- `ForceVoiceType`（字符串）：强制覆盖语音类型
- `FilePattern`（字符串）：覆盖文件模式
- `SoundKeys`（字符串数组）：仅匹配这些 soundKeys

**示例**：
```json
{
  "Team": "scav",
  "IconType": "boss",
  "FilePattern": "CustomEnemySounds/BossScav/{rank}_{voiceType}_{soundKey}{ext}"
}
```

## 变体机制

支持为同一 soundKey 提供多个变体文件，系统会自动识别并随机选择：

### 变体命名规则

基础文件名后添加 `_1`、`_2`、`_3` 等后缀（必须连续）：

```
CustomEnemySounds/Scav/
├─ normal_scav_surprise.mp3      # 基础文件（变体 0）
├─ normal_scav_surprise_1.mp3    # 变体 1
├─ normal_scav_surprise_2.mp3    # 变体 2
└─ normal_scav_surprise_3.mp3    # 变体 3
```

系统会在 `[0, 3]` 范围内随机选择一个变体播放。



## 触发模式控制

模块提供三种触发模式，可在 ModConfig UI 或 settings.json 中切换：

### 原版模式（Original）

- 与原版游戏一致，只要有战斗或行为就触发语音
- 所有敌人的语音事件都会播放
- 适合喜欢热闹氛围的玩家

### 仅玩家相关模式（PlayerOnly）

- 只有与玩家有关的事件才触发语音
- 判定条件：
  - 敌人与玩家距离在 20 米内
  - 敌人能看到玩家（视野判定：15 米内、60 度视角、无遮挡）
  - 敌人正在与玩家战斗（开火、快速移动等）
  - `surprise` 事件会额外抑制 30 米外的触发
- 适合喜欢安静、专注的玩家

### 混合模式（Hybrid）

- 距离玩家 50 米内时按 PlayerOnly 模式
- 距离玩家 50 米外时按 Original 模式
- 兼顾临场感与安静度

### NPC 对 NPC 战斗语音

独立开关，控制是否允许 NPC 之间的战斗触发语音：
- 启用：NPC 之间战斗也会触发语音
- 禁用：强制使用 PlayerOnly 模式，只有与玩家相关的事件才触发

## ModConfig UI 配置

在游戏内 ModConfig 菜单中可以调整以下选项（需要安装 ModConfig Mod）：

### 允许 NPC-对-NPC 战斗语音

- 类型：开关
- 默认值：启用
- 作用：控制 NPC 之间战斗是否触发语音

### 语音触发模式

- 类型：下拉列表
- 选项：
  - `1) 原版: 所有战斗语音`
  - `2) 玩家相关: 仅玩家触发`
  - `3) 混合: 距离智能切换`
- 默认值：原版
- 作用：控制语音触发的条件

## settings.json 配置

除了 ModConfig UI，也可以直接编辑 `DuckovCustomSounds/settings.json`：

```json
{
  "enableNPCtoNPCCombatVoices": true,
  "enemyVoiceTriggerMode": "Original",
  "deathVoiceFrequency": "always",
  "npcGrenadeSurprisedFrequency": "always",
  "npcGrenadeSurprisedMaxDistance": 10.0
}
```

### 配置项说明

#### enableNPCtoNPCCombatVoices

- 类型：布尔值
- 默认值：`true`
- 作用：是否允许 NPC 对 NPC 的战斗触发语音

#### enemyVoiceTriggerMode

- 类型：字符串
- 可选值：`Original`、`PlayerOnly`、`Hybrid`
- 默认值：`Original`
- 作用：语音触发模式

#### deathVoiceFrequency

- 类型：字符串或数字
- 可选值：
  - `"always"`：总是播放
  - 数字（如 `6` 或 `"6.0f"`）：冷却秒数
  - `"off"` 或 `false`：禁用
- 默认值：`"always"`
- 作用：死亡语音触发频率（全局节流）

#### npcGrenadeSurprisedFrequency

- 类型：字符串或数字
- 可选值：同 deathVoiceFrequency
- 默认值：`"always"`
- 作用：NPC 听到手雷的提示频率（全局节流）

#### npcGrenadeSurprisedMaxDistance

- 类型：浮点数
- 默认值：`10.0`
- 范围：`5.0` - `50.0`
- 作用：NPC 对手雷做出提示的最大距离（米）

## 目录结构示例

### 完整示例（多角色）

```
DuckovCustomSounds/
└─ CustomEnemySounds/
   ├─ voice_rules.json              # 配置文件
   ├─ Scav/
   │  ├─ normal_scav_normal.mp3
   │  ├─ normal_scav_surprise.mp3
   │  ├─ normal_scav_surprise_1.mp3  # 变体
   │  ├─ normal_scav_grenade.mp3
   │  ├─ normal_scav_death.mp3
   │  └─ elite_scav_normal.mp3       # 精英 Scav
   ├─ Usec/
   │  ├─ normal_usec_normal.mp3
   │  ├─ normal_usec_surprise.mp3
   │  └─ normal_usec_death.mp3
   └─ Wolf/
      ├─ boss_wolf_normal.mp3        # Boss Wolf
      └─ boss_wolf_death.mp3
```

### 复杂规则示例（按阵营分类）

```
DuckovCustomSounds/
└─ CustomEnemySounds/
   ├─ voice_rules.json              # UseSimpleRules: false
   ├─ scav/
   │  ├─ normal_duck_normal.mp3
   │  ├─ normal_duck_surprise.mp3
   │  ├─ elite_duck_normal.mp3
   │  └─ boss_duck_death.mp3
   └─ pmc/
      ├─ normal_robot_normal.mp3
      └─ elite_robot_surprise.mp3
```

## 常见问题

### 1. 为什么我的语音文件没有播放？

**可能原因**：
- 文件命名不符合规则
- 配置文件中的 SimpleRules 没有匹配到对应的 NameKey
- 文件扩展名不在 PreferredExtensions 列表中
- 文件路径错误

**解决方法**：
1. 打开 `voice_rules.json`，设置 `Debug.Enabled: true` 和 `Debug.Level: "Verbose"`
2. 进入游戏触发语音事件
3. 查看日志文件（`DuckovCustomSounds/Logs/`），搜索 `[CES]` 或 `[EnemyVoice]` 关键字
4. 日志会显示所有尝试的路径，对比实际文件路径找出差异

### 2. 如何为 Boss 敌人单独设置语音？

**方法一：使用 SimpleRules + IconType**
```json
{
  "SimpleRules": [
    {
      "NameKey": "Cname_Wolf",
      "IconType": "boss",
      "FilePattern": "CustomEnemySounds/BossWolf"
    }
  ]
}
```

**方法二：使用复杂规则**
```json
{
  "UseSimpleRules": false,
  "Rules": [
    {
      "IconType": "boss",
      "FilePattern": "CustomEnemySounds/Bosses/{rank}_{voiceType}_{soundKey}{ext}"
    }
  ]
}
```

### 3. 如何让同一个敌人每次都播放同一套变体？

在 `voice_rules.json` 中设置：
```json
{
  "BindVariantIndexPerEnemy": true
}
```

这样，同一个敌人实例会固定使用同一个变体索引，保证该敌人的所有语音使用同一套变体。

### 4. 语音触发太频繁，如何减少？

**方法一：使用 PlayerOnly 或 Hybrid 模式**
```json
{
  "enemyVoiceTriggerMode": "PlayerOnly"
}
```

**方法二：设置频率节流**
```json
{
  "deathVoiceFrequency": "5.0",
  "npcGrenadeSurprisedFrequency": "3.0"
}
```

### 5. 如何禁用某个 soundKey 的语音？

不提供对应的音频文件，并设置 `UseOriginalWhenMissing: false`：
```json
{
  "Fallback": {
    "UseOriginalWhenMissing": false
  }
}
```

这样，找不到自定义文件时不会回退到原版音效，相当于静音。

### 6. 如何为不同等级的敌人设置不同语音？

使用 `iconPrefix` 区分：
```
CustomEnemySounds/Scav/
├─ normal_scav_normal.mp3    # 普通 Scav
├─ elite_scav_normal.mp3     # 精英 Scav
└─ boss_scav_normal.mp3      # Boss Scav
```

系统会根据敌人的图标类型或血量自动判断等级。

### 7. 如何使用 .ogg 或 .flac 格式的音频？

在 `voice_rules.json` 中添加扩展名：
```json
{
  "Fallback": {
    "PreferredExtensions": [".mp3", ".wav", ".ogg", ".flac"]
  }
}
```

系统会按顺序尝试这些扩展名。

### 8. 如何让一个音频文件覆盖多个 soundKey？

使用通配符回退机制，只提供不带 soundKey 的文件：
```
CustomEnemySounds/Scav/
└─ normal_scav.mp3    # 会匹配所有 soundKey
```

系统会在找不到 `normal_scav_normal.mp3`、`normal_scav_surprise.mp3` 等文件时，回退到 `normal_scav.mp3`。

## 技术细节

### 上下文采集

模块在敌人生成时（`AICharacterController.Init`）注册 `EnemyContext`，包含：
- `InstanceId`：GameObject 实例 ID
- `Team`：队伍（归一化为 scav/pmc/player/unknown）
- `IconType`：图标类型（boss/elite/merchant/pet/none）
- `NameKey`：敌人唯一标识（如 `Cname_Scav`）
- `VoiceType`：语音类型（从 NameKey 提取或使用原始值）
- `Health`：生命值（用于估算等级）

### 规则引擎

`VoiceRuleEngine` 负责路径匹配：
1. 如果启用 SimpleRules，按 NameKey 或 Team 快速匹配
2. 否则，遍历 Rules 列表，检查所有条件（Team、IconType、Health、NameKeyContains 等）
3. 使用匹配到的 FilePattern 或 DefaultPattern 生成候选路径
4. 按 PreferredExtensions 顺序尝试每个扩展名
5. 如果启用变体，会检测 `_1`、`_2` 等后缀并随机选择

### 过滤器

`EnemyVoiceFilter` 根据触发模式决定是否允许播放：
- **Original 模式**：总是允许
- **PlayerOnly 模式**：
  - 检查敌人与玩家距离（20 米内）
  - 检查视野（15 米内、60 度视角、无遮挡）
  - 检查战斗状态（开火、快速移动）
  - 对 `surprise` 事件额外抑制 30 米外的触发
- **Hybrid 模式**：50 米内按 PlayerOnly，50 米外按 Original

过滤器还会排除商人（Merchant）角色，避免触发不必要的语音。

### 3D 音效

模块使用 `AudioManager.PostCustomSound` 接口播放自定义音频，该接口会：
- 自动创建 FMOD EventInstance
- 自动设置 3D 位置属性
- 自动跟随 GameObject 移动
- 自动应用距离衰减（与原版一致）
- 自动清理资源

### 优先级打断

`CoreSoundTracker` 管理每个敌人当前播放的语音：
- 记录当前播放的 soundKey 和优先级
- 新语音到来时，比较优先级
- 如果新语音优先级更高，停止当前语音并播放新语音
- 如果新语音优先级更低或相等，忽略新语音

### 变体机制

系统会自动检测变体文件：
1. 尝试基础文件（如 `normal_scav_surprise.mp3`）
2. 尝试 `_1`、`_2`、`_3` 等后缀（必须连续）
3. 记录最大变体索引
4. 随机选择一个变体播放
5. 如果启用 `BindVariantIndexPerEnemy`，会为每个敌人实例固定一个变体索引

## 排错指引

### 启用详细日志

编辑 `voice_rules.json`：
```json
{
  "Debug": {
    "Enabled": true,
    "Level": "Verbose",
    "ValidateFileExists": true
  }
}
```

### 查看日志文件

日志位于：`DuckovCustomSounds/Logs/`

搜索关键字：
- `[CES]`：敌人语音模块日志
- `[EnemyVoice]`：语音事件日志
- `[EnemyContext]`：上下文采集日志
- `[VoiceRuleEngine]`：规则匹配日志
- `[EnemyVoiceFilter]`：过滤器日志

### 常见错误信息

- `File not found`：文件路径错误或文件不存在
- `No matching rule`：没有匹配的规则，检查 SimpleRules 或 Rules 配置
- `Filtered out`：被过滤器拦截，检查触发模式设置
- `Priority blocked`：被优先级打断机制拦截，当前有更高优先级的语音正在播放

### 测试建议

1. 先使用最简单的配置（SimpleRules + 默认设置）
2. 确保文件命名完全符合规则
3. 使用 `.mp3` 格式（兼容性最好）
4. 启用详细日志，观察路径匹配过程
5. 逐步添加复杂功能（变体、优先级、过滤等）
- `true`：同一敌人实例始终使用同一变体索引，保证该敌人的所有语音使用同一套变体

