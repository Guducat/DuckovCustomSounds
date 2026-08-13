# 鸭科夫自定义音效音乐Mod/Duckov Custom Sounds

*图标由GPT-image-2生成*

一个为《逃离鸭科夫》(Escape from Duckov) 游戏设计的自定义音效Mod，允许玩家替换游戏中的背景音乐和各种音效，

本mod用于替换各种BGM，规则较复杂，有问题请提issues或进群咨询。  
本mod还在持续更新中，欢迎关注插眼。致力做成最大最多基础功能的音效mod！

# 自定义音效文档请[点击这里查看](https://guducat.github.io/DuckovCustomSounds/)！

![订阅](https://img.shields.io/steam/subscriptions/3592591001?style=for-the-badge&label=订阅&color=b4e419) ![下载](https://img.shields.io/steam/downloads/3592591001?style=for-the-badge&label=下载&color=00adb5) ![浏览量](https://img.shields.io/steam/views/3592591001?style=for-the-badge&label=浏览量&color=ff5719) ![发布日期](https://img.shields.io/steam/release-date/3592591001?style=for-the-badge&label=发布日期&color=ffb300) ![更新日期](https://img.shields.io/steam/update-date/3592591001?style=for-the-badge&label=更新日期&color=515de9)

[我的另外一款MOD自定义玩家F1声音的MOD](https://github.com/Guducat/DuckovCustomPlayerQuak)
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
- 自定义命中与击杀提示音
- 自定义玩家与NPC受击音效
- 自定义打药/喝水/进食音效
- 自定义行走/跑步/翻滚音效
- BOSS BGM
- 进图音效

### 更多功能仍在开发

<details >
<summary>大饼</summary>

- 环境音效
- 不同手雷投掷物分别自定义不同音效
- 在不同增益buff或状态下脚步声音不同
- 在不同buff时有不同音效，buff消失音效消失
- 角色血量偏低时播放警示音

</details>

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
├── API/                      # 公共API接口
│   ├── CustomModController.cs
│   ├── EnemyContextData.cs
│   ├── ExternalRouter.cs
│   ├── IVoicePackProvider.cs
│   └── ProxyEvent_Patches.cs
├── CustomBGM/                # 背景音乐模块
│   ├── Core/
│   ├── AmbientIntercept/
│   ├── BossBGM/
│   ├── ExtractionBGM/
│   ├── HomeBGM/
│   └── SceneBGM/
├── CustomEnemySounds/        # 敌人音效模块
│   ├── Audio/
│   ├── Config/
│   ├── Context/
│   ├── Filters/
│   └── Rules/
├── CustomFootStepSounds/     # 脚步音效模块
├── CustomGrenadeSounds/      # 手雷音效模块
├── CustomGunSounds/           # 枪声音效模块
├── CustomHitAndKillSounds/    # 命中、击杀与受击音效模块
├── CustomItemSounds/         # 物品音效模块
├── CustomMeleeSounds/         # 近战音效模块
├── Logging/                  # 日志系统
├── ModConfig/                # Mod配置
├── SoundPack/                # 音效包系统
├── Common/                   # 公共组件
├── AudioDistanceHelper.cs    # 音频距离辅助工具
├── ModBehaviour.cs           # Mod主控制器
├── ModSettings.cs            # Mod设置
└── DuckovCustomSounds.csproj # 项目文件
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
- [x] BOSS BGM
- [ ] 自定义环境音效
- [ ] 无限可能……

### 不重复实现的功能
- [x] 自定义玩家F1声音替换/DuckovCustomPlayerQuak —— https://steamcommunity.com/sharedfiles/filedetails/?id=3596875485  由我自己的MOD实现。
- [x] 自定义搜索/搜出音效 — 由 @dzj0821 的 物品价值稀有度与搜索音效 mod 提供
- [x] 自定义文本（交战、换弹、躲避手雷等） — 由 @MajMaj 的 三角洲人机文本替换增加人机 交战 换弹 躲避手雷文本 mod 提供

---

### 示例资源包
[蓝奏云-10月27日23时50分版本](https://guducat.lanzoul.com/iyxyE39i2cid)，音效请解压在*游戏根目录(Escape from Duckov/DuckovCustomSounds/...)下，确保结果与文档一致*。

**额外说明**：资源包**仅作演示**，请自行修改。目前包括：DeltaForce 音效、阿萨拉小兵语音、罐头笑声、专业配音员手雷掷出声、优质战士、电棍、Minecraft。
务必删除："Escape from Duckov\DuckovCustomSounds\CustomFootStepSounds\player\"这整个player文件夹！否则很吵！

自定义资源包请参考[这里](https://guducat.github.io/DuckovCustomSounds/)。

如果各位有好的想法欢迎提issues！

---

## 适配本Mod的优质资源包

逃离塔科夫音效包-EFTSE

https://steamcommunity.com/sharedfiles/filedetails/?id=3593231529

<br>

叛乱：沙暴 沉浸式音效

https://steamcommunity.com/sharedfiles/filedetails/?id=3595007718

使用教学： https://steamcommunity.com/sharedfiles/filedetails/?id=3594399867

<br>

[玩点好的]三角洲音效mod替换包

https://steamcommunity.com/sharedfiles/filedetails/?id=3596300200

## TODO 待办与改进项

### 改进方向
- 敌人接敌语音过于频繁，需要降低触发频率或增加间隔控制
- 第三方 MOD 武器枪声匹配优化

### 待复测问题（可能未修复）
- 油桶爆炸与手雷爆炸无法区分（两者共用同一音效）
- 高射速枪吞声
- 医疗箱音效重复播放两次

个人能力有限暂时无法做到面面俱到，mod更新可能没有很快，还请大家海涵！

其他mod作者欢迎联动！有意向/各种意见/BUG反馈也请发邮件到guducat@qq.com

## 如果有问题/疑问/建议，欢迎加入反馈
## 群号979203137，豹猫和朋友们｜鸭科夫mod综合售后群


---

### 贡献指南
欢迎提交问题报告和功能建议！
- 请确保描述清晰，包含复现步骤
- 代码贡献请遵循项目的编码规范
- 提交前请确保代码通过基本测试
- QQ群：979203137
