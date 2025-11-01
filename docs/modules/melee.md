---
title: 近战音效
---

# 近战音效

本模块用于替换游戏内近战武器的音效。模块支持按武器 TypeID 精确匹配，并提供通用回退机制，同时保持与原版 FMOD 3D 音效的一致性。

> [!NOTE]
> 目前 ModConfig UI 支持尚未完善，你看到的属于前瞻性内容。

> [!NOTE]
> 此模块支持差分音效(xxx.mp3 xxx_1.mp3 etc.)

## 功能概述

- 替换近战武器音效
- 按武器 TypeID 精确匹配音效文件（TypeID 必须是数字）
- 自动 3D 定位与距离衰减
- ModConfig UI 热重载配置

## 快速开始

### 最小可用示例

假设你想为所有近战武器替换音效，只需：

1. 创建目录结构：
```
DuckovCustomSounds/
└─ CustomMeleeSounds/
   └─ default.mp3
```

2. 进入游戏，使用任意近战武器，观察音效是否替换成功。

### 为特定武器添加音效（推荐）

```
DuckovCustomSounds/
└─ CustomMeleeSounds/
   ├─ 98.ogg                 # 铲子音效（TypeID: 98）
   ├─ 156.mp3                # 某把近战武器音效（TypeID: 156）
   ├─ 203.mp3                # 某把近战武器音效（TypeID: 203）
   └─ default.mp3            # 通用回退音效
```

**重要说明**：
- TypeID 必须是数字（如 98、156、203）
- 不同的近战武器有不同的 TypeID
- 例如：铲子的 TypeID 是 98，文件名应为 `98.ogg` 或 `98.mp3`

## TypeID 说明

### TypeID（必须使用）

- **TypeID 是什么**：每把近战武器的唯一数字标识符（如 98、156、203）
- **特点**：
  - TypeID 必须是数字
  - 每把近战武器都有唯一的 TypeID
  - 例如：铲子的 TypeID 是 98
- **如何获取**：查看日志文件（详见"常见问题"章节）

### 游戏音效事件

游戏中的近战武器只会触发一个默认的音效事件，不区分攻击或挥舞。模块会拦截这个事件并替换为自定义音效。

## 文件命名规则

### 按 TypeID 匹配（强烈推荐）

系统会优先尝试按武器的 TypeID 匹配文件。TypeID 必须是数字。

```
CustomMeleeSounds/
└─ {TypeID}.mp3
```

**示例**（铲子的 TypeID 为 98）：
```
CustomMeleeSounds/
└─ 98.ogg             # 铲子音效
```

**更多示例**：
```
CustomMeleeSounds/
├─ 98.ogg             # TypeID 98 的近战武器
├─ 156.mp3            # TypeID 156 的近战武器
└─ 203.mp3            # TypeID 203 的近战武器
```

### 通用回退

如果找不到 TypeID 对应的文件，会使用通用回退文件：

```
CustomMeleeSounds/
└─ default.mp3
```

### 查找优先级

1. `{TypeID}(.ext)`（TypeID 必须是数字）
2. `default(.ext)`

### 差分音效（变体）

为同一近战武器提供多个差分音效时，使用 `_1`、`_2`、`_3` … 后缀，模块会在命中“基准文件”后自动检测是否存在差分文件并随机选择其一：

```
CustomMeleeSounds/
├─ 98.mp3
├─ 98_1.mp3
└─ 98_2.mp3
```

规则说明：
- 命中某个“基准文件”后（如 `98.mp3`），若存在同扩展名的差分（`98_1.mp3`、`98_2.mp3`…），将只在差分集合中随机选择其一播放；若无差分，则使用基准文件。
- 支持 `.mp3`、`.wav`、`.ogg`、`.oga`，差分检测仅在命中扩展名内进行（减少磁盘访问）。

**注意**：不支持使用武器名称（如 `knife.mp3`、`axe.mp3`）作为文件名，必须使用数字 TypeID。

## 支持的音频格式

模块支持以下音频格式（按优先级顺序尝试）：
- `.mp3`（推荐，兼容性最好）
- `.wav`
- `.ogg`
- `.oga`

## ModConfig UI 配置

在游戏内 ModConfig 菜单中可以调整以下选项（需要安装 ModConfig Mod）：

### 启用自定义近战音效

- 类型：开关
- 默认值：启用
- 作用：控制是否启用自定义近战音效

### 音量

- 类型：滑块（0.0 - 2.0）
- 默认值：1.0
- 作用：调整自定义近战音效的音量

配置会实时生效，无需重启游戏。

## 目录结构示例

### 完整示例（多种武器，使用 TypeID）

