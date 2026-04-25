# BOSS BGM 系统

为游戏中的 BOSS 添加专属背景音乐，支持距离触发、平滑淡入淡出、多 BOSS 优先级管理。

---

## 核心功能

- **BOSS 识别** - 自动检测 BOSS 生成（基于 `iconType` 和生命值）
- **距离触发** - 接近 BOSS 时音乐淡入，远离时淡出（可配置触发距离）
- **平滑淡入淡出** - 固定 2 秒淡入淡出，音乐过渡自然流畅
- **多 BOSS 优先级** - 多个 BOSS 同时存在时，只播放距离最近的 BOSS 音乐
- **自动清理** - BOSS 死亡或场景切换时自动停止音乐并清理资源
- **自定义音乐** - 支持为不同 BOSS 配置专属音乐

---

## 文件结构

```
DuckovCustomSounds/
├── BossBGM/
│   ├── config.json              # 配置文件（首次运行自动生成）
│   ├── default_boss.mp3         # 默认 BOSS 音乐（回退）
│   ├── BALeader.mp3             # BA队长专属音乐
│   ├── Boss_Sniper.mp3          # 劳登专属音乐
│   ├── ServerGuardian.mp3       # 矿长专属音乐
│   └── ...                      # 其他 BOSS 音乐
```

---

## 快速开始

### 1. 准备音乐文件

将 BOSS 音乐文件（MP3 格式）放入 `DuckovCustomSounds/BossBGM/` 文件夹：

```
DuckovCustomSounds/BossBGM/
├── default_boss.mp3      # 通用 BOSS 音乐（必需）
├── BALeader.mp3          # BA队长专属音乐（可选）
└── PrisonBoss.mp3        # 典狱长专属音乐（可选）
```

**文件命名规则**：
- 使用 BOSS 的 `nameKey`（去除 `Cname_` 前缀）
- 例如：`Cname_BALeader` → `BALeader.mp3`

### 2. 启动游戏测试

- 首次运行会自动生成 `config.json` 配置文件
- 进入地图，接近 BOSS 即可听到音乐
- 查看控制台日志 `[BossBGM]` 确认系统运行状态

---

## 配置文件（config.json）

**位置**：`DuckovCustomSounds/BossBGM/config.json`

**默认配置**：
```json
{
  "enabled": true,
  "triggerDistance": 30.0,
  "fadeDuration": 2.0,
  "updateInterval": 0.1,
  "managerUpdateInterval": 0.5
}
```

### 配置说明

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `enabled` | `true` | 是否启用 BOSS BGM 系统 |
| `triggerDistance` | `30.0` | 触发距离（米），玩家距离 BOSS 小于此值时音乐淡入 |
| `fadeDuration` | `2.0` | 淡入淡出时长（秒），音量从 0 到 1 或从 1 到 0 的时间 |
| `updateInterval` | `0.1` | 距离检测频率（秒），越小越精确，但性能开销越大 |
| `managerUpdateInterval` | `0.5` | 多 BOSS 优先级更新频率（秒），控制 BOSS 切换的响应速度 |

### 推荐配置

**性能优先**：
```json
{
  "enabled": true,
  "triggerDistance": 30.0,
  "fadeDuration": 2.0,
  "updateInterval": 0.2,
  "managerUpdateInterval": 1.0
}
```

**体验优先**：
```json
{
  "enabled": true,
  "triggerDistance": 40.0,
  "fadeDuration": 1.5,
  "updateInterval": 0.05,
  "managerUpdateInterval": 0.3
}
```

---

## BOSS 音乐文件命名对照表

完整的游戏内 BOSS 列表（根据 `nameKey` 匹配音乐文件）：

