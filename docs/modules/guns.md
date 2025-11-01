---
title: 枪械音效
---

# 枪械音效

本模块用于替换游戏内枪械的音效，包括射击音效和换弹音效。模块支持按武器类型精确匹配、消音器检测、射击速率限制、换弹取消处理等功能，同时保持与原版 FMOD 3D 音效的一致性。

> [!NOTE]
> 目前 ModConfig UI 支持尚未完善，你看到的属于前瞻性内容。

> [!NOTE]
> 此模块支持差分音效(xxx.mp3 xxx_1.mp3 etc.)

## 功能概述

- 替换枪械射击音效（支持消音器检测）
- 替换枪械换弹音效（支持开始/结束分段音效）
- 按武器 TypeID 精确匹配音效文件
- 射击速率限制（防止音效重叠）
- 换弹取消时自动停止音效
- 自动 3D 定位与距离衰减
- ModConfig UI 热重载配置

## 快速开始

### 最小可用示例

假设你想为所有枪械替换射击音效，只需：

1. 创建目录结构：
```
DuckovCustomSounds/
└─ CustomGunSounds/
   └─ default.mp3
```

2. 进入游戏，使用任意枪械射击，观察音效是否替换成功。

### 为特定枪械添加音效（推荐）

```
DuckovCustomSounds/
└─ CustomGunSounds/
   ├─ 258.mp3               # 某把枪的射击音效（TypeID: 258）
   ├─ 655.mp3               # 某把枪的射击音效（TypeID: 655）
   ├─ 258_mute.mp3          # TypeID 258 的消音器音效
   ├─ 258_reload.mp3        # TypeID 258 的换弹音效
   ├─ 258_reload_start.mp3  # TypeID 258 的换弹开始音效
   ├─ 258_reload_end.mp3    # TypeID 258 的换弹结束音效
   └─ default.mp3           # 通用回退音效
```

**重要说明**：
- TypeID 是每把枪的唯一数字标识符（如 258、655、783）
- 不同的枪即使属于同一枪族（如 AK47、AK103、AK74）也有不同的 TypeID
- 使用 TypeID 可以为每把枪提供独特的音效

## TypeID 与 soundKey 说明

### TypeID（推荐使用）

- **TypeID 是什么**：每把枪的唯一数字标识符（如 258、655、783）
- **特点**：
  - 每把枪都有唯一的 TypeID
  - 即使是同一枪族的不同型号（如 AK47、AK103、AK74）也有不同的 TypeID
  - 使用 TypeID 可以为每把枪提供独特的音效
- **如何获取**：查看日志文件（详见"常见问题"章节）

### soundKey（枪族标识符）

- **soundKey 是什么**：枪族的通用标识符（如 `rifle_ak`、`pistol_light`、`smg_mp5`）
- **特点**：
  - 同一枪族的所有枪械共享相同的 soundKey
  - 例如：AK47、AK103、AK74 可能都使用 `rifle_ak` 作为 soundKey
  - 使用 soundKey 会导致同一枪族的所有枪械共享音效
- **使用场景**：当你想为整个枪族提供统一音效时

### 音效事件格式

**射击音效**：`SFX/Combat/Gun/Shoot/{soundKey}`
- 消音器检测：如果武器装备了消音器，会优先查找带 `_mute` 后缀的文件

**换弹音效**：`SFX/Combat/Gun/Reload/{soundKey}`
- `_start` 后缀：换弹开始音效
- `_end` 后缀：换弹结束音效
- 无后缀：完整换弹音效

如果未提供分段音效，系统会使用完整换弹音效。

## 文件命名规则

### 按 TypeID 匹配（强烈推荐）

系统会优先尝试按武器的 TypeID 匹配文件。TypeID 是数字，不是枪械名称。

```
CustomGunSounds/
├─ {TypeID}.mp3              # 普通射击音效
├─ {TypeID}_mute.mp3         # 消音器音效
├─ {TypeID}_reload.mp3       # 换弹音效
├─ {TypeID}_reload_start.mp3 # 换弹开始音效
└─ {TypeID}_reload_end.mp3   # 换弹结束音效
```

