---
title: 声音包系统
---

# 声音包系统

声音包让你在不同成套音频资源之间一键切换。切换后需重启游戏生效。

## 装别人的包

1. 把包文件夹放到 `DuckovCustomSounds/` 下。
2. 启动游戏，ModConfig → 选你的声音包。
3. 重启游戏。

包没出现在列表？检查文件夹里有没有 `pack.json`，文件格式对不对。

## 切换与恢复

- **ModConfig（推荐）**：游戏内 ModConfig → 声音包下拉列表，选择后重启。
- **settings.json（备选）**：编辑 `currentSoundPack` 为空串 `""` 恢复默认，或设为包文件夹名。
- **恢复 Default**：在 ModConfig 选 "Default"，或清空 `currentSoundPack`，重启。

模块文件只在当前声音包目录里查找（选 Default 时是根目录）；包内没有的模块不会回退到根目录，而是直接使用原版音效。

## 自己做包

1. 在 `DuckovCustomSounds/` 下创建包文件夹，名字用英文、数字、下划线（这就是包 ID）。例如 `MyPack/`。
2. 按模块目录结构放入你要替换的音频（只放你需要的部分）。
3. 在包文件夹里创建 `pack.json`。
4. 重启游戏，ModConfig 选中，再重启。

**目录示例**：
```
DuckovCustomSounds/
├── TitleBGM/                     # Default（根目录资源）
├── HomeBGM/
├── BossBGM/
├── MyPack/                       # 你的包（包 ID = MyPack）
│   ├── pack.json
│   ├── HomeBGM/
│   └── CustomEnemySounds/
└── AnotherPack/
    ├── pack.json
    └── CustomFootStepSounds/
```

## pack.json

**最小配置**：
```json
{
  "name": "你的包名",
  "author": "你的名字",
  "version": "1.0.0"
}
```

**完整配置**：
```json
{
  "name": "My Custom Sounds",
  "author": "YourName",
  "version": "1.0.0",
  "description": "替换了BGM和敌人语音",
  "compatibleModVersion": "2.0.0",
  "requiredModules": ["CustomBGM", "CustomEnemySounds"],
  "optional": {
    "homepage": "https://example.com",
    "qq": "123456"
  }
}
```

### 字段说明

| 字段 | 必填 | 说明 |
|------|------|------|
| `name` | 是 | 在 UI 列表里显示的名字 |
| `author` | 是 | 作者名 |
| `version` | 是 | 版本号，建议语义化（1.0.0） |
| `description` | 否 | 简介（当前仅写入 pack.json，UI 不显示） |
| `compatibleModVersion` | 否 | 目标 Mod 兼容版本，只是说明，不影响加载 |
| `requiredModules` | 否 | 涉及的模块列表，只是说明，不做强校验 |
| `optional.homepage` | 否 | 主页链接 |
| `optional.qq` | 否 | QQ 号、群号或链接皆可 |

注意：
- 包 ID 是**文件夹名**，不在 pack.json 里设置。
- `name`/`author`/`version` 缺一个，系统会忽略这个包。
- 日志显示格式：`名称 v版本号 by 作者 - 描述`；ModConfig 下拉列表只显示 `name`。

## 涉及模块

声音包的子目录名和模块目录名一致：

| 模块 | 目录 |
|------|------|
| BGM | `TitleBGM/`、`HomeBGM/`、`SceneBGM/`、`Extraction/`、`BossBGM/` |
| 敌人语音 | `CustomEnemySounds/` |
| 脚步 | `CustomFootStepSounds/` |
| 枪械 | `CustomGunSounds/` |
| 近战 | `CustomMeleeSounds/` |
| 手雷 | `CustomGrenadeSounds/` |
| 命中与击杀 | `CustomHitAndKillSounds/` |
| 物品 | `CustomItemSounds/` |

## 常见问题

- **看不到声音包选项**：`DuckovCustomSounds/` 下没有包含有效 `pack.json` 的子文件夹，且根目录也没有带音频的典型资源目录（`HomeBGM`/`BossBGM`/`SceneBGM`/`TitleBGM`）时，不会显示任何选项。根目录有典型资源目录且有音频时，会自动出现 "Default" 选项。
- **切换不生效**：需重启游戏。检查 `settings.json` 里 `currentSoundPack` 是否正确。
- **包没出现**：`pack.json` JSON 格式错误或缺少必填字段。
- **怎么恢复默认**：UI 选 "Default"，或清空 `currentSoundPack` 值重启。
- **不支持嵌套**：只扫描 `DuckovCustomSounds/` 第一层子目录。
