---
title: 物品音效
---

# 物品音效

本模块用于替换游戏内消耗品使用音效，包括食物、饮料、药品、注射针剂等。模块支持按物品 TypeID 精确匹配，并提供通用回退机制，同时保持与原版 FMOD 3D 音效的一致性。

> [!WARNING]
> 目前版本存在注射剂和药品音效的丢失，请等待修复，而且音效文件命名规则可能存在问题，请先忽略本文档的药品与注射针剂相关的内容，食物不受影响。

> [!NOTE]
> 目前 ModConfig UI 支持尚未完善，你看到的属于前瞻性内容。

## 功能概述

- 替换消耗品使用音效（食物、饮料、药品、注射针剂音效）
- 按物品 TypeID 精确匹配音效文件（TypeID 必须是数字）
- 按 soundKey 匹配（如 `food`、`meds`、`syringe`）
- 分类开关（可单独启用/禁用食物、药品、注射针剂音效）
- 自动 3D 定位与距离衰减
- ModConfig UI 热重载配置

## 快速开始

### 最小可用示例

假设你想为所有消耗品替换音效，只需：

1. 创建目录结构：
```
DuckovCustomSounds/
└─ CustomItemSounds/
   └─ default.mp3
```

2. 进入游戏，使用任意消耗品，观察音效是否替换成功。

### 为特定物品添加音效（推荐）

```
DuckovCustomSounds/
└─ CustomItemSounds/
   ├─ 84.mp3                 # 物品 TypeID 84 的音效
   ├─ 20.mp3                 # 物品 TypeID 20 的音效
   ├─ 156.mp3                # 物品 TypeID 156 的音效
   └─ default.mp3            # 通用回退音效
```

**重要说明**：
- TypeID 必须是数字（如 84、20、156）
- 不同的物品有不同的 TypeID
- 需要通过日志查看具体的 TypeID（详见"常见问题"章节）

### 按类别组织（可选）

你也可以按物品类别组织音效文件：

```
DuckovCustomSounds/
└─ CustomItemSounds/
   ├─ food/                  # 食物类别
   │  ├─ 84.mp3              # 食物 TypeID 84
   │  └─ default.mp3         # 食物通用音效
   ├─ meds/                  # 药品类别
   │  ├─ 20.mp3              # 药品 TypeID 20
   │  └─ default.mp3         # 药品通用音效
   └─ syringe/               # 注射针剂类别
      └─ default.mp3         # 注射针剂通用音效
```

## TypeID 与 soundKey 说明

### TypeID（强烈推荐）

- **TypeID 是什么**：每个物品的唯一数字标识符（如 84、20、156）
- **特点**：
  - TypeID 必须是数字
  - 每个物品都有唯一的 TypeID
  - 例如：某个食物的 TypeID 是 84
- **如何获取**：查看日志文件（详见"常见问题"章节）

### soundKey（物品类别）

- **soundKey 是什么**：物品类别标识符（如 `food`、`meds`、`syringe`）
- **特点**：
  - soundKey 是字符串标识符
  - 同一类别的所有物品共享相同的 soundKey
  - 例如：所有食物的 soundKey 都是 `food`
- **使用场景**：当你想为整个类别提供统一音效时

### 游戏音效事件

游戏中的物品使用音效事件格式为：`SFX/Item/use_{soundKey}`

常见的 soundKey：
- `food`：食物和饮料
- `meds`：药品（最近可能有修改！）
- `syringe`：注射针剂（最近可能有修改！）

## 文件命名规则

### 按 TypeID 匹配（强烈推荐）

系统会优先尝试按物品的 TypeID 匹配文件。TypeID 必须是数字。

```
CustomItemSounds/
└─ {TypeID}.mp3
```

**示例**（假设某个食物的 TypeID 为 84）：
```
CustomItemSounds/
└─ 84.mp3                 # 物品 TypeID 84 的音效
```

