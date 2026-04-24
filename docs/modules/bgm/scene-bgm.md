---
title: 场景 BGM
---

# 场景 BGM

场景 BGM 采用两段式结构：进入一次（Enter）+ 常驻循环（Loop），支持按场景名/类型关键词/默认回退匹配。

## 目录与命名

```
SceneBGM/
├── Enter/                  # 入场音乐（不循环，播放一次）
│   ├── zero_enter.mp3
│   ├── fram_enter.mp3
│   └── default_enter.mp3
└── Loop/                   # 常驻循环音乐
    ├── zero_loop.mp3
    ├── fram_loop.mp3
    └── default_loop.mp3
```

### 匹配优先级

1. **精准场景名**：`<场景名>_enter.mp3` / `<场景名>_loop.mp3`
2. **场景名无后缀**：`<场景名>.mp3`（无 `_enter`/`_loop` 后缀，如 `loadingscreen_getout.mp3`）
3. **sceneId 匹配**：`<sceneId>_enter.mp3` / `<sceneId>_loop.mp3`（如 `level_farm_main_enter.mp3`）
4. **类型关键词**：`loading_*`、`lab_*`、`factory_*`、`farm_*`、`zero_*`、`expedition_*`、`outskirts_*`
5. **默认**：`default_enter.mp3` / `default_loop.mp3`

注意：加载界面属于"loading"类型，Enter 不会对加载场景播放默认音乐（避免黑屏时误播）。

## ModConfig 设置

| 设置 | 默认 | 说明 |
|------|------|------|
| 启用场景音乐系统 | 开 | 总开关 |
| 启用进入场景 BGM | 开 | 播放 Enter 音乐 |
| 进入音乐音量 | 80% | 0-100% |
| 启用循环场景 BGM | 开 | 播放 Loop 音乐 |
| 循环音乐音量 | 60% | 0-100% |
| 覆盖默认场景音乐 | 开 | Loop 是否覆盖游戏原场景音乐 |

改动即时生效；音量变化平滑过渡。

## 典型流程

1. 关卡初始化完成 → 延迟 `sceneLoadDelay` 秒
2. 如果有 Enter 音乐 → 淡入播放，结束后自动销毁
3. 切入 Loop 音乐 → 与 Enter 交叉渐变
4. 退出关卡时自动停止

## 高级配置（config.json）

文件位置：`SceneBGM/config.json`。首次运行自动生成。

```json
{
  "enterFadeDuration": 1.5,
  "loopFadeDuration": 2.0,
  "sceneLoadDelay": 2.0,
  "crossfadeDuration": 1.0
}
```

| 参数 | 说明 |
|------|------|
| `enterFadeDuration` | Enter 淡入/淡出时间（秒） |
| `loopFadeDuration` | Loop 淡入/淡出时间（秒） |
| `sceneLoadDelay` | 场景加载后延迟（秒） |
| `crossfadeDuration` | Enter→Loop 交叉渐变时间（秒） |

## 优先级

Boss BGM > 场景 Loop > 场景 Enter。Boss 激活时场景 BGM 自动降级或停止。
