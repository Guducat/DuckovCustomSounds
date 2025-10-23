# 逃离鸭科夫自定义音效Mod

一个为《逃离鸭科夫》(Escape from Duckov) 游戏设计的自定义音效Mod，允许玩家替换游戏中的背景音乐和击杀音效等。


### 自定义背景音乐
- **主菜单音乐**：替换游戏主菜单的背景音乐
- **大厅音乐**：支持多首自定义音乐循环播放，可通过游戏内唱片机界面切换
- **智能文件解析**：自动从文件名提取歌曲名和作者信息（格式：`歌曲名 - 作者.mp3`）
- **音量控制**：已集成入游戏音量系统

### 自定义击杀音效（开发中）
- **普通击杀音效**：替换普通击杀的音效
- **爆头击杀音效**：替换爆头击杀的特殊音效
- **功能开发中**：后续实现支持连杀支持或更多

### 仍在开发
- **敌方音效**：包含普通、警觉等
- **更多可自定义的音效**


## 安装说明

### 1. 下载和安装
1. 下载relase或自行编译
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
├── TitleBGM/              # 主菜单音乐文件夹（可选）
│   └── title.mp3          # 主菜单音乐文件
├── HomeBGM/               # 大厅音乐文件夹（可选）
│   ├── 歌曲1 - 艺术家.mp3
│   ├── 歌曲2 - 艺术家.mp3
│   └── my_fav_track.mp3   # 支持任意文件名
└── KillSound/             # 击杀音效文件夹（开发中）
│   ├── headshot.mp3       # 爆头音效
│   └── normalkill.mp3     # 普通击杀音效
└── 开发中，未来期望添加击杀音效，其他语音
```

### 3. 音频文件要求
- **格式**：MP3
- **命名规则**：
  - 背景音乐：`歌曲名 - 作者.mp3` 或直接使用歌曲名

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
出现BUG提出议题时，请务必先查看或附上文件下的player.log ，以便作者分析问题
```
C:\Users\{用户名}\AppData\LocalLow\TeamSoda\Duckov\
```

### 获取支持
如果遇到问题，请提供以下信息：
- 错误截图或日志文件
- 复现流程与描述
- 音频文件信息（可选）

## 开发信息

### 项目结构
```
DuckovCustomSounds/
├── CustomBGM/
│   ├── CustomBGM.cs           # BGM管理器
│   └── CustomBGM_Patches.cs   # BGM相关补丁
├── CustomEnemySounds/
│   └── CustomEnemySounds.cs    # 敌方音效管理器
├── ModBehaviour.cs            # Mod主控制器
└── DuckovCustomSounds.csproj  # 项目文件
```

---

### 里程碑
- [x] 自定义主菜单音乐
- [x] 自定义安全屋背景音乐
- [x] 安全屋播放音乐时留声机显示歌曲名和作者
- [x] 自定义搜索/搜出音效 - 由 [@dzj0821](https://steamcommunity.com/profiles/76561198053835373) 的 [物品价值稀有度与搜索音效](https://steamcommunity.com/sharedfiles/filedetails/?id=3588386576) mod提供
- [x] 自定义击杀音效 - 由 [@F_O_G](https://steamcommunity.com/id/For_Of_Des) 的 [CF击杀反馈](https://steamcommunity.com/sharedfiles/filedetails/?id=3590362366) mod提供
- [x] 自定义敌方音效(巡逻、惊讶、死亡等)
- [x] 自定义文本 - 由 [@MajMaj]( ) 的 [三角洲人机语音文本替换、补充人机默认文本](https://steamcommunity.com/sharedfiles/filedetails/?id=3591752102) mod 提供
- [ ] 自定义脚步
- [ ] 自定义枪声
- [ ] 更多可自定义的音效


---

### 贡献指南
欢迎提交问题报告和功能建议！
- 请确保描述清晰，包含复现步骤
- 代码贡献请遵循项目的编码规范
- 提交前请确保代码通过基本测试