**更多示例**：
```
CustomItemSounds/
├─ 84.mp3                 # TypeID 84 的物品
├─ 20.mp3                 # TypeID 20 的物品
└─ 156.mp3                # TypeID 156 的物品
```

### 按 soundKey 匹配

如果找不到 TypeID 对应的文件，会尝试使用 soundKey：

```
CustomItemSounds/
└─ {soundKey}.mp3
```

**示例**：
```
CustomItemSounds/
├─ food.mp3               # 所有食物使用此音效
├─ meds.mp3               # 所有药品使用此音效
└─ syringe.mp3            # 所有注射针剂使用此音效
```

### 通用回退

如果以上都找不到，会使用通用回退文件：

```
CustomItemSounds/
└─ default.mp3
```

### 按类别组织（可选）

你可以按物品类别组织音效文件：

```
CustomItemSounds/
├─ food/                  # 食物类别
│  ├─ 84.mp3              # TypeID 84
│  └─ default.mp3         # 食物通用音效
├─ meds/                  # 药品类别
│  ├─ 20.mp3              # TypeID 20
│  └─ default.mp3         # 药品通用音效
└─ syringe/               # 注射针剂类别
   └─ default.mp3         # 注射针剂通用音效
```

### 查找优先级

**扁平结构**（推荐）：
1. `{TypeID}.mp3`（TypeID 必须是数字）
2. `{soundKey}.mp3`
3. `default.mp3`

**分类结构**（可选）：
1. `{category}/{TypeID}.mp3`
2. `{category}/default.mp3`
3. `{TypeID}.mp3`（回退到扁平结构）
4. `default.mp3`

### 支持的音频格式

系统会按以下顺序尝试不同的文件扩展名：
1. `.mp3`
2. `.wav`
3. `.ogg`
4. `.oga`

**示例**：如果 TypeID 为 84，系统会按以下顺序查找：
```
84.mp3 → 84.wav → 84.ogg → 84.oga →
food.mp3 → food.wav → food.ogg → food.oga →
default.mp3 → default.wav → default.ogg → default.oga
```

## ModConfig UI 配置

模块支持通过 ModConfig UI 进行热重载配置（无需重启游戏）：

- **启用自定义物品音效**：开关模块功能
- **音量倍率 (0~2)**：调整音效音量（1.0 为原始音量）
- **启用 食物/饮料 声音**：单独控制食物音效
- **启用 药品 声音**：单独控制药品音效
- **启用 注射器 声音**：单独控制注射器音效

配置变更会立即生效，无需重启游戏。

## 目录结构示例

### 完整示例（扁平结构，使用 TypeID）

```
DuckovCustomSounds/
└─ CustomItemSounds/
   ├─ 84.mp3                 # TypeID 84 的物品音效
   ├─ 20.mp3                 # TypeID 20 的物品音效
   ├─ 156.mp3                # TypeID 156 的物品音效
   ├─ 203.mp3                # TypeID 203 的物品音效
   └─ default.mp3            # 通用回退音效
```

### 完整示例（分类结构）

```
DuckovCustomSounds/
└─ CustomItemSounds/
   ├─ food/                  # 食物类别
   │  ├─ 84.mp3              # 食物 TypeID 84
   │  ├─ 85.mp3              # 食物 TypeID 85
   │  └─ default.mp3         # 食物通用音效
   ├─ meds/                  # 药品类别
   │  ├─ 20.mp3              # 药品 TypeID 20
   │  ├─ 21.mp3              # 药品 TypeID 21
   │  └─ default.mp3         # 药品通用音效
   └─ syringe/               # 注射器类别
      ├─ 156.mp3             # 注射器 TypeID 156
      └─ default.mp3         # 注射器通用音效
```

### 最小示例（仅通用音效）

```
DuckovCustomSounds/
└─ CustomItemSounds/
   └─ default.mp3            # 通用音效
```

所有物品都会使用这个通用音效。

### 混合示例（部分物品专用，其他通用）

