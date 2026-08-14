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
| `iconPrefix` | Determined by that SimpleRule's `IconType`; empty defaults to `normal`, can be set to `elite`/`boss` |
| `voiceType` | If NameKey exists, extracted from the NameKey's second segment with original casing (`Cname_Scav` → `Scav`); matched first, falling back to the raw VoiceType. Otherwise, uses the game's raw VoiceType enum: `Duck`, `Robot`, `Wolf`, `Chicken`, `Crow`, `Eagle`, `coalball` |
| `soundKey` | The four soundKeys above |
| `ext` | `.mp3` or `.wav` (set in `voice_rules.json` `PreferredExtensions`) |

**Recommended naming** (Scav):
```
Scav/normal_scav_normal.mp3
Scav/normal_scav_surprise.mp3
Scav/normal_scav_grenade.mp3
Scav/normal_scav_death.mp3
Scav/elite_scav_normal.mp3      # Only matches with a rule like { "NameKey": "Cname_Scav", "IconType": "elite", "FilePattern": "CustomEnemySounds/Scav" }
```

**Not recommended (but still matches)**:
```
Scav/normal_duck_normal.mp3  ⚠️  # Only when no "scav"-derived file exists does the engine fall back to the raw VoiceType (e.g. Duck), so this file can still match Scavs (case-insensitive on Windows); it is shared by every enemy of that voice line, so prefer the "scav" name
```

Players (NameKey empty) are matched by Team, for example:
```
Player/normal_duck_surprise.mp3
```
(corresponding rule: `{ "Team": "player", "FilePattern": "CustomEnemySounds/Player" }`; footstep/dash sounds belong to the Footsteps module — see that page)

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
| `Fallback.UseOriginalWhenMissing` | Reserved field, not functional in the current build (the vanilla sound always plays when no custom file matches) |
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

### Complex Rules (`Rules[]`)

With `UseSimpleRules: false`, the `Rules[]` array is used:

| Field | Description |
|------|------|
| `Team` | Team matching (e.g. `scav`, `pmc`) |
| `IconType` | Prefix (same as simple mode; empty = `normal`) |
| `MinHealth` / `MaxHealth` | Health range |
| `NameKeyContains` | Partial NameKey match |
| `ForceVoiceType` | Override voiceType (e.g. `Duck`/`Robot`) |
| `SoundKeys` | Only match these soundKeys |
| `FilePattern` | Audio directory; supports tokens `{team}`, `{rank}`, `{voiceType}`, `{soundKey}`, etc. |

- `DefaultPattern`: default template `CustomEnemySounds/{team}/{rank}_{voiceType}_{soundKey}{ext}`; the `{team}`/`{rank}` tokens only apply in complex rules (in simple mode the prefix comes only from IconType).
- Unknown soundKeys also participate at normal priority (10), not just the four built-in ones.
- Simple mode has two wildcard fallbacks: dropping the soundKey (`{iconPrefix}_{voiceType}.{ext}`) and ignoring voiceType (`{iconPrefix}_*_{soundKey}{ext}`).
- `MinCooldownSeconds` is not used by the voice module (it applies to the footsteps module).

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
  "enemyVoiceVolumeScale": 1.0,
  "deathVoiceFrequency": "always",
  "npcGrenadeSurprisedFrequency": "always",
  "npcGrenadeSurprisedMaxDistance": 10.0
}
```

- `enemyVoiceVolumeScale`: Voice volume scale, default 1.0, range 0–2
- `deathVoiceFrequency`: `"always"` / number in seconds (cooldown) / `"off"` to disable
- `npcGrenadeSurprisedFrequency`: Same as above
- `npcGrenadeSurprisedMaxDistance`: Max distance for NPC grenade alert, default 10 meters

## FAQ

**Voice not playing**: Set `Debug.Level: "Verbose"`, search `player.log` for `[CES]` to see the matching process. Check file naming, paths, and extensions. Prefer the `scav` name for Scavs; `duck` is only a raw voice-line fallback, not recommended as the primary name.

**Separate Boss voices**: Add a SimpleRules entry like `{ "NameKey": "Cname_Wolf", "FilePattern": "CustomEnemySounds/BossWolf" }`.

**Triggering too frequently**: Use `PlayerOnly` mode, or set `deathVoiceFrequency: "5.0"`.

**Disable a certain soundKey**: There is no per-soundKey mute option in the current build; the vanilla sound always plays when no custom file matches. To reduce triggering, use the "Player Only" trigger mode or raise the corresponding frequency cooldown.

**Different voices for different ranks**: Add multiple SimpleRules entries for the same NameKey with different `IconType` values (`elite`/`boss`) and name the files with the matching prefix; `elite_`/`boss_` filenames alone do not work without such rules.

**One file covering all soundKeys**: Just place `normal_scav.mp3`; the system falls back to this when specific soundKey files are not found.

## Advanced — Appendix: Common NameKey Reference

The auto-generated `voice_rules.json` only includes Scav and USEC by default. To customize voices for more characters, reference the common NameKeys below and add corresponding entries to SimpleRules.

| NameKey | Description |
|---------|-------------|
| `Cname_Scav` | Regular Scav |
| `Cname_Usec` | USEC soldiers |
| `Cname_Wolf` | Wolf (Boss) |
| `Cname_Boss_Sniper` | Lauden (Boss) |
| `Cname_Boss_Shot` | Shotgunner (Boss) |
| `Cname_ServerGuardian` | Mine Chief (Boss) |
| `Cname_Speedy` | Speedy Captain (Boss) |
| `Cname_SpeedyChild` | Speedy Captain Minion |
| `Cname_Prison_Boss` | Warden (Boss) |
| `Cname_BALeader` | BA Leader (Boss) |
| `Cname_BALeader_Child` | BA Leader Minion |
| `Cname_Boss_Fly` | Fly Captain (Boss) |
| `Cname_Boss_Fly_Child` | Fly Captain Minion |
| `Cname_Boss_Arcade` | Rampant Arcade (Boss) |
| `Cname_Boss_3Shot` | Three-Shot (Boss) |
| `Cname_StormBoss1` ~ `StormBoss5` | Storm Bosses (Pulongpulong etc.) |
| `Cname_ShortEagle` | Short Eagle (Boss) |
| `Cname_UltraMan` | Ultraman (Boss) |
| `Cname_CrazyRob` | Crazy Robot Spider (Boss) |
| `Cname_Vida` | Vida (Boss) |
| `Cname_ScavRage` | Enraged Scav |
| `Cname_Raider` | Raider |
| `Cname_RobSpider` | Robot Spider |
| `Cname_StormCreature` | Storm Creature |
| `Cname_Mushroom` | Mushroom Man |
| `Cname_SchoolBully` | School Bully |

For more NameKeys, search `player.log` for `[CES]` to see the actual enemy names being matched.
