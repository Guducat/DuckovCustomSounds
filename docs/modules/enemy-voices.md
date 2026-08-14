---
title: 敌人语音
---

# 敌人语音

替换游戏里敌人的喊叫声、惊呼、手雷提示、死亡语音等。支持按阵营、等级、具体角色类型匹配，支持变体随机播放和优先级打断。

## 快速开始

1. 创建目录结构：
```
CustomEnemySounds/
├── Scav/
│   ├── normal_scav_normal.mp3
│   ├── normal_scav_surprise.mp3
│   ├── normal_scav_grenade.mp3
│   └── normal_scav_death.mp3
└── Usec/
    ├── normal_usec_normal.mp3
    ├── normal_usec_surprise.mp3
    └── normal_usec_death.mp3
```

2. 首次运行会自动生成 `voice_rules.json`（默认包含 Scav 和 USEC 的简化规则）。
3. 进游戏测试。

## soundKey 说明

内置 soundKey 及其优先级：

| soundKey | 用途 | 优先级 |
|----------|------|--------|
| `death` | 死亡 | 100 |
| `surprise` | 遭遇/受惊 | 50 |
| `grenade` | 听到手雷（10米内） | 40 |
| `normal` | 一般语音 | 10 |

优先级高的语音可以打断优先级低的。可在 `voice_rules.json` 中关掉 `PriorityInterruptEnabled` 禁用打断。

## 文件命名

### 简化规则（默认推荐）

规则格式：`{FilePattern}/{iconPrefix}_{voiceType}_{soundKey}{ext}`

| 变量 | 说明 |
|------|------|
| `FilePattern` | 目录，在 `voice_rules.json` 的 SimpleRules 里设置，如 `CustomEnemySounds/Scav` |
| `iconPrefix` | 由该条 SimpleRule 的 `IconType` 决定；留空默认 `normal`，可设为 `elite`/`boss` |
| `voiceType` | 有 NameKey 时从 NameKey 第二段提取，保留原始大小写（`Cname_Scav` → `Scav`）；先按该段匹配，未命中再回退原始 VoiceType。没有 NameKey 时直接用游戏 VoiceType 枚举值：`Duck`、`Robot`、`Wolf`、`Chicken`、`Crow`、`Eagle`、`coalball` |
| `soundKey` | 以上四个 soundKey |
| `ext` | `.mp3` 或 `.wav`（在 `voice_rules.json` 的 `PreferredExtensions` 里设置） |

**推荐命名**（Scav）：
```
Scav/normal_scav_normal.mp3
Scav/normal_scav_surprise.mp3
Scav/normal_scav_grenade.mp3
Scav/normal_scav_death.mp3
Scav/elite_scav_normal.mp3      # 需配合规则 { "NameKey": "Cname_Scav", "IconType": "elite", "FilePattern": "CustomEnemySounds/Scav" } 才会命中
```

**不推荐命名（但会命中）**：
```
Scav/normal_duck_normal.mp3  ⚠️  # 仅当 "scav" 命名的文件不存在时，系统会回退用原始 VoiceType（如 Duck）匹配，因此该文件对 Scav 仍会命中（Windows 大小写不敏感）；但它会与所有同声线敌人共用，建议优先用 "scav" 命名
```

玩家（NameKey 为空）按 Team 匹配，例如：
```
Player/normal_duck_surprise.mp3
```
（对应规则 `{ "Team": "player", "FilePattern": "CustomEnemySounds/Player" }`；脚步/冲刺音效属于「脚步」模块，见该模块文档）

### 通配回退

系统找不到 `normal_scav_surprise.mp3` 时，会尝试 `normal_scav.mp3`（一个文件覆盖所有 soundKey）。

## voice_rules.json 配置

文件位置：`CustomEnemySounds/voice_rules.json`

### 简化规则（推荐）

