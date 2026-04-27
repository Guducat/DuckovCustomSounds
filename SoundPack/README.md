# 声音包系统（Sound Pack System）

给资源包作者和普通玩家看的说明。各模块（BGM、敌人语音等）相关口径与此保持一致，重点讲怎么制作和切换声音包，不扯实现细节。

---

## 概述

- 自 DuckovCustomSounds v2.0.0 起，本 Mod 支持"声音包"功能。不改各模块配置，一键在多套音频资源之间切换。
- 声音包 = 一个文件夹 + 一份 `pack.json` 元数据；包内目录结构与各模块的默认目录一致（例如 `HomeBGM/`、`BossBGM/`、`CustomEnemySounds/` 等）。
- 切换入口在 ModConfig UI；也可直接改 `settings.json`。切换后需重启游戏生效。

---

## 适用范围与优先级

- 涉及模块（举例）：
  - 自定义BGM：`TitleBGM/`、`HomeBGM/`、`SceneBGM/`、`Extraction/`、`BossBGM/`
  - 自定义敌人语音：`CustomEnemySounds/`
  - 自定义脚步声：`CustomFootStepSounds/`
  - 其他：`CustomGunSounds/`、`CustomMeleeSounds/`、`CustomGrenadeSounds/`、`CustomHitAndKillSounds/`、`CustomItemSounds/` 等
- 读取顺序与回退：当前声音包 > Default（根目录资源）> 原版游戏音频。
- 没检测到任何可用声音包时（包括没有可判定为 Default 的根目录资源），ModConfig UI 里不显示"声音包选择"。
- "Default"是否出现在列表，由系统在根目录探测到典型资源目录且存在音频文件时判定（见"识别逻辑"）。

---

## 目录结构

```
DuckovCustomSounds/
├─ settings.json                  # 全局设置（含 currentSoundPack）
├─ TitleBGM/                      # Default 资源（根目录即 Default）
├─ HomeBGM/
├─ SceneBGM/
├─ BossBGM/
│  └─ ...
├─ BattlefieldSounds/             # 声音包示例 1（包 ID = 文件夹名）
│  ├─ pack.json                   # 元数据（必需）
│  ├─ HomeBGM/
│  ├─ BossBGM/
│  └─ ...
└─ TarkovSounds/                  # 声音包示例 2
   ├─ pack.json
   ├─ CustomEnemySounds/
   └─ CustomFootStepSounds/
```

说明：
- "包 ID"即包文件夹名称（如 `BattlefieldSounds`），存到 `settings.json` 的 `currentSoundPack` 字段。
- 包内子目录名称与各模块的默认目录一致；只需放入你要替换的部分，没提供的模块按回退规则处理。

---

## 作者向：制作你的声音包

1) 在 `DuckovCustomSounds/` 下新建包文件夹，名字建议只用英文、数字与下划线，例如：`MyPack/`（也就是包 ID）。  
2) 按模块放音频文件（只放需要替换的部分）：
   - BGM：`TitleBGM/`、`HomeBGM/`、`SceneBGM/`、`Extraction/`、`BossBGM/`
   - 敌人语音：`CustomEnemySounds/`
   - 脚步声：`CustomFootStepSounds/`
   - 其他模块参见对应 README
3) 在包文件夹内创建 `pack.json`（字段规范见下文，可参考 `SoundPack/pack.json.template`）。  
4) 启动游戏，在 ModConfig UI 中选择你的声音包；重启游戏后生效。  
5) 分发建议：只打包"你的包文件夹"，不要包含 `settings.json` 等用户本地配置文件。

---

## pack.json 规范

示例：
```json
{
  "name": "My Custom Sounds",
  "author": "YourName",
  "version": "1.0.0",
  "description": "一套示例音频资源",
  "compatibleModVersion": "2.0.0",
  "requiredModules": [
    "CustomBGM",
    "CustomEnemySounds",
    "CustomFootStepSounds",
    "CustomHitAndKillSounds"
  ],
  "optional": {
    "homepage": "https://example.com",
    "qq": "123456 / QQ群链接"
  }
}
```

字段说明：
- 必填
  - `name`：显示名称（UI 列表显示）。
  - `author`：作者名。
  - `version`：版本号，建议遵循语义化（如 `1.0.0`）。
- 可选
  - `description`：简要说明（UI 会显示在名称后）。
  - `compatibleModVersion`：目标 Mod 兼容版本（仅用于说明）。
  - `requiredModules`：此包涉及的模块列表（仅用于说明，不做强校验）。
  - `optional.homepage`、`optional.qq`：额外信息（页面/交流群等）。

