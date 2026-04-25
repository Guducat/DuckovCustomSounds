---
title: Quickstart
---

# Quickstart

This page helps you quickly install, place audio files, and verify that replacements are working.

## Installation

- Place the mod in your game's `Mods/` directory and confirm it loads correctly (see repo root `README.md` for details).
- On first launch, `DuckovCustomSounds/settings.json` and default config files for each module are auto-generated.

## Minimal Working Directory

Prepare a minimal set of audio files to verify the replacement works:

```
DuckovCustomSounds/
├─ TitleBGM/
│  └─ title.mp3                # Title screen music
├─ HomeBGM/
│  ├─ ExampleTrackA.mp3
│  └─ ExampleTrackB.mp3
└─ CustomEnemySounds/
   └─ voice_rules.json         # Auto-generated template on first launch
```

- `.mp3` is recommended; supported formats vary slightly per module (see individual module pages).
- Files placed in the root directory are the "Default" resource set; you can later switch between multiple sets using "Sound Packs".

## Sound Pack Switching (Optional)

- Create a subfolder under `DuckovCustomSounds/` (e.g. `MyPack/`), with the same module structure and audio as the root.
- Create a `pack.json` inside the subfolder.
- Select your sound pack in the ModConfig UI, then restart to apply.

## Verification

1. Enter the title screen, confirm `TitleBGM/title.mp3` plays.
2. Enter the home, confirm `HomeBGM` tracks can switch.
3. Enter a match, test whether enemy voices or other modules trigger. If not, enable Debug for the relevant module or check the logs.

## Logging & Troubleshooting

- For path mismatches, files not found, etc., enable Debug level on the relevant module to see the resolution process.
- You can also place a `debug_off` or `.nolog` file under `DuckovCustomSounds/` to quickly suppress logs.
- See "Advanced > Logging & Troubleshooting" for details.

## Next Steps

- For quick switching between full resource sets, see "Modules > Sound Pack System".
- To customize BGM (Title/Home/Extraction/Scene/Boss), see individual "Modules > BGM" pages.
- To customize enemy voices, footsteps, guns, melee, grenades, or items, jump to the corresponding module page.
