---
title: 撤离 BGM
---

# 撤离 BGM

本模块用于自定义撤离区域的音效，包括倒计时音效和撤离成功音效。模块提供三种工作模式，支持独立控制倒计时和成功音效，采用 SFX 音频通道以避免与场景 BGM 冲突。

> [!WARNING]
> 目前一些地图的撤离成功音效（游戏原本的）可能会被错误触发，请等待后续跟进修复。

> [!NOTE]
> 目前 ModConfig UI 支持尚未完善，你看到的属于前瞻性内容。

## 功能概述

- 替换撤离倒计时音效（剩余时间 ≤ 5 秒时触发）
- 替换撤离成功音效（成功撤离时播放）
- 三种工作模式：禁用、倒计时模式、成功替换模式
- 自动停止音效（场景切换、死亡、中止撤离时）
- ModConfig UI 热重载配置
- 兼容旧版配置（settings.json）

## 快速开始

### 最小可用示例（倒计时模式）

假设你想在撤离倒计时 ≤ 5 秒时播放自定义音效：

1. 创建目录结构：
```
DuckovCustomSounds/
└─ Extraction/
   └─ countdown.mp3
```

2. 在 ModConfig UI 中选择"倒计时音效模式"

3. 进入游戏，进入撤离区域，等待倒计时 ≤ 5 秒，观察音效是否触发

### 最小可用示例（成功替换模式）

假设你想替换撤离成功时的音效：

1. 创建目录结构：
```
DuckovCustomSounds/
└─ Extraction/
   └─ success.mp3
```

2. 在 ModConfig UI 中选择"成功音效替换模式"

3. 进入游戏，成功撤离，观察音效是否替换

## 工作模式

模块提供三种互斥的工作模式，通过 ModConfig UI 选择：

### 模式 1：禁用

- **行为**：不修改任何撤离音效，使用原版音效
- **适用场景**：不需要自定义撤离音效时

### 模式 2：倒计时音效模式（推荐）

- **行为**：
  1. 进入撤离区域并开始倒计时
  2. 当剩余时间 ≤ 5 秒时，播放自定义倒计时音效（单次播放）
  3. 撤离成功时，屏蔽原版成功音效，让倒计时音效自然播放完成
  4. 中止撤离时，立即停止倒计时音效

- **文件查找顺序**：
  1. `Extraction/countdown.mp3`
  2. `Extraction/countdown.wav`
  3. `Extraction/extraction.mp3`（回退）
  4. `Extraction/extraction.wav`（回退）

- **适用场景**：
  - 想要一个连贯的撤离音效（从倒计时到成功）
  - 音效长度覆盖倒计时和成功阶段（建议 10-15 秒）

### 模式 3：成功音效替换模式

- **行为**：
  1. 不修改倒计时音效（使用原版）
  2. 撤离成功时，拦截原版成功音效，播放自定义成功音效

- **文件查找顺序**：
  1. `Extraction/success.mp3`
  2. `Extraction/success.wav`
  3. `TitleBGM/extraction.mp3`（兼容旧版）

- **适用场景**：
  - 只想替换成功音效，保留原版倒计时音效
  - 音效长度较短（建议 3-5 秒）

## 文件命名规则

### 倒计时音效（倒计时模式）

**推荐命名**：
```
DuckovCustomSounds/
└─ Extraction/
   └─ countdown.mp3
```

**回退命名**（兼容旧版）：
```
DuckovCustomSounds/
└─ Extraction/
   └─ extraction.mp3
```

**查找优先级**：
1. `countdown.mp3`
2. `countdown.wav`
3. `extraction.mp3`
4. `extraction.wav`

### 成功音效（成功替换模式）

**推荐命名**：
```
DuckovCustomSounds/
└─ Extraction/
   └─ success.mp3
```

**回退命名**（兼容旧版）：
```
DuckovCustomSounds/
└─ TitleBGM/
   └─ extraction.mp3
```

**查找优先级**：
1. `Extraction/success.mp3`
2. `Extraction/success.wav`
3. `TitleBGM/extraction.mp3`（兼容旧版）

### 支持的音频格式

系统会按以下顺序尝试不同的文件扩展名：
1. `.mp3`（推荐）
2. `.wav`

## ModConfig UI 配置

模块支持通过 ModConfig UI 进行热重载配置（无需重启游戏）：

