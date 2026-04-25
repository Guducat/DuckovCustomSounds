---
title: 地堡留声机 BGM
---

# 地堡留声机 BGM

本模块用于自定义地堡主页和主菜单的背景音乐，支持播放列表、随机播放、自动切歌、音量控制等功能。模块会拦截游戏的留声机系统，使用自定义音乐替换原版 BGM。

## 功能概述

- 替换地堡留声机 BGM（HomeBGM 文件夹中的所有音乐）
- 支持播放列表（任意数量的音乐文件）
- 自动切歌（曲目结束后自动播放下一首）
- 随机播放（支持避免立即重复）
- 音量控制（0-100%）
- 总线切换（Music 总线或 SFX 总线）
- ModConfig UI 热重载配置

## 快速开始

### 最小可用示例（地堡留声机）

假设你想在地堡主页播放自定义音乐：

1. 创建目录结构：
```
DuckovCustomSounds/
└─ HomeBGM/
   ├─ 曲名1 - 艺术家1.mp3
   ├─ 曲名2 - 艺术家2.mp3
   └─ 曲名3 - 艺术家3.mp3
```

2. 进入游戏，进入地堡主页，观察留声机是否播放自定义音乐

3. 使用留声机的"下一首"/"上一首"按钮切换音乐

## 文件命名规则

### 地堡留声机 BGM

**文件路径**：
```
DuckovCustomSounds/
└─ HomeBGM/
   ├─ 曲名1 - 艺术家1.mp3
   ├─ 曲名2 - 艺术家2.mp3
   └─ 曲名3 - 艺术家3.mp3
```

**文件名格式**：
- **推荐格式**：`曲名 - 艺术家.mp3`
  - 例如：`Never Gonna Give You Up - Rick Astley.mp3`
  - 模块会自动解析为：曲名 = "Never Gonna Give You Up"，艺术家 = "Rick Astley"

- **简单格式**：`曲名.mp3`
  - 例如：`My Favorite Song.mp3`
  - 模块会解析为：曲名 = "My Favorite Song"，艺术家 = "群星"

**说明**：
- 文件名可以任意，但推荐使用 `曲名 - 艺术家.mp3` 格式
- 支持任意数量的音乐文件
- 留声机会显示解析后的曲名和艺术家

### 支持的音频格式

目前仅支持 `.mp3` 格式。

## ModConfig UI 配置

模块支持通过 ModConfig UI 进行热重载配置（无需重启游戏）：

> [!NOTE]
> 目前 ModConfig UI 支持尚未完善，你看到的属于前瞻性内容。

**配置项：启用主页BGM**
- 启用或禁用整个模块
- 默认：启用

**配置项：启用进入基地音效 (start.mp3)**
- 启用或禁用进入基地时的音效
- 默认：启用
- 注意：此功能需要 `start.mp3` 文件（当前文档未涉及）

**配置项：音乐音量 (%)**
- 控制 HomeBGM 的音量（0-100%）
- 默认：100%
- 变更会立即应用到当前播放的音乐

**配置项：使用 SFX 总线播放（实验性）**
- 将音乐从 Music 总线切换到 SFX 总线
- 默认：禁用（使用 Music 总线）
- 用途：可能改善立体声效果，但受 SFX 音量控制影响
- 变更会重新播放当前音乐

**配置项：随机播放(Next)**
- 启用后，点击"下一首"会随机选择音乐
- 默认：禁用（顺序播放）

**配置项：上一曲也随机**
- 启用后，点击"上一首"也会随机选择音乐
- 默认：禁用（顺序播放）
- 需要先启用"随机播放(Next)"

**配置项：避免连续重复同一曲目**
- 启用后，随机播放时不会连续播放同一首音乐
- 默认：启用
- 仅在随机播放模式下生效

**配置项：自动播放下一曲**
- 启用后，曲目结束时自动播放下一首
- 默认：启用
- 禁用后，曲目结束时会循环播放当前曲目

## settings.json 配置

当 ModConfig UI 不可用时，模块会从 `settings.json` 读取配置。配置文件位于：
```
DuckovCustomSounds/settings.json
```

### 可用字段

**字段：homeBgmAutoPlayNext**
- 类型：`boolean`
- 默认值：`true`
- 说明：控制曲目结束后是否自动播放下一首。`true` 表示自动切歌，`false` 表示单曲循环

**字段：homeBgmRandomEnabled**
- 类型：`boolean`
- 默认值：`false`
- 说明：启用随机播放模式。`true` 表示点击"下一首"时随机选择，`false` 表示顺序播放

**字段：homeBgmRandomNoRepeat**
- 类型：`boolean`
- 默认值：`true`
- 说明：避免连续重复同一曲目。仅在 `homeBgmRandomEnabled=true` 时生效

