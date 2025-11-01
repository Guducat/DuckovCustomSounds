---
title: 声音包系统
---

# 声音包系统

本页面向“普通玩家”和“资源包作者”，介绍如何使用与制作声音包（Sound Pack），以及系统的工作原理与注意事项。你可以通过“声音包”在不同成套音频资源之间一键切换。

提示：切换声音包后需要重启游戏生效。

> [!NOTE]
> 目前 ModConfig UI 支持尚未完善，你看到的属于前瞻性内容。

## 如何安装与切换

- 安装位置
  - 游戏根目录下的 `DuckovCustomSounds/`。
  - “默认资源”（不启用任何包）放在根目录直下的模块目录中，如 `TitleBGM/`、`HomeBGM/`、`SceneBGM/`、`BossBGM/`、`Extraction/`、`CustomEnemySounds/`、`CustomFootStepSounds/` 等。
  - 声音包安装为子文件夹：`DuckovCustomSounds/<包ID>/`，该文件夹内必须包含一个 `pack.json` 元数据文件，以及你要替换的模块目录。

- 切换方法
  - 优先：在游戏内 ModConfig → “声音包”下拉列表选择需要的包名称，点击应用后重启游戏。
  - 备选：编辑 `DuckovCustomSounds/settings.json`，将 `currentSoundPack` 设置为目标包的“包ID”（即包文件夹名）。保存后重启游戏。

- 恢复默认
  - 在 ModConfig 中选择“Default”，或将 `settings.json.currentSoundPack` 置为空字符串 `""`，然后重启。

- 常见问题
  - 看不到任何包：请确认 `DuckovCustomSounds/` 下是否存在包含 `pack.json` 的包文件夹；如果没有声音包，也可以直接在根目录的默认模块目录中放入音频文件，系统会把它识别为“Default”。
  - 切换不生效：切换写入的是 `settings.json`，需“重启游戏”后才真正应用。
  - 只改了一部分模块：允许。未提供的部分会按“当前包 → Default → 原版”的顺序回退。
  - 如何确认当前选择：打开 `DuckovCustomSounds/settings.json` 查看 `currentSoundPack`；或在 ModConfig 的说明文本处查看当前包信息。

## 进阶：如何制作一个声音包

- 目录结构（示例）
  - 包应放在 `DuckovCustomSounds/<包ID>/` 下。包ID推荐使用英文、数字、下划线，避免空格与特殊字符。

  ```
  DuckovCustomSounds/
  ├─ settings.json                 # 全局设置（不要打包带走）
  ├─ TitleBGM/                     # Default（非包）示例：根目录直放即为“默认”
  ├─ HomeBGM/
  ├─ SceneBGM/
  ├─ BossBGM/
  │   └─ ...
  ├─ MyPack/                       # 声音包示例（包ID = MyPack）
  │  ├─ pack.json                  # 元数据（必须）
  │  ├─ HomeBGM/
  │  ├─ Extraction/
  │  └─ CustomEnemySounds/
  └─ AnotherPack/
     ├─ pack.json
     └─ CustomFootStepSounds/
  ```

- 必备文件：`pack.json`
  - 位置：包文件夹内（`DuckovCustomSounds/<包ID>/pack.json`）
  - 作用：在扫描时识别为“一个包”，并提供显示信息。未通过校验（缺少必填项）将不会出现在列表中。
  - 字段规范（最小必填：name、author、version）

  ```json
  {
    "name": "My Custom Sounds",
    "author": "YourName",
    "version": "1.0.0",
    "description": "对若干模块的替换示例",
    "compatibleModVersion": "2.0.0",
    "requiredModules": [
      "CustomBGM",
      "CustomEnemySounds",
      "CustomFootStepSounds"
    ],
    "optional": {
      "homepage": "https://example.com",
      "qq": "123456"
    }
  }
  ```

  - 注意
    - 包ID并不写在 `pack.json`，而是“包文件夹名”本身。显示文本为 `name vversion by author - description`（如果有描述）。
    - `compatibleModVersion` 与 `requiredModules` 仅作信息展示与约定，当前版本不会强制校验或阻断加载。
    - 不要把 `settings.json` 随包一并分发，它是用户本地全局配置。

