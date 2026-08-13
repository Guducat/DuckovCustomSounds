# 自定义BGM模块说明

用资源包中的音频替换游戏内 BGM 与相关提示音。

---

## 涉及内容

- **标题页 BGM**：进标题时先播 `startFX.*`（开场音效），播完后自动切到 `title.*`（循环播放）
- **基地提示音**：进入基地时播放 `start.*`（非循环，可在设置中开关）
- **死亡提示音**：死亡时播放 `death.*`（非循环）
- **基地留声机 BGM**：`HomeBGM/` 目录下的所有音乐，支持自动切歌、随机播放、音量控制
- **撤离音效**：倒计时提示音和成功提示音（`Extraction/` 目录）
- **Boss BGM**：为特定 Boss 播放专属 BGM（`BossBGM/` 目录）
- **场景 BGM**：进入场景和循环 BGM（`SceneBGM/Enter/` 和 `SceneBGM/Loop/`）

---

## 目录结构与文件

所有路径相对于资源包根目录 `DuckovCustomSounds/`。

### TitleBGM/

```
TitleBGM/
├── title.mp3        # 标题页BGM（循环播放）
├── startFX.flac     # 进标题时的开场音效（非循环，播完后自动切到 title）
├── start.ogg        # 进入基地短提示（非循环；可在设置中开关）
├── death.mp3        # 死亡提示（非循环）
└── extraction.flac  # [旧版兼容] 撤离成功缺省回退
```

### HomeBGM/

```
HomeBGM/
├── 天天天国地獄国 - Aiobahn +81 (feat. ななひら & P丸様。).mp3
├── 天知河 - 说说Crystal.mp3
└── 最炫民族风.mp3
```

- 文件名建议 `曲名 - 作者.mp3` 或 `曲名.mp3`。无作者时界面显示"群星"。
- 支持切换上一首/下一首，自动切歌，随机播放（可避免立即重复）。
- 音量随"音乐"滑块，或单独在 ModConfig 设置百分比。

### Extraction/

```
Extraction/
├── countdown.mp3    # 倒计时≤5s时播放（非循环）
├── success.flac     # 撤离成功替换音效
└── extraction.ogg   # [旧版兼容] 倒计时候选
```

模式（ModConfig → ExtractionBGM）：
- **关闭**：不处理撤离音效，但若 `TitleBGM/extraction.*` 存在，仍会播放（旧版兼容）
- **倒计时模式**：当撤离倒计时剩余 ≤5 秒时播放 `countdown.*`（若不存在则尝试 `extraction.*`），期间场景 BGM 快速降音量避免双重 BGM，取消后恢复。撤离成功时屏蔽原版 Stinger
- **成功替换模式**：替换撤离成功 Stinger，优先 `success.*`，不存在则回退到 `TitleBGM/extraction.*`

场景切换或 StopBGM 时自动停止正在播放的撤离音效。

### BossBGM/

```
BossBGM/
├── default_boss.mp3    # 默认回退
├── BALeader.mp3        # Cname_BALeader → BALeader.mp3
├── Boss_Sniper.ogg     # Cname_Boss_Sniper → Boss_Sniper.ogg
└── ServerGuardian.flac # Cname_ServerGuardian → ServerGuardian.flac
```

- 文件名 = 敌人 NameKey 去掉 `Cname_` 前缀。找不到专属文件时用 `default_boss.*`，再没有就不播。
- ModConfig：启用开关、触发距离（10~200 米，默认 40）、音量。
- 高级参数：`BossBGM/config.json`（淡入淡出、更新频率、防抖、死亡淡出等）。
- 优先级：Boss BGM > 场景 BGM。目标进入范围淡入，离开或死亡淡出；解除压制时若场景循环 BGM 意外停止会自动重建（自愈）。

### SceneBGM/

```
SceneBGM/
├── Enter/                # 入场音乐（不循环，播放一次）
│   ├── zero_enter.mp3
│   ├── fram_enter.mp3
│   └── default_enter.mp3
└── Loop/                 # 循环音乐
    ├── zero_loop.mp3
    ├── fram_loop.mp3
    └── default_loop.mp3
```

- 两段式模型：进入播 Enter（一次），然后切入 Loop（循环）。
- 命名匹配优先级：精准场景名 > sceneId 匹配 > sceneId 变体兼容 > 场景类型关键词（如 `loading_*`、`lab_*`、`farm_*`、`warehouse_*`）> 默认。同时支持无后缀匹配。
- sceneId 变体兼容示例：`Level_GroundZero_1` 可匹配 `level_groundzero_main_enter.mp3`，`Level_Farm_01` 可匹配 `level_farm_main_enter.mp3`，`Level_HiddenWarehouse_Main` 可匹配 `level_warehouse_main_enter.mp3`。
- 加载界面（Loading Screen）属于"loading"类型，Enter 不会对加载场景播默认音乐。
- 高级参数：`SceneBGM/config.json`（淡入淡出时长、场景加载延迟、交叉渐变等）。

---

## 设置项

推荐用 ModConfig 菜单调整：
- **HomeBGM**：启用、音量、SFX 总线、随机播放、上一首也随机、避免立即重复、自动切歌、启用 start.mp3
- **ExtractionBGM**：模式（关闭/倒计时/成功替换）
- **BossBGM**：启用、触发距离
- **SceneBGM**：启用 Enter/Loop、各自音量

若无 ModConfig，可编辑 `settings.json`：
```json
{
  "homeBgmAutoPlayNext": true,
  "homeBgmRandomEnabled": false,
  "homeBgmRandomNoRepeat": true,
  "homeBgmRandomizePrevious": false,
  "overrideExtractionBGM": false
}
```

- HomeBGM 的音量和 SFX 总线只能在 ModConfig 中设置。
- `overrideExtractionBGM` 为旧版兼容字段，`true` = 倒计时模式。新版建议用 ModConfig 选模式。

---

## 注意事项

- 路径相对于当前资源包根目录（`DuckovCustomSounds/` 或当前声音包路径）。
- 支持格式（按优先级）：`.mp3`、`.wav`、`.ogg`、`.oga`、`.flac`、`.aif`、`.aiff`、`.mp2`、`.m4a`、`.mp4`、`.wma`、`.asf`、`.fsb`、`.it`、`.mid`、`.midi`、`.mod`、`.s3m`、`.xm`。
- 自定义 BGM 默认走 Music 总线；HomeBGM 可切到 SFX 总线（实验性）。
- 文件名用半角连字符 `-` 分隔曲名和作者，不要用中文破折号。
- 与场景 BGM 并用时，Boss BGM 优先级更高（进入范围后自动压低场景音量）。
