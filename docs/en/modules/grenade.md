---
title: Grenade Sounds
---

# Grenade Sounds

Replace grenade and explosive sounds. Match by source and TypeID first, falling back to soundKey and default.

## Quick Start

```
CustomGrenadeSounds/
├── explode_grenade.mp3      # Legacy layout, still supported
├── default.mp3              # Legacy fallback
├── grenade_sound_map.json   # Optional map
├── grenade/
│   ├── 1234.mp3             # Match by grenade TypeID
│   ├── explode_grenade.mp3
│   └── default.mp3
├── breakable/
│   └── default.mp3          # Barrels and other breakables
└── proxy/
    └── default.mp3          # ExplosionProxy source
```

soundKey comes from the game internals — **example names are for reference only**. Actual names must be confirmed through logs.

Obtaining them: Set `logging.modules.Grenade.level` to `Debug`, trigger an explosion, search `player.log` for `[Grenade]` and look at `soundKey=xxx`.

## File Lookup

The legacy root layout remains supported: `CustomGrenadeSounds/{soundKey}.*` and `CustomGrenadeSounds/default.*` are still used as root fallbacks.

Grenade source lookup order:

1. `CustomGrenadeSounds/grenade/{TypeID}.*`
2. `CustomGrenadeSounds/grenade/{fileBase}.*`
3. `CustomGrenadeSounds/grenade/{soundKey}.*`
4. `CustomGrenadeSounds/grenade/default.*`
5. `CustomGrenadeSounds/{TypeID}.*`
6. `CustomGrenadeSounds/{fileBase}.*`
7. `CustomGrenadeSounds/{soundKey}.*`
8. `CustomGrenadeSounds/default.*`

> Note: steps 1 and 5 are skipped when the TypeID is unavailable (`fromWeaponItemID` ≤ 0); without a configured fileBase, `{fileBase}` and `{soundKey}` are the same file name.

Barrels and other breakables use:

1. `CustomGrenadeSounds/breakable/{fileBase}.*`
2. `CustomGrenadeSounds/breakable/{soundKey}.*`
3. `CustomGrenadeSounds/breakable/default.*`
4. `CustomGrenadeSounds/{fileBase}.*`
5. `CustomGrenadeSounds/{soundKey}.*`
6. `CustomGrenadeSounds/default.*`

`ExplosionProxy` lookup order: 1. `CustomGrenadeSounds/proxy/{fileBase}.*` 2. `CustomGrenadeSounds/proxy/{soundKey}.*` 3. `CustomGrenadeSounds/proxy/default.*` 4. `CustomGrenadeSounds/{fileBase}.*` 5. `CustomGrenadeSounds/{soundKey}.*` 6. `CustomGrenadeSounds/default.*`. Unknown sources use only the legacy root layout (`{fileBase}` → `{soundKey}` → `default`).

## Mapping and No-Event Injection

`grenade_sound_map.json` can remap TypeIDs, share file basenames, and inject sounds for grenades that do not emit a native explosion event, such as smoke or EMP-style grenades. Injected sounds are resolved through the grenade lookup chain (`grenade/` folder first, then the root fallback).

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

`soundKey` selects the category, while `fileBase` lets multiple TypeIDs share one file group. `forceWhenNoEvent` defaults to enabled, but injection only happens when the TypeID has a `soundKey` or `defaultWhenNoEvent` is configured.

## Variants

After `frag.mp3` is matched, files like `frag_1.mp3` and `frag_2.mp3` in the same folder are selected randomly; variants are also matched when `frag.mp3` itself is absent but `frag_1.mp3` exists. Only numeric suffixes with value ≥ 1, such as `_1` and `_2`, are treated as variants.

## ModConfig

| Setting | Default |
|------|------|
| Enable Custom Grenade Sounds | On |
| Volume Scale | 1.0 (0–2) |

## Formats

`.mp3` → `.wav` → `.ogg` → `.oga`.
