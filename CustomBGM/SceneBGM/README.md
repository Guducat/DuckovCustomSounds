# 场景 BGM 系统使用文档

## 功能概述

场景 BGM 系统为游戏不同场景提供自定义背景音乐，支持两种音乐类型：

1. **进入场景 BGM**：场景加载完成时播放一次的迎接音乐（单次播放）
2. **场景循环 BGM**：持续循环播放的背景音乐

## 快速开始

### 1. 文件夹结构

音乐文件应放置在以下目录：

```
DuckovCustomSounds/
└── SceneBGM/
    ├── Enter/          # 进入场景音乐（单次播放）
    │   ├── zero_enter.mp3
    │   ├── fram_enter.mp3
    │   ├── loading_enter.mp3     # 通用加载界面音乐（可选）
    │   └── default_enter.mp3     # 通用进入音乐（兜底）
    │
    └── Loop/           # 场景循环音乐（持续播放）
        ├── zero_loop.mp3
        ├── fram_loop.mp3
        ├── loading_loop.mp3      # 通用加载界面循环音乐（可选）
        └── default_loop.mp3      # 通用循环音乐（兜底）
```

### 2. 支持的音频格式

- `.mp3`（推荐）
- `.wav`
- `.ogg`
- `.flac`

### 3. 文件命名规则

#### 精确匹配（优先级最高）
两种方式均可：
- 带后缀：`<scene>_enter.mp3` / `<scene>_loop.mp3`
  - 例：`zero_enter.mp3` / `zero_loop.mp3`
  - 例：`level_groundzero_main_enter.mp3`
- 无后缀（放在哪个文件夹即代表 Enter/Loop）：`<scene>.mp3`
  - 例：`Enter/loadingscreen_getout.mp3`（进入音乐）
  - 例：`Loop/loadingscreen_getout.mp3`（循环音乐）
- sceneId 变体兼容：
  - `Level_GroundZero_1` 会继续尝试 `level_groundzero_main_enter.mp3`
  - `Level_Farm_01` 会继续尝试 `level_farm_main_enter.mp3`
  - `Level_HiddenWarehouse_Main` 会继续尝试 `level_warehouse_main_enter.mp3`

#### 场景类型匹配（优先级中）
使用场景类型关键词：
- `farm_enter.mp3` - 匹配所有农场类型场景
- `factory_enter.mp3` - 匹配所有工厂类型场景
- `expedition_enter.mp3` - 匹配所有探险类型场景
- `warehouse_enter.mp3` - 匹配仓库区类型场景
- `loading_enter.mp3` - 匹配所有加载界面（如 `LoadingScreen_Getout`）

#### 默认匹配（优先级低）
- `default_enter.mp3` - 默认进入音乐（没有精确匹配时使用）
- `default_loop.mp3` - 默认循环音乐

## ModConfig UI 配置

### 总开关
- **启用场景音乐系统**：总开关，关闭后整个系统停用

### 进入场景 BGM 配置
- **[进入BGM] 启用进入场景 BGM**：开关进入音乐播放
- **[进入BGM] 进入音乐音量**：调节音量（0-100%），默认 80%

### 场景循环 BGM 配置
- **[循环BGM] 启用场景循环 BGM**：开关循环音乐播放
- **[循环BGM] 循环音乐音量**：调节音量（0-100%），默认 60%
- **[循环BGM] 覆盖默认场景音乐**：是否覆盖游戏原版场景音乐

### 热配置功能
所有配置项支持**实时生效**，无需重启游戏：
- 关闭开关：当前音乐淡出停止
- 开启开关：立即重新播放场景音乐
- 音量调节：实时调整音量

## 高级配置（config.json）

在 `DuckovCustomSounds/SceneBGM/config.json` 中可配置高级参数：

```json
{
  "enterFadeDuration": 1.5,      // 进入音乐淡入时长（秒）
  "loopFadeDuration": 2.0,       // 循环音乐淡入时长（秒）
  "sceneLoadDelay": 2.0,         // 场景加载后延迟播放时间（秒）
  "crossfadeDuration": 1.0       // 进入→循环音乐过渡时长（秒）
}
```

## 播放流程

### 场景加载时
1. 场景完全初始化（对应 [Level Initialization] Done! 日志）
2. 延迟 `sceneLoadDelay` 秒（避免与场景加载音效冲突）
3. 播放进入音乐（如果启用且有文件）
4. 进入音乐淡入（`enterFadeDuration` 秒）
5. 进入音乐播放完成
6. 交叉淡入淡出过渡（`crossfadeDuration` 秒）
7. 循环音乐开始播放（如果启用且有文件）

### 仅进入音乐
- 播放完成后自动停止

### 仅循环音乐
- 立即开始循环播放

### 两者都有
- 进入音乐播放完成后，平滑过渡到循环音乐

## 优先级管理（与 BOSS BGM 协调）

### 优先级层级
1. **BOSS BGM**（最高优先级）- 距离 BOSS 30 米内触发
2. **场景循环 BGM**（中优先级）- 持续播放
3. **进入场景 BGM**（低优先级）- 单次播放

### 冲突处理
#### BOSS 出现时
- 场景循环 BGM：音量降至 0（保持运行）
- 进入 BGM：音量降至 0（如仍在播放）
- BOSS BGM：淡入接管

#### BOSS 消失后
- BOSS BGM：淡出停止
- 场景循环 BGM：音量恢复淡入

#### 场景切换时
- 所有旧场景音乐：立即淡出并清理
- 新场景音乐：重新开始播放流程

## 使用示例

### 示例 1：零号区（进入音乐 + 循环音乐）

**文件准备**：
```
SceneBGM/
├── Enter/zero_enter.mp3    # 3秒紧张警报音效
└── Loop/zero_loop.mp3      # 科技感循环音乐
```

