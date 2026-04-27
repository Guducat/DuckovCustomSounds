---
title: Ambient Intercept (Experimental)
---

# Ambient Intercept (Experimental)

::: warning Experimental Feature
This feature is not yet officially supported. It is disabled by default and has no ModConfig UI entry — it can only be enabled manually via `settings.json`.
:::

## Overview

Mutes all `Amb/amb_*` prefixed game ambient sound events (wind, insects, ambient noise, etc.), making BGM / custom sounds stand out more clearly.

## How to Enable

Edit `settings.json`:

```json
{
  "enableAmbientIntercept": true
}
```

Default is `false`. Restart the game for changes to take effect.

## Behavior

| Rule | Description |
|------|-------------|
| Intercept scope | All `Amb/amb_*` prefixed ambient sound events |
| Exception | `Amb/amb_storm` (storm ambient) is never intercepted |
| Fail-safe | On exception, the original method is allowed through, ensuring game stability |

## Use Cases

- When creating BGM packs: suppress ambient noise so custom music sounds cleaner
- When ambient sounds in certain scenes are too noisy and interfere with custom sound identification

## Caveats

- May make some scenes feel overly quiet or lacking atmosphere
- Only recommended for users with a clear need

## Related Logs

After enabling, search `player.log` for `[AmbientIntercept]` to see interception records.