注意：
- 包 ID = 包文件夹名，用于 `settings.json.currentSoundPack`。显示文本由 `name + version + author (+ description)` 拼成。
- 如果 `name`/`author`/`version` 缺失，系统认为该包的元信息无效，直接忽略。

---

## 用户向：如何切换声音包

方式一（推荐）：通过 ModConfig UI
- 游戏中按 ESC -> 设置 -> Mod 设置 -> 选择 `DuckovCustomSounds`
- 找到"声音包选择"下拉框，选择目标声音包
- 按提示重启游戏生效（当前版本不支持热切换）

方式二：编辑 `settings.json`
```json
{
  "currentSoundPack": "BattlefieldSounds"
}
```
- `""`（空）或缺省表示使用 Default（根目录资源）。
- 改完重启游戏。

看不到"声音包选择"的情况：系统没有检测到任何可用声音包（也没有可判定为 Default 的根目录资源）时，下拉框不会显示。

---

## 识别逻辑与回退

- 包扫描：系统在 `DuckovCustomSounds/` 的第一层子目录中查找含有 `pack.json` 的文件夹作为声音包。  
- Default 判定：当根目录存在典型资源目录（如 `HomeBGM/`、`BossBGM/`、`SceneBGM/`、`TitleBGM/`）且其中包含音频文件时，列表中会加一项"Default"。
- 回退与优先级：当前包没提供的文件与模块，按"当前包 > Default > 原版"顺序回退。
- 路径解析：所有模块自动基于当前包的根路径解析资源，不用为包内各模块单独改路径。

---

## 音频格式与命名（摘要）

- 一般推荐 `.mp3`；不同模块的格式支持略有差异：
  - `TitleBGM/`、`HomeBGM/`、`SceneBGM/`、`BossBGM/`、`Extraction/`：统一使用 BGM 音乐扩展名顺序，推荐 mp3/wav/ogg/flac
  - `CustomEnemySounds/`、`CustomFootStepSounds/` 等：详见对应模块 README 中的命名与规则
- 资源命名与匹配、事件键值、优先级等细节，参考：
  - `CustomBGM/readme.md`
  - `CustomBGM/BossBGM/README.md`
  - `CustomBGM/SceneBGM/README.md`
  - `CustomEnemySounds/README.md`
  - `CustomFootStepSounds/README.md`
  - 其他模块对应的 README

---

## 调试与日志

- 日志前缀：`[SoundPack]`（核心日志：扫描包、当前包、写入设置等）。
- 关键日志示例：
  - "开始初始化声音包系统…"
  - "检测到声音包: …" / "检测到 Default 资源目录"
  - "从 settings.json 读取当前声音包: …"
  - "用户选择了新的声音包: …，需重启游戏生效"
- 本地配置与日志位置：`DuckovCustomSounds/settings.json`；游戏日志看 `player.log`。

---

## 常见问题（FAQ）

- 看不到"声音包选择"
  - 没检测到可用包；确认子目录内有 `pack.json`。
  - 想显示"Default"的话，确保根目录典型目录中至少有一种 `mp3/wav/ogg` 音频文件。
- 切换不生效
  - 需要重启游戏；或检查 `settings.json.currentSoundPack` 是不是你期望的值。
- 只替换了部分模块
  - 正常。没提供的模块/文件会按回退规则用 Default 或原版。
- 包没出现在列表
  - `pack.json` JSON 格式错误或缺少必填项；包文件夹名即包 ID，避免与其他包重名。
- 怎么恢复默认资源
  - 在 UI 选"Default"（如果有的话）；或把 `currentSoundPack` 清空再重启。
- 支不支持嵌套包/多层目录
  - 不支持，只扫 `DuckovCustomSounds/` 第一层子目录。

---

## 示例清单（可直接对照）

```
DuckovCustomSounds/
├─ TitleBGM/
│  ├─ title.mp3
│  └─ start.flac
├─ HomeBGM/
│  ├─ A.mp3
│  └─ B.ogg
├─ BossBGM/
│  └─ default_boss.mp3
├─ CustomEnemySounds/
│  └─ voice_rules.json
├─ BattlefieldSounds/
│  ├─ pack.json
│  └─ HomeBGM/…
└─ TarkovSounds/
   ├─ pack.json
   ├─ CustomEnemySounds/…
   └─ CustomFootStepSounds/…
```

---

## 版本信息

- 模块版本：1.0.0（声音包系统）
- 兼容 Mod 版本：2.0.0
- 文档更新：2025-10-31

实现细节见 `SOUNDPACK_IMPLEMENTATION.md`；日常制作与使用以上文为准。