```
DuckovCustomSounds/
└─ CustomItemSounds/
   ├─ 84.mp3                 # TypeID 84 专用音效
   ├─ 20.mp3                 # TypeID 20 专用音效
   └─ default.mp3            # 其他物品使用此音效
```

## 常见问题

### 1. 如何知道物品的 TypeID？

**方法：查看日志**
1. 在 `settings.json` 中设置日志级别为 Debug：
```json
{
  "logLevels": {
    "Item": "Debug"
  }
}
```
2. 进入游戏使用物品
3. 查看日志文件（`DuckovCustomSounds/Logs/`），搜索 `[ItemUse]`
4. 日志会显示 `TypeID=xxx`（xxx 是数字）

**日志示例**：
```
[ItemUse] 记录 TypeID: 84
[ItemUse] TypeID=84, soundKey=food, 使用: 84.mp3
```

从这个日志可以看出：
- TypeID 是 `84`（数字）
- soundKey 是 `food`（类别）
- 系统使用了 `84.mp3` 文件

### 2. 为什么推荐使用 TypeID 而不是 soundKey？

**原因**：
- **TypeID 是唯一的**：每个物品都有唯一的 TypeID，可以为每个物品提供独特的音效
- **soundKey 是共享的**：同一类别的所有物品共享相同的 soundKey

**示例**：
- 所有食物都使用 `food` 作为 soundKey
- 如果你创建 `food.mp3`，所有食物都会使用这个音效
- 但如果你创建 `84.mp3`、`85.mp3`、`86.mp3`（假设这是它们的 TypeID），每个食物都会有独特的音效

**什么时候使用 soundKey**：
- 当你想为整个类别提供统一音效时
- 当你不在乎同一类别的不同物品使用相同音效时

### 3. 如何为所有物品使用相同的音效？

只需提供通用回退文件：
```
CustomItemSounds/
└─ default.mp3        # 所有物品的音效
```

### 4. 如何只替换食物音效，不替换药品音效？

使用 ModConfig UI 的分类开关：
- 启用"食物/饮料 声音"
- 禁用"药品 声音"
- 禁用"注射器 声音"

或者只提供食物的音效文件：
```
CustomItemSounds/
└─ food/
   └─ default.mp3        # 只有食物音效
```

### 5. 音效音量太大或太小怎么办？

**方法一：使用 ModConfig UI**

在游戏内调整音量滑块（0.0 - 2.0）。

**方法二：编辑音频文件**

使用音频编辑软件（如 Audacity）调整音频文件的音量。

### 6. 如何使用 .ogg 或 .wav 格式的音频？

模块已内置支持这些格式，直接使用即可：
```
CustomItemSounds/
├─ 84.ogg
└─ 20.wav
```

系统会按 `.mp3` → `.wav` → `.ogg` → `.oga` 的顺序尝试。

### 7. 为什么我的音效没有生效？

**可能原因**：
- 文件命名不正确（必须使用数字 TypeID）
- 文件路径错误
- 模块未启用（检查 ModConfig UI）
- 对应类别被禁用（检查 ModConfig UI 的分类开关）
- 文件格式不支持

**解决方法**：
1. 确认模块已启用（ModConfig UI）
2. 确认对应类别已启用（如"食物/饮料 声音"）
3. 启用 Debug 日志，查看实际的 TypeID
4. 检查文件命名是否与 TypeID 匹配（必须是数字，如 `84.mp3`）
5. 使用 `.mp3` 格式（兼容性最好）

### 8. 音效和原版音效同时播放怎么办？

这通常是因为模块未能正确拦截原版音效。请检查：
1. 确认模块已启用（ModConfig UI）
2. 查看日志，确认音效替换成功
3. 如果问题持续，请报告 Bug

## 技术细节

### 音效拦截

