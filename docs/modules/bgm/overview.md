---
title: BGM 模块总览
---

# BGM 模块总览

本模块覆盖标题/主页、关卡场景、Boss 与撤离流程的 BGM 自定义，支持与声音包系统协同工作。没有提供音频的部分会自动用原版音效。

## 目录与文件概览

### 标题与主页
| 目录 | 文件 | 行为 |
|------|------|------|
| `TitleBGM/` | `startFX.*` | 进标题时的开场音效（非循环，播完自动切到 title） |
| `TitleBGM/` | `title.*` | 标题/菜单循环 |
| `TitleBGM/` | `start.*` | 进入基地提示音（单次，可在 ModConfig 开关） |
| `TitleBGM/` | `death.*` | 死亡提示（单次） |
| `TitleBGM/` | `extraction.*` | 撤离成功缺省回退 |
| `HomeBGM/` | `任意.*` | 留声机播放列表，支持多首任意格式音乐 |

### 场景 BGM
| 目录 | 匹配规则 |
|------|---------|
| `SceneBGM/Enter/` | 进入场景时播放一次（不循环），文件名 `<场景名>_enter` 或精确匹配 |
| `SceneBGM/Loop/` | 场景循环音乐，文件名 `<场景名>_loop` 或精确匹配 |
| 默认 | `default_enter.*` / `default_loop.*` |
| 类型 | `loading_*`、`lab_*`、`factory_*`、`farm_*`、`zero_*` 等 |

### Boss BGM
| 目录 | 规则 |
|------|------|
| `BossBGM/` | 以 NameKey 去除 `Cname_` 前缀命名，如 `BALeader.mp3` |
| 默认 | `default_boss.*` |

### 撤离音效
| 目录 | 文件 | 行为 |
|------|------|------|
| `Extraction/` | `countdown.*` | 倒计时 ≤5s 时播放（单次） |
| `Extraction/` | `success.*` | 撤离成功替换 |
| `Extraction/` | `extraction.*` | 倒计时缺省回退 |

**支持格式**（按优先级）：`.mp3`、`.wav`、`.ogg`、`.oga`、`.flac`、`.aif`、`.aiff`、`.mp2`、`.m4a`、`.mp4`、`.wma`、`.asf`、`.fsb`、`.it`、`.mid`、`.midi`、`.mod`、`.s3m`、`.xm`。

---

## 优先级与协同

全局优先级（从高到低）：
1. Boss BGM（有 Boss 在触发距离内）
2. 场景 BGM（Enter 与 Loop 同被压制）
3. 标题/主页 BGM（菜单内）

切换时通过淡入淡出协调。倒计时音效走 SFX 总线；成功替换音效经 Music 总线播放。

---

## 配置入口（ModConfig）

- **HomeBGM**：启用、音量、随机、避免重复、上一首也随机、自动下一首、SFX 总线
- **SceneBGM**：启用 Enter/Loop、各自音量、是否覆盖原生
- **BossBGM**：启用、触发距离、音量。高级参数在 `BossBGM/config.json`
- **ExtractionBGM**：模式（关闭/倒计时/成功替换）、音量
- 配置即时生效；声音包切换需重启游戏。

---

## 常见问题

- **文件没生效**：检查文件名是否正确（区分大小写），目录是否在正确位置，模块是否在 ModConfig 中启用。
- **声音太大/太小**：在 ModConfig 中调对应模块的音量滑块。
- **多个 BGM 冲突**：优先级自动处理，Boss > 场景 > 标题/主页。不需要手动协调。

各子模块详细说明见对应页面。

---

## 实验性功能：环境音拦截

针对不希望听到游戏环境音（风声、虫鸣等 Amb/amb_* 事件）的场景，提供全局静音拦截。

- **开关**：ModConfig 中的 `DCSAmbientIntercept | 环境音拦截`，或在 `settings.json` 中设 `"enableAmbientIntercept": true`（默认 `false`）
- **行为**：拦截 `AudioObject.Post` 收到的 `Amb/amb_*` 前缀环境音事件
- **特例放行**：`Amb/amb_storm` 保留放行
- **风暴阶段提示音**：`拦截风暴阶段提示音（实验性）` 可额外拦截 `Music/Stinger/stg_storm_1` 与 `Music/Stinger/stg_storm_2`
- **风险提示**：实验性功能，可能导致部分场景过度静音；仅建议有明确需求的用户开启