| NameKey | 中文名称 | 音乐文件名 |
|---------|---------|-----------|
| `Cname_BALeader` | BA队长 | `BALeader.mp3` |
| `Cname_Boss_Sniper` | 劳登 | `Boss_Sniper.mp3` |
| `Cname_Boss_Shot` | 喷子 | `Boss_Shot.mp3` |
| `Cname_ServerGuardian` | 矿长 | `ServerGuardian.mp3` |
| `Cname_Speedy` | 急速团长 | `Speedy.mp3` |
| `Cname_Boss_Fly` | 蝇蝇队长 | `Boss_Fly.mp3` |
| `Cname_Boss_Arcade` | 暴走街机 | `Boss_Arcade.mp3` |
| `Cname_Boss_3Shot` | 三枪哥 | `Boss_3Shot.mp3` |
| `Cname_Prison_Boss` | 典狱长 | `Prison_Boss.mp3` |
| `Cname_StormBoss1` | 噗咙噗咙 | `StormBoss1.mp3` |
| `Cname_StormBoss2` | 咕噜咕噜 | `StormBoss2.mp3` |
| `Cname_StormBoss3` | 啪啦啪啦 | `StormBoss3.mp3` |
| `Cname_StormBoss4` | 比利比利 | `StormBoss4.mp3` |
| `Cname_StormBoss5` | 口口口口 | `StormBoss5.mp3` |
| `Cname_ShortEagle` | 矮鸭 | `ShortEagle.mp3` |
| `Cname_UltraMan` | 光之男 | `UltraMan.mp3` |
| `Cname_CrazyRob` | 失控机械蜘蛛 | `CrazyRob.mp3` |
| `Cname_Vida` | 维达 | `Vida.mp3` |

**注意**：
- 如果没有为特定 BOSS 配置音乐，系统会使用 `default_boss.mp3` 作为回退
- 如果 `default_boss.mp3` 也不存在，该 BOSS 将不会播放音乐

---

## 音乐文件要求

### 支持的音频格式
- **推荐**：MP3（兼容性最好）
- 也支持：WAV、OGG、FLAC

### 文件大小建议
- 单个音乐文件建议不超过 10MB
- 过大的文件可能导致加载延迟

### 音量规范化
- 建议音乐文件音量规范化到 **-14 LUFS** 标准
- 避免音量过大导致削波失真

### 循环播放建议
- 音乐会自动循环播放
- 建议使用无缝循环的音乐文件（避免首尾衔接有明显断点）

---

## 工作原理

### 1. BOSS 检测
系统在 AI 初始化时（`AICharacterController.Init`）检测 BOSS：
- **优先级 1**：`iconType == "boss"`
- **优先级 2**：生命值 ≥ 1000（兜底判断）

### 2. 音乐播放流程
```
BOSS 生成
↓
添加 BossBGMController 组件
↓
解析音乐文件路径（nameKey → 文件名）
↓
播放音乐（循环播放，初始音量为 0）
↓
注册到 BossBGMManager
↓
Update 循环：检测玩家距离
↓
距离 < 30m → 淡入（2秒）
距离 > 30m → 淡出（2秒）
↓
BOSS 死亡或场景切换
↓
停止音乐（淡出），释放资源
```

### 3. 多 BOSS 优先级管理
- 系统每 0.5 秒计算所有 BOSS 到玩家的距离
- **只播放距离最近的 BOSS 音乐**
- 其他 BOSS 的音乐保持静音状态
- 当玩家接近不同 BOSS 时，音乐会自动切换（平滑淡入淡出）

---

## 调试技巧

### 查看日志
控制台日志标签：`[BossBGM]`

**常见日志**：
```
[BossBGM] BossMusicResolver 初始化完成，找到 5 个音乐文件
[BossBGM] [Patch] 检测到 BOSS 生成: Cname_BALeader, rank=boss
[BossBGM] BOSS BGM Controller 已启动: Cname_BALeader (距离阈值: 30m, 淡入淡出: 2s)
[BossBGM] 切换活跃 BOSS BGM: Cname_BALeader (距离 25.3m)
[BossBGM] BOSS BGM 已停止（淡出）: Cname_BALeader
```

### 常见问题排查