**字段：homeBgmRandomizePrevious**
- 类型：`boolean`
- 默认值：`false`
- 说明：点击"上一首"时也随机选择。仅在 `homeBgmRandomEnabled=true` 时生效

### 完整配置示例

**示例 1：顺序播放 + 自动切歌（默认）**
```json
{
  "homeBgmAutoPlayNext": true,
  "homeBgmRandomEnabled": false,
  "homeBgmRandomNoRepeat": true,
  "homeBgmRandomizePrevious": false
}
```

**示例 2：随机播放 + 避免重复**
```json
{
  "homeBgmAutoPlayNext": true,
  "homeBgmRandomEnabled": true,
  "homeBgmRandomNoRepeat": true,
  "homeBgmRandomizePrevious": false
}
```

**示例 3：单曲循环**
```json
{
  "homeBgmAutoPlayNext": false,
  "homeBgmRandomEnabled": false,
  "homeBgmRandomNoRepeat": true,
  "homeBgmRandomizePrevious": false
}
```

**示例 4：完全随机（包括上一首）**
```json
{
  "homeBgmAutoPlayNext": true,
  "homeBgmRandomEnabled": true,
  "homeBgmRandomNoRepeat": true,
  "homeBgmRandomizePrevious": true
}
```

### 配置优先级

模块按以下优先级加载配置：
1. **ModConfig UI 配置**（如果可用）- 最高优先级，支持热重载
2. **settings.json 配置** - 回退方案，需要重启游戏生效

注意：
- 如果 ModConfig UI 可用，settings.json 中的这些字段会被忽略
- 如果 ModConfig UI 不可用，模块会自动使用 settings.json 配置
- 修改 settings.json 后需要重启游戏才能生效

## 目录结构示例

### 完整示例（地堡留声机）

```
DuckovCustomSounds/                     
└─ HomeBGM/
   ├─ Never Gonna Give You Up - Rick Astley.mp3
   ├─ Bohemian Rhapsody - Queen.mp3
   ├─ Hotel California - Eagles.mp3
   └─ Stairway to Heaven - Led Zeppelin.mp3
```

## 常见问题

### 1. 地堡留声机不播放自定义音乐？

**可能原因**：
- `HomeBGM/` 文件夹不存在或为空
- 文件格式不是 `.mp3`
- 模块未启用

**解决方法**：
1. 确认 `HomeBGM/` 文件夹存在且包含至少一首 `.mp3` 文件
2. 确认 ModConfig UI 中"启用主页BGM"已启用
3. 启用 Debug 日志，查看加载信息

### 2. 切歌时有爆音或卡顿？

**可能原因**：
- 音乐文件的采样率或响度不统一
- 音乐文件没有淡出

**解决方法**：
1. 使用音频编辑软件（如 Audacity）统一采样率（建议 44.1kHz 或 48kHz）
2. 统一响度（建议归一化到 -3dB 到 0dB）
3. 在音乐结尾添加 0.5-1 秒的淡出

### 3. 自动切歌不工作？

**可能原因**：
- ModConfig UI 中"自动播放下一曲"未启用
- 音乐文件循环播放（自动切歌时不会循环）

**解决方法**：
1. 确认 ModConfig UI 中"自动播放下一曲"已启用
2. 等待当前曲目播放完成（不要手动停止）
3. 启用 Debug 日志，查看自动切歌信息

### 4. 随机播放总是重复同一首？

**可能原因**：
- 只有一首音乐
- "避免连续重复同一曲目"未启用

**解决方法**：
1. 确认 `HomeBGM/` 文件夹中有多首音乐
2. 确认"避免连续重复同一曲目"已启用

### 5. 音量太小或太大？

**可能原因**：
- ModConfig UI 中的音量设置不合适
- 音乐文件本身的音量不合适
- 使用 SFX 总线时受游戏 SFX 音量影响

**解决方法**：
1. 调整 ModConfig UI 中的"音乐音量 (%)"
2. 使用音频编辑软件调整音乐文件的音量
3. 如果使用 SFX 总线，调整游戏内 SFX 音量

### 6. 留声机显示的曲名或艺术家不正确？

**可能原因**：
- 文件名格式不符合 `曲名 - 艺术家.mp3`

**解决方法**：
1. 重命名文件为 `曲名 - 艺术家.mp3` 格式
2. 确保使用英文减号 `-`（不是中文破折号 `—`）
3. 重新加载模块（重启游戏）

### 7. 场景切换后音乐还在播放？

这是异常情况。模块会在以下时机自动停止音乐：
- 场景切换
- 进入战斗
- 调用 `AudioManager.StopBGM()`

如果音乐没有停止，请报告 Bug。

## 技术细节

### 留声机拦截

模块使用 Harmony 补丁拦截以下方法：

