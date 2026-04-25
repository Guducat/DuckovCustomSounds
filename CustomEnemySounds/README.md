# CustomEnemySounds 使用说明（敌人语音自定义）

本模块用于替换游戏内“敌人语音（叫喊/惊呼/扔雷提示/死亡等）”为自定义音频，并在不破坏原有3D听感的前提下，按规则选择合适的文件进行播放。

—

## 功能范围
- 敌人语音替换：normal、surprise、grenade、death 等常见语音键（soundKey）。
- 分群体与个体：可按阵营（scav/pmc/player…）、强度（normal/elite/boss）以及具体 NameKey（如 Cname_Scav）精确匹配。
- 变体与稳定性：支持 base_1/base_2… 多版本；可按“同一只敌人固定变体”或“随机变体”两种策略切换。
- 优先级与打断：更紧急的语音（如 death）会中断较低优先级的语音（如 normal）。
- 兼容原事件3D参数：默认继承原FMOD事件的3D衰减范围，维持空间感一致。

—

## 目录与命名（推荐）
资源根目录：`DuckovCustomSounds/CustomEnemySounds/`

- 推荐按 NameKey 或按阵营建立子目录，再用“图标等级 + 声线 + 语音键”的三段式文件名：
  - 模板：`{iconPrefix}_{voiceType}_{soundKey}{ext}`
  - 其中：
    - `iconPrefix`：normal / elite / boss（若不清楚，默认用 normal 即可）
    - `voiceType`：建议使用小写，常见如 duck / robot / scav / usec；对 Scav/Usec 等，模块会优先尝试从 NameKey 推断对应的 voiceType（例如 Cname_Scav → “scav”）。
    - `soundKey`：normal / surprise / grenade / death …
    - `ext`：默认按 `voice_rules.json` 中的 PreferredExtensions 顺序尝试（默认 .mp3、.wav；可自行改为包含 .ogg、.flac 等）。

示例（仅示意，实际可按需要增减）：
```
DuckovCustomSounds/
└─ CustomEnemySounds/
   ├─ Scav/
   │  ├─ normal_scav_normal.mp3
   │  ├─ normal_scav_surprise.mp3
   │  ├─ normal_scav_grenade.mp3
   │  └─ normal_scav_death.mp3
   ├─ Usec/
   │  ├─ normal_usec_normal.mp3
   │  └─ normal_usec_surprise.mp3
   └─ voice_rules.json
```

提示：文件名大小写在 Windows 上不敏感，但为可移植性，建议统一小写。

—

## 快速开始
- 将音频文件按上节示例放入 `CustomEnemySounds/`。
- 启动游戏后，若 `CustomEnemySounds/voice_rules.json` 不存在，会自动生成。
- 进入地图靠近或对战对应敌人，验证是否成功触发自定义语音。
- 若未生效，参考“调试与日志”“常见问题”排查。

—

## 事件与 soundKey 约定
- `normal`：一般语音（站桩/搜索/注意等）
- `surprise`：遭遇/受惊/发现威胁（带距离与视野判定，过远时会抑制）
- `grenade`：听到手雷落地并在一定距离内（默认 10m，可改）
- `death`：死亡时触发

说明：模块会拦截 `AudioObject.PostQuak(soundKey)` 并按规则改播自定义文件；若未匹配则保留原声。未知的 soundKey 会按普通优先级处理并尽力匹配同名文件，如无则退回原声。

—

## 变体机制（多版本随机/绑定）
- 任意文件都可添加后缀 `_1`、`_2`、… 作为变体：
  - 例如：`normal_scav_surprise.mp3`、`normal_scav_surprise_1.mp3`、`normal_scav_surprise_2.mp3`。
- `voice_rules.json` 中 `BindVariantIndexPerEnemy`：
  - `false`（默认）：每次触发在全部变体中随机挑选。
  - `true`：同一只敌人固定挑选同一个变体，保证一致性。

—

## 优先级与打断
- 内置优先级（从高到低）：`death` > `surprise` > `grenade` > `normal`；未知键默认为普通优先级。
- 同一只敌人有新语音到来时，若新语音优先级更高，将中断当前较低优先级的播放（可在配置中关闭）。

—

## 进阶：voice_rules.json（规则与路径）
文件：`CustomEnemySounds/voice_rules.json`（首次运行自动生成）。关键字段：

- `Debug`
  - `Enabled`：是否输出模块日志
  - `Level`：Error/Warning/Info/Debug/Verbose
  - `ValidateFileExists`：true 时在播放前实际检查文件存在

- `Fallback`
  - `UseOriginalWhenMissing`：找不到自定义文件时是否保留原声（建议 true）
  - `PreferredExtensions`：扩展名优先顺序，例 `[".mp3", ".wav"]`（可改为加入 `.ogg`, `.flac` 等）

- `DefaultPattern`（默认模板）
  - 形如：`CustomEnemySounds/{team}/{rank}_{voiceType}_{soundKey}{ext}`
  - 令牌说明：
    - `{team}`：scav/pmc/player/unknown
    - `{rank}`：normal/elite/boss（由图标或血量估算）
    - `{voiceType}`：敌人的声线（如 duck/robot/scav/usec…）
    - `{soundKey}`：normal/surprise/grenade/death…
    - `{ext}`：扩展名（按 PreferredExtensions 依次尝试）
  - 回退：若模板中含 `_{soundKey}`，模块会在找不到对应文件时自动尝试“去掉 soundKey”的同名文件，等效于“通配符”，例如：
    - 首先尝试 `normal_scav_surprise.mp3`，找不到则尝试 `normal_scav.mp3`。

