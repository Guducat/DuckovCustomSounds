---
title: Sound Pack System
---

# Sound Pack System

Sound Packs let you switch between different audio resource sets with one click. A game restart is required for changes to take effect.

## Installing a Pack

1. Place the pack folder under `DuckovCustomSounds/`.
2. Launch the game, ModConfig → select your sound pack.
3. Restart the game.

Pack not showing up? Check that the folder contains a valid `pack.json`.

## Switching & Restoring

- **ModConfig (recommended)**: In-game ModConfig → Sound Pack dropdown → select, then restart.
- **settings.json (fallback)**: Edit `currentSoundPack` to `""` (empty string) to restore defaults, or set it to the pack folder name.
- **Restore Default**: Select "Default" in ModConfig, or clear `currentSoundPack` and restart.

Module files are looked up only inside the current pack's directory (the root directory when Default is selected); a module missing from the pack does not fall back to the root — the vanilla audio plays instead.

## Creating Your Own Pack

1. Create a pack folder under `DuckovCustomSounds/` using English letters, numbers, and underscores (this is the pack ID). Example: `MyPack/`.
2. Place your replacement audio inside using the same module directory structure (only include what you need).
3. Create a `pack.json` inside the pack folder.
4. Restart the game, select it in ModConfig, restart again.

**Directory example**:
```
DuckovCustomSounds/
├── TitleBGM/                     # Default (root resources)
├── HomeBGM/
├── BossBGM/
├── MyPack/                       # Your pack (pack ID = MyPack)
│   ├── pack.json
│   ├── HomeBGM/
│   └── CustomEnemySounds/
└── AnotherPack/
    ├── pack.json
    └── CustomFootStepSounds/
```

## pack.json

**Minimal config**:
```json
{
  "name": "Your Pack Name",
  "author": "Your Name",
  "version": "1.0.0"
}
```

**Full config**:
```json
{
  "name": "My Custom Sounds",
  "author": "YourName",
  "version": "1.0.0",
  "description": "Replaces BGM and enemy voices",
  "compatibleModVersion": "2.0.0",
  "requiredModules": ["CustomBGM", "CustomEnemySounds"],
  "optional": {
    "homepage": "https://example.com",
    "qq": "123456"
  }
}
```

### Field Reference

| Field | Required | Description |
|------|------|------|
| `name` | Yes | Display name in the UI list |
| `author` | Yes | Author name |
| `version` | Yes | Version number, semantic versioning recommended (1.0.0) |
| `description` | No | Brief description (currently only stored in pack.json; not shown in the UI) |
| `compatibleModVersion` | No | Target mod compatibility version, informational only, does not affect loading |
| `requiredModules` | No | List of involved modules, informational only, no strict validation |
| `optional.homepage` | No | Homepage link |
| `optional.qq` | No | QQ number, group number, or link |

Notes:
- The pack ID is the **folder name**, not set inside pack.json.
- If any of `name`/`author`/`version` is missing, the pack is ignored.
- Log display format: `Name vVersion by Author - Description`; the ModConfig dropdown shows only `name`.

## Involved Modules

Sound Pack subdirectory names match module directory names:

| Module | Directory |
|------|------|
| BGM | `TitleBGM/`, `HomeBGM/`, `SceneBGM/`, `Extraction/`, `BossBGM/` |
| Enemy Voices | `CustomEnemySounds/` |
| Footsteps | `CustomFootStepSounds/` |
| Guns | `CustomGunSounds/` |
| Melee | `CustomMeleeSounds/` |
| Grenades | `CustomGrenadeSounds/` |
| Hit & Kill | `CustomHitAndKillSounds/` |
| Items | `CustomItemSounds/` |

## FAQ

- **Sound Pack option not showing**: No subfolder with a valid `pack.json` exists under `DuckovCustomSounds/` and the root directory has no typical resource folder (`HomeBGM`/`BossBGM`/`SceneBGM`/`TitleBGM`) containing audio — no option is shown in that case. If the root directory has typical resource folders with audio, a "Default" option automatically appears.
- **Switching not taking effect**: A game restart is required. Check that `currentSoundPack` in `settings.json` is correct.
- **Pack not appearing**: `pack.json` has invalid JSON syntax or is missing required fields.
- **How to restore defaults**: Select "Default" in the UI, or clear `currentSoundPack` and restart.
- **No nesting support**: Only first-level subdirectories under `DuckovCustomSounds/` are scanned.
