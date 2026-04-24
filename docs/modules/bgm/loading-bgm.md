---
title: 加载界面/加载完成 BGM
---

# 加载界面 / 加载完成 BGM

为"加载中界面"与"加载完成后进入场景"添加自定义 BGM。本功能是"场景 BGM"模块的一部分。

## 放哪儿

把音频放到 `SceneBGM/` 下：

- `SceneBGM/Enter/`：加载完成、进入场景时播放一次
- `SceneBGM/Loop/`：在场景中循环播放

## 开箱即用的文件名

- 加载界面通用：`Enter/loading_enter.mp3`
- 加载界面循环：`Loop/loading_loop.mp3`
- 进入地图通用：`Enter/default_enter.mp3`
- 场景循环通用：`Loop/default_loop.mp3`

只放 2-4 个文件就能覆盖加载和进入的常见需求。

注意：加载界面属于"loading"类型，Enter BGM 不会对加载场景播默认音乐（只播你明确放的 `loading_enter.mp3`）。

## 想更精准

为具体地图准备专属文件：

- `Enter/loadingScreen_getout.mp3`
- `Enter/level_groundzero_main_enter.mp3`
- `Loop/level_groundzero_main_loop.mp3`

匹配优先级：精准场景名 > 类型关键词（`loading_*`、`lab_*`、`farm_*` 等）> 默认。

## 示例结构

```
SceneBGM/
├── Enter/
│   ├── loading_enter.mp3
│   └── default_enter.mp3
└── Loop/
    ├── loading_loop.mp3
    └── default_loop.mp3
```

## 注意

- 音量随"音乐"滑块；可在 ModConfig 分别调 Enter/Loop 音量。
- 与 Boss BGM 同时出现时自动淡入淡出，优先级 Boss > 场景。
- 更多参数见"场景 BGM"页面（`SceneBGM/config.json` 可调淡入淡出和延迟等）。
