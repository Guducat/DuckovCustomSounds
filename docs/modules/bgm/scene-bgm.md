---
title: 场景 BGM
---

# 场景 BGM(开发中，功能未启用，将来可能会调整)

场景 BGM 采用“两段式”结构：进入一次（Enter）+ 常驻循环（Loop），并支持按场景名/类型关键词匹配与默认回退。

## 目录与命名

```
DuckovCustomSounds/
└─ SceneBGM/
   ├─ Enter/                # 入场一次性音乐（不循环）
   │  ├─ zero_enter.mp3
   │  ├─ fram_enter.mp3
   │  └─ default_enter.mp3  # 兜底
   └─ Loop/                 # 常驻循环音乐
      ├─ zero_loop.mp3
      ├─ fram_loop.mp3
      └─ default_loop.mp3   # 兜底
```

- 推荐格式：`.mp3`；同时支持 `.wav/.ogg/.flac`。
- 命名规则
  - 精准匹配：`<场景名>_enter.mp3` / `<场景名>_loop.mp3`（优先级最高）
    - 例：`zero_enter.mp3` / `zero_loop.mp3`、`fram_enter.mp3` / `fram_loop.mp3`
  - 类型关键词匹配：`factory_enter.mp3`、`farm_enter.mp3`、`expedition_enter.mp3` 等
  - 默认：`default_enter.mp3` / `default_loop.mp3`

## 配置（ModConfig）

- 启用 Enter/Loop
- Enter 音量、Loop 音量（以 UI 为准）
- 覆盖原生（可选）

改动即时生效；音量变化会平滑过渡。

## 典型流程

1) 关卡初始化完成 → 延迟 `sceneLoadDelay` 秒避免干扰  
2) 若存在 Enter 曲目 → 按 `enterFadeDuration` 淡入，播放结束  
3) 开始 Loop 曲目 → 与 Enter 交叉渐变 `crossfadeDuration`  
4) 退出关卡时自动停止

## 高级（config.json）

位置：`DuckovCustomSounds/SceneBGM/config.json`

```json
{
  "enterFadeDuration": 1.5,
  "loopFadeDuration": 2.0,
  "sceneLoadDelay": 2.0,
  "crossfadeDuration": 1.0
}
```

- `enterFadeDuration`：Enter 淡入/淡出时间（秒）
- `loopFadeDuration`：Loop 淡入/淡出时间（秒）
- `sceneLoadDelay`：场景加载后延迟（秒）
- `crossfadeDuration`：Enter→Loop 的交叉渐变时间（秒）

## 优先级与协同

- Boss BGM > 场景 Loop > 场景 Enter
- Boss 激活时，场景 BGM 自动降级/停播；Boss 结束后按当前状态恢复。

## 原理与实现

- 使用“解析器 + 控制器 + 管理器”模式：解析匹配曲目，控制淡入淡出，统一管理跨状态切换。
- 匹配顺序为“精准场景名 > 类型关键词 > 默认”，并在切换时避免爆音或多重播放。