**示例**（假设某把枪的 TypeID 为 `258`）：
```
CustomGunSounds/
├─ 258.mp3
├─ 258_mute.mp3
├─ 258_reload.mp3
├─ 258_reload_start.mp3
└─ 258_reload_end.mp3
```

**实际查找示例**（来自日志）：
```
[GunShoot] TypeID=783, soundKey=pistol_light_mute
查找顺序:
[783_mute.mp3] → [783_mute.wav] → [783_mute.ogg] → [783_mute.oga] →
[pistol_light_mute.mp3] → [pistol_light_mute.wav] → ... →
[783.mp3] → [783.wav] → [783.ogg] → [783.oga]
最终使用: pistol_light_mute.mp3（若存在）；否则回退到 783.mp3 或 default
```

### 按 soundKey 匹配（不推荐）

如果找不到 TypeID 对应的文件，会回退到使用 soundKey：

```
CustomGunSounds/
└─ {soundKey}.mp3
```

**注意**：使用 soundKey 会导致同一枪族的所有枪械共享音效。例如：
- 如果使用 `rifle_ak.mp3`，所有 AK 系列枪械（AK47、AK103、AK74 等）都会使用这个音效
- 这通常不是你想要的结果

### 通用回退

如果以上都找不到，会使用通用回退文件：

```
CustomGunSounds/
├─ default.mp3               # 通用射击音效
├─ default_reload.mp3        # 通用换弹音效
├─ default_reload_start.mp3  # 通用换弹开始音效
└─ default_reload_end.mp3    # 通用换弹结束音效
```

### 查找优先级

**射击音效**（装备消音器时）：
1. `{TypeID}_mute(.ext)`
2. `{soundKey}_mute(.ext)`
3. `{TypeID}(.ext)`
4. `{soundKey}(.ext)`
5. `default(.ext)`

**射击音效**（未装备消音器时）：
1. `{TypeID}.mp3`
2. `{soundKey}.mp3`
3. `default.mp3`

**换弹开始音效**：
1. `{TypeID}_reload_start.mp3`
2. `{TypeID}_reload.mp3`
3. `{soundKey}.mp3`
4. `default_reload_start.mp3`
5. `default_reload.mp3`
6. `default.mp3`

**换弹结束音效**：
1. `{TypeID}_reload_end.mp3`
2. `{soundKey}.mp3`
3. `default_reload_end.mp3`
4. `default.mp3`

## 支持的音频格式

模块支持以下音频格式（按优先级顺序尝试）：
- `.mp3`（推荐，兼容性最好）
- `.wav`
- `.ogg`
- `.oga`

## 射击速率限制

为了防止高射速武器的音效重叠，模块提供射击速率限制功能。

### 全局速率限制

在 `settings.json` 中配置：

```json
{
  "gunShootRateLimitEnabled": true,
  "gunShootMinIntervalMs": 50.0
}
```

- `gunShootRateLimitEnabled`：是否启用速率限制
- `gunShootMinIntervalMs`：最小射击间隔（毫秒）

### 按武器类型限制

可以为不同武器设置不同的速率限制（使用 TypeID）：

```json
{
  "gunShootRateLimitPerType": {
    "258": 80.0,
    "655": 60.0,
    "783": 100.0
  }
}
```

**注意**：这里的键是 TypeID（数字字符串），不是枪械名称。

如果某个武器有专门的配置，会优先使用该配置，否则使用全局配置。

### 速率限制行为

当射击间隔小于设定值时：
- 原版音效会被静音（不会播放）
- 自定义音效也不会播放
- 日志会记录被抑制的射击事件（Debug 级别）

## 换弹取消处理

当玩家取消换弹时（如切换武器、开始射击等），模块会自动停止正在播放的换弹音效。

### 保留结束音效

为了保证音效的完整性，换弹结束音效（`*_reload_end`）不会被停止，只有开始音效和完整音效会被停止。

### 相关方法

模块会拦截以下方法：
- `ItemAgent_Gun.StopReloadSound()`：停止换弹音效
- `ItemAgent_Gun.CancleReload()`：取消换弹

## ModConfig UI 配置

在游戏内 ModConfig 菜单中可以调整以下选项（需要安装 ModConfig Mod）：

### 启用自定义枪械音效

- 类型：开关
- 默认值：启用
- 作用：控制是否启用自定义枪械音效

