# Logging & Troubleshooting

Logs are managed by the Unity engine and output to `player.log`. Log prefix format:
```
[DuckovCustomSounds][Module][SubModule][Level] Content
```

## Where to Find Logs

| System | Path |
|------|------|
| Windows | `C:\Users\{username}\AppData\LocalLow\TeamSoda\Duckov\player.log` |
| Mac | `~/Library/Application Support/TeamSoda/Duckov/player.log` |

Log entries containing the `[DuckovCustomSounds]` prefix are outputs from this mod.

## Log Levels

5 levels, from low to high:

| Level | Description |
|------|------|
| Error | Errors, features cannot work normally |
| Warning | Warnings, features may be affected |
| Info | Runtime information (default level) |
| Debug | Debugging details |
| Verbose | Most detailed trace information |

Setting a level outputs that level and all levels below it. For example, setting Debug outputs Error, Warning, Info, and Debug.

## Configuring Log Levels

### Method 1: settings.json

Add to `DuckovCustomSounds/settings.json`:

```json
{
  "logging": {
    "defaultLevel": "Info",
    "modules": {
      "Core": { "level": "Info" },
      "Enemy": { "level": "Debug" },
      "Gun": { "level": "Info" }
    }
  }
}
```

- `defaultLevel`: Default level for all modules
- `modules.ModuleName.level`: Override level for a specific module

Available module names: `Core`, `SoundPack`, `Enemy`, `Footstep`, `BGM`, `HomeBGM`, `SceneBGM`, `ExtractionBGM`, `Gun`, `Grenade`, `Item`, `Melee`, `HitAndKill`

### Method 2: debug_off / .nolog File

Create an empty file named `debug_off` or `.nolog` in the `DuckovCustomSounds/` directory to clamp all modules' log level to Info (suppressing Debug/Verbose).

### Method 3: voice_rules.json (Partial Modules Only)

The `Debug.Level` settings in `voice_rules.json` and `footstep_voice_rule.json` will also sync to the logging system (only when settings.json does not explicitly set the same module).

## FAQ

**Sounds not playing**:
1. Confirm the corresponding module is enabled in ModConfig
2. Verify files are in the correct directory with correct filenames
3. Check player.log, search for `not found` or `未找到` keywords
4. Check file formats: `.mp3`, `.wav`, `.ogg`, `.flac`

**Volume adjustment not working**: Fixed in v2.x, update to the latest version.

**Too many log entries**: Change `defaultLevel` to `Warning`, or place a `debug_off` file.

**ModConfig settings not saving**: Check if `settings.json` is read-only, or if disk space is sufficient.

**Sound delay/stutter**: Keep audio files under 5 MB, sample rate 44.1kHz, use compressed formats (`.ogg`). Installing the game on an SSD is recommended.

## Troubleshooting Methodology

1. Set the suspected module's level to `Debug` or `Verbose`
2. Reproduce the issue
3. Open player.log and search for the corresponding module prefix: `[Enemy]`, `[Gun]`, `[Item]`, `[BGM]`, etc.
4. Use the logs to identify path errors, missing files, unsupported formats, etc.

## Getting Help

- QQ Group: 979203137
- GitHub: https://github.com/Guducat/DuckovCustomSounds