**Q: BOSS BGM 没有播放？**
A:
1. 检查 `config.json` 中 `enabled` 是否为 `true`
2. 检查是否提供了音乐文件（特定 BOSS 或 `default_boss.mp3`）
3. 查看控制台日志，确认 BOSS 是否被正确识别
4. 确认玩家距离 BOSS 是否小于 `triggerDistance`

**Q: 音乐无法播放或报错？**
A:
1. 确认音频文件格式为 MP3/WAV/OGG/FLAC
2. 确认音频文件没有损坏
3. 检查文件名是否正确（区分大小写）

**Q: 音乐切换不流畅？**
A:
1. 降低 `managerUpdateInterval`（如 0.3 秒）
2. 降低 `updateInterval`（如 0.05 秒）
3. 注意：过低的值会增加性能开销

**Q: 多个 BOSS 时音乐频繁切换？**
A:
1. 增加 `managerUpdateInterval`（如 1.0 秒）
2. 这是正常行为，系统会播放距离最近的 BOSS 音乐

**Q: 如何禁用某个 BOSS 的音乐？**
A: 不提供该 BOSS 的音乐文件即可，系统会回退到 `default_boss.mp3`

---

## 性能优化

系统已内置多项性能优化：

1. **降低更新频率**
   - 距离检测：每 0.1 秒一次（可配置）
   - 优先级更新：每 0.5 秒一次（可配置）

2. **距离计算优化**
   - 使用 `sqrMagnitude` 避免开方运算
   - 缓存玩家 Transform 引用

3. **音乐文件缓存**
   - 路径解析结果缓存，避免重复文件系统查询

4. **FMOD 自动优化**
   - 使用 FMOD Studio Event，自动享受虚拟化系统
   - 静音的音乐会自动"休眠"节省 CPU

---

## 注意事项

1. **音频格式**
   - 推荐使用 MP3 格式
   - 文件命名必须严格按照规则（不区分大小写）

2. **文件大小**
   - 建议单个音乐文件不超过 10MB
   - 过大的文件可能导致加载延迟

3. **音量平衡**
   - BOSS BGM 跟随游戏 Music 音量设置
   - 建议音乐文件音量规范化（-14 LUFS 标准）

4. **兼容性**
   - BOSS BGM 不会影响原版游戏音乐
   - 没有配置音乐的 BOSS 不会播放（或使用默认音乐）

5. **多 BOSS 场景**
   - 系统只会播放距离最近的 BOSS 音乐
   - 多个 BOSS 距离相近时，音乐可能频繁切换（正常行为）

---

## 完整示例

```
DuckovCustomSounds/
├── BossBGM/
│   ├── config.json                 # 配置文件
│   ├── default_boss.mp3            # 默认 BOSS 音乐
│   ├── BALeader.mp3                # BA队长专属音乐
│   ├── Boss_Sniper.mp3             # 劳登专属音乐
│   ├── ServerGuardian.mp3          # 矿长专属音乐
│   ├── Prison_Boss.mp3             # 典狱长专属音乐
│   └── StormBoss1.mp3              # 噗咙噗咙专属音乐
```

**config.json 示例**：
```json
{
  "enabled": true,
  "triggerDistance": 35.0,
  "fadeDuration": 2.0,
  "updateInterval": 0.1,
  "managerUpdateInterval": 0.5
}
```

---

## 技术细节

### 架构设计
- **BossBGMConfig** - 配置管理
- **BossMusicResolver** - 音乐文件匹配与路径解析
- **BossBGMManager** - 静态管理器，处理多 BOSS 优先级
- **BossBGMController** - MonoBehaviour，附加到每个 BOSS GameObject
- **BossBGM_Patches** - Harmony Patches，Hook BOSS 生成和场景卸载

### 依赖系统
- 复用 `CustomEnemySounds` 的 `EnemyContext` 系统（BOSS 识别）
- 复用 `EnemyContextRegistry`（上下文注册）
- 使用新接口 `AudioManager.PostCustomSound(path, loop: true)` 播放音乐

