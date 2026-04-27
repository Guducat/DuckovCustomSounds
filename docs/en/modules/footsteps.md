---
title: Footsteps
---

# Footsteps

Replace footstep and dash sounds for characters (players, enemies, NPCs). Shares the same rule engine as enemy voices.

## Quick Start

```
CustomFootStepSounds/
├── footstep_voice_rule.json    # Auto-generated on first launch
├── Scav/
│   ├── normal_scav_footstep_walk_light.mp3
│   ├── normal_scav_footstep_run_heavy.mp3
│   └── normal_scav_dash.mp3
└── Player/
    ├── normal_duck_footstep_walk_light.mp3
    ├── normal_duck_footstep_run_heavy.mp3
    └── normal_duck_dash.mp3
```

On first run, `footstep_voice_rule.json` is auto-generated (includes simplified rules for Player, Scav, and Usec by default). **Note: The config file must be named `footstep_voice_rule.json`, NOT `footsteps.json`.**

## soundKey Reference

The system auto-generates a soundKey based on character actions, tried in priority order:

**Footsteps**: First `footstep_{move}_{strength}_{material}` → fallback `footstep_{move}_{strength}`

**Dash**: First `dash_{material}` → fallback `dash`

| Variable | Values |
|------|------|
| `{move}` | `walk` / `run` |
| `{strength}` | `light` / `heavy` |
| `{material}` | Game `FootStepMaterialType` enum: `organic` (humanoid), `mech` (mechanical), `danger`, `nosound` (silent — no custom trigger), `horse` |

Examples: `footstep_walk_light_organic`, `footstep_run_heavy_mech`, `dash_organic`

## File Naming (Simplified Rules, Default)

Format: `{FilePattern}/{iconPrefix}_{voiceType}_{soundKey}{ext}`

| Variable | Description |
|------|------|
| `{iconPrefix}` | Rank: `normal`, `elite`, `boss` |
| `{voiceType}` | If NameKey exists, extracted from NameKey (e.g. `Cname_Scav` → `scav`). Otherwise, uses the game's `VoiceType` enum: `Duck`, `Robot`, `Wolf`, `Chicken`, `Crow`, `Eagle`, `coalball` |
| `{soundKey}` | As described above |
| `{ext}` | `.mp3`, `.wav`, etc. (in `PreferredExtensions` order) |

Recommended: `Scav/normal_scav_footstep_walk_light.mp3`

**Note**: Scav's voiceType is `scav`, not `duck`. The system prioritizes the value extracted from NameKey.

## footstep_voice_rule.json

```json
{
  "Debug": { "Enabled": true, "Level": "Info", "ValidateFileExists": true },
  "Fallback": { "UseOriginalWhenMissing": true, "PreferredExtensions": [".mp3", ".wav"] },
  "DefaultPattern": "CustomFootStepSounds/{team}/{rank}_{voiceType}_{soundKey}{ext}",
  "MinCooldownSeconds": 0.3,
  "UseSimpleRules": true,
  "SimpleRules": [
    { "Team": "player", "FilePattern": "CustomFootStepSounds/Player" },
    { "NameKey": "Cname_Scav", "FilePattern": "CustomFootStepSounds/Scav" },
    { "NameKey": "Cname_Usec", "FilePattern": "CustomFootStepSounds/Usec" }
  ],
  "PriorityInterruptEnabled": false,
  "BindVariantIndexPerEnemy": false
}
```

### Config Options

| Field | Description | Default |
|------|------|------|
| `Debug.Level` | `Error` / `Warning` / `Info` / `Debug` / `Verbose` | Info |
| `Fallback.UseOriginalWhenMissing` | Use vanilla when file not found | true |
| `Fallback.PreferredExtensions` | Extension priority order; can add `.ogg`, `.flac` | [".mp3",".wav"] |
| `MinCooldownSeconds` | Minimum trigger interval for the same character to avoid sound overlap | 0.3 (0.05–2.0) |
| `UseSimpleRules` | Use simplified rules | true |
| `SimpleRules[].NameKey` | Enemy identifier, e.g. `Cname_Scav` | - |
| `SimpleRules[].Team` | Match by team when NameKey is empty (`player`/`scav`/`pmc`) | - |
| `SimpleRules[].FilePattern` | Directory path | - |
| `BindVariantIndexPerEnemy` | Same enemy always uses the same variant | false |

### Variants

Add `_1`, `_2` suffixes (must be contiguous):
```
Scav/normal_scav_footstep_walk_light.mp3     # Variant 0
Scav/normal_scav_footstep_walk_light_1.mp3   # Variant 1
```

## ModConfig Settings

| Setting | Default | Range |
|------|------|------|
| Enable Custom Footstep Sounds | On | On/Off |
| Volume Scale | 1.0 | 0–2 |

## settings.json

```json
{
  "enableCustomFootStepSounds": true,
  "footstepVolumeScale": 1.0
}
```

## FAQ

**Original sounds still play despite having files**: Enable Debug logging, search `player.log` for `[CFS:Route]`. Confirm the voiceType in the filename is correct (Scav → `scav`, not `duck`). Viewing path logs requires enabling Debug for SceneBGM/EnemyVoice as well.

**Footsteps frequently interrupted/missing**: Adjust `MinCooldownSeconds` (increase if interrupted, decrease if missing).

**Different sounds for different materials**: Add `_organic`, `_mech`, `_danger`, `_horse` to filenames. The system prioritizes files with material identifiers.

**Disable a certain character**: Remove the corresponding rule from SimpleRules.

**Log keywords**: Search `player.log` for `[CFS]`, `[CFS:Route]`, `[CFS:Path]`, `[CFS:Cooldown]`.
