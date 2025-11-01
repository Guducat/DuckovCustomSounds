---
title: 手雷音效
---

# 手雷音效

本模块用于替换游戏内手雷和爆炸物的音效。模块支持按 soundKey 精确匹配，并提供通用回退机制，同时保持与原版 FMOD 3D 音效的一致性。

> [!WARNING]
> 手雷的音效命名请不要参考本文中给出的示例，本文仅抛砖引玉，实际上的名字还需要寻找。

> [!NOTE]
> 目前 ModConfig UI 支持尚未完善，你看到的属于前瞻性内容。

## 功能概述

- 替换手雷和爆炸物音效
- 按 soundKey 精确匹配音效文件
- 自动 3D 定位与距离衰减
- ModConfig UI 热重载配置

## 快速开始

### 最小可用示例

假设你想为所有手雷和爆炸物替换音效，只需：

1. 创建目录结构：
```
DuckovCustomSounds/
└─ CustomGrenadeSounds/
   ├─ throw_grenade.mp3       # 通用投掷物音效
   └─ default.mp3
```

2. 进入游戏，投掷手雷或触发爆炸，观察音效是否替换成功。

### 为特定爆炸物添加音效

```
DuckovCustomSounds/
└─ CustomGrenadeSounds/
   ├─ explode_grenade.mp3       # 普通手雷音效
   ├─ explode_flash.mp3      # 闪光弹音效（示例，实际上不叫这个）
   ├─ explode_smoke.mp3      # 烟雾弹音效（示例，实际上不叫这个）
   └─ default.mp3            # 通用回退音效
```

**重要说明**：
- soundKey 是游戏内部的爆炸物标识符（如 `grenade_frag`、`grenade_flash`）
- 不同的爆炸物有不同的 soundKey
- 需要通过日志查看具体的 soundKey（详见"常见问题"章节）

## soundKey 说明

### soundKey（必须使用）

- **soundKey 是什么**：每种爆炸物的内部标识符（如 `explode_grenade`）
- **特点**：
  - soundKey 是字符串标识符
  - 不同的爆炸物有不同的 soundKey
  - 例如：手雷的 soundKey 是 `explode_grenade`
- **如何获取**：查看日志文件（详见"常见问题"章节）

### 游戏音效事件

游戏中的爆炸物音效事件格式为：`SFX/Combat/Explosive/{soundKey}`

模块会拦截这个事件并替换为自定义音效。

## 文件命名规则

### 按 soundKey 匹配（推荐）

系统会优先尝试按爆炸物的 soundKey 匹配文件：

```
CustomGrenadeSounds/
└─ {soundKey}.mp3
```

**示例**（假设破片手雷的 soundKey 为 `grenade_frag`）：
```
CustomGrenadeSounds/
└─ grenade_frag.mp3       # 破片手雷音效
```

**更多示例**：
```
CustomGrenadeSounds/
├─ grenade_frag.mp3       # 破片手雷
├─ grenade_flash.mp3      # 闪光弹
├─ grenade_smoke.mp3      # 烟雾弹
└─ explosive_c4.mp3       # C4 炸药
```

### 通用回退

如果找不到 soundKey 对应的文件，会使用通用回退文件：

```
CustomGrenadeSounds/
└─ default.mp3
```

### 查找优先级

1. `{soundKey}.mp3`
2. `default.mp3`

### 支持的音频格式

系统会按以下顺序尝试不同的文件扩展名：
1. `.mp3`
2. `.wav`
3. `.ogg`
4. `.oga`

**示例**：如果 soundKey 为 `grenade_frag`，系统会按以下顺序查找：
```
grenade_frag.mp3 → grenade_frag.wav → grenade_frag.ogg → grenade_frag.oga →
default.mp3 → default.wav → default.ogg → default.oga
```

## ModConfig UI 配置

模块支持通过 ModConfig UI 进行热重载配置（无需重启游戏）：

- **启用自定义手雷音效**：开关模块功能
- **音量 (0~2)**：调整音效音量（1.0 为原始音量）

配置变更会立即生效，无需重启游戏。

## 目录结构示例

### 完整示例（多种爆炸物）

```
DuckovCustomSounds/
└─ CustomGrenadeSounds/
   ├─ grenade_frag.mp3       # 破片手雷音效
   ├─ grenade_flash.mp3      # 闪光弹音效
   ├─ grenade_smoke.mp3      # 烟雾弹音效
   ├─ explosive_c4.mp3       # C4 炸药音效
   └─ default.mp3            # 通用回退音效
```

