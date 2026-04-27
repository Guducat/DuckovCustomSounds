---
title: 声音包生成器
---

# 声音包生成器

在线填表、即时生成符合格式的 pack.json。

## 使用方法

1. 填写声音包信息
2. 必填项（带 * 号）必须填完
3. 选填模块和额外信息
4. 右侧实时预览 JSON
5. 点"复制 JSON"
6. 在声音包文件夹里创建 pack.json，粘贴

<ClientOnly>
  <SoundPackGenerator />
</ClientOnly>

## 手动创建

### 最小配置

```json
{
  "name": "你的声音包名",
  "author": "你的名字",
  "version": "1.0.0"
}
```

### 完整配置

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

## 字段说明

### 必填

| 字段 | 说明 |
|------|------|
| `name` | UI 显示名称 |
| `author` | 作者 |
| `version` | 版本号（如 `1.0.0`） |

### 可选

| 字段 | 说明 |
|------|------|
| `description` | 简介，UI 显示在名称后 |
| `compatibleModVersion` | 兼容 Mod 版本，仅说明，不影响加载 |
| `requiredModules` | 涉及模块列表：`CustomBGM`、`CustomEnemySounds`、`CustomFootStepSounds`、`CustomGunSounds`、`CustomMeleeSounds`、`CustomGrenadeSounds`、`CustomItemSounds`。仅说明，不做强校验 |
| `optional.homepage` | 主页链接 |
| `optional.qq` | QQ 号、群号或链接皆可 |

## 注意

- 包 ID = 文件夹名，不在 pack.json 里设置。
- UI 显示：`名称 v版本号 by 作者 - 描述`。
- 缺必填字段的包会被忽略。
- 分发时只打包声音包文件夹，不要带 settings.json。

## 目录示例

```
DuckovCustomSounds/
├── MyPack/                    # 包 ID = MyPack
│   ├── pack.json
│   ├── HomeBGM/
│   ├── CustomEnemySounds/
│   └── CustomFootStepSounds/
└── AnotherPack/
    ├── pack.json
    └── CustomGunSounds/
```
