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
4. **sceneId 变体兼容**：`Level_Farm_01`、`Level_GroundZero_1` 会继续尝试 `level_farm_main_*`、`level_groundzero_main_*`
5. **类型关键词**：`loading_*`、`lab_*`、`factory_*`、`farm_*`、`zero_*`、`warehouse_*`、`expedition_*`、`outskirts_*`
6. **默认**：`default_enter.mp3` / `default_loop.mp3`

注意：加载界面属于"loading"类型，Enter 不会对加载场景播放默认音乐（避免黑屏时误播）。**类型关键词同样支持中文**（如 `农场_enter.mp3`、`仓库_enter.mp3`、`零号区_enter.mp3`、`工厂_enter.mp3` 等）。

#### v2.1.1 更新
`Level_HiddenWarehouse_Main` 同时兼容 `level_warehouse_main_*`，因为`仓库区`更改过场景名等数据，适合资源包使用稳定地图名。

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

Boss BGM > 场景 Loop > 场景 Enter。

- **Boss 压制**：Boss 进入触发距离内时，场景 Loop/Enter 音量平滑降至 0（实例保持播放）；Boss 离开触发距离或消失后自动恢复。
- **自愈（v2.3.3）**：Boss→Boss 切换的交叉淡出期间，若循环 BGM 实例被意外停止（共享音乐源争用），Boss 解除压制时会自动重建循环 BGM，场景音乐不会永久丢失。
- **撤离鸭子（v2.3.3）**：撤离倒计时期间场景 BGM 快速降音量（不停止），取消撤离后平滑恢复；撤离成功转场由游戏 StopBGM 处理。
