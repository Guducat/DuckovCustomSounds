# 自定义枪械音效（CustomGunSounds）

本模块允许你为“射击（Shoot）”与“换弹（Reload）”音效提供自定义替换，并支持差分音效（同名 `_1`、`_2` … 随机）。

## 一、放置音频文件

在 Mod 根目录（DuckovCustomSounds）下创建/使用目录：

```
DuckovCustomSounds/
└─ CustomGunSounds/
   ├─ default.mp3               # 射击通用回退
   ├─ default_reload.mp3        # 换弹通用回退（可选）
   ├─ default_reload_start.mp3  # 换弹开始通用回退（可选）
   ├─ default_reload_end.mp3    # 换弹结束通用回退（可选）
   └─ 258.mp3                   # 示例：TypeID 为 258 的射击音效（推荐按 TypeID 命名）
```

 - 支持的格式：`.mp3`（推荐）、`.wav`、`.ogg`、`.oga`
 - 射击命名（优先推荐 TypeID 数字命名）：`{TypeID}(.ext)`；消音器：`{TypeID}_mute(.ext)`；也可使用 `{soundKey}`/`{soundKey}_mute` 作为回退
 - 换弹命名：
   - 完整换弹：`{TypeID}_reload(.ext)`
   - 分段：`{TypeID}_reload_start(.ext)` / `{TypeID}_reload_end(.ext)`（取消换弹时仅停止开始段，结束段会保留）
   - 亦支持以 `{soundKey}` 为名的回退（不推荐）

## 二、查找优先级（摘要）

射击（已装备消音器）：
1) `{TypeID}_mute(.ext)` → 2) `{soundKey}_mute(.ext)` → 3) `{TypeID}(.ext)` → 4) `{soundKey}(.ext)` → 5) `default(.ext)`

射击（未装备消音器）：
1) `{TypeID}(.ext)` → 2) `{soundKey}(.ext)` → 3) `default(.ext)`

换弹开始：
1) `{TypeID}_reload_start(.ext)` → 2) `{TypeID}_reload(.ext)` → 3) `{soundKey}(.ext)` → 4) `default_reload_start(.ext)` → 5) `default_reload(.ext)` → 6) `default(.ext)`

换弹结束：
1) `{TypeID}_reload_end(.ext)` → 2) `{soundKey}(.ext)` → 3) `default_reload_end(.ext)` → 4) `default(.ext)`

## 三、差分音效（随机）

当命中某个“基准文件”后（如 `258.mp3` 或 `258_mute.mp3`），若同目录存在同扩展名的差分：
`258_1.mp3`、`258_2.mp3`… 将仅在这些差分中随机选择其一播放；若无差分则使用基准文件。

支持 `.mp3/.wav/.ogg/.oga`。

## 四、距离与路由

- 3D 最小/最大距离默认设置为 `1f / 50f`；如需调整，可修改 `CustomGunSounds_Patches.cs` 中的 `set3DMinMaxDistance` 调用。
- 音频路由：优先绑定到 `bus:/Master/SFX` 的 ChannelGroup，不存在时回退到 `bus:/SFX`，最后回退到 Master。

## 五、日志与排错

- 模块名：`CustomGunSounds`
- 日志级别可在 `DuckovCustomSounds/settings.json` 中配置（由统一 `Logging.LogManager` 管理）。
- 常见日志：
  - `[GunShoot] 替换 ... → ...`：已成功替换并播放自定义音效
  - `FMOD 未初始化`：大多发生在加载早期，通常会自动恢复
  - `createSound / playSound 失败`：检查文件路径与格式

## 六、换弹取消处理（行为说明）

取消换弹或调用 `StopReloadSound()` 时：
- 停止所有“开始段/完整段”的自定义换弹音效；
- 保留“结束段”音效以避免突兀中断；
- 详情见日志 `[GunReload]`。

## 七、与原版/其他模块的兼容

- 本补丁仅拦截射击事件；
- 与 `CustomGrenadeSounds`、`CustomEnemySounds` 等模块共享同一日志与 FMOD 总线获取策略，互不影响。

## 八、常见问答

- Q：如何知道我的武器对应的 `soundkey`？
  - A：在 Debug 日志中会打印拦截到的 `eventName`，其中 `Shoot/{soundkey}` 的最后一段即为 `soundkey`；也可参考游戏资源或解包信息。
 - Q：能否为同一武器提供多种变体并随机播放？
   - A：支持。为基准文件添加 `_1`、`_2` 等同扩展名差分即可。

