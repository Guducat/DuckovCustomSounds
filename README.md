# 逃离鸭科夫自定义音效Mod

一个为《逃离鸭科夫》(Escape from Duckov) 游戏设计的自定义音效Mod，允许玩家替换游戏中的背景音乐和击杀音效等。

**自定义音效文档请点击查看[这里](https://github.com/Guducat/DuckovCustomSounds/tree/master/CustomEnemySounds)**

---

### 支持自定义背景音乐
- **主菜单音乐**：替换游戏主菜单的背景音乐
- **大厅音乐**：支持多首自定义音乐循环播放，可通过游戏内唱片机界面切换
- **智能文件解析**：自动从文件名提取歌曲名和作者信息（格式：`歌曲名 - 作者.mp3`）
- **音量控制**：已集成入游戏音量系统

### 支持自定义敌人音效
- **不同状态的音效**：巡逻，警觉，死亡音效

### 更多功能仍在开发
- **更多音效**：包含脚步、枪械声音
- **更多可自定义的音效**

---

## 安装说明

### 1. (非创意工坊)下载和安装
1. 下载release中的文件或自行编译
2. 将Mod文件夹放置在游戏的Mods目录下
3. 确保目录结构如下：
```
Escape from Duckov/Duckov_Data
└── Mods/
    └── DuckovCustomSounds/
        ├── preview.png
        ├── info.ini
        ├── 0Harmony.dll
        └── DuckovCustomSounds.dll
```

### 2. 配置音频文件
在游戏根目录创建以下目录结构并放置音频文件：

```
DuckovCustomSounds/
├── TitleBGM/              # 主音乐文件夹（可选）
│   ├── title.mp3          # 主菜单音乐文件
│   ├── death.mp3          # 死亡时播放的过渡音乐
│   └── start.mp3          # 进入大厅时播放的过渡音乐
│
├── HomeBGM/               # 大厅音乐文件夹（可选）
│   ├── 歌曲1 - 艺术家.mp3
│   ├── 歌曲2 - 艺术家.mp3
│   └── my_fav_track.mp3   # 支持任意文件名
│
├── CustomEnemySounds/             # 敌人音效文件夹
│   └── scav       # 阵营名称
│       ├── normal_Duck_normal.mp3  # 敌人类型_语音类型_状态.mp3
│       ├── normal_Duck_normal_1.mp3  # 可以添加变体，随机播放
│       ├── normal_Duck_surprise.mp3
│       ├── normal_duck_grenade.mp3 # 敌人发现手雷的语音提示
│       └── normal_Duck_death.mp3
│
├── CustomGrenadeSounds/  # 手雷音效文件夹
│   ├── throw.mp3        # 手雷投掷音效
│   └── explode.mp3 # 手雷普通爆炸音效
│
├── CustomGunSounds/  # 射击音效文件夹
│
├── CustomFootstepSounds/ #脚步音效文件夹(开发中，可忽略)
│
└── 开发中，未来期望添加其他音效自定义，目前计划有脚步 枪械等
```

### 3. 音频文件要求
- **格式**：MP3
- **命名规则**：
  - 背景音乐：`歌曲名 - 作者.mp3` 或直接使用歌曲名（但作者会显示为"群星"）
  - 敌人音效：`敌人类型_语音类型_状态.mp3`，可选添加变体编号，例如 `normal_Duck_normal_1.mp3`

## 使用方法

### 背景音乐控制
- 游戏大厅内的唱片机界面可以切换自定义背景音乐
- 支持上一首/下一首切换
- 歌曲信息会自动显示

### 音量控制
- 使用游戏内置的音量设置调整自定义音效
- 背景音乐受"音乐"音量滑块控制
- 击杀音效受"音效"音量滑块控制


### 日志文件位置
出现BUG提出议题时，请务必(使用AI)查看或附上文件下的player.log ，以便作者分析问题
```
C:\Users\{用户名}\AppData\LocalLow\TeamSoda\Duckov\
```

### 获取支持
如果遇到问题，请提供以下信息：
- 错误截图或日志文件
- 复现流程与描述
- 音频文件信息（可选）

## 统一日志系统与配置（settings.json）

为便于调试与生产环境使用，Mod 提供“按模块可控”的统一日志系统。支持的模块：Core / CustomEnemySounds / CustomBGM / CustomGrenadeSounds。

- 配置文件位置：游戏根目录/DuckovCustomSounds/settings.json（与 debug_off/.nolog 同级）
- 自动生成：若文件不存在，会在启动时自动生成默认配置（所有模块 Info）
- 日志级别（从低到高）：Error, Warning, Info, Debug, Verbose（级别越高输出越多）
- 兼容性：
  - CustomEnemySounds 仍兼容 voice_rules.json 的 Debug 段配置（仅当 settings.json 未显式配置该模块时生效）
  - debug_off 或 .nolog 文件存在时，会将所有模块的级别钳制至 Info

示例配置：

```
{
  "logging": {
    "enabled": true,
    "defaultLevel": "Info",
    "modules": {
      "Core": { "level": "Info" },
      "CustomEnemySounds": { "level": "Info" },
      "CustomBGM": { "level": "Error" },
      "CustomGrenadeSounds": { "level": "Debug" }
    }
  }
}
```

说明：
- logging.enabled：全局开关（false 将关闭所有日志）
- logging.defaultLevel：未在 modules 中显式列出的模块使用该级别
- modules.*.level：对指定模块单独设置级别

快速验证：
1) 删除 settings.json 后启动游戏 → 检查同目录是否自动生成默认配置（所有模块 Info）
2) 将 CustomBGM 设为 Error → 仅 Error 输出；将 CustomGrenadeSounds 设为 Debug → Debug/Info/Warn/Error 输出
3) 创建 debug_off 文件 → 所有模块仅输出 Error/Warning/Info（Debug/Verbose 被抑制）

