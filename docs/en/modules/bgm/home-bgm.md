---
title: Home Phonograph BGM
---

# Home Phonograph BGM

Replace the phonograph playlist in the bunker home and main menu with custom music. Supports multiple tracks, auto‑advance, and random playback.

## Quick Start

1. Create the `DuckovCustomSounds/HomeBGM/` directory.
2. Place your music files inside:
```
HomeBGM/
├─ Never Gonna Give You Up - Rick Astley.mp3
├─ Bohemian Rhapsody - Queen.mp3
└─ My Favorite Song.mp3
```
3. Enter the game, go to the bunker home — the phonograph should play your music.

## File Naming

- **Recommended**: `Title - Artist.mp3`. Example: `Never Gonna Give You Up - Rick Astley.mp3`. The module auto‑splits into track title and artist.
- **Simple**: `Title.mp3`. Artist is shown as "Various Artists".
- Use a half‑width hyphen `-` as the separator, not a Chinese em dash.

## Supported Formats

`.mp3`, `.wav`, `.ogg`, `.oga`, `.flac`, `.aif`, `.aiff`, `.mp2`, `.m4a`, `.mp4`, `.wma`, `.asf`, `.fsb`, `.it`, `.mid`, `.midi`, `.mod`, `.s3m`, `.xm`.

## ModConfig Settings

| Setting | Default | Description |
|------|------|------|
| Enable Home Music | On | Master switch |
| Enable Base Entry Sound | On | Play start.mp3 when entering base |
| Music Volume (%) | 100 | 0–100%, takes effect immediately |
| Route Music through SFX Bus | Off | Experimental, routes through SFX instead of Music bus |
| Random Next Track | Off | Sequential or random playback |
| Random Previous Too | Off | Requires Random to be enabled first |
| Avoid Consecutive Repeat | On | Skips repeating the same track in random mode |
| Auto‑play Next Track | On | Automatically advances after a track ends |

## settings.json (fallback when ModConfig is unavailable)

```json
{
  "homeBgmAutoPlayNext": true,
  "homeBgmRandomEnabled": false,
  "homeBgmRandomNoRepeat": true,
  "homeBgmRandomizePrevious": false
}
```

## FAQ

**Phonograph not playing custom music**: Confirm `HomeBGM/` has files and the module is enabled. Enable Debug logging to see loading info.

**Track switching crackles/stutters**: Use Audacity to normalize sample rate (44.1kHz), normalize loudness (–3dB to 0dB), and add a 0.5–1s fade-out at the end of each track.

**Auto‑advance not working**: Confirm "Auto‑play Next Track" is enabled in ModConfig, and wait for the current track to finish naturally.

**Random keeps repeating the same track**: Ensure you have multiple tracks and "Avoid Consecutive Repeat" is enabled.

**Phonograph shows the wrong track name**: Rename the file to `Title - Artist.mp3` using a half‑width `-`.

**Volume not comfortable**: Adjust the ModConfig volume slider, or use Audacity to adjust file volume.