- `UseSimpleRules` 与 `SimpleRules`（默认启用，推荐给资源包作者）
  - 用更直观的“根目录 + 标准命名”实现：
    - 填写 `FilePattern` 为根目录（如 `CustomEnemySounds/Scav`），实际匹配模板固定为：
      - `{FilePattern}/{iconPrefix}_{voiceType}_{soundKey}{ext}`。
    - `iconPrefix`：为空则默认 `normal`，也可指定 `elite`/`boss`。
    - `NameKey`：指定某个具体敌人（如 `"Cname_Scav"`）；
    - `Team`：当游戏内敌人没有 NameKey 时，可用阵营匹配（如 `"scav"`）。
    - `voiceType`：优先从 NameKey 提取（如 Cname_Scav → "scav"），否则回退为该单位当前 voiceType。

- `Rules`（进阶用户用，按条件匹配）
  - 可按 `Team`、`IconType`（支持 normal/elite/boss 或原始图标名）、`Min/MaxHealth`、`NameKeyContains`、`SoundKeys` 等维度设定，`FilePattern` 可覆盖为任意模板。

- 其它
  - `PriorityInterruptEnabled`：是否启用优先级打断（默认 true）
  - `BindVariantIndexPerEnemy`：是否对同一只敌人固定变体（默认 false）
  - `MinCooldownSeconds`：预留字段，当前语音模块忽略（用于足音模块）。

—

## 全局设置：settings.json
文件：`DuckovCustomSounds/settings.json`。与本模块相关的常用键：

- 频率控制
  - `deathVoiceFrequency`：死亡语音频率  
    可写 `"always"`、秒数（数字或字符串，如 6 或 "6.0f"）、`"off"`/`false`，默认 `"always"`。
  - `npcGrenadeSurprisedFrequency`：NPC听到手雷的提示频率（全局节流）  
    同上，默认 `"always"`。
  - `npcGrenadeSurprisedMaxDistance`：NPC对手雷做出提示的最大距离（米），默认 10.0，可设 5–50。

- 触发模式
  - `enableNPCtoNPCCombatVoices`：是否允许 NPC 对 NPC 的战斗也触发语音（默认 true）。
  - `enemyVoiceTriggerMode`：`Original` | `PlayerOnly` | `Hybrid`
    - `Original`：与原版一致，只要有战斗/行为就触发。
    - `PlayerOnly`：只与玩家有关时才触发（含距离、视野、交战等判定，远处无关的“惊呼”等会抑制）。
    - `Hybrid`：距离玩家较近时按 `PlayerOnly`；较远时退回 `Original`，兼顾临场感与安静度。
  - 以上选项也可在 ModConfig UI 中动态切换（EnemyVoiceMod 兼容项）。

—

## 调试与日志
- 将 `voice_rules.json` 的 `Debug.Enabled` 设为 `true`，并把 `Level` 设为 `Info`/`Debug`/`Verbose` 以查看匹配细节。
- `ValidateFileExists=true` 时，会在日志中打印尝试过的完整路径，便于排错。
- 模块也支持全局文件开关：在 `DuckovCustomSounds/` 下放置 `debug_off` 或 `.nolog` 可快速降低日志噪声。

—

## 常见问题（FAQ）
- 文件命名了但不生效？
  - 检查路径是否与 `voice_rules.json` 的模板一致；扩展名是否在 `PreferredExtensions` 中；启用 `Debug` 并观察尝试路径。
- 想用 ogg/flac？
  - 修改 `PreferredExtensions`，把 `.ogg`、`.flac` 加进去即可。
- 想给 Boss 专用语音？
  - 使用 `SimpleRules` 针对具体 NameKey（如 `"Cname_Boss_..."`）建立根目录；或在 `Rules` 中指定 `IconType="boss"` 并设置专用 `FilePattern`。
- 声音太吵/信息量过载？
  - 把 `enemyVoiceTriggerMode` 设为 `PlayerOnly` 或 `Hybrid`；或调高 `npcGrenadeSurprisedFrequency` 的秒数。
- 同一只敌人每次随机用不同版本，不够稳定？
  - 将 `BindVariantIndexPerEnemy` 设为 `true`，使变体对同一只敌人固定。
- 想要“通配所有事件”的一条语音？
  - 可省略文件名里的 `_{soundKey}` 段，例如仅放 `normal_scav.mp3`，模块会在找不到更精确文件时回退使用它。

—

## 示例清单（可直接照抄改名）
```
DuckovCustomSounds/
└─ CustomEnemySounds/
   ├─ Scav/
   │  ├─ normal_scav_normal.mp3
   │  ├─ normal_scav_surprise.mp3
   │  ├─ normal_scav_grenade.mp3
   │  └─ normal_scav_death.mp3
   ├─ Usec/
   │  ├─ normal_usec_normal.mp3
   │  └─ normal_usec_surprise.mp3
   └─ voice_rules.json
```