---

## 开发信息

### 项目结构
```
DuckovCustomSounds/
├── CustomBGM/
│   ├── CustomBGM.cs           # BGM管理器
│   └── CustomBGM_Patches.cs   # BGM相关补丁
├── CustomEnemySounds/
│   └── CustomEnemySounds.cs    # 敌方音效管理器
├── CustomGrenadeSounds/
│   └── CustomGrenadeSounds.cs  # 手雷音效管理器
├── ModBehaviour.cs            # Mod主控制器
└── DuckovCustomSounds.csproj  # 项目文件
```

---

### 里程碑
- [x] 自定义主菜单音乐
- [x] 自定义安全屋背景音乐
- [x] 安全屋播放音乐时留声机显示歌曲名和作者
- [x] 自定义敌方音效(巡逻、惊讶、死亡等)
- [x] 自定义手雷音效
- [x] 自定义死亡音效
- [x] 增加敌人发现手雷的音效
- [x] 自定义撤离成功音乐
- [x] 自定义撤离倒计时音效
- [x] 自定义枪声
- [x] 自定义敌人换弹音效
- [x] 自定义近战音效
- [x] 自定义饮食/饮水/打药音效
- [ ] 自定义脚步音效
- [ ] 自定义环境音效
- [ ] 无限可能……

### 不重复实现的功能
- [x] 自定义文本 - 由 [@MajMaj](https://steamcommunity.com/profiles/76561198990516691) 的 [三角洲人机文本替换增加人机 交战 换弹 躲避手雷文本](https://steamcommunity.com/sharedfiles/filedetails/?id=3592577168
  ) mod 提供
- [x] 自定义搜索/搜出音效 - 由 [@dzj0821](https://steamcommunity.com/profiles/76561198053835373) 的 [物品价值稀有度与搜索音效](https://steamcommunity.com/sharedfiles/filedetails/?id=3588386576) mod提供
- [x] 自定义击杀音效 - 由 [@F_O_G](https://steamcommunity.com/id/For_Of_Des) 的 [CF击杀反馈](https://steamcommunity.com/sharedfiles/filedetails/?id=3590362366) mod提供

### 目前仍存的疑难杂症
- 拦截stg_map_base Stinger播放自定义声音时会回落到SFX，Music路由不可用

---

### 示例资源包

[蓝奏云-1025更新](https://guducat.lanzoul.com/i0uZu399sglc)，音效请解压在*游戏根目录(Escape from Duckov/DuckovCustomSounds/...)下，确保结果与文档一致*。
**额外说明**：资源包仅作演示，请自行修改。目前包括：五字搜打撤游戏BGM、阿萨拉小兵语音、罐头笑声、专业配音员手雷掷出声。
如果各位有好的想法欢迎提issues！

---

### 贡献指南
欢迎提交问题报告和功能建议！
- 请确保描述清晰，包含复现步骤
- 代码贡献请遵循项目的编码规范
- 提交前请确保代码通过基本测试
