# 自定义枪械音效（CustomGunSounds）

本模块允许你为游戏中的“射击（Shoot）”音效提供自定义替换。当前版本：
- 已启用：射击（Shoot）事件的完整替换（Harmony 补丁已启用）
- 暂未启用：换弹（Reload）、上膛（Chamber）等其他事件（仅提供调试方法模板，默认不打补丁）

## 一、放置音频文件

在 Mod 根目录（DuckovCustomSounds）下创建/使用目录：

```
DuckovCustomSounds/
└─ CustomGunSounds/
   ├─ default.mp3            # 可选：通用射击音效（当找不到更精确的映射时使用）
   ├─ ak47.mp3               # 示例：与游戏事件中的 soundkey 匹配
   └─ m4a1.mp3               # 示例：与游戏事件中的 soundkey 匹配
```

- 文件格式：建议使用 .mp3（已在补丁中默认查找 .mp3）；如需支持其他格式，可在代码中补充。
- 命名规则：当游戏触发 `SFX/Combat/Gun/Shoot/{soundkey}` 时，会优先查找 `CustomGunSounds/{soundkey}.mp3`；
  若不存在，则回退到 `CustomGunSounds/default.mp3`（若也不存在，则放行原始 FMOD 事件）。

## 二、工作原理（简述）

- 补丁目标：`AudioManager.Post(string eventName, GameObject gameObject)`
- 拦截范围：仅当 `eventName` 以 `SFX/Combat/Gun/Shoot/` 开头时拦截
- 替换逻辑：
  1. 解析 `soundkey`
  2. 在 `CustomGunSounds/` 中查找 `{soundkey}.mp3`，否则使用 `default.mp3`
  3. 使用 FMOD Core API 以 3D 方式播放，路由至 `SFX → Master`
  4. 阻止原始 FMOD 事件播放

## 三、距离与路由

- 3D 最小/最大距离默认设置为 `1f / 50f`；如需调整，可修改 `CustomGunSounds_Patches.cs` 中的 `set3DMinMaxDistance` 调用。
- 音频路由：优先绑定到 `bus:/Master/SFX` 的 ChannelGroup，不存在时回退到 `bus:/SFX`，最后回退到 Master。

## 四、日志与排错

- 模块名：`CustomGunSounds`
- 日志级别可在 `DuckovCustomSounds/settings.json` 中配置（由统一 `Logging.LogManager` 管理）。
- 常见日志：
  - `[GunShoot] 替换 ... → ...`：已成功替换并播放自定义音效
  - `FMOD 未初始化`：大多发生在加载早期，通常会自动恢复
  - `createSound / playSound 失败`：检查文件路径与格式

## 五、其他事件（Reload/Chamber 等）

- 代码中已提供调试模板方法（仅输出 Debug 日志），默认未加 HarmonyPatch 特性，因此不会被打补丁。
- 如需启用：
  1. 在 `GunReloadPatch_Template` 或 `GunChamberPatch_Template` 上方取消注释 `[HarmonyPatch(...)]` 特性
  2. 重新打包/加载 Mod
  3. 根据实际事件名（如 `SFX/Combat/Gun/Reload/{soundkey}`）补充替换逻辑

## 六、与原版/其他模块的兼容

- 本补丁仅拦截射击事件；
- 与 `CustomGrenadeSounds`、`CustomEnemySounds` 等模块共享同一日志与 FMOD 总线获取策略，互不影响。

## 七、常见问答

- Q：如何知道我的武器对应的 `soundkey`？
  - A：在 Debug 日志中会打印拦截到的 `eventName`，其中 `Shoot/{soundkey}` 的最后一段即为 `soundkey`；也可参考游戏资源或解包信息。
- Q：能否为同一武器提供多种变体并随机播放？
  - A：当前版本未内置该功能，可在补丁中自行扩展（例如在 `CustomGunSounds/{soundkey}/` 目录随机挑选）。