```json
{
  "Debug": { "Enabled": true, "Level": "Info", "ValidateFileExists": true },
  "Fallback": { "UseOriginalWhenMissing": true, "PreferredExtensions": [".mp3", ".wav"] },
  "UseSimpleRules": true,
  "SimpleRules": [
    { "NameKey": "Cname_Scav", "FilePattern": "CustomEnemySounds/Scav" },
    { "NameKey": "Cname_Usec", "FilePattern": "CustomEnemySounds/Usec" },
    { "Team": "player", "FilePattern": "CustomEnemySounds/Player" }
  ],
  "PriorityInterruptEnabled": true,
  "BindVariantIndexPerEnemy": false
}
```

| 字段 | 说明 |
|------|------|
| `Debug.Level` | `Error`/`Warning`/`Info`（推荐）/`Debug`/`Verbose` |
| `Debug.ValidateFileExists` | 是否在播放前检查文件存在（建议 `true`） |
| `Fallback.UseOriginalWhenMissing` | 预留字段，当前版本未生效（未匹配到自定义文件时始终保留原声） |
| `Fallback.PreferredExtensions` | 扩展名优先顺序，可加 `.ogg`、`.flac` 等 |
| `UseSimpleRules` | `true` = 用简化规则 |
| `SimpleRules[].NameKey` | 敌人唯一标识，如 `Cname_Scav` |
| `SimpleRules[].Team` | NameKey 为空时按队伍匹配（`player`、`scav`、`pmc`） |
| `SimpleRules[].FilePattern` | 音频存放目录 |
| `PriorityInterruptEnabled` | 高优先级是否打断低优先级 |
| `BindVariantIndexPerEnemy` | `true` = 同一敌人固定用同一个变体 |

### 变体

同一个 soundKey 可以有多个变体，加 `_1`、`_2` 后缀（必须连续）：
```
Scav/normal_scav_surprise.mp3     # 变体 0
Scav/normal_scav_surprise_1.mp3   # 变体 1
Scav/normal_scav_surprise_2.mp3   # 变体 2
```
系统随机选一个播放。开启 `BindVariantIndexPerEnemy` 后，同一敌人每次都用同一个变体。

### 复杂规则（Rules）

`UseSimpleRules: false` 时使用复杂规则数组 `Rules[]`：

| 字段 | 说明 |
|------|------|
| `Team` | 队伍匹配（如 `scav`、`pmc`） |
| `IconType` | 前缀（同简单模式，留空为 `normal`） |
| `MinHealth` / `MaxHealth` | 血量区间 |
| `NameKeyContains` | NameKey 部分匹配 |
| `ForceVoiceType` | 强制 voiceType（如 `Duck`/`Robot`） |
| `SoundKeys` | 仅匹配这些 soundKey |
| `FilePattern` | 音频目录，支持令牌 `{team}`、`{rank}`、`{voiceType}`、`{soundKey}` 等 |

- `DefaultPattern`：默认模板 `CustomEnemySounds/{team}/{rank}_{voiceType}_{soundKey}{ext}`，`{team}`/`{rank}` 令牌仅在复杂规则中生效（简单模式前缀只来自 IconType）。
- 未知的 soundKey 也按普通优先级（10）参与匹配，不只限于内置四个。
- 简单模式的通配回退有两种：去掉 soundKey 的 `{iconPrefix}_{voiceType}.{ext}`，以及忽略 voiceType 的 `{iconPrefix}_*_{soundKey}{ext}`。
- `MinCooldownSeconds` 字段在语音模块未使用（脚步模块才生效）。

## ModConfig 设置

| 设置 | 说明 |
|------|------|
| 允许 NPC-对-NPC 战斗语音 | 开关 NPC 之间的战斗语音 |
| 语音触发模式 | 原版（全部触发）/仅玩家相关/混合（50米外原版，50米内仅玩家） |
| 语音音量倍率 | 0-2，默认 1.0 |

## settings.json

