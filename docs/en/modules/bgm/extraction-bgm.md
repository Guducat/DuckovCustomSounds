---
title: Extraction BGM
---

# Extraction BGM

Customize extraction zone sounds, supporting countdown cues and success stinger replacement.

Success replacement is triggered by the game's confirmed evacuation event and filtered by the source level. Maps with previously unknown `stg_map_*` suffixes, including Hidden Warehouse, are covered while base and non-evacuation map cues remain vanilla.

## Three Modes

Select via ModConfig → ExtractionBGM.

### Disabled
No changes; vanilla audio is used.

### Countdown Mode
When the extraction countdown reaches ≤ 5 seconds, a custom sound plays (one-shot, via SFX bus) while scene BGM ducks quickly (volume only, never stopped, avoiding double BGM); scene BGM restores smoothly if extraction is aborted. On extraction success, the vanilla Stinger is suppressed so the countdown sound can finish naturally.

File lookup order: `Extraction/countdown.*` → `Extraction/extraction.*`.

Recommended sound length: 10–15 seconds.

### Success Stinger Mode
Plays custom music after evacuation is confirmed and suppresses the vanilla map Stinger during the short evacuation transition. Existing countdown behavior is preserved.

File lookup order: `Extraction/success.*` → `TitleBGM/extraction.*`.

## Directory Structure

Countdown mode:
```
Extraction/
└─ countdown.mp3
```

Success replacement mode:
```
Extraction/
└─ success.mp3
```

Legacy compatibility (still supported):
```
Extraction/
└─ extraction.mp3

TitleBGM/
└─ extraction.mp3   # Success fallback
```

## ModConfig Settings

| Setting | Options |
|------|------|
| Extraction Music Mode | Disabled / Countdown Sound Mode / Success Stinger Mode |
| Extraction Sound Volume | 0–100% |

## settings.json (legacy compatibility)

```json
{
  "overrideExtractionBGM": true   // true = countdown mode, false = disabled
}
```

Using ModConfig is recommended over directly editing settings.json.

## FAQ

**Countdown sound doesn't trigger**: Confirm "Countdown Mode" is selected, the file exists (`countdown.*` or `extraction.*`), and the countdown has ≤ 5 seconds remaining (by design it does not play while more than 5 seconds remain).

**Success stinger not replaced**: Confirm "Success Stinger Mode" is selected, the file exists (`success.*` or `TitleBGM/extraction.*`), and the log contains `撤离音乐替换已触发`.

**Hear two sounds in countdown mode**: Since v2.3.1 the vanilla success Stinger is suppressed in Countdown Mode, so a second sound should not occur; if it still does, confirm you are not on an older version and check the log that the countdown sound actually triggered.

**Can't use countdown and success replacement together**: The two modes are mutually exclusive. For a combined effect, use Countdown Mode with a long clip (covering countdown through success).

**Volume not comfortable**: The countdown sound uses the SFX bus (affected by the game's SFX volume); the success replacement sound uses the Music bus. Both can be adjusted via the Extraction Sound Volume in ModConfig.
