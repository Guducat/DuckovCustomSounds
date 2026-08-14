# ModConfig Options

All settings can be changed through the in-game ModConfig UI. Except for Sound Pack switching (requires restart), other options take effect immediately. Each group is shown in the UI as `DCSxxx | Chinese name` (as in the section headings below).

## Item Sounds (DCSItem)

| Setting | Type | Default | Description |
|--------|------|------|------|
| Enable Custom Item Sounds | Bool | On | Master switch |
| Volume Scale | Float | 1.0 | 0–2 |
| Min Action Audible Duration | Float | 0.35 s | 0–3 |
| Enable Food/Drink | Bool | On | |
| Enable Bandage/Medicine | Bool | On | Includes meds category |
| Enable Syringe | Bool | On | |

## Gun Sounds (DCSGun)

| Setting | Type | Default | Description |
|--------|------|------|------|
| Enable Custom Gun Sounds | Bool | On | |
| Volume Scale | Float | 1.0 | 0–2 |

## Melee Sounds (DCSMelee)

| Setting | Type | Default | Description |
|--------|------|------|------|
| Enable Custom Melee Sounds | Bool | On | |
| Volume Scale | Float | 1.0 | 0–2 |

## Grenade Sounds (DCSGrenade)

| Setting | Type | Default | Description |
|--------|------|------|------|
| Enable Custom Grenade Sounds | Bool | On | |
| Volume Scale | Float | 1.0 | 0–2 |

## Hit & Kill Sounds (DCSHitAndKill)

| Setting | Type | Default | Description |
|--------|------|------|------|
| Enable Hit & Kill Sounds | Bool | On | Master switch |
| Replace Hit/Kill Marker Sounds | Bool | On | Covers hitmarker and killmarker |
| Play Hurt Sounds | Bool | On | Player hurt and NPC hurt |
| Enable Reflection Diagnostics | Bool | Off | Collects Unity serialized audio fields |
| Volume Scale | Float | 1.0 | 0–2 |
| Marker Cooldown Ms | Float | 30 | 0–500 |
| Hurt Cooldown Ms | Float | 120 | 0–1000 |

## Footsteps (DCSFootstep)

| Setting | Type | Default | Description |
|--------|------|------|------|
| Enable Custom Footstep Sounds | Bool | On | |
| Volume Scale | Float | 1.0 | 0–2 |

## Enemy Voices (DCSEnemyVoice)

| Setting | Type | Default | Description |
|--------|------|------|------|
| Allow NPC‑vs‑NPC Combat Voices | Bool | On | |
| Voice Trigger Mode | Enum | Original | Original / Player Only / Mixed |
| Voice Volume Scale | Float | 1.0 | 0–2 |

## Home Music (DCSHomeBGM)

| Setting | Type | Default | Description |
|--------|------|------|------|
| Enable Home Music | Bool | On | |
| Enable Base Entry Sound | Bool | On | Controls start.mp3 |
| Music Volume | Int | 100% | 0–100 |
| Route Music through SFX Bus | Bool | Off | Experimental, routes via SFX instead of Music bus |
| Random Next Track | Bool | Off | |
| Random Previous Too | Bool | Off | Requires Random to be enabled first |
| Avoid Consecutive Repeat | Bool | On | Effective in random mode |
| Auto‑play Next Track | Bool | On | |

## Scene Music (DCSSceneBGM)

| Setting | Type | Default | Description |
|--------|------|------|------|
| Enable Scene Music System | Bool | On | |
| [Enter BGM] Enable Enter Scene BGM | Bool | On | |
| [Enter BGM] Enter Music Volume | Float | 80% | 0–100 |
| [Loop BGM] Enable Loop Scene BGM | Bool | On | |
| [Loop BGM] Loop Music Volume | Float | 60% | 0–100 |
| [Loop BGM] Override Default Scene Music | Bool | On | Reserved setting, currently has no effect |

## Boss Music (DCSBossBGM)

| Setting | Type | Default | Description |
|--------|------|------|------|
| Enable Boss Music | Bool | On | |
| Trigger Distance | Float | 40 m | 10–200 |
| Boss Music Volume | Float | 70% | 0–100 |

## Extraction Music (DCSExtractionBGM)

| Setting | Type | Default | Description |
|--------|------|------|------|
| Extraction Music Mode | Enum | Disabled | Disabled / Countdown Sound / Success Replacement |
| Extraction Sound Volume | Float | 100% | 0–100 |

## Ambient Intercept (DCSAmbientIntercept)

| Setting | Type | Default | Description |
|--------|------|------|------|
| Enable Ambient Intercept (Experimental) | Bool | Off | Intercepts `Amb/amb_*` ambience while allowing `Amb/amb_storm` through |
| Intercept Storm Phase Stingers (Experimental) | Bool | Off | Intercepts `Music/Stinger/stg_storm_1` and `Music/Stinger/stg_storm_2` |

## Sound Logging (DCSLogging)

| Setting | Type | Default | Description |
|--------|------|------|------|
| Enable Log Output | Bool | On | Global master switch; disables all module logging when off |
| Core / SoundPack / Enemy / Footstep / BGM / HomeBGM / SceneBGM / ExtractionBGM / Gun / Grenade / Item / Melee log level | Dropdown | Info | Error/Warning/Info/Debug/Verbose, hot-switchable |

Note: the `HitAndKill` module's log level is **not** in this UI group; it can only be set via `logging.modules.HitAndKill.level` in settings.json.

## Sound Pack (DCSSoundPack)

| Setting | Type | Default | Description |
|--------|------|------|------|
| Sound Pack | Dropdown | Default | Requires restart after selecting |

## Opening ModConfig

In-game: ESC → Mod Settings → DuckovCustomSounds.
