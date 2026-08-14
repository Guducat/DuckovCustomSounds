---
title: Gun Sounds
---

# Gun Sounds

Replace gunshot and reload sounds. Use TypeID to create unique sounds for each gun.

## Quick Start

Place a `default.mp3` to test:
```
CustomGunSounds/
└── default.mp3
```

Create per‑gun sounds:
```
CustomGunSounds/
├── 258.mp3               # TypeID 258 gunshot
├── 258_mute.mp3          # With suppressor attached
├── 258_reload.mp3        # Reload
├── 258_reload_start.mp3  # Reload start
├── 258_reload_end.mp3    # Reload end
└── default.mp3
```

## TypeID

Each gun has a unique numeric ID. Different models within the same weapon family (AK47, AK103, AK74) have different TypeIDs.

**Getting them**: Set `logging.modules.Gun.level` to `Debug` in `settings.json`, fire the weapon, then search `player.log` for `[GunShoot]` and look at `TypeID=xxx`.

## File Lookup Priority

**Shoot (suppressed)**: `{TypeID}_mute` → `{soundKey}_mute` → `{TypeID}` → `{soundKey}` → `default`

**Shoot (unsuppressed)**: `{TypeID}` → `{soundKey}` → `default`

**Reload start**: `{TypeID}_reload_start` → `{TypeID}_reload` → `{soundKey}` → `default_reload_start` → `default_reload` → `default`

**Reload end**: `{TypeID}_reload_end` → `{soundKey}` → `default_reload_end` → `default`

Where `{soundKey}` is the weapon sound identifier obtained from the `soundKey=xxx` field in the `[GunShoot]` line in `player.log`.

## Variants

Add `_1`, `_2` suffixes to the same TypeID for random selection:
```
├── 258_mute.mp3
├── 258_mute_1.mp3
├── 258_mute_2.mp3
```

## Shoot Rate Limiting

Prevents high‑rate‑of‑fire sound overlap. Set in `settings.json` (requires `gunShootDev` enabled, which is never written automatically):
```json
{
  "enableGunShootRateLimit": true,
  "gunShootMinIntervalMs": 95.0,
  "gunShootRateLimitPerType": { "258": 150 }
}
```

If `gunShootMinIntervalMs` is not set, the default is 95 (range 0–1000); `gunShootRateLimitPerType` overrides the interval per TypeID.

## ModConfig

| Setting | Default |
|------|------|
| Enable Custom Gun Sounds | On |
| Volume Scale | 1.0 (0–2) |

## FAQ

**How to find TypeID**: Set Gun Debug logging, fire the weapon, search `player.log` for `[GunShoot]`.

**Suppressor sound not working**: Filename must be `{TypeID}_mute.mp3`; confirm the weapon actually has a suppressor attached.

**Reload interruption looks bad**: Use segmented sounds (`_reload_start` + `_reload_end`); the end segment won't be interrupted.

**Formats**: `.mp3` → `.wav` → `.ogg` → `.oga`.