```
DuckovCustomSounds/
└─ CustomMeleeSounds/
   ├─ 98.ogg                 # 铲子音效（TypeID: 98）
   ├─ 156.mp3                # TypeID 156 的近战武器
   ├─ 203.mp3                # TypeID 203 的近战武器
   ├─ 247.mp3                # TypeID 247 的近战武器
   └─ default.mp3            # 通用回退音效
```

### 最小示例（仅通用音效）

```
DuckovCustomSounds/
└─ CustomMeleeSounds/
   └─ default.mp3            # 通用音效
```

所有近战武器都会使用这个通用音效。

### 混合示例（部分武器专用，其他通用）

```
DuckovCustomSounds/
└─ CustomMeleeSounds/
   ├─ 98.ogg                 # 铲子专用音效
   ├─ 156.mp3                # TypeID 156 专用音效
   └─ default.mp3            # 其他武器使用此音效
```

## 常见问题

### 1. 如何知道武器的 TypeID？

**方法：查看日志**
1. 在 `settings.json` 中设置日志级别为 Debug：
```json
{
  "logLevels": {
    "Melee": "Debug"
  }
}
```
2. 进入游戏使用近战武器
3. 查看日志文件（`DuckovCustomSounds/Logs/`），搜索 `[MeleeAttack]` 或 `[MeleeSwing]`
4. 日志会显示 `TypeID=xxx`（xxx 是数字）

**日志示例**：
```
[MeleeAttack] TypeID=98, soundKey=shovel
查找顺序: [98.mp3] → [98.wav] → [98.ogg] → [98.oga] → [default.mp3]
最终使用: 98.ogg
```

从这个日志可以看出：
- TypeID 是 `98`（数字）
- 系统优先查找 `98.ogg`，然后才回退到 `default.mp3`

### 2. 为什么必须使用数字 TypeID？

游戏中每把近战武器都有唯一的数字 TypeID。模块通过 TypeID 来识别和匹配音效文件。

**错误示例**：
```
CustomMeleeSounds/
└─ knife.mp3          # ❌ 错误：不能使用武器名称
```

**正确示例**：
```
CustomMeleeSounds/
└─ 98.mp3             # ✅ 正确：使用数字 TypeID
```

### 3. 如何为所有武器使用相同的音效？

只需提供通用回退文件：
```
CustomMeleeSounds/
└─ default.mp3        # 所有武器的音效
```

### 4. 音效音量太大或太小怎么办？

**方法一：使用 ModConfig UI**

在游戏内调整音量滑块（0.0 - 2.0）。

**方法二：编辑音频文件**

使用音频编辑软件（如 Audacity）调整音频文件的音量。

### 5. 如何使用 .ogg 或 .wav 格式的音频？

模块已内置支持这些格式，直接使用即可：
```
CustomMeleeSounds/
├─ 98.ogg
└─ 156.wav
```

系统会按 `.mp3` → `.wav` → `.ogg` → `.oga` 的顺序尝试。

### 6. 为什么我的音效没有生效？

**可能原因**：
- 文件命名不正确（必须使用数字 TypeID）
- 文件路径错误
- 模块未启用（检查 ModConfig UI）
- 文件格式不支持

**解决方法**：
1. 确认模块已启用（ModConfig UI）
2. 检查文件命名是否正确（必须是数字，如 `98.mp3`）
3. 启用 Debug 日志，查看文件查找过程
4. 使用 `.mp3` 格式（兼容性最好）

### 7. 音效和原版音效同时播放怎么办？

这通常是因为模块未能正确拦截原版音效。请检查：
1. 确认模块已启用（ModConfig UI）
2. 查看日志，确认音效替换成功
3. 如果问题持续，请报告 Bug




## 技术细节

### 音效拦截

模块使用 Harmony Postfix 补丁拦截 `AudioManager.Post(string eventName, GameObject gameObject)` 方法：
- 检查 `eventName` 是否为近战音效事件
- 如果匹配，查找自定义文件
- 如果找到自定义文件，替换原版音效并播放自定义音效

### 3D 音效

模块使用 `AudioManager.PostCustomSFX` 接口播放自定义音频，该接口会：
- 自动创建 FMOD EventInstance
- 自动设置 3D 位置属性
- 自动跟随 GameObject 移动
- 自动应用距离衰减（默认：最小 1 米，最大 50 米）
- 自动路由到 SFX 总线
- 自动清理资源

### TypeID 获取

模块通过以下方式获取近战武器 TypeID：
1. 从 GameObject 获取 `CharacterMainControl` 组件
2. 调用 `GetMeleeWeapon()` 获取 `ItemAgent_MeleeWeapon` 组件
3. 如果失败，尝试从 GameObject 直接获取 `ItemAgent_MeleeWeapon` 组件
4. 如果失败，尝试从父对象获取
5. 如果失败，尝试从子对象获取
6. 从 `melee.Item.TypeID` 获取 TypeID（数字）