### 模块与目录对照（概览）

- BGM（背景音乐）
  - `TitleBGM/`：`title.mp3`（标题/菜单循环）、`start.mp3`（进入地图提示，单次）、`death.mp3`（死亡提示，单次）、`extraction.mp3`（作为 Extraction 缺省回退）
  - `HomeBGM/`：放多首 mp3，即可在大厅进行播放与切换（随机/不重复/上一首等由设置控制）
  - `SceneBGM/`：场景/关卡 BGM
  - `BossBGM/`：以敌人 NameKey（去掉 `Cname_` 前缀）命名的文件，找不到则用 `default_boss.*`
- Extraction（撤离提示）
  - `Extraction/`：`countdown.mp3|wav`（倒计时模式，循环）、`success.mp3|wav`（撤离成功 stinger）、可选 `extraction.mp3|wav`（作为缺省）
- 敌人语音、脚步与其他 SFX
  - `CustomEnemySounds/`、`CustomFootStepSounds/`、`CustomGunSounds/`、`CustomMeleeSounds/`、`CustomGrenadeSounds/`、`CustomItemSounds/`、`CustomKillFeedback/`
  - 各模块的键名/文件组织、格式细节，请参考对应模块页面与各模块 README（本页仅做总览）。

音频格式支持（概览，以模块 README 为准）
- `HomeBGM/`、`TitleBGM/`：推荐 mp3；
- 其他常见目录：通常支持 mp3/wav/ogg/flac；
- 若模块另有特殊限制或优先级，请以相应模块文档为准。

### 回退与优先级（通用）
- 资源解析按以下顺序回退：当前“声音包” → Default（根目录直放） → 原版游戏音频。
- 未覆盖的模块或文件，系统会自动回退；因此你可以只提供自己想要替换的那一部分，不必“做满整套”。

### 发布建议
- 只打包你的“包文件夹”（例如 `MyPack/`），不要包含 `settings.json`；
- 在 README/发布页介绍清楚支持的模块、建议格式、推荐音量；
- 如涉及他人作品，请明确授权信息。

## 原理与实现（给想深入的你）
- 启动时扫描与识别
  - 系统仅扫描 `DuckovCustomSounds/` 的一级子目录，存在 `pack.json` 则识别为一个包；根目录若在典型目录中（`HomeBGM/`、`BossBGM/`、`SceneBGM/`、`TitleBGM/`）发现音频文件，则也会提供一个名为“Default”的选项。
- 选择与保存
  - 游戏内 ModConfig 的“声音包”下拉列表来源于扫描结果；当你更改选择时，仅写入 `DuckovCustomSounds/settings.json` 的 `currentSoundPack`，需要重启后生效。
- 元数据
  - `pack.json` 反序列化到 `SoundPackInfo`，必须字段为 `name/author/version`，显示文本由 `GetDisplayText()` 生成。
- 设置文件读取
  - `settings.json` 也会在全局设置初始化时被读取，`currentSoundPack` 会被保留为字符串（空字符串代表 Default）。

## 附录

- `settings.json` 片段
  ```json
  {
    "currentSoundPack": "MyPack"
  }
  ```

- `pack.json` 模板
  ```json
  {
    "name": "Your Sound Pack Name",
    "author": "Your Name",
    "version": "1.0.0",
    "description": "对哪些模块做了替换的说明",
    "compatibleModVersion": "2.0.0",
    "requiredModules": ["CustomBGM", "CustomFootStepSounds", "CustomEnemySounds"],
    "optional": {
      "homepage": "https://your-website.com",
      "qq": "123456"
    }
  }
  ```

如需进一步定制与调试，请继续阅读各模块的专页（BGM、敌人语音、脚步、武器/近战/手雷/物品/击杀反馈等）。

