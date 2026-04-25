---
title: Grenade Sounds
---

# Grenade Sounds

Replace grenade and explosive sounds. Matched by soundKey.

## Quick Start

```
CustomGrenadeSounds/
├── explode_grenade.mp3
└── default.mp3
```

soundKey comes from the game internals — **example names are for reference only**. Actual names must be confirmed through logs.

Obtaining them: Set `logging.modules.Grenade.level` to `Debug`, trigger an explosion, search `player.log` for `[Grenade]` and look at `soundKey=xxx`.

## File Lookup

1. `{soundKey}.mp3` → 2. `default.mp3`

## ModConfig

| Setting | Default |
|------|------|
| Enable Custom Grenade Sounds | On |
| Volume Scale | 1.0 (0–2) |

## Formats

`.mp3` → `.wav` → `.ogg` → `.oga`.
