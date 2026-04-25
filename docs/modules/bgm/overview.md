---
title: BGM 模块总览
---

# BGM 模块总览

本模块提供对标题/主页、关卡场景、Boss 与撤离流程的多段式 BGM 自定义，支持与声音包系统协同工作。你可以只替换自己需要的部分，未提供的音频会自动回退。

## 目录与文件概览

- 标题与主页
  - `TitleBGM/`：`title.mp3`（标题/菜单循环）、`start.mp3`（进入地图提示，单次）、`death.mp3`（死亡提示，单次）、`extraction.mp3`（撤离缺省回退）。
  - `HomeBGM/`：任意多首 `*.mp3`，在主页可选择与切换。
- 场景 BGM
  - `SceneBGM/Enter/`：进入场景时的一次性“入场”音乐，文件名为`<场景名>_enter`，例如 `zero_enter.mp3`。
  - `SceneBGM/Loop/`：场景常驻循环音乐，文件名为`<场景名>_loop`，例如 `zero_loop.mp3`。
  - 支持“默认”：`default_enter.mp3` / `default_loop.mp3`。
- Boss BGM
  - `BossBGM/`：以敌人 `NameKey`（去掉前缀 `Cname_`）命名，例如 `BALeader.mp3`；未命中时退回到 `default_boss.*`。
- 撤离音效
  - `Extraction/`：`countdown.mp3|wav`（倒计时模式，≤5s 启动）、`success.mp3|wav`（撤离成功替换）、可选 `extraction.mp3|wav`（倒计时模式缺省）。

提示：若启用声音包，实际查找路径会基于当前包根目录（详见“声音包系统”）。

## 优先级与协同

- 全局优先级（从高到低）
  - Boss BGM（活动 Boss 附近时）
  - 场景 Loop BGM（常驻）
  - 场景 Enter BGM（一次性）
  - 标题/主页 BGM（菜单内）
- 冲突处理
  - 发生优先级切换时通过淡入淡出与交叉渐变协调。
  - 撤离模式下的 SFX 会避开 BGM 管线以减少冲突。

## 配置入口（ModConfig）

- HomeBGM：启用、音量、随机、避免立即重复、上一首也随机、自动下一首、实验性“使用 SFX 总线”。
- SceneBGM：启用 Enter/Loop、各自音量（具体以 UI 为准）、是否覆盖原生。
- BossBGM：启用、触发距离、淡入淡出时间、检测频率、切换抖动保护等（更多见 Boss BGM 页面）。
- ExtractionBGM：模式（关闭 / 倒计时 / 成功替换）。

所有设置即时写入配置；部分行为（如声音包切换）需重启游戏才生效。

## 原理与实现

- 标题/死亡提示通过拦截 FMOD Stinger 事件，在 BGM 管线播放自定义文件。
- HomeBGM 提供独立音量与随机策略，可选将输出改走 SFX 总线以便调试。
- SceneBGM 使用“进入一次 + 常驻循环”的双阶段模型，匹配优先级为“精准场景名 → 场景类型关键词 → 默认”。
- BossBGM 基于敌人 `NameKey` 追踪最近/最高优先级 Boss，距离阈值触发并在淡入淡出中切换。
- 撤离音效采用两种模式：倒计时（≤5s 启动一次性 SFX）与成功替换（拦截成功 Stinger 并播放自定义 SFX），场景切换或停止 BGM 时强制停止活动 SFX。

详细用法与示例请分别查看本章各子页面。

