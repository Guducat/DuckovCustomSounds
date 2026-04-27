---
title: 地堡留声机 BGM
---

# 地堡留声机 BGM

用自定义音乐替换地堡主页和主菜单的留声机播放列表。支持多首曲目、自动切歌、随机播放。

## 快速开始

1. 创建 `DuckovCustomSounds/HomeBGM/` 目录
2. 放音乐文件进去：
```
HomeBGM/
├─ Never Gonna Give You Up - Rick Astley.mp3
├─ Bohemian Rhapsody - Queen.mp3
└─ My Favorite Song.mp3
```
3. 进入游戏，进入地堡主页，留声机应该播放你的音乐

## 文件命名

- **推荐**：`曲名 - 作者.mp3`。例如 `Never Gonna Give You Up - Rick Astley.mp3`。模块会自动拆分为曲名和作者。
- **简单**：`曲名.mp3`。作者显示为"群星"。
- 用半角连字符 `-` 分隔，不要用中文破折号。

## 支持格式

`.mp3`、`.wav`、`.ogg`、`.oga`、`.flac`、`.aif`、`.aiff`、`.mp2`、`.m4a`、`.mp4`、`.wma`、`.asf`、`.fsb`、`.it`、`.mid`、`.midi`、`.mod`、`.s3m`、`.xm`。

## ModConfig 设置

| 设置 | 默认 | 说明 |
|------|------|------|
| 启用主页 BGM | 开 | 总开关 |
| 启用进入基地音效 | 开 | 进入基地时播放 start.mp3 |
| 音乐音量 (%) | 100 | 0-100%，立即生效 |
| 音乐走音效总线 | 关 | 实验性，走 SFX 而非 Music 总线 |
| 下一首随机播放 | 关 | 顺序播放或随机 |
| 上一首也随机 | 关 | 需要先开随机播放 |
| 避免连续重复 | 开 | 随机时不连续播同一首 |
| 自动播放下一曲 | 开 | 曲目结束后自动切歌 |

## settings.json（ModConfig 不可用时的回退）

```json
{
  "homeBgmAutoPlayNext": true,
  "homeBgmRandomEnabled": false,
  "homeBgmRandomNoRepeat": true,
  "homeBgmRandomizePrevious": false
}
```

## 常见问题

**留声机不播自定义音乐**：确认 `HomeBGM/` 有文件且模块已启用。打开 Debug 日志查看加载信息。

**切歌爆音/卡顿**：用 Audacity 统一采样率（44.1kHz），归一化响度（-3dB 到 0dB），音乐结尾加 0.5-1 秒淡出。

**自动切歌不工作**：确认 ModConfig 中"自动播放下一曲"已开启，等待当前曲目自然结束。

**随机总重复同一首**：确认有多首音乐且"避免连续重复"已开启。

**留声机显示曲名不对**：文件名改为 `曲名 - 艺术家.mp3`，确保用半角 `-`。

**音量不适**：调 ModConfig 的音量滑块，或用 Audacity 调整文件音量。
