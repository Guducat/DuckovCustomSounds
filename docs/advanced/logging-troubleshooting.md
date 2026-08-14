# 日志与排错

日志由 Unity 引擎管理，输出到 `player.log`。日志前缀格式：
```
[DuckovCustomSounds][模块][子模块][等级] 内容
```

## 看日志在哪

| 系统 | 路径 |
|------|------|
| Windows | `C:\Users\{用户名}\AppData\LocalLow\TeamSoda\Duckov\player.log` |
| Mac | `~/Library/Logs/Unity/Player.log` |

日志包含 `[DuckovCustomSounds]` 前缀的条目就是本 Mod 的输出。

## 日志级别

5 个级别，从低到高：

| 级别 | 说明 |
|------|------|
| Error | 错误，功能无法正常工作 |
| Warning | 警告，功能可能受影响 |
| Info | 运行信息（默认级别） |
| Debug | 调试详情 |
| Verbose | 最详细的追踪信息 |

设置有某个级别后，会输出该级别及以下所有日志。比如设 Debug，会输出 Error、Warning、Info、Debug。

## 配置日志级别

### 方式一：settings.json

在 `DuckovCustomSounds/settings.json` 里加：

```json
{
  "logging": {
    "enabled": true,
    "defaultLevel": "Info",
    "modules": {
      "Core": { "level": "Info" },
      "Enemy": { "level": "Debug" },
      "Gun": { "level": "Info" }
    }
  }
}
```

- `enabled`：全局总开关，`false` 时所有模块日志完全关闭（默认 true）
- `defaultLevel`：所有模块的默认级别
- `modules.模块名.level`：覆盖某个模块的级别

settings.json 的日志设置与 `debug_off`/`.nolog` 文件均为**热生效**（约 1 秒内），无需重启游戏。

可选模块名：`Core`、`SoundPack`、`Enemy`、`Footstep`、`BGM`、`HomeBGM`、`SceneBGM`、`ExtractionBGM`、`Gun`、`Grenade`、`Item`、`Melee`、`HitAndKill`

### 方式二：debug_off / .nolog 文件

在 `DuckovCustomSounds/` 目录下创建空文件名为 `debug_off` 或 `.nolog`，所有模块的日志级别钳制到 Info（不输出 Debug/Verbose）。

### 方式三：voice_rules.json（仅部分模块）

`voice_rules.json` 和 `footstep_voice_rule.json` 里的 Debug.Level 设置也会同步到日志系统（仅当 settings.json 未对同名模块显式设置时）。其中 `Debug.Enabled=false` 会完全关闭该模块的日志输出。

## 常见问题

**音效不播放**：
1. 确认 ModConfig 对应模块已开启
2. 检查文件在正确目录、文件名正确
3. 看 player.log，搜 `not found` 或 `未找到` 关键词
4. 检查文件格式：`.mp3`、`.wav`、`.ogg`、`.flac`

**音量调节无效**：v2.x 已修复，更新到最新版。

**日志太多**：把 `defaultLevel` 改成 `Warning`，或者放 `debug_off`。

**ModConfig 配置不保存**：检查 `settings.json` 是否被只读，磁盘空间是否足够。

**音效延迟/卡顿**：建议音频文件控制在 5MB 以内、采样率 44.1kHz，优先用压缩格式（`.ogg`）。建议游戏装 SSD。

## 排查方法

1. 把怀疑模块的级别设为 `Debug` 或 `Verbose`
2. 复现问题
3. 打开 player.log 搜对应模块的前缀：`[Enemy]`、`[Gun]`、`[Item]`、`[BGM]` 等
4. 根据日志定位路径错误、文件缺失、格式不支持等问题

## 获取帮助

- QQ 群：979203137
- GitHub：https://github.com/Guducat/DuckovCustomSounds