### 最小示例（仅通用音效）

```
DuckovCustomSounds/
└─ CustomGrenadeSounds/
   └─ default.mp3            # 通用音效
```

所有爆炸物都会使用这个通用音效。

### 混合示例（部分爆炸物专用，其他通用）

```
DuckovCustomSounds/
└─ CustomGrenadeSounds/
   ├─ grenade_frag.mp3       # 破片手雷专用音效
   ├─ grenade_flash.mp3      # 闪光弹专用音效
   └─ default.mp3            # 其他爆炸物使用此音效
```

## 常见问题

### 1. 如何知道爆炸物的 soundKey？

**方法：查看日志**
1. 在 `settings.json` 中设置日志级别为 Debug：
```json
{
  "logLevels": {
    "Grenade": "Debug"
  }
}
```
2. 进入游戏投掷手雷或触发爆炸
3. 查看日志文件（`DuckovCustomSounds/Logs/`），搜索 `[Grenade]`
4. 日志会显示 `soundKey=xxx`

**日志示例**：
```
[Grenade] soundKey=grenade_frag, 使用: grenade_frag.mp3
```

从这个日志可以看出：
- soundKey 是 `grenade_frag`
- 系统使用了 `grenade_frag.mp3` 文件

### 2. 如何为所有爆炸物使用相同的音效？

只需提供通用回退文件：
```
CustomGrenadeSounds/
└─ default.mp3        # 所有爆炸物的音效
```

### 3. 音效音量太大或太小怎么办？

**方法一：使用 ModConfig UI**

在游戏内调整音量滑块（0.0 - 2.0）。

**方法二：编辑音频文件**

使用音频编辑软件（如 Audacity）调整音频文件的音量。

### 4. 如何使用 .ogg 或 .wav 格式的音频？

模块已内置支持这些格式，直接使用即可：
```
CustomGrenadeSounds/
├─ grenade_frag.ogg
└─ grenade_flash.wav
```

系统会按 `.mp3` → `.wav` → `.ogg` → `.oga` 的顺序尝试。

### 5. 为什么我的音效没有生效？

**可能原因**：
- 文件命名不正确（soundKey 不匹配）
- 文件路径错误
- 模块未启用（检查 ModConfig UI）
- 文件格式不支持

**解决方法**：
1. 确认模块已启用（ModConfig UI）
2. 启用 Debug 日志，查看实际的 soundKey
3. 检查文件命名是否与 soundKey 匹配
4. 使用 `.mp3` 格式（兼容性最好）

### 6. 音效和原版音效同时播放怎么办？

这通常是因为模块未能正确拦截原版音效。请检查：
1. 确认模块已启用（ModConfig UI）
2. 查看日志，确认音效替换成功
3. 如果问题持续，请报告 Bug

### 7. 闪光弹和烟雾弹有音效吗？

闪光弹和烟雾弹可能有爆炸音效（取决于游戏设计）。如果有，可以通过日志查看它们的 soundKey 并替换。

### 8. 如何区分不同类型的手雷？

通过日志查看每种手雷的 soundKey，然后为每个 soundKey 创建对应的音效文件。

## 技术细节

### 音效拦截

模块使用 Harmony Postfix 补丁拦截 `AudioManager.Post(string eventName, GameObject gameObject)` 方法：
- 检查 `eventName` 是否以 `SFX/Combat/Explosive/` 开头
- 如果匹配，提取 soundKey 并查找自定义文件
- 如果找到自定义文件，替换原版音效并播放自定义音效

### 3D 音效

模块使用 `AudioManager.PostCustomSFX` 接口播放自定义音频，该接口会：
- 自动创建 FMOD EventInstance
- 自动设置 3D 位置属性
- 自动跟随 GameObject 移动
- 自动应用距离衰减（从原版事件提取或使用默认值）
- 自动路由到 SFX 总线
- 自动清理资源

### 文件查找策略

模块使用两级查找策略：
1. **主要候选**：尝试 soundKey
2. **回退候选**：使用 `default` 作为通用回退

对于每个候选文件名，会尝试所有支持的扩展名（`.mp3`、`.wav`、`.ogg`、`.oga`）。

### 距离衰减

