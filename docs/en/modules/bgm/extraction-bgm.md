---
title: Extraction BGM
---

# Extraction BGM

Customize extraction zone sounds, supporting countdown cues and success stinger replacement.

> On some maps the extraction success sound may falsely trigger; a future fix is planned.

## Three Modes

Select via ModConfig → ExtractionBGM.

### Disabled
No changes; vanilla audio is used.

### Countdown Mode
When the extraction countdown reaches ≤ 5 seconds, a custom sound plays (one-shot, via SFX bus). On extraction success, the vanilla Stinger is suppressed so the countdown sound can finish naturally. Aborting extraction stops it immediately.

File lookup order: `Extraction/countdown.*` → `Extraction/extraction.*`.

Recommended sound length: 10–15 seconds.

### Success Stinger Mode
Only replaces the extraction success Stinger. Vanilla countdown sounds are preserved.

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

**Countdown sound doesn't trigger**: Confirm "Countdown Mode" is selected, the file exists (`countdown.*` or `extraction.*`), and the countdown is > 5 seconds.

**Success stinger not replaced**: Confirm "Success Stinger Mode" is selected, the file exists (`success.*` or `TitleBGM/extraction.*`).

**Hear two sounds in countdown mode**: The countdown sound clip is too short — the vanilla success Stinger plays before it ends. Use a 10–15 second clip.

**Can't use countdown and success replacement together**: The two modes are mutually exclusive. For a combined effect, use Countdown Mode with a long clip (covering countdown through success).

**Volume not comfortable**: Extraction sounds use the SFX bus and are affected by the game's SFX volume. You can also adjust extraction sound volume independently in ModConfig.
