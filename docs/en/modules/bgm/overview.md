---
title: BGM Module Overview
---

# BGM Module Overview

This module covers BGM customization for title/home, level scenes, Boss encounters, and extraction sequences, working together with the Sound Pack system. Any audio not provided falls back to the vanilla sounds.

## Directory & File Overview

### Title & Home
| Directory | File | Behavior |
|------|------|------|
| `TitleBGM/` | `startFX.*` | Intro fanfare when entering the title screen (one-shot, auto-transitions to title) |
| `TitleBGM/` | `title.*` | Title/menu loop |
| `TitleBGM/` | `start.*` | Base entry chime (one-shot, toggleable in ModConfig) |
| `TitleBGM/` | `death.*` | Death notification (one-shot) |
| `TitleBGM/` | `extraction.*` | Extraction success default fallback |
| `HomeBGM/` | `Any.*` | Phonograph playlist, supports multiple tracks in any format |

### Scene BGM
| Directory | Matching Rule |
|------|---------|
| `SceneBGM/Enter/` | Plays once on scene entry (non-looping), filename `<scene_name>_enter` or exact match |
| `SceneBGM/Loop/` | Scene looping music, filename `<scene_name>_loop` or exact match |
| Default | `default_enter.*` / `default_loop.*` |
| Types | `loading_*`, `lab_*`, `factory_*`, `farm_*`, `zero_*`, etc. |

### Boss BGM
| Directory | Rule |
|------|------|
| `BossBGM/` | Named by NameKey minus `Cname_` prefix, e.g. `BALeader.mp3` |
| Default | `default_boss.*` |

### Extraction Sounds
| Directory | File | Behavior |
|------|------|------|
| `Extraction/` | `countdown.*` | Plays when countdown ≤ 5s (one-shot) |
| `Extraction/` | `success.*` | Extraction success replacement |
| `Extraction/` | `extraction.*` | Countdown default fallback |

**Supported formats** (by priority): `.mp3`, `.wav`, `.ogg`, `.oga`, `.flac`, `.aif`, `.aiff`, `.mp2`, `.m4a`, `.mp4`, `.wma`, `.asf`, `.fsb`, `.it`, `.mid`, `.midi`, `.mod`, `.s3m`, `.xm`.

---

## Priority & Coordination

Global priority (highest to lowest):
1. Boss BGM (a Boss is within trigger distance)
2. Scene Loop BGM (continuous cycling)
3. Scene Enter BGM (one-shot)
4. Title/Home BGM (in menus)

Transitions are handled via fade in/out. Extraction mode uses the SFX bus and is independent from the BGM pipeline.

---

## Config Entry (ModConfig)

- **HomeBGM**: Enable, volume, random, avoid repeat, randomize previous, auto next, SFX bus
- **SceneBGM**: Enable Enter/Loop, individual volumes, override native
- **BossBGM**: Enable, trigger distance, volume. Advanced params in `BossBGM/config.json`
- **ExtractionBGM**: Mode (off/countdown/success replacement), volume
- Config changes apply immediately; Sound Pack switching requires a game restart.

---

## FAQ

- **Files not taking effect**: Check that filenames are correct (case-sensitive), directories are in the right place, and the module is enabled in ModConfig.
- **Sound too loud/quiet**: Adjust the corresponding module's volume slider in ModConfig.
- **Multiple BGM conflict**: Priority is handled automatically: Boss > Scene > Title/Home. No manual coordination needed.

See individual sub-module pages for detailed instructions.
