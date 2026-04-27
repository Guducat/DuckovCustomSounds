---
title: 物品音效
---

# 物品音效

替换消耗品使用音效：食物、饮料、药品、注射针剂、绷带等。用 TypeID（数字）匹配具体物品，用 soundKey 匹配类别。

## 快速开始

```
CustomItemSounds/
└── default.mp3
```

为特定物品做专属：
```
CustomItemSounds/
├── 84.mp3              # TypeID 84
├── 20.mp3              # TypeID 20
└── default.mp3
```

按类别分目录（推荐）：
```
CustomItemSounds/
├── food/
│   ├── 84.mp3
│   └── default.mp3
├── bandage/
│   ├── 20.mp3
│   └── default.mp3
└── syringe/
    └── default.mp3
```

## TypeID 与 soundKey

- **TypeID**：每件物品的唯一数字 ID。获取方式：设 `logging.modules.Item.level` 为 `Debug`，用物品，player.log 搜 `[ItemUse]` 看 `TypeID=xxx`。
- **soundKey**：物品类别。`food`（食物饮料）、`bandage`（绷带/药品，主分类）、`syringe`（注射器）。游戏事件：`SFX/Item/use_{soundKey}`。`meds` 是 `bandage` 的别名。**注意：饮料没有独立的 `drink` 类别，统一归在 `food` 下。**

## 文件查找优先级

1. `CustomItemSounds/{类别}/{TypeID}.*`
2. `CustomItemSounds/{类别}/default.*`
3. `CustomItemSounds/{TypeID}.*`
4. `CustomItemSounds/{soundKey}.*`
5. `CustomItemSounds/default.*`

## 分段音效

拆成过程段和完成段。取消使用时只停过程段，完成段保留：

```
bandage/
├── 20_action.mp3   或 20_start.mp3
└── 20_finish.mp3   或 20_end.mp3
```

## ModConfig

| 设置 | 默认 |
|------|------|
| 启用自定义物品音效 | 开 |
| 音量倍率 | 1.0（0~2） |
| 启用 食物/饮料 | 开 |
| 启用 绷带/药品 | 开 |
| 启用 注射器 | 开 |
| Action 最短可听时间 | 0.35 秒（0~3） |

## item_sound_map.json

文件位置：`CustomItemSounds/item_sound_map.json`。首次运行自动生成。用于精细控制 TypeID→类别映射，以及给无原版音效的物品主动注入音效。

```json
{
  "defaultWhenNoEvent": null,
  "aliases": { "meds": "bandage" },
  "items": {
    "403": { "soundKey": "bandage", "forceWhenNoEvent": true },
    "23":  { "soundKey": "bandage", "fileBase": "64" },
    "25":  { "soundKey": "bandage", "fileBase": "64" },
    "156": { "soundKey": "syringe", "actionFileBase": "needle", "finishFileBase": "needle_done" }
  }
}
```

| 字段 | 说明 |
|------|------|
| `defaultWhenNoEvent` | 无原版事件时回退到此类别（可为 null） |
| `aliases` | 类别别名，内置 `meds` → `bandage` |
| `items.{TypeID}.soundKey` | 指定该物品的类别 |
| `items.{TypeID}.actionKey / finishKey` | 两阶段不同类别（可选） |
| `items.{TypeID}.fileBase / actionFileBase / finishFileBase` | 共享音频基名，多 TypeID 共用同一文件（可选） |
| `items.{TypeID}.forceWhenNoEvent` | 无原版事件时是否主动注入（默认 true） |

示例：TypeID 23 和 25 共用 `64.mp3`（放在 `bandage/` 目录下）。

修改后回主菜单或重启生效。

## 格式

`.mp3` → `.wav` → `.ogg` → `.oga`。