### 音量

- 类型：滑块（0.0 - 2.0）
- 默认值：1.0
- 作用：调整自定义枪械音效的音量

配置会实时生效，无需重启游戏。

## settings.json 配置

除了 ModConfig UI，也可以直接编辑 `DuckovCustomSounds/settings.json`：

```json
{
  "gunShootRateLimitEnabled": true,
  "gunShootMinIntervalMs": 50.0,
  "gunShootRateLimitPerType": {
    "258": 80.0,
    "655": 60.0
  }
}
```

### 配置项说明

#### gunShootRateLimitEnabled

- 类型：布尔值
- 默认值：`true`
- 作用：是否启用射击速率限制

#### gunShootMinIntervalMs

- 类型：浮点数
- 默认值：`50.0`
- 单位：毫秒
- 作用：全局最小射击间隔

#### gunShootRateLimitPerType

- 类型：对象（键值对）
- 默认值：`{}`
- 作用：为特定武器类型设置专门的速率限制
- 格式：`{ "TypeID": 间隔毫秒数 }`

## 目录结构示例

### 完整示例（多种武器，使用 TypeID）

```
DuckovCustomSounds/
└─ CustomGunSounds/
   ├─ 258.mp3               # TypeID 258 的射击音效
   ├─ 258_1.mp3               # TypeID 258 的射击音效差分
   ├─ 258_mute.mp3          # TypeID 258 的消音器音效
   ├─ 258_reload.mp3        # TypeID 258 的换弹音效
   ├─ 258_reload_start.mp3  # TypeID 258 的换弹开始音效
   ├─ 258_reload_end.mp3    # TypeID 258 的换弹结束音效
   ├─ 655.mp3               # TypeID 655 的射击音效
   ├─ 655_mute.mp3          # TypeID 655 的消音器音效
   ├─ 655_reload.mp3        # TypeID 655 的换弹音效
   ├─ 783.mp3               # TypeID 783 的射击音效
   ├─ 783_reload.mp3        # TypeID 783 的换弹音效
   └─ default.mp3           # 通用回退音效
```

### 最小示例（仅通用音效）

```
DuckovCustomSounds/
└─ CustomGunSounds/
   ├─ default.mp3
   └─ default_reload.mp3
```

所有武器都会使用这两个通用音效。

## 常见问题

### 1. 如何知道武器的 TypeID？

**方法一：查看日志（推荐）**
1. 在 `settings.json` 中设置日志级别为 Debug：
```json
{
  "logLevels": {
    "Gun": "Debug"
  }
}
```
2. 进入游戏射击武器
3. 查看日志文件（`DuckovCustomSounds/Logs/`），搜索 `[GunShoot]`
4. 日志会显示 `TypeID=xxx`（xxx 是数字）

**日志示例**：
```
[GunShoot] TypeID=783, soundKey=pistol_light_mute
查找顺序:
[783_mute.mp3] → [783_mute.wav] → [783_mute.ogg] → [783_mute.oga] →
[783.mp3] → [783.wav] → [783.ogg] → [783.oga] →
[pistol_light_mute.mp3] → [pistol_light_mute.wav] → ...
最终使用: 783.mp3
```

从这个日志可以看出：
- TypeID 是 `783`（数字）
- soundKey 是 `pistol_light_mute`（枪族标识符）
- 系统优先查找 `783.mp3`，然后才回退到 `pistol_light_mute.mp3`

**方法二：使用 soundKey（不推荐）**

如果不知道 TypeID，可以使用日志中显示的 soundKey 作为文件名，但这会导致同一枪族的所有枪械共享音效。

### 2. 为什么推荐使用 TypeID 而不是 soundKey？

**原因**：
- **TypeID 是唯一的**：每把枪都有唯一的 TypeID，可以为每把枪提供独特的音效
- **soundKey 是共享的**：同一枪族的所有枪械共享相同的 soundKey

**示例**：
- AK47、AK103、AK74 可能都使用 `rifle_ak` 作为 soundKey
- 如果你创建 `rifle_ak.mp3`，这三把枪都会使用这个音效
- 但如果你创建 `258.mp3`、`655.mp3`、`783.mp3`（假设这是它们的 TypeID），每把枪都会有独特的音效

