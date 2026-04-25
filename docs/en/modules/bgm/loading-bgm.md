---
title: Loading Screen / Load Complete BGM
---

# Loading Screen / Load Complete BGM

Add custom BGM for the loading screen and the "load complete enter scene" moment. This is part of the "Scene BGM" module.

## Where to Put Files

Place your audio under `SceneBGM/`:

- `SceneBGM/Enter/`：Plays once when loading completes and the scene begins
- `SceneBGM/Loop/`：Loops during the scene

## Ready‑to‑Use Filenames

- Loading screen generic: `Enter/loading_enter.mp3`
- Loading screen loop: `Loop/loading_loop.mp3`
- Enter map generic: `Enter/default_enter.mp3`
- Scene loop generic: `Loop/default_loop.mp3`

Just 2–4 files cover the most common loading and entry needs.

Note: Loading screens are of "loading" type; Enter BGM will not play default music for loading scenes (only your explicitly placed `loading_enter.mp3`).

## More Precision

Prepare dedicated files for specific maps:

- `Enter/loadingScreen_getout.mp3`
- `Enter/level_groundzero_main_enter.mp3`
- `Loop/level_groundzero_main_loop.mp3`

Matching priority: exact scene name > type keyword (`loading_*`, `lab_*`, `farm_*`, etc.) > default.

## Example Structure

```
SceneBGM/
├── Enter/
│   ├── loading_enter.mp3
│   └── default_enter.mp3
└── Loop/
    ├── loading_loop.mp3
    └── default_loop.mp3
```

## Notes

- Volume follows the "Music" slider; Enter/Loop volumes can be adjusted individually in ModConfig.
- When a Boss BGM is simultaneously active, auto fade in/out applies; priority: Boss > Scene.
- For more parameters, see the "Scene BGM" page (`SceneBGM/config.json` lets you tune fade, delays, etc.).
