---
title: Melee Sounds
---

# Melee Sounds

Replace melee weapon sounds. Matched by TypeID (numeric).

## Quick Start

```
CustomMeleeSounds/
├── 98.ogg              # TypeID 98 (e.g. shovel)
├── 156.mp3             # TypeID 156
└── default.mp3         # Generic fallback
```

## TypeID

Each melee weapon has a unique numeric ID. **Must use the number**; weapon names are NOT valid (e.g. `knife.mp3` is wrong).

Obtaining them: Set `logging.modules.Melee.level` to `Debug`, attack with the weapon, search `player.log` for `[MeleeAttack]` and look at `TypeID=xxx`.

## File Lookup

1. `{TypeID}.mp3` → 2. `default.mp3`

## Variants

Add `_1`, `_2` suffixes for random selection:
```
├── 98.mp3
├── 98_1.mp3
└── 98_2.mp3
```

## ModConfig

| Setting | Default |
|------|------|
| Enable Custom Melee Sounds | On |
| Volume Scale | 1.0 (0–2) |

## Formats

`.mp3` → `.wav` → `.ogg` → `.oga`.
