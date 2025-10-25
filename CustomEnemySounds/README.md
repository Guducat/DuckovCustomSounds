# CustomEnemySounds 敌人自定义声音框架（开发者调试指南）

本模块提供一个稳定、可配置的敌人语音/音效自定义框架，并附带完善的调试日志，方便第三方开发者快速集成与排查问题。

## 目录结构与资源命名
- 模组根目录：DuckovCustomSounds/
- 本模块目录：DuckovCustomSounds/CustomEnemySounds/
- 默认路径模板（可在 voice_rules.json 自定义）：
  DuckovCustomSounds/CustomEnemySounds/{team}/{rank}_{voiceType}_{soundKey}{ext}
  例如：DuckovCustomSounds/CustomEnemySounds/scav/normal_Duck_surprise.mp3

- 语音变体命名（支持随机选择）：
  - 基础：normal_Duck_surprise.mp3
  - 变体：normal_Duck_surprise_1.mp3、normal_Duck_surprise_2.mp3、...（编号从 1 开始、必须连续）

## 配置文件（voice_rules.json）
- Debug.Enabled：是否启用模块日志
- Debug.Level：Error / Info / Debug / Verbose
- Debug.ValidateFileExists：播放前是否校验磁盘文件存在
- Fallback.UseOriginalWhenMissing：找不到自定义文件时是否回退原版事件
- Fallback.PreferredExtensions：优先扩展名（如 [".mp3", ".wav"]）
- DefaultPattern 与 Rules：路径模板与规则匹配（支持 {team},{rank},{voiceType},{soundKey},{ext}）

- UseSimpleRules：是否启用简化规则（按 nameKey 直配，跳过复杂 Rules）
- SimpleRules：数组项：NameKey、IconType（可空；空=匹配所有，且文件前缀用 normal_；不空=使用 {IconType}_ 前缀）、FilePattern（目录/路径前缀）
  - 示例：
    ```json
    { "NameKey": "Cname_Scav", "IconType": "", "FilePattern": "CustomEnemySounds/Scav" }
    ```
    对应文件名：CustomEnemySounds/Scav/normal_Duck_death.mp3（或根据 soundKey/voiceType 变化）
- 通配回退：若精确匹配 {iconPrefix}_{voiceType}_{soundKey}{ext} 未命中，将自动回退查找 {iconPrefix}_*_{soundKey}{ext}，便于跨 voiceType 复用同一音频文件


## 语言对照表
| NameKey                   | 中文名称         |
|--------------------------|------------------|
| Cname_Wolf               | 狼               |
| Cname_ShortEagle         | 矮鸭             |
| Cname_UltraMan           | 光之男           |
| Cname_CrazyRob           | 失控机械蜘蛛     |
| Cname_Scav               | 拾荒者           |
| Cname_Usec               | 雇佣兵           |
| Cname_RobSpider          | 机械蜘蛛         |
| Cname_ScavRage           | 暴走拾荒者       |
| Cname_Boss_Sniper        | 劳登             |
| Cname_SpeedyChild        | 急速团成员       |
| Cname_Speedy             | 急速团长         |
| Cname_Merchant_Myst      | 神秘商人         |
| Cname_Prison             | 狱卒             |
| Cname_Boss_Shot          | 喷子             |
| Cname_ServerGuardian     | 矿长             |
| Cname_Raider             | 游荡者           |
| Cname_LabTestObjective   | 测试对象         |
| Cname_StormCreature      | 风暴生物         |
| Cname_XING               | 大兴兴           |
| Cname_XINGS              | 小小兴           |
| Cname_DengWolf           | 劳登的狗         |
| Cname_Mushroom           | 行走菇           |
| Cname_Vida               | 维达             |
| Cname_Roadblock          | 路障             |
| Cname_StormBoss1         | 噗咙噗咙         |
| Cname_StormBoss2         | 咕噜咕噜         |
| Cname_StormBoss3         | 啪啦啪啦         |
| Cname_StormBoss4         | 比利比利         |
| Cname_StormBoss5         | 口口口口         |
| Cname_Boss_Fly           | 蝇蝇队长         |
| Cname_Boss_Fly_Child     | 蝇蝇队员         |
| Cname_Boss_Arcade        | 暴走街机         |
| Cname_BALeader           | BA队长           |
| Cname_BALeader_Child     | 普通BA           |
| Cname_SenorEngineer      | 高级工程师       |
| Cname_Boss_3Shot         | 三枪哥           |
| Cname_3Shot_Child        | 三枪弟           |
| Cname_Grenade            | 炸弹狂人         |
| Cname_SchoolBully        | 校霸             |
| Cname_SchoolBully_Child  | 校友             |
| Cname_RPG                | 迷塞尔           |
| Cname_Drone              | 侦察机           |
| Cname_StormVirus         | 风暴？           |
| Cname_MonsterClimb       | 风暴虫           |
| Cname_Boss_Red           | ???              |
| Cname_Football_1         | 足球队长         |
| Cname_Football_2         | 足球队员         |
| Cname_Prison_Boss        | 典狱长           |
| Cname_StormBoss1_Child   | 噗咙             |

