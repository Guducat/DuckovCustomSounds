---
title: 加载界面/加载完成 BGM
---

# 加载界面 / 加载完成 BGM

这是一份简明指引，帮你快速为“加载中界面”与“加载完成后的场景进入”添加自定义 BGM。需要更完整的匹配与参数说明，请参见“场景 BGM”。

## 放哪儿？

把音频放到当前声音包（或根目录）的 `SceneBGM/` 下面：

- `SceneBGM/Enter/`：加载完成、进入场景时播放一次（入场提示）
- `SceneBGM/Loop/`：在场景中循环播放的背景音乐

支持格式：`.mp3`（推荐）、`.wav`、`.ogg`、`.flac`

## 开箱即用的文件

- 加载界面（Loading Screen）通用：`Enter/loading_enter.mp3`
- 加载界面循环：`Loop/loading_loop.mp3`
- 进入地图通用（无专属时）：`Enter/default_enter.mp3`
- 场景循环通用（无专属时）：`Loop/default_loop.mp3`

只放以上 2–4 个文件，即可覆盖“加载中/加载完成”的常见需求。

## 想更精准？

为具体地图准备专属文件（示例）：

- `Enter/loadingScreen_getout.mp3`（“出发，嘎嘎搜刮”页面播放）
- `Enter/level_groundzero_main_enter.mp3`（进入“零号区”时播）
- `Loop/level_groundzero_main_loop.mp3`（在“零号区”循环播放）

命名优先级：精准场景名 > 类型关键词（如 `loading_*`/`lab_*`/`farm_*`）> 默认。

## 示例结构

```
DuckovCustomSounds/
└── SceneBGM/
    ├── Enter/
    │   ├── loading_enter.mp3
    │   └── default_enter.mp3
    └── Loop/
        ├── loading_loop.mp3
        └── default_loop.mp3
```

## 体验与注意

- 音量随“音乐”滑块；可在 Mod 设置分别调节 Enter/Loop 音量或关闭。
- 与 Boss BGM 同时出现时会自动淡入淡出并协调优先级。
- 该功能当前为“场景 BGM”的一部分；更多细节与进阶配置见「场景 BGM」。

— 祝你拥有更顺滑的加载过场与入场氛围！