---

## 常见问题

**Q: 为什么我的音乐没有播放？**
A:
1. 检查文件名是否正确（如 `BALeader.mp3`）
2. 检查 `config.json` 中 `enabled` 是否为 `true`
3. 启用日志查看 `[BossBGM]` 确认系统状态
4. 确认音频文件格式为 MP3

**Q: 如何只为某个特定 BOSS 配置音乐？**
A: 只需要放置该 BOSS 的音乐文件即可，其他 BOSS 会使用 `default_boss.mp3`

**Q: 可以禁用 BOSS BGM 系统吗？**
A: 可以，在 `config.json` 中设置 `"enabled": false`

**Q: 如何调整触发距离？**
A: 修改 `config.json` 中的 `triggerDistance` 参数（单位：米）

**Q: 日志太多怎么办？**
A: BOSS BGM 系统的日志输出较少，主要在 BOSS 生成和音乐切换时输出

**Q: 如何减少 CPU 占用？**
A: 增加 `updateInterval` 和 `managerUpdateInterval`（如 0.2 和 1.0）

---

## 快速上手（5分钟）

1. **准备音乐文件**：将 `default_boss.mp3` 放入 `DuckovCustomSounds/BossBGM/` 文件夹
2. **启动游戏**：系统会自动生成 `config.json`
3. **进入地图**：接近 BOSS 即可听到音乐（默认 30 米触发）
4. **查看日志**：确认 `[BossBGM]` 日志输出
5. **调整配置**：修改 `config.json` 自定义触发距离和淡入淡出时长

完成！享受你的 BOSS BGM 吧！🎵

# BossBGM 资源包制作说明

为特定 BOSS 播放专属背景音乐。系统会根据与玩家的距离在靠近时淡入、远离时淡出；同时存在多个 BOSS 时，会自动选择“更有资格”的那位播放，其余静音等待。

---

## 适用对象
- 面向“资源包作者/整合作者”。不需要了解游戏代码，只需按文档准备音频与文件名即可。
- 不包含开发者/调试接口说明（这些在源码中）。

---

## 功能与优先级
- BOSS 识别：自动发现带有 boss 标识的敌人并尝试播放其专属 BGM。
- 距离触发：玩家距离小于触发距离时淡入；超出触发距离或离开场景时淡出并停止。
- 切换规则：多个 BOSS 同时存在时，仅播放“当前有效 BOSS”的音乐；内置抖动抑制与最小间隔，避免频繁切换。
- 优先级：BossBGM > 场景BGM > 基地/标题BGM。BossBGM 播放时，其它 BGM 会让位。
- 音量与总线：默认走 Music 总线，跟随游戏“音乐”音量滑块。

---

## 文件夹结构
```
DuckovCustomSounds/
└── BossBGM/
    ├── config.json          # 可选，高级参数；首次运行会自动生成
    ├── default_boss.mp3     # 通用回退（当找不到专属文件时使用）
    ├── BALeader.mp3         # 例：Cname_BALeader → BALeader.mp3
    ├── Boss_Sniper.ogg      # 例：Cname_Boss_Sniper → Boss_Sniper.ogg
    └── ServerGuardian.flac  # 例：Cname_ServerGuardian → ServerGuardian.flac
```

---

## 命名规则
- 取敌人 `NameKey` 去掉前缀 `Cname_` 作为文件名（不区分大小写）。
  - 例如：`Cname_BALeader` → `BALeader.mp3`
  - 例如：`Cname_Boss_Sniper` → `Boss_Sniper.ogg`
- 回退文件：若没有找到对应 BOSS 的专属文件，则使用 `default_boss.*`；若连回退也不存在，则不播放。

---

## 支持格式与建议
- 支持：MP3 / WAV / OGG / FLAC
- 建议：
  - 采样率 44.1kHz 或 48kHz，16bit
  - 时长不做硬性限制；建议循环或可长时间聆听的编曲
  - 控制响度，避免削波。参考混音目标 -14 LUFS（非强制）
  - 尽量控制单文件体积（建议 ≤10MB），以减少加载和切换的顿挫

