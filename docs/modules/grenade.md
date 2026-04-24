---
title: 手雷音效
---

# 手雷音效

替换手雷和爆炸物音效。按 soundKey 匹配。

## 快速开始

```
CustomGrenadeSounds/
├── explode_grenade.mp3
└── default.mp3
```

soundKey 来自游戏内部，**示例名仅供参考**。实际名称需要通过日志确认。

获取：设 `logging.modules.Grenade.level` 为 `Debug`，触发爆炸，player.log 搜 `[Grenade]`，看 `soundKey=xxx`。

## 文件查找

1. `{soundKey}.mp3` → 2. `default.mp3`

## ModConfig

| 设置 | 默认 |
|------|------|
| 启用自定义手雷音效 | 开 |
| 音量倍率 | 1.0（0~2） |

## 格式

`.mp3` → `.wav` → `.ogg` → `.oga`。
