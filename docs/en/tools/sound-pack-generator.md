---
title: Sound Pack Generator
---

# Sound Pack Generator

Online form, instant generation of a properly formatted pack.json.

## How to Use

1. Fill in sound pack information
2. Required fields (marked with *) must be completed
3. Select optional modules and extra information
4. JSON preview on the right updates in real time
5. Click "Copy JSON"
6. Create a pack.json inside your sound pack folder and paste

<ClientOnly>
  <SoundPackGenerator />
</ClientOnly>

## Manual Creation

### Minimal Config

```json
{
  "name": "Your Sound Pack Name",
  "author": "Your Name",
  "version": "1.0.0"
}
```

### Full Config

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

## Field Reference

### Required

| Field | Description |
|------|------|
| `name` | Display name in the UI |
| `author` | Author |
| `version` | Version number (e.g. `1.0.0`) |

### Optional

| Field | Description |
|------|------|
| `description` | Brief description, shown after the name in the UI |
| `compatibleModVersion` | Compatible mod version, informational only, does not affect loading |
| `requiredModules` | List of involved modules: `CustomBGM`, `CustomEnemySounds`, `CustomFootStepSounds`, `CustomGunSounds`, `CustomMeleeSounds`, `CustomGrenadeSounds`, `CustomHitAndKillSounds`, `CustomItemSounds`. Informational only, no strict validation |
| `optional.homepage` | Homepage link |
| `optional.qq` | QQ number, group number, or link |

## Notes

- Pack ID = folder name, not set inside pack.json.
- UI display: `Name vVersion by Author - Description`.
- Packs missing required fields are ignored.
- When distributing, only include the sound pack folder, do not include settings.json.

## Directory Example

```
DuckovCustomSounds/
├── MyPack/                    # Pack ID = MyPack
│   ├── pack.json
│   ├── HomeBGM/
│   ├── CustomEnemySounds/
│   └── CustomFootStepSounds/
└── AnotherPack/
    ├── pack.json
    └── CustomGunSounds/
```
