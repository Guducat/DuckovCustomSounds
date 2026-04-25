# CustomItemSounds 使用说明

本模块为“消耗品使用音效”的统一入口，拦截所有形如 `SFX/Item/use_*` 的事件（例如：`use_food`、`use_bandage`、`use_syringe`），并将其替换为自定义音频文件。

- 事件 → 目录映射规则：`use_xxx` → `CustomItemSounds/xxx/`
- 类型匹配：根据最近一次被使用的物品 `TypeID` 选择同名音频文件
- 分段支持：可选 `*_action|_start` 与 `*_finish|_end` 两段文件（见下）
- 兼容别名：原版的 `use_meds` 会自动映射为 `use_bandage`（目录名也统一为 `bandage/`）

## 目录结构（推荐，优先匹配）

```
CustomItemSounds/
├── food/          # 对应 SFX/Item/use_food
│   ├── 84.mp3
│   ├── 84_1.mp3   # 84 的变体（可有多个：_1、_2、_3 ...）
│   └── default.mp3
├── bandage/       # 对应 SFX/Item/use_bandage（原 use_meds 也会被重定向到此）
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

## 命名与分段（可选）
- 指定某个物品类型：`<TypeID>.mp3`（例如 `84.mp3`）
- 可选分段：
  - 过程段：`<TypeID>_action.(ext)` 或 `<TypeID>_start.(ext)`
  - 完成段：`<TypeID>_finish.(ext)` 或 `<TypeID>_end.(ext)`
- 类别回退分段：也支持在分类目录或根目录使用 `food_action.(ext)`、`default_finish.(ext)` 等命名作为回退
- 默认音效：在分类目录放置 `default.(ext)`，当找不到某个 TypeID 的自定义文件时回退

说明：当前版本“物品音效”未内置随机差分（`_1/_2/...`）选择逻辑；如需差分，请在上游制作时自行合并或选择固定文件名。

## 如何扩展新的消耗品类型
- 游戏新增了 `SFX/Item/use_xxx` 事件时，只需新增文件夹 `CustomItemSounds/xxx/` 并放入音频文件即可

## 配置（预留）

## item_sound_map.json（类型映射 + 无事件注入 + 共享文件）
位置：`CustomItemSounds/item_sound_map.json`（首次运行自动生成）。示例：

- defaultWhenNoEvent：原版没有任何事件时，回退到该类别；可为 null
- aliases：类别别名映射（内置："meds" → "bandage"）
- items：按 TypeID 覆盖类别、分段类别、是否强制在“无事件”时注入，以及文件基名（fileBase）

示例：

```
{
  "defaultWhenNoEvent": null,
  "aliases": { "meds": "bandage" },
  "items": {
    "403": { "soundKey": "bandage", "forceWhenNoEvent": true },
    "23":  { "soundKey": "bandage", "fileBase": "64" },
    "25":  { "soundKey": "bandage", "fileBase": "64" }
  }
}
```

说明：
- 将 403 映射到 bandage，且该物品若原版没有事件，会在阶段结束时主动注入 bandage 声音
- 23 和 25 共用 bandage/64.(ext) 这一份文件；也支持按分段指定 `actionFileBase`/`finishFileBase`

文件放置：
- `CustomItemSounds/bandage/64.mp3`（或 .wav/.ogg）
- 可选分段：`bandage/64_action.(ext)`、`bandage/64_finish.(ext)`

注：如需覆盖分类默认，仍可放 `bandage/<TypeID>.(ext)` 或 `bandage/default.(ext)`；查找优先级见上文。

- 计划支持 `item_voice_rules.json`：用于配置特定 `TypeID` 是否启用替换、映射到不同的分类/文件、变体权重等（当前版本尚未启用，仅在代码中预留了注释位置）

## 日志
- Info：关键路径（例如：未找到自定义文件）
- Debug：详细过程（例如：拦截到的事件/类别、TypeID、候选路径、最终使用的文件名）

## 常见问题
- 没有声音？
  - 检查 `CustomItemSounds/<category>/<TypeID>.ext` 是否存在
  - 确认扩展名是否在支持列表内
  - 查看日志（Debug 级别）以确认事件类别、TypeID 是否正确被捕获
