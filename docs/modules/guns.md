---
title: 枪械音效
---

# 枪械音效

替换枪械的射击和换弹音效。用 TypeID 为每把枪做专属音效。

## 快速开始

放个 `default.mp3` 测试：
```
CustomGunSounds/
└── default.mp3
```

为特定枪做专属：
```
CustomGunSounds/
├── 258.mp3               # TypeID 258 射击
├── 258_mute.mp3          # 装消音器后
├── 258_reload.mp3        # 换弹
├── 258_reload_start.mp3  # 换弹开始
├── 258_reload_end.mp3    # 换弹结束
└── default.mp3
```

## TypeID

每把枪的唯一数字 ID。同一枪族不同型号（AK47、AK103、AK74）也有不同 TypeID。

**获取**：在 `settings.json` 设 `logging.modules.Gun.level` 为 `Debug`，开枪后在 player.log 搜 `[GunShoot]`，看 `TypeID=xxx`。

## 文件查找优先级

**射击（消音器）**：`{TypeID}_mute` → `{soundKey}_mute` → `{TypeID}` → `{soundKey}` → `default`

**射击（无消音器）**：`{TypeID}` → `{soundKey}` → `default`

**换弹开始**：`{TypeID}_reload_start` → `{TypeID}_reload` → `default_reload_start` → `default_reload` → `default`

**换弹结束**：`{TypeID}_reload_end` → `default_reload_end` → `default`

其中 `{soundKey}` 是从 player.log 中 `[GunShoot]` 行的 `key=xxx` 获取的武器音效标识。

## 变体

同一个 TypeID 可以加 `_1`、`_2` 后缀随机选择：
```
├── 258_mute.mp3
├── 258_mute_1.mp3
├── 258_mute_2.mp3
```

## 射击速率限制

防止高射速音效重叠。在 `settings.json` 设（需开启 `gunShootDev`）：
```json
{
  "enableGunShootRateLimit": true,
  "gunShootMinIntervalMs": 50.0
}
```

## ModConfig

| 设置 | 默认 |
|------|------|
| 启用自定义枪械音效 | 开 |
| 音量倍率 | 1.0（0~2） |

## 常见问题

**TypeID 怎么找**：设 Gun Debug 日志，开枪，player.log 搜 `[GunShoot]`。

**消音器音效不生效**：文件名为 `{TypeID}_mute.mp3`，确认武器装了消音器。

**换弹被打断效果差**：用分段音效（`_reload_start` + `_reload_end`），结束段不会被打断。

**格式**：`.mp3` → `.wav` → `.ogg` → `.oga`。
