# 日志与排错

本文档介绍 DuckovCustomSounds 的日志系统和常见问题排查方法。

## 日志系统

### 日志文件位置

所有日志文件位于游戏根目录的 `DuckovCustomSounds/logs/` 文件夹中:

```
游戏根目录/
└── DuckovCustomSounds/
    ├── logs/
    │   ├── DuckovCustomSounds_YYYYMMDD_HHMMSS.log  # 主日志
    │   ├── Item_YYYYMMDD_HHMMSS.log                # 物品模块日志
    │   ├── Gun_YYYYMMDD_HHMMSS.log                 # 枪械模块日志
    │   └── ...                                      # 其他模块日志
    ├── settings.json                                # 日志配置文件
    └── ...
```

### 日志级别

日志系统支持 5 个级别(从低到高):

| 级别 | 说明 | 用途 |
|------|------|------|
| Error | 错误 | 严重问题,功能无法正常工作 |
| Warning | 警告 | 潜在问题,功能可能受影响 |
| Info | 信息 | 重要的运行信息 |
| Debug | 调试 | 详细的调试信息 |
| Verbose | 详尽 | 最详细的跟踪信息 |

**级别规则**: 设置某个级别后,会输出该级别及以下所有级别的日志。例如设置为 `Info`,会输出 Error、Warning、Info 三个级别的日志。

### 配置日志级别

#### 方法 1: 使用 settings.json

在 `DuckovCustomSounds/settings.json` 中配置:

```json
{
  "logging": {
    "defaultLevel": "Info",
    "modules": {
      "Core": {
        "level": "Info"
      },
      "CustomItemSounds": {
        "level": "Debug"
      },
      "CustomGunSounds": {
        "level": "Info"
      }
    }
  }
}
```

**配置说明**:
- `defaultLevel`: 所有模块的默认日志级别
- `modules.*.level`: 为特定模块单独设置级别

#### 方法 2: 使用 debug_off 文件

在 `DuckovCustomSounds/` 目录下创建名为 `debug_off` 或 `.nolog` 的空文件,会将所有模块的日志级别限制为 Info(抑制 Debug 和 Verbose 输出)。

**优先级**: `debug_off` 文件的优先级高于 `settings.json` 配置。

### 自动生成配置

如果 `settings.json` 不存在,Mod 会在首次启动时自动生成默认配置(所有模块设为 Info 级别)。

## 常见问题排查

### 音效不播放

#### 症状
游戏中听不到自定义音效,仍然是原版音效或无声。

#### 排查步骤

1. **检查 ModConfig 配置**
   - 打开 ModConfig,确认对应模块的"启用"开关已打开
   - 检查音量设置是否为 0

2. **检查文件是否存在**
   - 确认音频文件已放置在正确的目录
   - 检查文件名是否正确(区分大小写)
   - 支持的格式: `.wav`, `.mp3`, `.ogg`

3. **查看日志**
   - 打开对应模块的日志文件
   - 搜索 "未找到" 或 "not found" 关键词
   - 查看是否有文件路径错误

4. **检查文件格式**
   - 确认音频文件格式正确
   - 尝试使用其他格式(推荐 `.wav`)
   - 检查文件是否损坏

#### 常见原因

- ❌ 文件名拼写错误
- ❌ 文件放在了错误的目录
- ❌ 音频格式不支持
- ❌ ModConfig 中模块未启用
- ❌ 音量设置为 0

### 音量调节无效

#### 症状
在 ModConfig 中调整音量后,实际音量没有变化。

#### 解决方案

**此问题已在 v2.x 版本修复**。如果你使用的是旧版本,请更新到最新版本。

修复的模块:
- ✅ Item (物品)
- ✅ Gun (枪械)
- ✅ Melee (近战)
- ✅ Grenade (手雷)

### 日志文件过大

#### 症状
日志文件占用大量磁盘空间。

#### 解决方案

1. **降低日志级别**
   - 将不需要调试的模块设为 `Info` 或 `Warning`
   - 在 `settings.json` 中调整 `defaultLevel`

