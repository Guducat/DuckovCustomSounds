---
title: 手雷音效
---

# 手雷音效

替换手雷和爆炸物音效。优先按来源与 TypeID 匹配，回退到 soundKey 与 default。

## 快速开始

```
CustomGrenadeSounds/
├── explode_grenade.mp3      # 旧结构，继续兼容
├── default.mp3              # 旧结构通用回退
├── grenade_sound_map.json   # 可选映射
├── grenade/
│   ├── 1234.mp3             # 按手雷 TypeID 匹配
│   ├── explode_grenade.mp3
│   └── default.mp3
├── breakable/
│   └── default.mp3          # 油桶、可破坏物等来源
└── proxy/
    └── default.mp3          # ExplosionProxy 来源
```

soundKey 来自游戏内部，**示例名仅供参考**。实际名称需要通过日志确认。

获取：设 `logging.modules.Grenade.level` 为 `Debug`，触发爆炸，player.log 搜 `[Grenade]`，看 `soundKey=xxx`。

## 文件查找

旧结构完全保留：`CustomGrenadeSounds/{soundKey}.*` 和 `CustomGrenadeSounds/default.*` 仍会作为根目录回退。

手雷来源查找顺序：

1. `CustomGrenadeSounds/grenade/{TypeID}.*`
2. `CustomGrenadeSounds/grenade/{fileBase}.*`
3. `CustomGrenadeSounds/grenade/{soundKey}.*`
4. `CustomGrenadeSounds/grenade/default.*`
5. `CustomGrenadeSounds/{TypeID}.*`
6. `CustomGrenadeSounds/{fileBase}.*`
7. `CustomGrenadeSounds/{soundKey}.*`
8. `CustomGrenadeSounds/default.*`

> 注：TypeID 无效（`fromWeaponItemID` ≤ 0）时步骤 1 与 5 会跳过；未配置 fileBase 时，`{fileBase}` 与 `{soundKey}` 为同一文件名。

油桶、可破坏物等来源查找顺序：

1. `CustomGrenadeSounds/breakable/{fileBase}.*`
2. `CustomGrenadeSounds/breakable/{soundKey}.*`
3. `CustomGrenadeSounds/breakable/default.*`
4. `CustomGrenadeSounds/{fileBase}.*`
5. `CustomGrenadeSounds/{soundKey}.*`
6. `CustomGrenadeSounds/default.*`

`ExplosionProxy` 来源查找顺序：1. `CustomGrenadeSounds/proxy/{fileBase}.*` 2. `CustomGrenadeSounds/proxy/{soundKey}.*` 3. `CustomGrenadeSounds/proxy/default.*` 4. `CustomGrenadeSounds/{fileBase}.*` 5. `CustomGrenadeSounds/{soundKey}.*` 6. `CustomGrenadeSounds/default.*`。未识别来源（Unknown）只使用根目录旧结构（`{fileBase}` → `{soundKey}` → `default`）。

## 映射与无事件补声

`grenade_sound_map.json` 可按 TypeID 改写分类名、共享文件名，并为烟雾弹、电磁手雷等没有原版爆炸事件的投掷物补声。补声文件按手雷来源查找链解析（`grenade/` 目录优先，根目录回退）。

```json
{
  "defaultWhenNoEvent": null,
  "items": {
    "1234": {
      "soundKey": "explode_grenade",
      "fileBase": "frag",
      "forceWhenNoEvent": true
    }
  },
  "aliases": {
    "explode_grenade_old": "explode_grenade"
  }
}
```

`soundKey` 用于指定分类，`fileBase` 用于多个 TypeID 共用一组文件。`forceWhenNoEvent` 缺省为开启，但只有配置了该 TypeID 的 `soundKey` 或 `defaultWhenNoEvent` 时才会补声。

## 变体

命中 `frag.mp3` 后，如果同目录存在 `frag_1.mp3`、`frag_2.mp3`，会随机选择数字后缀变体；即使没有 `frag.mp3`，只存在 `frag_1.mp3` 也会被匹配。只识别数字后缀（数值 ≥ 1），如 `_1`、`_2`。

## ModConfig

| 设置 | 默认 |
|------|------|
| 启用自定义手雷音效 | 开 |
| 音量倍率 | 1.0（0~2） |

## 格式

`.mp3` → `.wav` → `.ogg` → `.oga`。
