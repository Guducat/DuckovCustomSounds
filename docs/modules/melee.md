---
title: 近战音效
---

# 近战音效

替换近战武器音效。用 TypeID（数字）匹配。

## 快速开始

```
CustomMeleeSounds/
├── 98.ogg              # TypeID 98（如铲子）
├── 156.mp3             # TypeID 156
└── default.mp3         # 通用回退
```

## TypeID

每把近战武器的唯一数字 ID。**必须用数字**，不能用武器名（`knife.mp3` 是错的）。

获取：设 `logging.modules.Melee.level` 为 `Debug`，用武器攻击，player.log 搜 `[MeleeAttack]`，看 `TypeID=xxx`。

## 文件查找

1. `{TypeID}.mp3` → 2. `default.mp3`

## 变体

加 `_1`、`_2` 后缀随机选择：
```
├── 98.mp3
├── 98_1.mp3
└── 98_2.mp3
```

## ModConfig

| 设置 | 默认 |
|------|------|
| 启用自定义近战音效 | 开 |
| 音量倍率 | 1.0（0~2） |

## 格式

`.mp3` → `.wav` → `.ogg` → `.oga`。