**留声机系统拦截**：
- `BaseBGMSelector.Awake()`：重建 `entries` 列表，添加自定义 BGM
- `BaseBGMSelector.Set(int, bool, bool)`：拦截播放，使用自定义音乐
- `BaseBGMSelector.SetNext()`：处理随机模式和索引循环
- `BaseBGMSelector.SetPrevious()`：处理随机模式和索引循环
- `BaseBGMSelector.Set(string)`：拦截 UI 直接点击条目

**场景切换保护**：
- `AudioManager.StopBGM()`：场景切换时停止自定义音乐
- `MultiSceneCore.LocalOnSubSceneWillBeUnloaded()`：场景卸载时停止音乐

**Stinger 抑制**：
- `AudioObject.Post(string, bool)`：抑制留声机 Stinger 事件（仅在主基地/主菜单）

### 自动切歌机制

1. **播放开始**：播放音乐时，设置 `loop=false`（不循环单曲）
2. **状态检测**：每帧检查播放状态（PLAYING → STOPPED）
3. **武装机制**：检测到 PLAYING 状态后武装，检测到 STOPPED 状态后触发切歌
4. **切换下一首**：调用 `PlayNextHomeBGM()`，根据随机模式选择下一首
5. **防误触发**：只有武装后才会触发自动切歌


### 音频通道

模块支持两种音频通道：

**Music 总线（默认）**：
- 使用 `AudioManager.bgmSource.PostFile()`
- 事件路径：`Music/custom_loop`（循环）或 `Music/custom`（不循环）
- `doRelease=false`：保持实例有效，便于自动切歌检测
- 优点：不受 SFX 音量影响
- 缺点：可能与其他 BGM 冲突

**SFX 总线（实验性）**：
- 使用 `AudioManager.stingerSource.PostCustomSFX()`
- 优点：可能改善立体声效果
- 缺点：受游戏 SFX 音量控制影响

### 文件名解析

模块会自动解析文件名：

**格式 1：`曲名 - 艺术家.mp3`**
- 解析为：`name = "曲名"`, `author = "艺术家"`
- 例如：`Never Gonna Give You Up - Rick Astley.mp3`
  - `name = "Never Gonna Give You Up"`
  - `author = "Rick Astley"`

**格式 2：`曲名.mp3`**
- 解析为：`name = "曲名"`, `author = "群星"`
- 例如：`My Favorite Song.mp3`
  - `name = "My Favorite Song"`
  - `author = "群星"`

### 随机播放算法

**避免立即重复**：
- 排除当前索引的等概率采样
- 算法：`r = Random.Range(0, count - 1); if (r >= current) r++;`
- 确保不会连续播放同一首音乐

**不避免重复**：
- 等概率采样：`r = Random.Range(0, count);`

### 实例管理

模块会追踪当前播放的音乐实例：
- `_currentBGMInstance`：当前播放的 BGM 实例

在以下时机会停止实例：
- 场景切换
- 播放新音乐
- 模式切换（热重载）

### 递归保护

模块使用多种机制防止递归调用：
- `_isStoppingBGM`：防止 `StopCurrentBGM()` 递归
- `_isProcessingSet`：防止 `Set()` 重入
- `_manualInvokeFlag`：标记手动切歌，确保信息气泡显示

## 排错指引

### 启用详细日志

编辑 `settings.json`：
```json
{
  "logLevels": {
    "HomeBGM": "Debug"
  }
}
```

### 查看日志文件

日志位于：`DuckovCustomSounds/Logs/`

搜索关键字：
- `找到主菜单音乐`：主菜单 BGM 加载
- `在 HomeBGM 中找到`：HomeBGM 文件夹扫描
- `加载 BGM 元数据`：音乐文件解析
- `拦截到 mus_title`：主菜单 BGM 拦截
- `已重建 BaseBGMSelector.entries`：留声机列表重建
- `拦截留声机播放自定义 BGM`：留声机播放拦截
- `自动切歌`：自动切歌触发

### 常见错误信息

- `未找到主菜单音乐文件（可选）`：`TitleBGM/title.mp3` 不存在
- `未找到大厅音乐文件夹（可选）`：`HomeBGM/` 文件夹不存在
- `播放主菜单 BGM 失败`：播放主菜单 BGM 时出错
- `播放 Home BGM 失败`：播放地堡留声机 BGM 时出错
- `播放自定义 BGM 失败`：播放自定义音乐时出错

### 测试建议

1. 先使用最简单的配置（只提供 1-2 首音乐）
2. 确认模块已启用（ModConfig UI）
3. 使用 `.mp3` 格式（兼容性最好）
4. 启用 Debug 日志，观察加载和播放过程
5. 测试不同场景：
   - 主菜单
   - 地堡主页
   - 留声机切歌
   - 自动切歌
   - 场景切换

### 性能优化

模块已进行以下性能优化：
- 日志字符串仅在对应级别启用时才构建
- 使用弱引用避免内存泄漏
- 自动清理音频资源
- 防重入保护避免递归调用