模块会尝试从原版音效事件中提取距离衰减参数（minDistance、maxDistance），并应用到自定义音效上，确保音效的空间感与原版一致。

## 排错指引

### 启用详细日志

编辑 `settings.json`：
```json
{
  "logLevels": {
    "Grenade": "Debug"
  }
}
```

或者使用 Verbose 级别查看所有路径尝试：
```json
{
  "logLevels": {
    "Grenade": "Verbose"
  }
}
```

### 查看日志文件

日志位于：`DuckovCustomSounds/Logs/`


### 测试建议

1. 先使用最简单的配置（只提供 `default.mp3`）
2. 确认模块已启用（ModConfig UI）
3. 使用 `.mp3` 格式（兼容性最好）
4. 启用 Debug 日志，观察文件查找过程
5. 逐步添加更多爆炸物的音效

### 性能优化

模块已进行以下性能优化：
- 日志字符串仅在对应级别启用时才构建
- 使用 LINQ 优化文件查找
- 自动清理音频资源

### 与其他模块的兼容性

手雷音效模块与以下模块完全兼容：
- 敌人语音模块（CustomEnemySounds）
- 脚步声模块（CustomFootStepSounds）
- 枪械模块（CustomGunSounds）
- 近战模块（CustomMeleeSounds）
- 物品模块（CustomItemSounds）

所有模块共享同一日志系统和 FMOD 总线获取策略，互不影响。

## 高级用法

### 为特定爆炸物禁用自定义音效

如果你想为某个爆炸物使用原版音效，只需不提供对应的文件即可。模块会自动回退到原版音效。

### 混合使用自定义和原版音效

你可以只替换部分爆炸物的音效：
```
CustomGrenadeSounds/
├─ grenade_frag.mp3       # 自定义破片手雷音效
└─ grenade_flash.mp3      # 自定义闪光弹音效
```

其他爆炸物会继续使用原版音效。

### 使用通用音效作为回退

提供 `default.mp3` 作为通用回退：
```
CustomGrenadeSounds/
├─ grenade_frag.mp3       # 破片手雷专用音效
├─ grenade_flash.mp3      # 闪光弹专用音效
└─ default.mp3            # 其他爆炸物使用此音效
```

### 音效制作建议

1. **采样率**：建议使用 44.1kHz 或 48kHz
2. **比特率**：MP3 建议 192kbps 或更高
3. **声道**：单声道或立体声均可（3D 音效会自动处理）
4. **长度**：1-3 秒（爆炸音效通常较长，包含余音）
5. **音量**：建议归一化到 -3dB 到 0dB
6. **格式**：优先使用 MP3（兼容性最好）

### 音效设计建议

- 应该有明显的爆炸冲击感
- 可以包含低频轰鸣声（增强震撼感）
- 可以包含碎片飞溅声（增强真实感）
- 音量应该较大，有存在感
- 应该包含余音和回响（增强空间感）
- 不同类型的爆炸物应该有不同的音效特征：
  - 破片手雷：尖锐的爆炸声 + 金属碎片声
  - 闪光弹：短促的爆炸声 + 高频啸叫
  - 烟雾弹：低沉的爆炸声 + 气体释放声
  - C4 炸药：深沉的爆炸声 + 强烈的低频

### 音效来源

可以从以下渠道获取音效：
- Freesound.org（免费音效库）
- Zapsplat.com（免费音效库）
- 自己录制或合成
- 从其他游戏提取（注意版权）

注意：使用他人的音效时，请遵守相关版权协议。

## 示例配置

### 示例 1：为所有爆炸物使用相同音效

```
CustomGrenadeSounds/
└─ default.mp3            # 统一音效
```

### 示例 2：为不同爆炸物使用不同音效

```
CustomGrenadeSounds/
├─ grenade_frag.mp3       # 破片手雷音效
├─ grenade_flash.mp3      # 闪光弹音效
├─ grenade_smoke.mp3      # 烟雾弹音效
├─ explosive_c4.mp3       # C4 炸药音效
└─ default.mp3            # 其他爆炸物的通用音效
```

### 示例 3：混合配置

```
CustomGrenadeSounds/
├─ grenade_frag.mp3       # 破片手雷专用音效
└─ default.mp3            # 其他爆炸物的音效
```

这样，破片手雷会使用专用音效，其他爆炸物会使用通用音效。

