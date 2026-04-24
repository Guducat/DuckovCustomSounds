---
title: 起步
---

# 起步

本页帮你快速完成安装、放置音频文件并验证替换是否生效。

## 安装

- 把 Mod 放入游戏 `Mods/` 目录，确认能正常加载（详见仓库根 `README.md`）。
- 首次启动会自动生成 `DuckovCustomSounds/settings.json` 和各模块的默认配置文件。

## 最小可用目录

准备最少量的音频文件，就能验证替换：

```
DuckovCustomSounds/
├─ TitleBGM/
│  └─ title.mp3                # 标题页音乐
├─ HomeBGM/
│  ├─ 示例曲目A.mp3
│  └─ 示例曲目B.mp3
└─ CustomEnemySounds/
   └─ voice_rules.json         # 首次启动自动生成模板
```

- 建议用 `.mp3`，不同模块支持的格式略有差异（见各模块页）。
- 文件放到根目录就是"Default"资源；后续可用"声音包"在多套资源间切换。

## 声音包切换（可选）

- 在 `DuckovCustomSounds/` 下创建子文件夹（例如 `MyPack/`），放入与根目录一致的模块结构和音频。
- 在子文件夹内创建 `pack.json`。
- 在 ModConfig UI 选择你的声音包，重启后生效。

## 验证

1. 进标题页，确认 `TitleBGM/title.mp3` 是否播放。
2. 进主页，确认 `HomeBGM` 曲库能切换。
3. 进对局，测试敌人语音或其他模块是否触发。如不生效，开启对应模块的 Debug 或查看日志。

## 日志与排错

- 遇到路径不匹配、文件未找到等问题，打开相关模块的 Debug 级别查看解析过程。
- 也可以在 `DuckovCustomSounds/` 下放 `debug_off` 或 `.nolog` 文件快速关闭日志。
- 详见"高级 > 日志与排错"。

## 下一步

- 需要整套资源快速切换，看"模块 > 声音包系统"。
- 定制 BGM（标题/主页/撤离/场景/Boss），看"模块 > BGM"各页。
- 定制敌人语音、脚步声、枪械、近战、手雷、物品，跳转对应模块页。
