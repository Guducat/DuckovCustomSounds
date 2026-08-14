---
title: Item Sounds
---

# Item Sounds

Replace consumable usage sounds: food, drinks, medicine, syringes, bandages, etc. Match specific items by TypeID (numeric) and categories by soundKey.

## Quick Start

```
CustomItemSounds/
└── default.mp3
```

Create per‑item sounds:
```
CustomItemSounds/
├── 84.mp3              # TypeID 84
├── 20.mp3              # TypeID 20
└── default.mp3
```

Organize by category directory (recommended):
```
CustomItemSounds/
├── food/
│   ├── 84.mp3
│   └── default.mp3
├── bandage/
│   ├── 20.mp3
│   └── default.mp3
└── syringe/
    └── default.mp3
```

## TypeID and soundKey

- **TypeID**: Unique numeric ID for each item. Obtain it by setting `logging.modules.Item.level` to `Debug`, using the item, then searching `player.log` for `[ItemUse]` to see `TypeID=xxx`.
- **soundKey**: Item category. `food` (food/drink), `bandage` (bandages/medicine, primary category), `syringe` (injectors). Game event: `SFX/Item/use_{soundKey}`. `meds` is an alias for `bandage`. **Note: Drinks do not have a separate `drink` category; they belong under `food`.**

## File Lookup Priority

1. Phase files (only while a use is in progress): `CustomItemSounds/{category}/{TypeID}_action|_start` / `_finish|_end` (falling back to `{soundKey}_...`, then `default_...` in the same folder)
2. `CustomItemSounds/{category}/{TypeID}.*`
3. `CustomItemSounds/{category}/{fileBase}.*` (`{soundKey}` when no fileBase is configured)
4. `CustomItemSounds/{category}/default.*`
5. Root phase files: `CustomItemSounds/{TypeID}_action|_start` / `_finish|_end`
6. `CustomItemSounds/{TypeID}.*`
7. `CustomItemSounds/{fileBase}.*` (`{soundKey}` when no fileBase is configured)
8. `CustomItemSounds/default.*`

## Variants

Same as guns/melee: `_1`, `_2` suffixes are selected randomly (e.g. `bandage/20_1.mp3`); variants are recognized even when the base file `20.mp3` is absent.

## Segmented Sounds

Split into action and finish segments. Canceling usage only stops the action segment; the finish segment is preserved:

```
bandage/
├── 20_action.mp3   or 20_start.mp3
└── 20_finish.mp3   or 20_end.mp3
```

## ModConfig

| Setting | Default |
|------|------|
| Enable Custom Item Sounds | On |
| Volume Scale | 1.0 (0–2) |
| Enable Food/Drink | On |
| Enable Bandage/Medicine | On |
| Enable Syringe | On |
| Min Action Audible Duration | 0.35 s (0–3) |

## item_sound_map.json

File location: `CustomItemSounds/item_sound_map.json`. Auto-generated on first run. Used for fine‑grained control of TypeID→category mapping, and proactively injecting sounds for items without native audio events.

> The following is an example config; the file generated on first run contains no `items` entries (only `defaultWhenNoEvent: null` and `aliases: {"meds": "bandage"}`).

```json
{
  "defaultWhenNoEvent": null,
  "aliases": { "meds": "bandage" },
  "items": {
    "403": { "soundKey": "bandage", "forceWhenNoEvent": true },
    "23":  { "soundKey": "bandage", "fileBase": "64" },
    "25":  { "soundKey": "bandage", "fileBase": "64" },
    "156": { "soundKey": "syringe", "actionFileBase": "needle", "finishFileBase": "needle_done" }
  }
}
```

| Field | Description |
|------|------|
| `defaultWhenNoEvent` | Fallback category when no native event exists (can be null) |
| `aliases` | Category aliases; built‑in `meds` → `bandage` |
| `items.{TypeID}.soundKey` | Specifies the category for this item |
| `items.{TypeID}.actionKey / finishKey` | Different categories for the two phases (optional) |
| `items.{TypeID}.fileBase / actionFileBase / finishFileBase` | Shared audio base name; multiple TypeIDs can share the same file (optional) |
| `items.{TypeID}.forceWhenNoEvent` | Whether to proactively inject sound when no native event exists (default true) |

Example: TypeIDs 23 and 25 share `64.mp3` (placed in the `bandage/` directory).

Changes take effect after restarting the game.

## Formats

`.mp3` → `.wav` → `.ogg` → `.oga`.