```json
{
  "enableNPCtoNPCCombatVoices": true,
  "enemyVoiceTriggerMode": "Original",
  "enemyVoiceVolumeScale": 1.0,
  "deathVoiceFrequency": "always",
  "npcGrenadeSurprisedFrequency": "always",
  "npcGrenadeSurprisedMaxDistance": 10.0
}
```

- `enemyVoiceVolumeScale`：语音音量倍率，默认 1.0，范围 0~2
- `deathVoiceFrequency`：`"always"` / 数字秒（冷却）/ `"off"` 禁用
- `npcGrenadeSurprisedFrequency`：同上
- `npcGrenadeSurprisedMaxDistance`：NPC 对手雷提示的最大距离，默认 10 米

## 常见问题

**语音不播放**：设置 `Debug.Level: "Verbose"`，在 player.log 里搜 `[CES]` 看匹配过程。检查文件命名、路径、扩展名。Scav 优先用 `scav` 命名；`duck` 只是原始声线回退，不建议依赖。

**Boss 单独语音**：在 SimpleRules 加一条 `{ "NameKey": "Cname_Wolf", "FilePattern": "CustomEnemySounds/BossWolf" }`。

**触发太频繁**：用 `PlayerOnly` 模式，或设 `deathVoiceFrequency: "5.0"`。

**禁用某个 soundKey**：当前版本没有按 soundKey 静音的配置项；未提供自定义文件时始终保留原声。需要减少触发可改用「仅玩家相关（PlayerOnly）」触发模式，或调大对应频率冷却。

**不同等级不同语音**：在 SimpleRules 里为同一 NameKey 添加多条 `IconType` 不同的规则（`elite`/`boss`），文件用对应前缀命名；仅靠 `elite_`/`boss_` 文件名而不加规则不会生效。

**一个文件覆盖所有 soundKey**：只放 `normal_scav.mp3`，系统找不到细分文件时回退到这个。

## 高级 - 附录：常见 NameKey 参考

首次运行生成的 `voice_rules.json` 仅含 Scav 和 USEC。如需为更多角色定制语音，参考以下游戏内常见 NameKey，在 SimpleRules 中添加对应规则即可。

| NameKey | 说明 |
|---------|------|
| `Cname_Scav` | 普通 Scav |
| `Cname_Usec` | USEC 士兵 |
| `Cname_Wolf` | 沃尔夫（Boss） |
| `Cname_Boss_Sniper` | 劳登（Boss） |
| `Cname_Boss_Shot` | 喷子（Boss） |
| `Cname_ServerGuardian` | 矿长（Boss） |
| `Cname_Speedy` | 急速团长（Boss） |
| `Cname_SpeedyChild` | 急速团长随从 |
| `Cname_Prison_Boss` | 典狱长（Boss） |
| `Cname_BALeader` | BA队长（Boss） |
| `Cname_BALeader_Child` | BA队长随从 |
| `Cname_Boss_Fly` | 蝇蝇队长（Boss） |
| `Cname_Boss_Fly_Child` | 蝇蝇队长随从 |
| `Cname_Boss_Arcade` | 暴走街机（Boss） |
| `Cname_Boss_3Shot` | 三枪哥（Boss） |
| `Cname_StormBoss1` ~ `StormBoss5` | 噗咙噗咙等风暴 Boss |
| `Cname_ShortEagle` | 矮鸭（Boss） |
| `Cname_UltraMan` | 光之男（Boss） |
| `Cname_CrazyRob` | 失控机械蜘蛛（Boss） |
| `Cname_Vida` | 维达（Boss） |
| `Cname_ScavRage` | 愤怒 Scav |
| `Cname_Raider` | 掠夺者 |
| `Cname_RobSpider` | 机械蜘蛛 |
| `Cname_StormCreature` | 风暴生物 |
| `Cname_Mushroom` | 蘑菇人 |
| `Cname_SchoolBully` | 校霸 |

更多 NameKey 可在 player.log 中搜 `[CES]` 查看实际匹配的敌人名称。