### 文件查找策略

模块使用两级查找策略：
1. **主要候选**：尝试 TypeID（数字）
2. **回退候选**：使用 `default` 作为通用回退

对于每个候选文件名，会尝试所有支持的扩展名（`.mp3`、`.wav`、`.ogg`、`.oga`）。

## 排错指引

### 启用详细日志

编辑 `settings.json`：
```json
{
  "logLevels": {
    "Melee": "Debug"
  }
}
```

或者使用 Verbose 级别查看所有路径尝试：
```json
{
  "logLevels": {
    "Melee": "Verbose"
  }
}
```

### 查看日志文件

日志位于：`DuckovCustomSounds/Logs/`

搜索关键字：
- `[MeleeAttack]`：攻击音效日志
- `[MeleeSwing]`：挥舞音效日志

### 常见错误信息

- `未找到`：没有找到匹配的自定义文件，会使用原版音效
- `新接口播放失败`：播放自定义音效时出错，检查文件格式和路径
- `Postfix 覆盖异常`：补丁执行时出错，查看详细错误信息
- `未捕获到 ItemAgent_MeleeWeapon`：无法获取近战武器组件，会回退到 soundKey 查找模式

### 测试建议

1. 先使用最简单的配置（只提供 `default.mp3` 和 `default_swing.mp3`）
2. 确认模块已启用（ModConfig UI）
3. 使用 `.mp3` 格式（兼容性最好）
4. 启用 Debug 日志，观察文件查找过程
5. 逐步添加更多武器的音效

### 性能优化

模块已进行以下性能优化：
- 日志字符串仅在对应级别启用时才构建
- 使用 LINQ 延迟执行减少内存分配
- 缓存 TypeID 查找结果

### 与其他模块的兼容性

近战音效模块与以下模块完全兼容：
- 敌人语音模块（CustomEnemySounds）
- 脚步声模块（CustomFootStepSounds）
- 枪械模块（CustomGunSounds）
- 手雷模块（CustomGrenadeSounds）
- 物品模块（CustomItemSounds）

所有模块共享同一日志系统和 FMOD 总线获取策略，互不影响。

## 高级用法

### 为特定武器禁用自定义音效

如果你想为某个武器使用原版音效，只需不提供对应的文件即可。模块会自动回退到原版音效。

### 混合使用自定义和原版音效

你可以只替换部分武器的音效（使用 TypeID）：
```
CustomMeleeSounds/
├─ 98.ogg             # 自定义铲子音效
└─ 156.mp3            # 自定义 TypeID 156 的音效
```

其他武器会继续使用原版音效。

### 使用通用音效作为回退

提供 `default.mp3` 作为通用回退：
```
CustomMeleeSounds/
├─ 98.ogg             # 铲子专用音效
├─ 156.mp3            # TypeID 156 专用音效
└─ default.mp3        # 其他武器使用此音效
```


### 音效制作建议

1. **采样率**：建议使用 44.1kHz 或 48kHz
2. **比特率**：MP3 建议 192kbps 或更高
3. **声道**：单声道或立体声均可（3D 音效会自动处理）
4. **长度**：0.3-1.5 秒（短促有力）
5. **音量**：建议归一化到 -3dB 到 0dB
6. **格式**：优先使用 MP3（兼容性最好）

### 音效设计建议

- 应该有明显的冲击感
- 可以包含武器击中目标的声音（如刀刺入身体、斧头砍入骨头、铲子敲击声）
- 音量应该较大，有存在感
- 可以包含轻微的回响或余音

### 音效来源

可以从以下渠道获取音效：
- 免费音效库（如 Freesound.org）
- 游戏音效包
- 自己录制
- 从其他游戏提取（注意版权）

注意：使用他人的音效时，请遵守相关版权协议。

## 示例配置

### 示例 1：为所有武器使用相同音效

```
CustomMeleeSounds/
└─ default.mp3            # 统一音效
```

### 示例 2：为不同武器使用不同音效（使用 TypeID）

```
CustomMeleeSounds/
├─ 98.ogg                 # 铲子音效（TypeID: 98）
├─ 156.mp3                # TypeID 156 的近战武器音效
├─ 203.mp3                # TypeID 203 的近战武器音效
└─ default.mp3            # 其他武器的通用音效
```

### 示例 3：混合配置

```
CustomMeleeSounds/
├─ 98.ogg                 # 铲子专用音效
├─ 156.mp3                # TypeID 156 专用音效
└─ default.mp3            # 其他武器的音效
```

这样，铲子和 TypeID 156 的武器会使用专用音效，其他武器会使用通用音效。
