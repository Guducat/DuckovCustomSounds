---
title: Boss BGM
---

# Boss BGM

Play exclusive BGM for specific Bosses. Auto fade-in when entering trigger range, smooth fade-out on leaving or death. When multiple Bosses are present, the closest and highest-priority rule is selected.

## Directory & Naming

```
BossBGM/
├─ default_boss.mp3    # Default fallback
├─ BALeader.mp3        # Cname_BALeader → BALeader.mp3
├─ Boss_Sniper.ogg     # Cname_Boss_Sniper → Boss_Sniper.ogg
└─ ServerGuardian.flac # Cname_ServerGuardian → ServerGuardian.flac
```

- Filename = enemy NameKey minus the `Cname_` prefix.
- When no specific file is found, `default_boss.*` is used; if that is also missing, nothing plays.
- Supports `.mp3`/`.wav`/`.ogg`/`.flac` and all formats listed in `AudioFileExtensions`.

## ModConfig Settings

| Setting | Default | Range |
|------|------|------|
| Enable Boss BGM | On | On/Off |
| Trigger Distance | 50 m | 10–200 m |
| Volume | 70% | 0–100% |

## Advanced Config (config.json)

File location: `BossBGM/config.json`. Auto-generated on first run.

```json
{
  "fadeDuration": 2.0,
  "updateInterval": 0.1,
  "managerUpdateInterval": 0.5,
  "minSwitchIntervalSeconds": 2.0,
  "minDistanceDeltaToSwitch": 5.0,
  "resumePlaybackEnabled": true,
  "delayedStopEnabled": true,
  "delayedStopSeconds": 1.0,
  "bossDeathFadeOutSeconds": 3.0
}
```

| Parameter | Description |
|------|------|
| `fadeDuration` | Fade in/out duration (seconds) |
| `updateInterval` | Per‑entity update frequency (seconds) |
| `managerUpdateInterval` | Manager update frequency (seconds) |
| `minSwitchIntervalSeconds` | Switch debounce interval (seconds) |
| `minDistanceDeltaToSwitch` | Minimum distance change to trigger a switch (meters) |
| `resumePlaybackEnabled` | Resume playback from pause position |
| `delayedStopEnabled` | Delay stop after leaving range (avoids boundary flicker) |
| `delayedStopSeconds` | Delayed stop duration (seconds) |
| `bossDeathFadeOutSeconds` | Fade-out duration after Boss death (seconds) |

## Priority

Boss BGM has higher priority than Scene BGM and Title/Home BGM. Only one Boss track plays at a time; switching is handled with debounce and fade in/out.
