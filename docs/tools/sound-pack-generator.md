---
title: 声音包生成器
description: 使用在线工具快速生成符合格式的pack.json文件
---

# 声音包生成器

这个在线工具可以帮助您快速生成符合DuckovCustomSounds Mod要求的pack.json文件。只需填写表单，即可获得格式正确的JSON配置文件。

## 使用方法

1. 在下方表单中填写您的声音包信息
2. 必填项（带*号）必须填写完整
3. 可根据需要选择涉及的模块和添加额外信息
4. 右侧会实时显示生成的JSON预览
5. 确认无误后点击"复制JSON"按钮
6. 在您的声音包文件夹中创建pack.json文件并粘贴内容

## 在线生成器

<ClientOnly>
  <SoundPackGenerator />
</ClientOnly>

## 手动创建说明

如果您更喜欢手动创建pack.json文件，请参考以下格式：

### 最小必填配置

```json
{
  "name": "您的声音包名称",
  "author": "您的名字",
  "version": "1.0.0"
}
```

### 完整配置示例

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
    "CustomFootStepSounds"
  ],
  "optional": {
    "homepage": "https://example.com",
    "qq": "123456 / QQ群链接"
  }
}
```

## 字段说明

### 必填字段

- **name**: 声音包的显示名称，将在ModConfig UI中显示
- **author**: 作者名称
- **version**: 版本号，建议遵循语义化版本格式（如1.0.0）

### 可选字段

- **description**: 声音包的简要描述，将在UI中显示在名称后
- **compatibleModVersion**: 目标Mod兼容版本，仅用于说明
- **requiredModules**: 此包涉及的模块列表，仅用于说明，不做强校验
  - 可选值：`CustomBGM`, `CustomFootStepSounds`, `CustomEnemySounds`, `CustomGunSounds`, `CustomMeleeSounds`, `CustomGrenadeSounds`, `CustomItemSounds`, `CustomKillFeedback`
- **optional.homepage**: 作者或声音包的主页链接
- **optional.qq**: QQ号或QQ群号

## 注意事项

1. **包ID**: 包ID就是文件夹名称，不需要在pack.json中指定
2. **显示格式**: 在UI中的显示格式为 `名称 v版本号 by 作者 - 描述`
3. **验证**: 如果必填字段缺失，系统会认为该包的元信息无效并忽略
4. **分发**: 分发时只需打包您的声音包文件夹，不要包含settings.json等用户本地配置文件

## 目录结构示例

```
DuckovCustomSounds/
├─ settings.json                 # 全局设置（不要打包带走）
├─ TitleBGM/                     # Default（非包）示例
├─ HomeBGM/
├─ SceneBGM/
├─ BossBGM/
├─ MyPack/                       # 您的声音包（包ID = MyPack）
│  ├─ pack.json                  # 使用本工具生成的元数据文件
│  ├─ HomeBGM/
│  │  ├─ song1.mp3
│  │  └─ song2.mp3
│  ├─ CustomEnemySounds/
│  │  └─ voice_rules.json
│  └─ CustomFootStepSounds/
│     └─ footsteps/
│        ├─ concrete/
│        └─ grass/
└─ AnotherPack/
   ├─ pack.json
   └─ CustomGunSounds/
```

## 相关文档

- [声音包系统概述](../modules/soundpack.md)
- [自定义BGM](../modules/bgm/overview.md)
- [自定义敌人语音](../modules/enemy-voices.md)
- [自定义脚步声](../modules/footsteps.md)