**配置项：撤离音乐模式**

- **禁用：不修改任何撤离音效**
  - 使用原版撤离音效

- **倒计时音效模式：播放倒计时音效并屏蔽成功Stinger**
  - 在倒计时 ≤ 5 秒时播放自定义音效
  - 撤离成功时屏蔽原版成功音效

- **成功音效替换模式：仅替换成功Stinger**
  - 只替换撤离成功时的音效
  - 保留原版倒计时音效

配置变更会立即生效，并自动停止当前播放的撤离音效。

## settings.json 配置

当 ModConfig UI 不可用时，模块会从 `settings.json` 读取配置。配置文件位于：
```
DuckovCustomSounds/settings.json
```

### 可用字段

**字段：overrideExtractionBGM**
- 类型：`boolean`
- 默认值：`false`
- 说明：旧版配置字段，用于兼容旧版本。`true` 表示启用倒计时音效模式，`false` 表示禁用

### 配置示例

**示例 1：禁用（默认）**
```json
{
  "overrideExtractionBGM": false
}
```

**示例 2：启用倒计时音效模式**
```json
{
  "overrideExtractionBGM": true
}
```

### 配置优先级

模块按以下优先级加载配置：
1. **ModConfig UI 配置**（如果可用）- 最高优先级，支持热重载
2. **settings.json 配置** - 回退方案，需要重启游戏生效

注意：
- 如果 ModConfig UI 可用，settings.json 中的 `overrideExtractionBGM` 字段会被忽略
- 如果 ModConfig UI 不可用，模块会自动迁移旧版配置：
  - `overrideExtractionBGM: true` → 倒计时音效模式
  - `overrideExtractionBGM: false` → 禁用
- 修改 settings.json 后需要重启游戏才能生效
- **推荐使用 ModConfig UI 进行配置**，支持三种模式且更直观

## 目录结构示例

### 完整示例（倒计时模式）

```
DuckovCustomSounds/
└─ Extraction/
   └─ countdown.mp3          # 倒计时音效（≤5秒时触发）
```

### 完整示例（成功替换模式）

```
DuckovCustomSounds/
└─ Extraction/
   └─ success.mp3            # 成功音效
```

### 完整示例（兼容旧版）

```
DuckovCustomSounds/
├─ Extraction/
│  └─ extraction.mp3         # 倒计时音效（回退）
└─ TitleBGM/
   └─ extraction.mp3         # 成功音效（回退）
```

## 常见问题

### 1. 倒计时音效没有触发？

**可能原因**：
- 模式未选择"倒计时音效模式"
- 文件不存在或命名错误
- 倒计时时间过短（< 5 秒）

**解决方法**：
1. 确认 ModConfig UI 中选择了"倒计时音效模式"
2. 确认文件存在：`Extraction/countdown.mp3` 或 `Extraction/extraction.mp3`
3. 确保倒计时时间 > 5 秒（音效在剩余 ≤ 5 秒时触发）
4. 启用 Debug 日志，查看触发信息

### 2. 成功音效没有替换？

**可能原因**：
- 模式未选择"成功音效替换模式"
- 文件不存在或命名错误

**解决方法**：
1. 确认 ModConfig UI 中选择了"成功音效替换模式"
2. 确认文件存在：`Extraction/success.mp3` 或 `TitleBGM/extraction.mp3`
3. 启用 Debug 日志，查看拦截信息

### 3. 音效在场景切换后还在播放？

这是异常情况。模块会在以下时机自动停止音效：
- 场景切换
- 死亡
- 中止撤离
- 调用 `AudioManager.StopBGM()`

如果音效没有停止，请报告 Bug。

### 4. 倒计时模式下，成功音效播放了两次？

这是正常行为。倒计时模式会：
1. 在倒计时 ≤ 5 秒时播放倒计时音效
2. 撤离成功时屏蔽原版成功音效，让倒计时音效自然播放完成

如果你听到两次音效，可能是：
- 倒计时音效太短，播放完成后原版成功音效又播放了
- 建议使用较长的倒计时音效（10-15 秒）

### 5. 如何同时使用倒计时和成功音效？

模块的两种模式是互斥的，不能同时使用。但你可以：

**方案 1：使用倒计时模式 + 长音效**
- 创建一个 10-15 秒的音效，包含倒计时和成功两个阶段
- 在倒计时 ≤ 5 秒时触发，持续到成功

