---
title: 标题 BGM
---

# 标题 BGM

替换标题/菜单相关的音乐与提示音。文件缺失时自动回退到原版事件，不影响正常流程。

## 目录与文件

- 目录：`TitleBGM/`
- 文件与行为
  - `title.mp3`：标题/菜单循环播放。
  - `start.mp3`：进入地图提示音，单次播放；需在 HomeBGM 设置中开启“启用进入地图提示音（start.mp3）”。
  - `death.mp3`：死亡提示音，单次播放。
  - `extraction.mp3`：撤离成功的缺省回退（当 `Extraction/success.*` 不存在且选择“成功替换”模式时备用）。

建议格式：`.mp3`。

## 设置项

- 进入地图提示音（start.mp3）
  - 位置：ModConfig → HomeBGM → “启用进入地图提示音（start.mp3）”
  - 关闭时将不拦截原生 Enter Stinger。

## 优先级与交互

- 标题 BGM 不参与战斗内优先级；离开菜单后，场景与 Boss BGM 接管。
- 撤离成功时如启用“成功替换”模式，会优先查找 `Extraction/success.*`，找不到才回退 `TitleBGM/extraction.mp3`。

## 常见问题

- start/death 不生效
  - 确保文件存在且为 `.mp3`。
  - 检查是否关闭了“启用进入地图提示音（start.mp3）”。
- 声音太大/太小
  - 标题相关目前不暴露单独音量；可用外部混音器或统一音量工具批量处理文件。

## 原理与实现

- Enter/Death 事件拦截
  - Hook `Music/Stinger/stg_map_base` → 若启用且存在 `TitleBGM/start.mp3`，改为一次性播放自定义文件。
  - Hook `Music/Stinger/stg_death` → 若存在 `TitleBGM/death.mp3`，改为一次性播放自定义文件。
- 撤离回退：成功替换模式中优先使用 `Extraction/success.*`，缺失则使用 `TitleBGM/extraction.mp3`。

