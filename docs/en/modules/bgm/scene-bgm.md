---
title: Scene BGM
---

# Scene BGM

Scene BGM uses a two-phase structure: Enter (one‑shot) + Loop (continuous), with matching by scene name, type keyword, or default fallback.

## Directory & Naming

```
SceneBGM/
├── Enter/                  # Entry music (non-looping, plays once)
│   ├── zero_enter.mp3
│   ├── fram_enter.mp3
│   └── default_enter.mp3
└── Loop/                   # Continuous looping music
    ├── zero_loop.mp3
    ├── fram_loop.mp3
    └── default_loop.mp3
```

### Matching Priority

1. **Exact scene name**: `<scene_name>_enter.mp3` / `<scene_name>_loop.mp3`
2. **Scene name without suffix**: `<scene_name>.mp3` (no `_enter`/`_loop` suffix, e.g. `loadingscreen_getout.mp3`)
3. **sceneId match**: `<sceneId>_enter.mp3` / `<sceneId>_loop.mp3` (e.g. `level_farm_main_enter.mp3`)
4. **sceneId variant compatibility**: `Level_Farm_01`, `Level_GroundZero_1` will also try `level_farm_main_*`, `level_groundzero_main_*`
5. **Type keywords**: `loading_*`, `lab_*`, `factory_*`, `farm_*`, `zero_*`, `warehouse_*`, `expedition_*`, `outskirts_*`
6. **Default**: `default_enter.mp3` / `default_loop.mp3`

Note: Loading screens belong to the "loading" type; Enter BGM will not play default music for loading scenes (to avoid unwanted playback during black screens). **Type keywords also support Chinese** (e.g. `农场_enter.mp3`, `仓库_enter.mp3`, `零号区_enter.mp3`, `工厂_enter.mp3`, etc.).

#### v2.1.1 Update
`Level_HiddenWarehouse_Main` is now also compatible with `level_warehouse_main_*`, since the warehouse map had its scene name changed and resource packs should use the stable map name.

## ModConfig Settings

| Setting | Default | Description |
|------|------|------|
| Enable Scene Music System | On | Master switch |
| Enable Enter Scene BGM | On | Play Enter music |
| Enter Music Volume | 80% | 0–100% |
| Enable Loop Scene BGM | On | Play Loop music |
| Loop Music Volume | 60% | 0–100% |
| Override Default Scene Music | On | Whether Loop overrides native scene music |

Changes take effect immediately; volume changes transition smoothly.

## Typical Flow

1. Level initialization complete → delay `sceneLoadDelay` seconds
2. If Enter music exists → fade in and play, auto-destroy on finish
3. Cross-fade into Loop music
4. Auto-stop on exiting the level

## Advanced Config (config.json)

File location: `SceneBGM/config.json`. Auto-generated on first run.

```json
{
  "enterFadeDuration": 1.5,
  "loopFadeDuration": 2.0,
  "sceneLoadDelay": 2.0,
  "crossfadeDuration": 1.0
}
```

| Parameter | Description |
|------|------|
| `enterFadeDuration` | Enter fade in/out time (seconds) |
| `loopFadeDuration` | Loop fade in/out time (seconds) |
| `sceneLoadDelay` | Delay after scene load (seconds) |
| `crossfadeDuration` | Enter→Loop crossfade time (seconds) |

## Priority

Boss BGM > Scene Loop > Scene Enter. Scene BGM is automatically downgraded or stopped when a Boss is active.