---

## 快速开始
1) 在 `BossBGM/` 放入 `default_boss.mp3` 作为通用回退
2) 根据目标 BOSS 的 `NameKey` 去掉 `Cname_` 前缀命名对应文件（如 `BALeader.mp3`）
3) 进入游戏靠近 BOSS，听是否正确播放；查看控制台 `[BossBGM]` 日志可确认匹配情况

---

## 配置（ModConfig 与 config.json）

**ModConfig（游戏内菜单）**
- 启用 BossBGM
- 触发距离：10–200（默认 40）

**高级配置（`BossBGM/config.json`）**
- 首次运行自动生成，以下为主要字段（默认值仅供参考）：
```json
{
  "fadeDuration": 2.0,              // 淡入/淡出时长（秒）
  "updateInterval": 0.1,            // 控制器刷新间隔（秒）
  "managerUpdateInterval": 0.5,     // 管理器选择“当前BOSS”的刷新间隔（秒）
  "minSwitchIntervalSeconds": 2.0,  // 在不同BOSS间切换的最短间隔（秒）
  "minDistanceDeltaToSwitch": 5.0,  // 切换所需的最小距离优势（米）
  "resumePlaybackEnabled": true,    // 重新激活时是否尝试从上次进度恢复
  "delayedStopEnabled": true,       // 离开后延迟停止（避免瞬时抖动）
  "delayedStopSeconds": 1.0,        // 延迟停止时长（秒）
  "bossDeathFadeOutSeconds": 3.0    // BOSS 死亡时的淡出时长（秒）
}
```

说明
- ModConfig 负责“是否启用、触发距离”这类面向玩家的设置；
- config.json 负责淡入淡出与切换策略等高级细节，不建议频繁改动；
- 修改后无需重启即可生效（少量参数可能在下一次播放时生效）。

---

## 播放行为（你将听到什么）
- 接近 BOSS → 渐入（音量上升到工作电平）
- 远离或失去目标 → 渐出（音量下降至 0 后停止）
- 多个 BOSS 存在 → 仅播放一个；需要有明显距离优势才会切换，且切换频率受最小间隔限制
- 场景切换 / 关卡卸载 → 自动停止并清理

---

## 示例清单
```
BossBGM/
├── default_boss.mp3
├── BALeader.mp3
├── Boss_Sniper.ogg
└── ServerGuardian.flac
```

---

## 常见问题
- 听不到 BOSS 音乐？
  - 是否放在 `DuckovCustomSounds/BossBGM/`，且文件名与 `NameKey`（去掉 `Cname_`）匹配
  - 是否至少存在一个文件（专属或 `default_boss.*`）
  - ModConfig 中是否启用 BossBGM；触发距离是否过小
  - 查看控制台 `[BossBGM]` 日志确认匹配与播放状态
- 多个 BOSS 时为何不总是切过去？
  - 为避免来回抖动，系统设置了“最小切换间隔”和“最小距离优势”门槛；可在 config.json 调整
+- 音量不一致？
  - 音量随“音乐”滑块；建议在导出时统一响度，避免素材间差异过大

---

## 制作建议
- 建议循环段自然衔接；如不能无缝循环，确保段尾不突兀
- 素材层级简洁，避免过多超低频/超高频能量造成混浊或刺耳
- 对长时素材可考虑单声道或较低比特率的 MP3/OGG，减小体积

---

## 日志与排查
- 控制台日志前缀：`[BossBGM]`
- 常见日志：
  - “初始化完成，找到 N 个音乐文件”——扫描成功
  - “匹配到 BOSS 音乐: Cname_xxx → File”——命名匹配成功
  - “切换/停止”——表示优先级或距离触发的结果

---

如需更全面的技术细节（例如事件时序、控制器状态机），可参考源码，但对资源包作者不是必需。
