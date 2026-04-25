# Changelog

::: info NOTICE
Unless otherwise specified, all dates and times are in UTC+8 (China Standard Time).
:::

## 2025-11-02 v1.0

- Fix: Abnormal gunshot volume.
- Fix: Syringe and medication usage logic.
- Fix: Sound effects stopping abnormally for certain item usage scenarios.
- New: Customizable "Loading / Load Complete" BGM.
- New: Experimental "Scene BGM".
- New: Differential sound effects for guns and melee weapons (supports `xxx_1.mp3`, `xxx_2.mp3`… for the same event).

Tip: The new additions above default to the "Music" volume slider; they auto-coordinate priority when Boss BGM is also active.

## 2026-04-25 v2.1.0

- New: Fixed `TitleBGM/startFX.*` not playing.
- New: *Music* files now support 18 formats including `.flac`, `.ogg`, `.wav`, etc., no longer limited to `.mp3`.
- New: Multiple modules now have independent volume sliders (0–200%), tied to the "SFX" bus, adjustable in ModConfig.
- New: Runtime hot-switch of log level — change log level in ModConfig or `settings.json`, takes effect immediately without restart.
- New: Footstep and dash sounds can now play simultaneously without interrupting each other.

- Fix: Extraction success sound now works for all maps in theory — short extraction music interception and replacement now supported.
- Fix: Loading screen sound effects not triggering.
- Fix: Sound effects continuing to play incorrectly after quick-canceling item usage.
- Fix: Footsteps triggering would unexpectedly cut off dash sounds, and complete silence during dash cooldown.

- Improvement: Scene music now supports Chinese filenames, e.g. `零号区.mp3`, see logs.
- Improvement: ModConfig settings UI fully localized to Chinese — module groups use Chinese names, dropdown options also localized.
- Improvement: Legacy `settings.json` config keys remain usable in new versions; upgrading won't lose settings.
- Improvement: Log output now includes a module name prefix so you can tell which sub‑module each log entry comes from, making troubleshooting easier.
- Improvement: Boss BGM and extraction sounds now have independent volume sliders (0–100%).
- Improvement: All module documentation rewritten — more concise, more consistent, easier to read.
- Improvement: GitHub Actions auto-deploy VitePress documentation site; v2.* tag pushes auto-publish DLL.

- Removed: Unused "Kill Feedback" module code and documentation entry.

## 2026-04-26 v2.1.1
- Improvement: Improved scene entry music matching logic, reducing the chance of missing entry music.
