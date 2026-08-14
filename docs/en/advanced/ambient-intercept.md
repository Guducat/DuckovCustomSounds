---
title: Ambient Intercept (Experimental)
---

# Ambient Intercept (Experimental)

::: warning Experimental Feature
This feature is not yet officially supported and is disabled by default. It can be enabled in ModConfig; when ModConfig is unavailable, it can still be enabled manually through `settings.json`.
:::

## Overview

Mutes all `Amb/amb_*` prefixed game ambient sound events (wind, insects, ambient noise, etc.), making BGM / custom sounds stand out more clearly.

## How to Enable

Recommended in-game ModConfig entry:

```text
DCSAmbientIntercept | 环境音拦截 -> 启用环境音拦截（实验性） (Enable Ambient Intercept, Experimental)
DCSAmbientIntercept | 环境音拦截 -> 拦截风暴阶段提示音（实验性） (Intercept Storm Phase Stingers, Experimental)
```

The actual UI labels are in Chinese (shown above); the English text in parentheses is the meaning for reference.

Changes made through ModConfig take effect immediately.

If ModConfig is unavailable, edit `settings.json`:

```json
{
  "enableAmbientIntercept": true,
  "interceptStormStingers": true
}
```

Both `settings.json` fields default to `false`. Restart the game after manual edits.

## Source Check

This behavior has been checked against the game source:

| Game source | Behavior |
|-------------|----------|
| `AudioManager.OnSubSceneLoaded` | Reads `SubSceneEntry.AmbientSound`, then plays scene ambience through `ambientSource.Post("Amb/amb_{soundkey}")` |
| `WeatherFxControl` | Uses `Amb/amb_rain` as the default rain sound and plays it through `AudioManager.Post` |
| `TimeOfDayController` | Storm phase cues are `Music/Stinger/stg_storm_1` and `Music/Stinger/stg_storm_2`, so they are outside the `Amb/amb_*` scope and are controlled by a separate option |

The mod checks the event name in a Harmony Prefix on `AudioObject.Post(string, bool)`, so it covers ambient events that reach `AudioObject.Post` through `AudioManager`.

## Behavior

| Rule | Description |
|------|-------------|
| Intercept scope | `Amb/amb_*` prefixed ambient events received by `AudioObject.Post` |
| Exception | `Amb/amb_storm` is still allowed through |
| Storm phase stingers | When `Intercept Storm Phase Stingers (Experimental)` is enabled, `Music/Stinger/stg_storm_1` and `Music/Stinger/stg_storm_2` are also intercepted |
| Fail-safe | On exception, the original method is allowed through, ensuring game stability |

## Use Cases

- When creating BGM packs: suppress ambient noise so custom music sounds cleaner
- When ambient sounds in certain scenes are too noisy and interfere with custom sound identification

## Caveats

- May make some scenes feel overly quiet or lacking atmosphere
- Only recommended for users with a clear need

## Related Logs

After enabling, search `player.log` for `[AmbientIntercept]` to see interception records.
