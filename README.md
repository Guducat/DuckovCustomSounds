# 逃离鸭科夫自定义音效Mod

一个为《逃离鸭科夫》(Escape from Duckov) 游戏设计的自定义音效Mod，允许玩家替换游戏中的背景音乐和击杀音效等。

# 自定义音效文档请点击查看[这里](https://github.com/Guducat/DuckovCustomSounds/wiki)！
## 自定义音效文档请点击查看[这里](https://github.com/Guducat/DuckovCustomSounds/wiki)！！
### 自定义音效文档请点击查看[这里](https://github.com/Guducat/DuckovCustomSounds/wiki)！！！
**上面都是同一个链接**

#### 自v2.x起，本MOD将以大版本的形式开源，给各位带来的不便敬请谅解。

---

### 本MOD做到了什么？
- 自定义主菜单音乐
- 自定义地堡背景音乐
- 自定义敌方音效（巡逻、惊讶、死亡等）
- 自定义手雷音效
- 增加敌方因手雷受惊的音效
- 自定义撤离倒计时音效/音乐
- 自定义枪声
- 自定义近战声
- 自定义打药/喝水/进食音效
- 自定义行走/跑步/翻滚音效
- BOSS BGM




### 更多功能仍在开发
- 环境音效
- 更多可自定义的音效，拥有无限可能
- **更多可自定义的音效**

---

## 安装说明

### 1. (非创意工坊)下载和安装
1. 下载release中的文件或自行编译(v1.x)
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
参考WIKI，在指定目录下放置音频文件

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

---

## 开发信息

### 项目结构
```
DuckovCustomSounds/
├── CustomBGM/
├── CustomEnemySounds/
├── CustomFootStepSounds/
├── CustomGrenadeSounds/
├── CustomGunSounds/
├── CustomItemSounds/
├── CustomMeleeSounds/
├── Logging/
├── ModConfig/
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
- [x] 自定义脚步音效
- [ ] 自定义环境音效
- [ ] 无限可能……

### 不重复实现的功能
- [x] 自定义文本 - 由 [@MajMaj](https://steamcommunity.com/profiles/76561198990516691) 的 [三角洲人机文本替换增加人机 交战 换弹 躲避手雷文本](https://steamcommunity.com/sharedfiles/filedetails/?id=3592577168
  ) mod 提供
- [x] 自定义搜索/搜出音效 - 由 [@dzj0821](https://steamcommunity.com/profiles/76561198053835373) 的 [物品价值稀有度与搜索音效](https://steamcommunity.com/sharedfiles/filedetails/?id=3588386576) mod提供
- [x] 自定义击杀音效 - 由 [@F_O_G](https://steamcommunity.com/id/For_Of_Des) 的 [CF击杀反馈](https://steamcommunity.com/sharedfiles/filedetails/?id=3590362366) mod提供

---

### 示例资源包
[蓝奏云-10月27日23时50分版本](https://guducat.lanzoul.com/iyxyE39i2cid)，音效请解压在*游戏根目录(Escape from Duckov/DuckovCustomSounds/...)下，确保结果与文档一致*。
**额外说明**：资源包仅作演示，请自行修改。目前包括：五字搜打撤游戏BGM、阿萨拉小兵语音、罐头笑声、专业配音员手雷掷出声。
务必删除：“Escape from Duckov\DuckovCustomSounds\CustomFootStepSounds\player\"这整个player文件夹！否则很吵！
如果各位有好的想法欢迎提issues！

---

### 贡献指南
欢迎提交问题报告和功能建议！
- 请确保描述清晰，包含复现步骤
- 代码贡献请遵循项目的编码规范
- 提交前请确保代码通过基本测试
- QQ群：979203137
