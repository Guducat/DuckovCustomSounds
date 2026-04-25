---
title: Title BGM
---

# Title BGM

Replace title/menu-related music and sound cues. Missing files automatically fall back to vanilla audio.

## Directory & Files

Directory: `TitleBGM/`

| File | Behavior | Description |
|------|------|------|
| `startFX.*` | Intro fanfare, one-shot | Plays first when entering the title screen, then auto-switches to `title.*` |
| `title.*` | Title/menu loop | Main menu background music |
| `start.*` | Base entry chime, one-shot | Toggleable in HomeBGM settings ("Enable Base Entry Sound") |
| `death.*` | Death notification, one-shot | Triggers on player death |
| `extraction.*` | Extraction success default fallback | When `Extraction/success.*` is absent and "Success Replacement" mode is selected |

## Settings

- **Base entry chime**: ModConfig → HomeBGM → "Enable Base Entry Sound". Turning it off preserves the vanilla stg_map_base Stinger.

## FAQ

- **start/death not playing**: Verify the file exists with the correct extension; check that the setting toggle is on.
- **Sound too loud/quiet**: Title-related sounds have no independent volume control; use external tools to batch-adjust file volume.