2. **启用 debug_off**
   - 创建 `DuckovCustomSounds/debug_off` 文件
   - 这会抑制所有 Debug 和 Verbose 输出

3. **定期清理**
   - 手动删除旧的日志文件
   - 日志文件按日期命名,可以安全删除旧文件

### ModConfig 配置不保存

#### 症状
在 ModConfig 中修改配置后,重启游戏配置恢复为默认值。

#### 排查步骤

1. **检查文件权限**
   - 确认游戏目录有写入权限
   - 检查 `ModConfig.ES3` 文件是否存在

2. **检查磁盘空间**
   - 确认磁盘有足够的剩余空间

3. **查看日志**
   - 搜索 "ES3" 或 "save" 关键词
   - 查看是否有保存失败的错误信息

### 音效延迟或卡顿

#### 症状
音效播放有明显延迟,或游戏出现卡顿。

#### 可能原因

1. **音频文件过大**
   - 建议单个音频文件不超过 5MB
   - 使用压缩格式(如 `.ogg`)

2. **音频格式问题**
   - 某些高比特率的音频可能导致性能问题
   - 建议使用 44.1kHz 采样率

3. **硬盘读取速度**
   - 如果游戏安装在机械硬盘上,可能出现读取延迟
   - 建议将游戏安装在 SSD 上

### 特定场景音效异常

#### 症状
某些特定场景或情况下音效不正常。

#### 排查方法

1. **启用 Debug 日志**
   ```json
   {
     "logging": {
       "modules": {
         "CustomItemSounds": {
           "level": "Debug"
         }
       }
     }
   }
   ```

2. **重现问题**
   - 在游戏中重现问题场景
   - 记录触发问题的具体操作

3. **分析日志**
   - 查看 Debug 日志中的详细信息
   - 搜索错误或警告信息

4. **报告问题**
   - 将日志文件和问题描述发送到 QQ 群: 979203137

## 调试技巧

### 启用详细日志

对于需要深入调试的模块,可以启用 Verbose 级别:

```json
{
  "logging": {
    "modules": {
      "CustomItemSounds": {
        "level": "Verbose"
      }
    }
  }
}
```

**注意**: Verbose 级别会产生大量日志,仅在调试时使用。

### 查看音频加载过程

在 Debug 或 Verbose 级别下,日志会显示:
- 音频文件查找路径
- 文件是否存在
- 最终使用的文件
- 音量应用情况

示例日志:
```
[ItemUse] 查找顺序: [food_1.wav] → [food.wav] → [default.wav]
[ItemUse] TypeID=123, soundKey=food, 使用: food.wav
[ItemUse] 已应用音量: 1.50
```

### 测试音效包

使用音效包时,可以通过日志确认:
- 音效包是否正确加载
- 使用的是哪个音效包
- 音效包中的文件是否被正确识别

## 性能优化建议

### 音频文件优化

1. **格式选择**
   - 短音效(< 5秒): 使用 `.wav` (无损,加载快)
   - 长音效(> 5秒): 使用 `.ogg` (压缩,节省空间)
   - 避免使用高比特率的 `.mp3`

2. **采样率**
   - 推荐: 44.1kHz
   - 避免: 96kHz 或更高(过度浪费)

3. **声道**
   - 3D 音效: 单声道(Mono)
   - 2D 音效: 立体声(Stereo)

### 日志优化

1. **生产环境**
   - 使用 `Info` 级别
   - 启用 `debug_off` 文件

2. **开发/调试**
   - 仅对需要调试的模块启用 `Debug` 或 `Verbose`
   - 其他模块保持 `Info`

## 获取帮助

如果以上方法无法解决你的问题:

1. **收集信息**
   - 相关模块的日志文件
   - 问题的详细描述和重现步骤
   - 你的 Mod 版本和游戏版本

2. **联系支持**
   - QQ 群: 979203137 (鸭科夫自定义音效mod交流反馈群)
   - GitHub Issues: [DuckovCustomSounds](https://github.com/Guducat/DuckovCustomSounds)

3. **提供日志**
   - 将相关日志文件打包
   - 说明问题发生的时间点
   - 描述你尝试过的解决方法

