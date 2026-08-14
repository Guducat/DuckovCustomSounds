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

Each melee weapon has a unique numeric ID. **Use the numeric TypeID as the primary name**; files named by the event soundKey (e.g. `knife.mp3`) also work as a fallback, with numeric files taking priority.

Obtaining them: Set `logging.modules.Melee.level` to `Debug`, attack with the weapon, search `player.log` for `[MeleeAttack]` and look at `TypeID=xxx`.

## File Lookup

Attack:
1. `{TypeID}.mp3` → 2. `{soundKey}.mp3` → 3. `default.mp3`

Swing:
1. `{TypeID}_swing.mp3` → 2. `{soundKey}.mp3` → 3. `default_swing.mp3`

`{soundKey}` is the part of the event name after the `SFX/Combat/Melee/attack_` / `swing_` prefix (see `soundKey=xxx` in the `[MeleeAttack]` / `[MeleeSwing]` logs).

## Swing Sounds

The melee swing (wind‑up/whiff) sound is separate from the attack: name files `{TypeID}_swing.mp3` (e.g. `98_swing.mp3`), falling back to `default_swing.mp3`; `_1`/`_2` variants are also supported. Obtain the TypeID the same way as for attacks (search the logs for `[MeleeSwing]`).

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