**播放效果**：
1. 进入零号区
2. 播放 3 秒警报音效（紧张氛围）
3. 平滑过渡到循环音乐
4. 持续播放科技感音乐直到离开场景

### 示例 2：农场镇（仅循环音乐）

**文件准备**：
```
SceneBGM/
└── Loop/fram_loop.mp3      # 轻松的田园音乐
```

**播放效果**：
1. 进入农场镇
2. 立即播放田园音乐
3. 循环播放直到离开场景

### 示例 3：实验室（仅进入音乐）

**文件准备**：
```
SceneBGM/
└── Enter/lab_enter.mp3     # 5秒实验室氛围音效
```

**播放效果**：
1. 进入实验室
2. 播放 5 秒氛围音效
3. 音效播放完成后停止（无循环音乐）

### 示例 4：默认音乐（所有场景通用）

**文件准备**：
```
SceneBGM/
├── Enter/default_enter.mp3
└── Loop/default_loop.mp3
```

**播放效果**：
- 所有未配置专属音乐的场景使用默认音乐

### 示例 5：加载界面与地图进入（通用 + 专属）

**文件准备**：
```
SceneBGM/
├── Enter/loading_enter.mp3          # 加载界面通用进入音乐
├── Loop/loading_loop.mp3            # 加载界面通用循环音乐
└── Enter/default_enter.mp3          # 进入地图后的通用进入音乐（无专属时）
```

**播放效果**：
1. 进入 `LoadingScreen_Getout` → 播放 `loading_enter`，若有则继续 `loading_loop`
2. 地图加载完成（如 `Level_GroundZero_Main`）→ 播放该地图的专属 `*_enter`，否则播放 `default_enter`

## 场景类型映射

系统支持以下场景类型自动匹配：

| 类型关键词 | 匹配场景名称 |
|-----------|-------------|
| `loading` | loading, loadingscreen, loading_screen, 加载 |
| `lab` | lab, 实验室, 研究所 |
| `factory` | factory, 工厂, 工业区 |
| `farm` | farm, fram, 农场, 农场镇 |
| `zero` | zero, groundzero, 零号区, 0号区 |
| `warehouse` | warehouse, hiddenwarehouse, 仓库, 仓库区 |
| `expedition` | expedition, 探险, 任务 |
| `outskirts` | outskirts, 郊区, 边缘 |

> 提示：若希望“加载界面”与“进入地图后”的音乐不同，可以在 `Enter/` 中同时放置 `loading_enter.mp3`（加载界面）与 `default_enter.mp3`（进入地图通用）。当存在场景专属文件（如 `level_groundzero_main_enter.mp3`）时，仍以专属文件优先。

## 调试与日志

### 日志前缀
所有场景 BGM 日志使用 `[SceneBGM]` 前缀

### 常见日志
```
[SceneBGM] 场景 BGM 系统初始化完成
[SceneBGM] 场景加载完成: sceneId=xxx, displayName=xxx
[SceneBGM] 匹配到场景进入音乐（精确）: zero -> zero_enter.mp3
[SceneBGM] 进入音乐已播放: 零号区
[SceneBGM] 场景 Enter BGM 播放完成: 零号区
[SceneBGM] 循环音乐已播放: 零号区
[SceneBGM] BOSS BGM 状态变更: true
[SceneBGM] Loop BGM 优先级抑制: 零号区 -> true
```

### 调试功能
使用 `CustomSceneBGM.GetPlaybackStatus()` 获取当前播放状态

## 常见问题

### Q: 音乐没有播放？
**A**: 检查以下几点：
1. 文件是否放在正确的文件夹（Enter 或 Loop）
2. 文件名是否正确（场景名称_enter/loop.mp3）
3. ModConfig UI 中是否启用了对应开关
4. 查看日志确认文件是否被识别

### Q: 音乐突然变小声或停止？
**A**: 这是正常的优先级管理：
- BOSS 出现时，场景音乐会自动降低音量
- BOSS 消失后会自动恢复

### Q: 进入音乐和循环音乐如何平滑过渡？
**A**: 系统自动处理交叉淡入淡出，过渡时长由 `crossfadeDuration` 配置

### Q: 可以只用进入音乐或只用循环音乐吗？
**A**: 完全可以！两种音乐是独立的，可以只配置其中一种

### Q: 如何为新场景添加音乐？
**A**: 只需按命名规则放置文件到对应文件夹，系统会自动识别

## 技术细节

### 架构组成
- **SceneBGMConfig**: 配置管理（ModConfig UI + config.json）
- **SceneMusicResolver**: 音频文件解析和匹配
- **SceneBGMController**: 单个音乐播放控制
- **SceneBGMManager**: 双通道管理（进入 + 循环）
- **CustomSceneBGM**: 模块主入口

### 事件系统
- 使用 `SceneLoader.onAfterSceneInitialize` 事件
- 时机：场景完全初始化后触发
- 确保场景已就绪，音乐播放不会被打断

### 优先级通信
- BOSS BGM Manager 主动通知场景 BGM Manager
- 通过 `SetBossBGMActive(bool)` 接口通信
- 场景 BGM 响应优先级变化，自动调整音量

## 更新日志

### v1.0.0 (2025-10-30)
- ✅ 初始版本发布
- ✅ 支持进入场景 BGM（单次播放）
- ✅ 支持场景循环 BGM（持续播放）
- ✅ ModConfig UI 热配置支持
- ✅ 与 BOSS BGM 优先级协调
- ✅ 自动场景识别和文件匹配
- ✅ 淡入淡出和交叉过渡效果

## 技术支持

如有问题或建议，请联系 Mod 开发者。

---

**Enjoy your custom scene music! 🎵**
