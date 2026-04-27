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

## Appendix: BOSS NameKey → Filename Reference

Filename = NameKey minus the `Cname_` prefix. Supports `.mp3`/`.wav`/`.ogg`/`.flac`.

| NameKey | Chinese Name | Filename |
|---------|-------------|----------|
| `Cname_BALeader` | BA队长 (BA Leader) | `BALeader.{ext}` |
| `Cname_Boss_Sniper` | 劳登 (Lauden) | `Boss_Sniper.{ext}` |
| `Cname_Boss_Shot` | 喷子 (Shotgunner) | `Boss_Shot.{ext}` |
| `Cname_ServerGuardian` | 矿长 (Mine Chief) | `ServerGuardian.{ext}` |
| `Cname_Speedy` | 急速团长 (Speedy Captain) | `Speedy.{ext}` |
| `Cname_Boss_Fly` | 蝇蝇队长 (Fly Captain) | `Boss_Fly.{ext}` |
| `Cname_Boss_Arcade` | 暴走街机 (Rampant Arcade) | `Boss_Arcade.{ext}` |
| `Cname_Boss_3Shot` | 三枪哥 (Three-Shot) | `Boss_3Shot.{ext}` |
| `Cname_Prison_Boss` | 典狱长 (Warden) | `Prison_Boss.{ext}` |
| `Cname_StormBoss1` | 噗咙噗咙 (Pulongpulong) | `StormBoss1.{ext}` |
| `Cname_StormBoss2` | 咕噜咕噜 (Gulugulu) | `StormBoss2.{ext}` |
| `Cname_StormBoss3` | 啪啦啪啦 (Palapala) | `StormBoss3.{ext}` |
| `Cname_StormBoss4` | 比利比利 (Bilibili) | `StormBoss4.{ext}` |
| `Cname_StormBoss5` | 口口口口 (Koukoukoukou) | `StormBoss5.{ext}` |
| `Cname_ShortEagle` | 矮鸭 (Short Eagle) | `ShortEagle.{ext}` |
| `Cname_UltraMan` | 光之男 (Ultraman) | `UltraMan.{ext}` |
| `Cname_CrazyRob` | 失控机械蜘蛛 (Crazy Robot Spider) | `CrazyRob.{ext}` |
| `Cname_Vida` | 维达 (Vida) | `Vida.{ext}` |

BOSSes not listed here or when no specific file is found fall back to `default_boss.{ext}`.
