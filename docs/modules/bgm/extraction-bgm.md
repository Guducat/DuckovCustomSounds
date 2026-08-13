---
title: 撤离 BGM
---

# 撤离 BGM

自定义撤离区域的音效，支持倒计时提示音和成功音效替换。

成功替换由游戏的撤离确认事件触发，并按来源关卡过滤。仓库区等使用未知 `stg_map_*` 后缀的地图也能覆盖；基地和撤离转场之外的地图提示继续使用游戏原声。

## 三种模式

通过 ModConfig → ExtractionBGM 选择。

### 关闭（Disabled）
不做任何处理，使用原版音效。

### 倒计时模式（CountdownMode）
撤离倒计时剩余 ≤5 秒时，播放自定义音效（单次播放，走 SFX 总线），同时场景 BGM 快速降音量（不停止、避免双重 BGM）；中止撤离后场景 BGM 平滑恢复。撤离成功时屏蔽原版 Stinger，让倒计时音效自然放完。

文件查找顺序：`Extraction/countdown.*` → `Extraction/extraction.*`。

建议音效长度 10-15 秒。

### 成功替换模式（SuccessStingerMode）
确认撤离成功后播放自定义音乐，并在短暂的撤离转场期间抑制原版地图 Stinger。保留原有倒计时逻辑。

文件查找顺序：`Extraction/success.*` → `TitleBGM/extraction.*`。

## 目录结构

倒计时模式：
```
Extraction/
└─ countdown.mp3
```

成功替换模式：
```
Extraction/
└─ success.mp3
```

旧版兼容（仍支持）：
```
Extraction/
└─ extraction.mp3

TitleBGM/
└─ extraction.mp3   # 成功回退
```

## ModConfig 设置

| 设置 | 选项 |
|------|------|
| 撤离音乐模式 | 禁用 / 倒计时音效模式 / 成功音效替换模式 |
| 撤离音效音量 | 0-100% |

## settings.json（旧版兼容）

```json
{
  "overrideExtractionBGM": true   // true=倒计时模式, false=关闭
}
```

推荐用 ModConfig 设置而非直接编辑 settings.json。

## 常见问题

**倒计时音效没触发**：确认选的是"倒计时模式"，文件存在（`countdown.*` 或 `extraction.*`），倒计时 > 5 秒。

**成功音效没替换**：确认选的是“成功替换模式”，文件存在（`success.*` 或 `TitleBGM/extraction.*`），日志中包含 `撤离音乐替换已触发`。

**倒计时模式听到两次音效**：倒计时音效太短，还没结束原版成功 Stinger 就播了。用 10-15 秒长的音效。

**不能同时用倒计时和成功替换**：两种模式互斥。需要同时效果的话，用倒计时模式 + 长音效（覆盖倒计时到成功全过程）。

**音量不适**：撤离音效走 SFX 总线，受游戏 SFX 音量影响。也可在 ModConfig 单独调撤离音效音量。