模块使用 Harmony Postfix 补丁拦截 `AudioManager.Post(string eventName, GameObject gameObject)` 方法：
- 检查 `eventName` 是否以 `SFX/Item/use_` 开头
- 如果匹配，提取 soundKey 并查找自定义文件
- 如果找到自定义文件，替换原版音效并播放自定义音效

### TypeID 获取

模块通过 Harmony Prefix 补丁拦截 `CA_UseItem.SetUseItem` 方法：
1. 从 `Item` 参数获取 `TypeID`
2. 从 `CA_UseItem` 组件获取 `CharacterMainControl`
3. 将 TypeID 与 CharacterMainControl 关联存储
4. 在播放音效时，通过 GameObject 查找对应的 TypeID

### 3D 音效

模块使用 `AudioManager.PostCustomSFX` 接口播放自定义音频，该接口会：
- 自动创建 FMOD EventInstance
- 自动设置 3D 位置属性
- 自动跟随 GameObject 移动
- 自动应用距离衰减（默认：最小 1 米，最大 50 米）
- 自动路由到 SFX 总线
- 自动清理资源

### 文件查找策略

模块使用多级查找策略：
1. **主要候选**：尝试 TypeID（数字）
2. **次要候选**：尝试 soundKey（类别）
3. **回退候选**：使用 `default` 作为通用回退

对于每个候选文件名，会尝试所有支持的扩展名（`.mp3`、`.wav`、`.ogg`、`.oga`）。

如果使用分类结构，会先在分类目录下查找，然后回退到根目录。

### 阶段追踪

模块会追踪物品使用的阶段（Action/Finish）：
- **Action 阶段**：物品使用过程中的音效（如吃东西的咀嚼声）
- **Finish 阶段**：物品使用完成后的音效（如吞咽声）

当物品使用被取消时（如切换武器），Action 阶段的音效会被停止，但 Finish 阶段的音效会保留。

### 分类开关

模块支持按类别启用/禁用音效：
- `food`：食物和饮料
- `meds`：药品
- `syringe`：注射器

当某个类别被禁用时，该类别的物品会使用原版音效。

## 排错指引

### 启用详细日志

编辑 `settings.json`：
```json
{
  "logLevels": {
    "Item": "Debug"
  }
}
```

或者使用 Verbose 级别查看所有路径尝试：
```json
{
  "logLevels": {
    "Item": "Verbose"
  }
}
```

### 查看日志文件

日志位于：`DuckovCustomSounds/Logs/`

搜索关键字：
- `[ItemUse]`：物品使用音效日志
- `记录 TypeID`：TypeID 记录日志
- `追踪阶段`：阶段追踪日志

### 常见错误信息

- `未找到 TypeID`：没有找到物品的 TypeID，会使用 soundKey 作为回退
- `未找到自定义文件`：没有找到匹配的自定义文件，会使用原版音效
- `新接口播放失败`：播放自定义音效时出错，检查文件格式和路径
- `Postfix 覆盖异常`：补丁执行时出错，查看详细错误信息

### 测试建议

1. 先使用最简单的配置（只提供 `default.mp3`）
2. 确认模块已启用（ModConfig UI）
3. 确认对应类别已启用（如"食物/饮料 声音"）
4. 使用 `.mp3` 格式（兼容性最好）
5. 启用 Debug 日志，观察文件查找过程
6. 逐步添加更多物品的音效

### 性能优化

模块已进行以下性能优化：
- 日志字符串仅在对应级别启用时才构建
- 使用 Dictionary 缓存 TypeID 查找结果
- 使用 LINQ 优化文件查找
- 自动清理音频资源

### 与其他模块的兼容性

物品音效模块与以下模块完全兼容：
- 敌人语音模块（CustomEnemySounds）
- 脚步声模块（CustomFootStepSounds）
- 枪械模块（CustomGunSounds）
- 近战模块（CustomMeleeSounds）
- 手雷模块（CustomGrenadeSounds）

所有模块共享同一日志系统和 FMOD 总线获取策略，互不影响。
