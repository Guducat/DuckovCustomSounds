# 自定义BGM模块说明
自定义背景音乐系统——用资源包中的音频替换游戏内BGM与相关提示音

---

## 涉及内容
- 标题页BGM：替换主菜单音乐（TitleBGM/title.mp3，循环）
- 基地提示音：进入基地短提示（TitleBGM/start.mp3，非循环，可在设置中开关）
- 死亡提示音：死亡Stinger（TitleBGM/death.mp3，非循环）
- 基地BGM：多首歌，自动切歌/随机/顺序（HomeBGM/，仅MP3）
- 撤离相关：倒计时提示音与成功提示音（Extraction/，见下）
- BossBGM：为特定BOSS播放专属BGM（BossBGM/，见下）
- 播放控制：基地界面可切换上一首/下一首；支持自动切歌、随机；音量随“音乐”滑块
- 说明：场景BGM与BossBGM为独立子模块；本文含简要指引，完整细节见相应目录下的 README

涉及的文件夹（相对于资源包根目录 DuckovCustomSounds/）
```
TitleBGM/
├── title.mp3        # 标题页BGM（循环）
├── start.mp3        # 进入基地提示（非循环；可在设置中启用/禁用）
├── death.mp3        # 死亡提示（非循环）
└── extraction.mp3   # 兼容备用：当缺少 Extraction/success.* 时用于“撤离成功”回退

HomeBGM/             # 基地BGM（仅MP3）
├── 天天天国地獄国 - Aiobahn +81 (feat. ななひら & P丸様。).mp3
├── 天知河 - 说说Crystal.mp3
└── 最炫民族风.mp3
└── ...

Extraction/          # 撤离相关音效
├── countdown.mp3    # 撤离区倒计时≤5s时播放（非循环，推荐命名）
├── success.mp3      # 撤离成功Stinger（非循环，推荐命名）
├── countdown.wav    # （可选）同上
├── success.wav      # （可选）同上
├── extraction.mp3   # （兼容旧包）作为倒计时音效的候选
└── extraction.wav   # （兼容旧包）作为倒计时音效的候选
```

## 自定义指引

**1. 标题页音乐（TitleBGM）**
- 位置：`TitleBGM/title.mp3`
- 格式：MP3
- 缺失时：使用游戏原曲

**2. 基地BGM（HomeBGM）**
- 位置：`HomeBGM/` 文件夹
- 格式：MP3（仅此格式）
- 文件命名：建议 `曲名 - 作者.mp3` 或 `曲名.mp3`；未写作者时界面显示为“群星”
- 切歌与显示：基地界面可切换上一首/下一首；切换时会显示“曲名 - 作者（序号）”
- 自动切歌与循环：默认自动切到下一首；可在设置中改为单曲循环
- 随机播放：可启用随机；支持“避免立即重复”；可选“上一首也随机”

**3. 撤离相关音效（Extraction）**
- 模式选择（游戏内 ModConfig → ExtractionBGM）：
  - 关闭：不改动任何撤离音效
  - 倒计时模式：当撤离区倒计时剩余≤5秒时播放 `Extraction/countdown.mp3|wav`（非循环）。若没有 `countdown.*` 会依次尝试 `Extraction/extraction.mp3|wav`（兼容旧包）。
  - 成功替换模式：替换撤离成功的Stinger，优先使用 `Extraction/success.mp3|wav`；若缺失则回退到 `TitleBGM/extraction.mp3`。
- 场景切换与停止：切换场景或游戏触发 `StopBGM` 时，会自动停止正在播放的倒计时/兼容旧版撤离声音

## BossBGM 简明指引

- 功能：为特定 BOSS 播放专属 BGM，按与玩家距离自动淡入/淡出；BOSS 消失或离开范围后停止。与场景/基地 BGM 同时出现时，BossBGM 优先级最高。
- 文件夹：`BossBGM/`
- 支持格式：`.mp3`、`.wav`、`.ogg`、`.flac`
- 命名规则：
  - 使用敌人 `NameKey` 去掉 `Cname_` 前缀作为文件名。例如：`Cname_BALeader` → `BALeader.mp3`；`Cname_Boss_Sniper` → `Boss_Sniper.ogg`
  - 通用回退：提供 `default_boss.*`；未找到专属文件时使用它；若仍缺失则不播放
- 配置：
  - ModConfig：启用开关；触发距离（10–200，默认 40）
  - 高级参数：`BossBGM/config.json` 可调整淡入淡出时长、更新频率、最小切换间隔、距离阈值、是否记忆进度、延迟停止与死亡淡出等
- 示例：
```
DuckovCustomSounds/
└── BossBGM/
    ├── default_boss.mp3
    ├── BALeader.mp3
    ├── Boss_Sniper.ogg
    └── ServerGuardian.flac
```
- 说明：音量随“音乐”滑块；默认走 Music 总线。完整说明见 `CustomBGM/BossBGM/README.md`。

## 设置项（ModConfig 与 settings.json）
- 推荐使用游戏内 ModConfig 菜单调整：
  - HomeBGM：启用、音量（百分比）、使用SFX总线（实验性）、随机播放、上一首也随机、避免立即重复、自动切歌、启用 start.mp3
  - ExtractionBGM：模式（关闭 / 倒计时 / 成功替换）
  - BossBGM：启用、触发距离
- 若没有 ModConfig，可编辑 `DuckovCustomSounds/settings.json`（键名大小写不敏感）：
```json
{
  "homeBgmAutoPlayNext": true,
  "homeBgmRandomEnabled": false,
  "homeBgmRandomNoRepeat": true,
  "homeBgmRandomizePrevious": false,
  "overrideExtractionBGM": false,
  "enableAmbientIntercept": false
}
```
- 说明：
  - HomeBGM 的音量与“切到SFX总线”仅能在 ModConfig 中设置
  - 所有自定义BGM默认走 Music 总线，和游戏“音乐”滑块一致

## 资源结构示例
```
DuckovCustomSounds/
├── TitleBGM/
│   ├── title.mp3
│   ├── start.mp3
│   ├── death.mp3
│   └── extraction.mp3
├── HomeBGM/
│   ├── 雨天 - 某人.mp3
│   ├── Skyline.mp3
│   └── 晴天.mp3
├── Extraction/
│   ├── countdown.mp3
│   └── success.mp3
└── BossBGM/
    ├── default_boss.mp3
    ├── BALeader.mp3
    └── Boss_Sniper.ogg
```

## 注意事项
- 路径均相对于当前资源包根目录（`DuckovCustomSounds/` 或当前选中的音效包路径）
- HomeBGM 与 TitleBGM 仅支持 MP3；Extraction 支持 MP3/WAV；BossBGM 支持 MP3/WAV/OGG/FLAC
- 自定义BGM默认走 Music 总线；可在 ModConfig 将“基地BGM”切到 SFX 总线（实验）以减少与环境音混合
- 当游戏内其他系统停止音乐时，自定义BGM也会停止
- 与场景BGM/BossBGM并用时，优先级：BossBGM > 场景BGM > 基地/标题BGM
- 文件名尽量使用半角连字符“-”分隔“曲名 - 作者”；避免使用中文破折号或下划线
- 建议音频规范：44.1k/48kHz，16bit，合理响度，避免削波

