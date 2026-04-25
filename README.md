# 鸭科夫自定义音效音乐Mod/Duckov Custom Sounds

一个为《逃离鸭科夫》(Escape from Duckov) 游戏设计的自定义音效Mod，允许玩家替换游戏中的背景音乐和各种音效，仍在开发中，计划支持里程碑中的更多功能。

本mod用于替换各种BGM，规则较复杂，有问题请提issues或进群咨询。  
本mod还在持续更新中，欢迎关注插眼。致力做成最大最多基础功能的音效mod！

一个为《逃离鸭科夫》(Escape from Duckov) 游戏设计的自定义音效Mod，允许玩家替换游戏中的背景音乐和击杀音效等。

# 自定义音效文档请点击查看[这里](https://github.com/Guducat/DuckovCustomSounds/wiki)！
## 自定义音效文档请点击查看[这里](https://github.com/Guducat/DuckovCustomSounds/wiki)！！
### 自定义音效文档请点击查看[这里](https://github.com/Guducat/DuckovCustomSounds/wiki)！！！
**上面都是同一个链接**

#### 自v2.x起，本MOD将以大版本的形式开源，给各位带来的不便敬请谅解。

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
- 自定义打药/喝水/进食音效
- 自定义行走/跑步/翻滚音效
- BOSS BGM
- 进图音效

### 更多功能仍在开发
- 环境音效
- 不同手雷投掷物分别自定义不同音效[计划内]
- 进游戏音效[调研中]
- 受击音效、命中音效自定义[计划内]

- 不同血包分别自定义音效（另外如果扎针音效能放完就行了，现在超过扎针时间的声音会戛然而止）[已完成？]

- 在不同增益buff或状态下脚步声音不同[计划内]
- 在不同buff时有不同音效，buff消失音效消失[计划内]
- 角色血量偏低时播放警示音[调研中]

- bossBGM扩大播放范围，bgm由远到近声音逐渐变大[调研中]

- 更多可自定义的音效，拥有无限可能
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
├── API/                      # 公共API接口
│   ├── CustomModController.cs
│   ├── EnemyContextData.cs
│   ├── ExternalRouter.cs
│   ├── IVoicePackProvider.cs
│   ├── PlaybackRequest.cs
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
├── CustomItemSounds/         # 物品音效模块
├── CustomKillFeedback/       # 击杀反馈模块
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

### 核心技术特性

#### 1. 统一音频接口
- **PostCustomSound(string filePath, GameObject gameObject, bool loop = false)** - 3D音效接口
- **PostCustomSound(string filePath, bool loop = false)** - 2D音效接口
- 自动路由到正确的FMOD总线（音效/音乐）
- 自动继承3D空间属性和全局音频效果
- 完整的生命周期管理和资源清理

#### 2. 模块化架构
- 每个音效类型独立模块管理
- 统一的配置系统和日志系统
- 支持热重载和动态配置更新
- 高度可扩展的规则引擎

#### 3. 智能音效管理
- 基于上下文的音效选择系统
- 优先级管理和限流控制
- 变体绑定和随机播放机制
- 距离衰减和空间音频处理

#### 4. 性能优化
- 事件实例缓存和复用
- 智能预加载和延迟释放
- 批量处理和异步操作
- 内存使用优化

---

### 更新日志

#### 2.0.0版本 (2025-10-29)
- 完全适配官方接口
- 添加BOSS BGM功能
- 解决枪声限流问题
- 重构音频系统架构
- 优化性能和内存使用
- 统一日志系统
- 完善音效包支持

#### 里程碑
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
- [x] 自定义击杀音效 — 由 @F_O_G 的 CF击杀反馈 mod 提供
- [x] 自定义文本（交战、换弹、躲避手雷等） — 由 @MajMaj 的 三角洲人机文本替换增加人机 交战 换弹 躲避手雷文本 mod 提供

---

### 示例资源包
[蓝奏云-10月27日23时50分版本](https://guducat.lanzoul.com/iyxyE39i2cid)，音效请解压在*游戏根目录(Escape from Duckov/DuckovCustomSounds/...)下，确保结果与文档一致*。
**额外说明**：资源包仅作演示，请自行修改。目前包括：DeltaForce 音效、阿萨拉小兵语音、罐头笑声、专业配音员手雷掷出声、优质战士、电棍、Minecraft。
务必删除："Escape from Duckov\DuckovCustomSounds\CustomFootStepSounds\player\"这整个player文件夹！否则很吵！
如果各位有好的想法欢迎提issues！

---

## 适配本Mod的优质资源包

逃离塔科夫音效包-EFTSE
https://steamcommunity.com/sharedfiles/filedetails/?id=3593231529

叛乱：沙暴 沉浸式音效
https://steamcommunity.com/sharedfiles/filedetails/?id=3595007718
使用教学： https://steamcommunity.com/sharedfiles/filedetails/?id=3594399867

## 已知问题说明

- 由于油桶爆炸和手雷爆炸用的是一个声效，需要花时间适配。
- 打药声音走的是另外的逻辑，需要花时间适配。
- 部分自定义枪械MOD的逻辑需要花时间适配。
- start.mp3暂时被拦截，需要花时间适配（如果有人需要）。

个人能力有限暂时无法做到面面俱到，mod更新可能没有很快，还请大家海涵！

其他mod作者欢迎联动！如没有Github账号和不想进群，有意向/各种意见/BUG反馈也请发邮件到guducat@qq.com

## 如果有问题/疑问/建议，欢迎加入979203137，鸭科夫自定义音效mod交流反馈群

## 统一日志系统与配置（settings.json）

为便于调试与生产环境使用，Mod 提供"按模块可控"的统一日志系统。支持的模块：Core / CustomEnemySounds / CustomBGM / CustomGrenadeSounds。

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

### 贡献指南
欢迎提交问题报告和功能建议！
- 请确保描述清晰，包含复现步骤
- 代码贡献请遵循项目的编码规范
- 提交前请确保代码通过基本测试
- QQ群：979203137
