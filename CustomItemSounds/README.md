# CustomItemSounds 使用说明

本模块为“消耗品使用音效”的统一入口，拦截所有形如 `SFX/Item/use_*` 的事件（例如：`use_food`、`use_meds`、`use_syringe`），并将其替换为自定义音频文件。

- 事件 → 目录映射规则：`use_xxx` → `CustomItemSounds/xxx/`
- 类型匹配：根据最近一次被使用的物品 `TypeID` 选择同名音频文件
- 变体随机：当存在 `TypeID_1.mp3`、`TypeID_2.mp3` 等多个变体时，随机选择其中一个播放（与敌人语音的“变体随机”一致的思路）

## 目录结构（推荐，优先匹配）

```
CustomItemSounds/
├── food/          # 对应 SFX/Item/use_food
│   ├── 84.mp3
│   ├── 84_1.mp3   # 84 的变体（可有多个：_1、_2、_3 ...）
│   └── default.mp3
├── meds/          # 对应 SFX/Item/use_meds
│   ├── 20.mp3
│   └── default.mp3
└── syringe/       # 对应 SFX/Item/use_syringe
    └── default.mp3
```

- 支持的扩展名：`.mp3` `.wav` `.ogg` `.oga`
- 优先级：
  1) `CustomItemSounds/<category>/<TypeID>[_变体].ext`
  2) `CustomItemSounds/<category>/default.ext`
  3) 兼容旧扁平结构：`CustomItemSounds/<TypeID>[_变体].ext`、`CustomItemSounds/default.ext`

> 注意：旧的根目录 `CustomFoodSounds` 已废弃，不再支持。请迁移到 `CustomItemSounds/`。但在 `CustomItemSounds/` 下仍兼容“旧扁平结构”。

## 命名与随机播放
- 指定某个物品类型：`<TypeID>.mp3`（例如 `84.mp3`）
- 添加随机变体：`<TypeID>_1.mp3`、`<TypeID>_2.mp3` ... 将在这些文件中随机挑选一个播放
- 默认音效：在分类目录下放置 `default.ext`，当找不到某个 `TypeID` 的自定义文件时回退

## 如何扩展新的消耗品类型
- 游戏新增了 `SFX/Item/use_xxx` 事件时，只需新增文件夹 `CustomItemSounds/xxx/` 并放入音频文件即可

## 配置（预留）
- 计划支持 `item_voice_rules.json`：用于配置特定 `TypeID` 是否启用替换、映射到不同的分类/文件、变体权重等（当前版本尚未启用，仅在代码中预留了注释位置）

## 日志
- Info：关键路径（例如：未找到自定义文件）
- Debug：详细过程（例如：拦截到的事件/类别、TypeID、候选路径、最终使用的文件名）

## 常见问题
- 没有声音？
  - 检查 `CustomItemSounds/<category>/<TypeID>.ext` 是否存在
  - 确认扩展名是否在支持列表内
  - 查看日志（Debug 级别）以确认事件类别、TypeID 是否正确被捕获

