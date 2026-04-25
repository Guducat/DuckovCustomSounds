---
title: Enemy Voices
---

# Enemy Voices

Replace in‑game enemy shouts, surprise calls, grenade alerts, death sounds, and more. Supports matching by faction, rank, and specific character type, with random variants and priority‑based interruption.

## Quick Start

1. Create the directory structure:
```
CustomEnemySounds/
├── Scav/
│   ├── normal_scav_normal.mp3
│   ├── normal_scav_surprise.mp3
│   ├── normal_scav_grenade.mp3
│   └── normal_scav_death.mp3
└── Usec/
    ├── normal_usec_normal.mp3
    ├── normal_usec_surprise.mp3
    └── normal_usec_death.mp3
```

2. On first run, `voice_rules.json` is auto-generated (includes simplified rules for Scav and USEC by default).
3. Enter a game to test.

## soundKey Reference

Built‑in soundKeys and their priorities:

| soundKey | Usage | Priority |
|----------|------|--------|
| `death` | Death | 100 |
| `surprise` | Encounter/startled | 50 |
| `grenade` | Heard grenade (within 10m) | 40 |
| `normal` | General voice lines | 10 |

Higher‑priority sounds can interrupt lower‑priority ones. Disable this by setting `PriorityInterruptEnabled` to `false` in `voice_rules.json`.

## File Naming

### Simplified Rules (Default, Recommended)

Rule format: `{FilePattern}/{iconPrefix}_{voiceType}_{soundKey}{ext}`

| Variable | Description |
|------|------|
| `FilePattern` | Directory, set in `voice_rules.json` SimpleRules, e.g. `CustomEnemySounds/Scav` |
| `iconPrefix` | Rank: `normal`, `elite`, `boss` |
| `voiceType` | If NameKey exists, extracted from NameKey (`Cname_Scav` → `scav`, `Cname_Usec` → `usec`). Otherwise, uses the game's raw VoiceType enum: `Duck`, `Robot`, `Wolf`, `Chicken`, `Crow`, `Eagle`, `coalball` |
| `soundKey` | The four soundKeys above |
| `ext` | `.mp3` or `.wav` (set in `voice_rules.json` `PreferredExtensions`) |

**Recommended naming** (Scav):
```
Scav/normal_scav_normal.mp3
Scav/normal_scav_surprise.mp3
Scav/normal_scav_grenade.mp3
Scav/normal_scav_death.mp3
Scav/elite_scav_normal.mp3      # Elite Scav
```

**Wrong naming**:
```
Scav/normal_duck_normal.mp3  ❌  # Scav's voiceType is "scav", not "duck"
```

Players (NameKey empty) are matched by Team:
```
Player/normal_duck_footstep_walk_light.mp3
```

### Wildcard Fallback

When the system can't find `normal_scav_surprise.mp3`, it will try `normal_scav.mp3` (one file covering all soundKeys).

## voice_rules.json Configuration

File location: `CustomEnemySounds/voice_rules.json`

### Simplified Rules (Recommended)

```json
{
  "Debug": { "Enabled": true, "Level": "Info", "ValidateFileExists": true },
  "Fallback": { "UseOriginalWhenMissing": true, "PreferredExtensions": [".mp3", ".wav"] },
  "UseSimpleRules": true,
  "SimpleRules": [
    { "NameKey": "Cname_Scav", "FilePattern": "CustomEnemySounds/Scav" },
    { "NameKey": "Cname_Usec", "FilePattern": "CustomEnemySounds/Usec" },
    { "Team": "player", "FilePattern": "CustomEnemySounds/Player" }
  ],
  "PriorityInterruptEnabled": true,
  "BindVariantIndexPerEnemy": false
}
```

| Field | Description |
|------|------|
| `Debug.Level` | `Error`/`Warning`/`Info` (recommended)/`Debug`/`Verbose` |
| `Debug.ValidateFileExists` | Whether to check if the file exists before playing (recommended `true`) |
| `Fallback.UseOriginalWhenMissing` | Use vanilla when custom file is not found (recommended `true`) |
| `Fallback.PreferredExtensions` | Extension priority order; can add `.ogg`, `.flac`, etc. |
| `UseSimpleRules` | `true` = use simplified rules |
| `SimpleRules[].NameKey` | Unique enemy identifier, e.g. `Cname_Scav` |
| `SimpleRules[].Team` | Match by team when NameKey is empty (`player`, `scav`, `pmc`) |
| `SimpleRules[].FilePattern` | Directory where audio files are stored |
| `PriorityInterruptEnabled` | Whether higher priority interrupts lower priority |
| `BindVariantIndexPerEnemy` | `true` = same enemy always uses the same variant |

### Variants

Add `_1`, `_2` suffixes to the same soundKey (must be contiguous):
```
Scav/normal_scav_surprise.mp3     # Variant 0
Scav/normal_scav_surprise_1.mp3   # Variant 1
Scav/normal_scav_surprise_2.mp3   # Variant 2
```
The system picks one at random. With `BindVariantIndexPerEnemy` enabled, the same enemy always uses the same variant.

## ModConfig Settings

| Setting | Description |
|------|------|
| Allow NPC‑vs‑NPC Combat Voices | Toggle NPC‑to‑NPC combat voice lines |
| Voice Trigger Mode | Original (trigger all) / Player Only / Mixed (vanilla beyond 50m, player only within 50m) |
| Voice Volume Scale | 0–2, default 1.0 |

## settings.json

```json
{
  "enableNPCtoNPCCombatVoices": true,
  "enemyVoiceTriggerMode": "Original",
  "deathVoiceFrequency": "always",
  "npcGrenadeSurprisedFrequency": "always",
  "npcGrenadeSurprisedMaxDistance": 10.0
}
```

- `deathVoiceFrequency`: `"always"` / number in seconds (cooldown) / `"off"` to disable
- `npcGrenadeSurprisedFrequency`: Same as above
- `npcGrenadeSurprisedMaxDistance`: Max distance for NPC grenade alert, default 10 meters

## FAQ

**Voice not playing**: Set `Debug.Level: "Verbose"`, search `player.log` for `[CES]` to see the matching process. Check file naming, paths, and extensions. Scav's voiceType is `scav`, not `duck`.

**Separate Boss voices**: Add a SimpleRules entry like `{ "NameKey": "Cname_Wolf", "FilePattern": "CustomEnemySounds/BossWolf" }`.

**Triggering too frequently**: Use `PlayerOnly` mode, or set `deathVoiceFrequency: "5.0"`.

**Disable a certain soundKey**: Don't provide the corresponding file, and set `UseOriginalWhenMissing: false` for silence.

**Different voices for different ranks**: Use `normal_`, `elite_`, `boss_` prefix to distinguish ranks.

**One file covering all soundKeys**: Just place `normal_scav.mp3`; the system falls back to this when specific soundKey files are not found.