**什么时候使用 soundKey**：
- 当你想为整个枪族提供统一音效时
- 当你不在乎同一枪族的不同型号使用相同音效时

### 3. 为什么消音器音效没有生效？

**可能原因**：
- 文件命名不正确（应为 `{TypeID}_mute.mp3`）
- 武器未装备消音器
- 文件路径错误

**解决方法**：
1. 确认武器已装备消音器
2. 检查文件命名是否正确（使用数字 TypeID，如 `783_mute.mp3`）
3. 查看日志，确认查找路径

### 4. 如何禁用射击速率限制？

在 `settings.json` 中设置：
```json
{
  "gunShootRateLimitEnabled": false
}
```

或者将 `gunShootMinIntervalMs` 设置为 `0`。

### 5. 换弹音效被打断了怎么办？

这是正常行为。当玩家取消换弹时（如切换武器、开始射击），换弹音效会被停止。

如果希望保留完整的换弹音效，可以使用分段音效：
- `{TypeID}_reload_start.mp3`：换弹开始音效（会被停止）
- `{TypeID}_reload_end.mp3`：换弹结束音效（不会被停止）

例如：`258_reload_start.mp3` 和 `258_reload_end.mp3`

### 6. 如何为同一武器提供多个变体？

现在支持差分音效（变体）随机播放。为同一键名提供多个文件时，使用 `_1`、`_2`、`_3` … 后缀：

```
CustomGunSounds/
├─ 258.mp3               # 基准文件
├─ 258_1.mp3             # 差分 1
├─ 258_2.mp3             # 差分 2
├─ 258_mute.mp3          # 消音器基准
├─ 258_mute_1.mp3        # 消音器差分 1
└─ 258_mute_2.mp3        # 消音器差分 2
```

规则：
- 先按“查找优先级”决定命中的基准文件（如 `258_mute.mp3`）。
- 若存在同扩展名的差分（`*_1.mp3`、`*_2.mp3`…），在差分集合中随机选择其一播放；若无差分，使用基准文件。
- 支持 `.mp3`、`.wav`、`.ogg`、`.oga`。

### 7. 音效音量太大或太小怎么办？

**方法一：使用 ModConfig UI**

在游戏内调整音量滑块（0.0 - 2.0）。

**方法二：编辑音频文件**

使用音频编辑软件（如 Audacity）调整音频文件的音量。

### 8. 如何使用 .ogg 或 .wav 格式的音频？

模块已内置支持这些格式，直接使用即可：
```
CustomGunSounds/
├─ 258.ogg
└─ 655.wav
```

系统会按 `.mp3` → `.wav` → `.ogg` → `.oga` 的顺序尝试。

### 9. 射击音效和原版音效同时播放怎么办？

这通常是因为模块未能正确静音原版音效。请检查：
1. 确认模块已启用（ModConfig UI 或 settings.json）
2. 查看日志，确认音效替换成功
3. 如果问题持续，请报告 Bug

## 技术细节

### 音效拦截

模块使用 Harmony Postfix 补丁拦截 `AudioManager.Post(string eventName, GameObject gameObject)` 方法：
- 检查 `eventName` 是否以 `SFX/Combat/Gun/Shoot/` 或 `SFX/Combat/Gun/Reload/` 开头
- 如果匹配，提取 soundKey 并查找自定义文件
- 如果找到自定义文件，静音原版音效并播放自定义音效

### 原版音效静音

模块会对原版 FMOD EventInstance 执行以下操作：
1. 设置 `Mute` 参数为 `1.0`
2. 设置音量为 `0.0`
3. 立即停止播放（`STOP_MODE.IMMEDIATE`）

这确保原版音效不会与自定义音效同时播放。

### 3D 音效

模块使用 `AudioManager.PostCustomSFX` 接口播放自定义音频，该接口会：
- 自动创建 FMOD EventInstance
- 自动设置 3D 位置属性
- 自动跟随 GameObject 移动
- 自动应用距离衰减（默认：最小 1 米，最大 50 米）
- 自动路由到 SFX 总线
- 自动清理资源

### 射击速率限制实现