**方案 2：使用成功替换模式**
- 只替换成功音效，保留原版倒计时音效

### 6. 如何禁用撤离音效？

在 ModConfig UI 中选择"禁用：不修改任何撤离音效"。

### 7. 音效音量太大或太小怎么办？

撤离音效使用 SFX 音频通道，音量受游戏 SFX 音量设置影响。

**解决方法**：
1. 调整游戏内 SFX 音量
2. 使用音频编辑软件（如 Audacity）调整音频文件的音量

### 8. 旧版配置（settings.json）还能用吗？

可以。模块会自动迁移旧版配置：
- `overrideExtractionBGM: true` → 倒计时音效模式
- `overrideExtractionBGM: false` → 禁用

但推荐使用 ModConfig UI 进行配置，支持热重载且更直观。

## 技术细节

### 音效拦截

模块使用 Harmony 补丁拦截以下方法：

**倒计时生命周期**：
- `CountDownArea.BeginCountDown`：倒计时开始
- `CountDownArea.UpdateCountDown`：每帧检查剩余时间
- `CountDownArea.AbortCountDown`：中止撤离
- `CountDownArea.OnCountdownSucceed`：撤离成功

**成功音效拦截**：
- `AudioManager.PlayStringer(string key)`：拦截撤离成功 Stinger

**场景切换保护**：
- `AudioManager.StopBGM()`：场景切换时停止音效

### 撤离成功 Stinger 识别

模块使用白名单精确匹配撤离成功 Stinger：
- `stg_map_zero`：主要撤离成功 Stinger
- `stg_map_farm`：农场地图撤离成功 Stinger

只有这些 Stinger 会被拦截，其他 Stinger 会放行。

### 倒计时触发机制

1. 进入撤离区域，`BeginCountDown` 被调用
2. 每帧调用 `UpdateCountDown`，检查剩余时间
3. 当剩余时间 ≤ 5 秒时，触发倒计时音效（单次）
4. 撤离成功时，屏蔽原版成功 Stinger，让倒计时音效自然播放完成
5. 中止撤离时，立即停止倒计时音效

### 音频通道

撤离音效使用 SFX 音频通道，不占用 BGM 通道：
- **倒计时音效**：使用 `AudioManager.PostCustomSFX()`（2D 音效）
- **成功音效**：使用 `AudioManager.PlayCustomBGM()`（BGM 通道，但不循环）

### 实例管理

模块会追踪当前播放的音效实例：
- `_currentCountdownInstance`：倒计时音效实例
- `_currentLegacyInstance`：旧版 extraction.mp3 实例

在以下时机会停止实例：
- 中止撤离
- 场景切换
- 模式切换（热重载）

### 弱引用追踪

模块使用 `WeakReference` 追踪当前的 `CountDownArea` 实例：
- 避免内存泄漏
- 确保只追踪一个活动的撤离区域

## 排错指引

### 启用详细日志

编辑 `settings.json`：
```json
{
  "logLevels": {
    "ExtractionBGM": "Debug"
  }
}
```

### 查看日志文件

日志位于：`DuckovCustomSounds/Logs/`

搜索关键字：
- `撤离倒计时开始`：倒计时开始
- `倒计时音效已触发`：倒计时音效触发
- `撤离成功`：撤离成功
- `检测到撤离Stinger事件`：成功 Stinger 拦截
- `已播放自定义撤离成功音效`：成功音效播放

### 常见错误信息

- `未找到倒计时音效文件`：倒计时音效文件不存在
- `未找到成功音效文件`：成功音效文件不存在
- `播放倒计时音效失败`：播放倒计时音效时出错
- `播放成功音效失败`：播放成功音效时出错

### 测试建议

1. 先使用最简单的配置（只提供 `countdown.mp3` 或 `success.mp3`）
2. 确认模式选择正确（ModConfig UI）
3. 使用 `.mp3` 格式（兼容性最好）
4. 启用 Debug 日志，观察触发过程
5. 测试不同场景：
   - 正常撤离成功
   - 中止撤离
   - 场景切换

### 性能优化

模块已进行以下性能优化：
- 日志字符串仅在对应级别启用时才构建
- 使用弱引用避免内存泄漏
- 自动清理音频资源
- 使用白名单精确匹配 Stinger，避免误拦截
