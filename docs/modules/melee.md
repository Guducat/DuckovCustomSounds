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

每把近战武器的唯一数字 ID。**推荐用数字 TypeID**；也可用事件名（soundKey）命名文件（如 `knife.mp3`）作为回退，数字文件优先。

获取：设 `logging.modules.Melee.level` 为 `Debug`，用武器攻击，player.log 搜 `[MeleeAttack]`，看 `TypeID=xxx`。

## 文件查找

攻击（attack）：
1. `{TypeID}.mp3` → 2. `{soundKey}.mp3` → 3. `default.mp3`

挥击（swing）：
1. `{TypeID}_swing.mp3` → 2. `{soundKey}.mp3` → 3. `default_swing.mp3`

`{soundKey}` 为事件名 `SFX/Combat/Melee/attack_` / `swing_` 前缀后的部分，见 `[MeleeAttack]` / `[MeleeSwing]` 日志的 `soundKey=xxx`。

## 挥击（swing）音效

近战挥舞（挥空/起手）音效独立于攻击：文件名为 `{TypeID}_swing.mp3`（如 `98_swing.mp3`），回退 `default_swing.mp3`；同样支持 `_1`/`_2` 变体。TypeID 获取方式与攻击一致（日志搜 `[MeleeSwing]`）。

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