## 日志与调试开关
- 运行时日志前缀：[CustomEnemySounds] 或 [CustomEnemySounds:Debug]
- 快速文件开关（位于模组根目录 DuckovCustomSounds/）：
  - 创建 debug_off 或 .nolog 文件：自动把日志级别下调到 Info，抑制 Debug/Verbose 级别输出
- 关键日志节点：
  - [CES:Hook] AudioObject.PostQuak Postfix ENTER：命中语音播放入口（保留原始生命周期）
  - [CES:Rule]：规则逐项匹配与候选路径打印
  - [CES:Path] cand / fallback-cand：候选路径枚举
  - [CES:Path] 找到 N 个语音变体... / 最终选择文件 -> ...：随机变体选择
  - [CES:Hook] Postfix: 正在静音原始事件（不停止）：原生事件被静音但不停止
  - [CES:Hook] Postfix: createSound / playSound 结果：FMOD Core 播放结果
  - [CES:Core] CoreSoundTracker 已启动 / 回收自定义声音：生命周期跟踪器运行情况
  - 死亡语音：
    - [CES:Hook] Death Postfix ENTER (...)：命中死亡钩子
    - [CES:Hook] Death: 规则匹配结果... / 播放自定义死亡语音 -> ...

## 快速自测流程
1) 将若干 mp3/wav 放入默认路径，例如：CustomEnemySounds/scav/normal_Duck_normal.mp3
2) 在 voice_rules.json 中把 Debug.Enabled=true, Level="Debug", ValidateFileExists=true
3) 进入地图与敌人交战，观察控制台：
   - 应看到 Postfix ENTER、规则匹配、路径解析、createSound/playSound 的日志
4) 测试回退：删除某个文件，确认日志提示回退并仍有原声
5) 测试随机变体：
   - 在同目录放入 normal_Duck_surprise.mp3、normal_Duck_surprise_1.mp3、normal_Duck_surprise_2.mp3
   - 多次触发同一 soundKey，观察 [CES:Path] 变体数量与“最终选择文件”日志是否随机变化

## 实现概览（供开发者参考）
- Harmony 拦截：
  - AudioObject.PostQuak(string) 使用 Postfix（保留生命周期，仅替换输出）
  - 敌人死亡：CharacterMainControl.OnDead(DamageInfo)、Health.Hurt(DamageInfo) Postfix 触发 death 语音
- 生命周期：
  - 原事件保留并被静音（setVolume(0)），避免打断游戏事件管理
  - 自定义声音通过 FMOD Core API 播放，并由 CoreSoundTracker 轮询回收（channel.isPlaying）
- 声道：优先 bus:/Master/SFX 的 ChannelGroup，失败回退 master
- 文件选择：PathBuilder 构造候选路径 → TryResolveExisting 命中基础文件后进行“变体枚举与随机选择”

## 常见排错
- 日志没有打印：
  - 检查 voice_rules.json 的 Debug.Enabled/Level
  - 模组根目录是否存在 debug_off/.nolog 导致等级被下调
- 创建声音成功但无声：
  - 确认 SFX 音量未被拉低；确认日志中是否立即 release（当前版本已使用 CoreSoundTracker 处理）
- 文件不生效：
  - 开启 ValidateFileExists，观察 [CES:Path] 检测结果；确认文件扩展名与 PreferredExtensions 顺序

## 贡献指南
- 提交 PR 前请尽量保持日志级别与风格一致
- 如需新增 Hook 点，请使用 Postfix 优先策略并记录完整日志
- 如果需要扩展 3D 空间化，请在 CoreSoundTracker.Track 后设置 channel 的 3D 属性（目前默认 2D+LOOP_OFF）

