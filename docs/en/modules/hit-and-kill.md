---
title: Hit & Kill Sounds
---

# Hit & Kill Sounds

Replaces hit marker and kill marker sounds, and adds player hurt and NPC hurt sounds.

## Quick Start

Place default files:

```text
CustomHitAndKillSounds/
├── hitmarker.mp3
├── hitmarker_head.mp3
├── killmarker.mp3
├── killmarker_head.mp3
├── player_hurt.mp3
└── npc_hurt.mp3
```

## File Names

| File | Trigger |
|------|---------|
| `hitmarker.*` | Player hits a target |
| `hitmarker_head.*` | Player crits a target |
| `killmarker.*` | Player kills a target |
| `killmarker_head.*` | Player crit-kills a target |
| `player_hurt.*` | Player gets hurt |
| `player_hurt_crit.*` | Player receives a crit |
| `npc_hurt.*` | Player hurts an NPC |
| `npc_hurt_crit.*` | Player crits an NPC |

Supported formats: `.mp3`, `.wav`, `.ogg`, `.oga`.

## Variants

Add `_1`, `_2`, and later numeric suffixes for random variants:

```text
CustomHitAndKillSounds/
├── hitmarker.mp3
├── hitmarker_1.mp3
├── hitmarker_2.mp3
└── player_hurt_1.mp3
```

## Coverage

Hit and kill marker replacement starts from known native events:

```text
SFX/Combat/Marker/hitmarker
SFX/Combat/Marker/hitmarker_head
SFX/Combat/Marker/killmarker
SFX/Combat/Marker/killmarker_head
```

Hurt sounds are added through runtime events. The module subscribes to `Health.OnHurt`, `Health.OnDead`, `HealthSimpleBase.OnSimpleHealthHit`, and `HealthSimpleBase.OnSimpleHealthDead`, then classifies `DamageInfo`.

Some audio events live in Unity serialized resources. Decompiled C# shows the field names, but not every runtime value. Reflection diagnostics can log common fields such as `critSfx`, `nonCritSfx`, `hurtSfx`, and `soundKey` so future patches can cover more cases.

## ModConfig

| Setting | Default |
|--------|---------|
| Enable Hit & Kill Sounds | On |
| Replace Hit/Kill Marker Sounds | On |
| Play Hurt Sounds | On |
| Enable Reflection Diagnostics | Off |
| Volume Scale | 1.0 (0–2) |
| Marker Cooldown Ms | 30 |
| Hurt Cooldown Ms | 120 |

## Logging

Set in `settings.json`:

```json
{
  "logging": {
    "modules": {
      "HitAndKill": { "level": "Debug" }
    }
  }
}
```

For unknown hurt events, enable both `enableAudioPostLogger` and `reflectionDiagnostics`.