模块为每个 GameObject 记录最后一次射击时间：
- 使用 `GameObject.GetInstanceID()` 作为键
- 记录 `Time.realtimeSinceStartup`
- 新射击到来时，计算时间差
- 如果时间差小于设定值，抑制射击

### 换弹音效追踪

模块为每个枪械实例追踪正在播放的换弹音效：
- 使用 `ItemAgent_Gun.GetInstanceID()` 作为键
- 记录 EventInstance 和是否为结束音效
- 取消换弹时，停止所有非结束音效
- 保留结束音效，确保音效完整性

### 实例数量限制

为了防止音效堆叠，模块限制每个 soundKey 同时播放的实例数量：
- 最大实例数：10
- 超过限制时，停止最早的实例
- 自动清理已停止的实例

### 消音器检测

模块通过以下方式检测消音器：
1. 检查 `ItemAgent_Gun.Silenced` 属性
2. 检查 soundKey 是否以 `_mute` 结尾

如果检测到消音器，会优先查找 `{TypeID}_mute` 文件。

### TypeID 获取

模块通过以下方式获取武器 TypeID：
1. 从 GameObject 获取 `ItemAgent_Gun` 组件
2. 尝试从 `gun.Item.TypeID` 获取
3. 如果失败，尝试通过反射从 `gun.TypeID` 获取
4. 如果都失败，使用 soundKey 作为回退

## 排错指引

### 启用详细日志

编辑 `settings.json`：
```json
{
  "logLevels": {
    "Gun": "Debug"
  }
}
```

或者使用 Verbose 级别查看所有路径尝试：
```json
{
  "logLevels": {
    "Gun": "Verbose"
  }
}
```

### 查看日志文件

日志位于：`DuckovCustomSounds/Logs/`

搜索关键字：
- `[GunShoot]`：射击音效日志
- `[GunReload]`：换弹音效日志
- `[GunShoot:RateLimit]`：速率限制日志
- `[GunShoot:Limit]`：实例数量限制日志

### 常见错误信息

- `未找到`：没有找到匹配的自定义文件，会使用原版音效
- `新接口播放失败`：播放自定义音效时出错，检查文件格式和路径
- `Postfix 覆盖异常`：补丁执行时出错，查看详细错误信息

### 测试建议

1. 先使用最简单的配置（只提供 `default.mp3`）
2. 确认模块已启用（ModConfig UI）
3. 使用 `.mp3` 格式（兼容性最好）
4. 启用 Debug 日志，观察文件查找过程
5. 逐步添加更多武器的音效

### 性能优化

模块已进行以下性能优化：
- 日志字符串仅在对应级别启用时才构建
- 使用对象池减少 GC 压力
- 缓存 TypeID 查找结果
- 使用 LinkedList 管理实例列表

### 与其他模块的兼容性

枪械音效模块与以下模块完全兼容：
- 敌人语音模块（CustomEnemySounds）
- 脚步声模块（CustomFootStepSounds）
- 手雷模块（CustomGrenadeSounds）
- 近战模块（CustomMeleeSounds）
- 物品模块（CustomItemSounds）

所有模块共享同一日志系统和 FMOD 总线获取策略，互不影响。

## 高级用法

### 为特定武器禁用自定义音效

如果你想为某个武器使用原版音效，只需不提供对应的文件即可。模块会自动回退到原版音效。

### 混合使用自定义和原版音效

你可以只替换部分武器的音效（使用 TypeID）：
```
CustomGunSounds/
├─ 258.mp3           # 自定义 TypeID 258 的音效
└─ 655.mp3           # 自定义 TypeID 655 的音效
```

其他武器会继续使用原版音效。

### 使用通用音效作为回退

提供 `default.mp3` 作为通用回退：
```
CustomGunSounds/
├─ 258.mp3           # TypeID 258 专用音效
├─ 655.mp3           # TypeID 655 专用音效
└─ default.mp3       # 其他武器使用此音效
```

### 分段换弹音效

为了更好的音效体验，可以使用分段换弹音效：
```
CustomGunSounds/
├─ 258_reload_start.mp3  # 换弹开始（退弹匣）
└─ 258_reload_end.mp3    # 换弹结束（拉枪栓）
```

这样，即使换弹被取消，结束音效也会保留，避免音效突然中断。
