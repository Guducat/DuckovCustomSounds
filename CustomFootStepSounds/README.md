## CustomFootStepSounds 使用说明（脚步/冲刺音效自定义）

本模块用于替换角色脚步与冲刺音效，复用 CustomEnemySounds 的角色识别与规则系统（EnemyContext / EnemyContextRegistry / VoiceRuleEngine），同时与原版 FMOD 事件的 3D 衰减保持一致。

- 配置文件：DuckovCustomSounds/CustomFootStepSounds/footstep_voice_rule.json（首次运行自动生成示例）
- 资源根目录建议：DuckovCustomSounds/CustomFootStepSounds/
- 总开关：DuckovCustomSounds/settings.json → enableCustomFootStepSounds（默认 true）
- 日志：settings.json → logging.modules.CustomFootStepSounds.level（Error/Warning/Info/Debug/Verbose）

---

### 事件与 soundKey 约定

模块会根据动作与材质生成以下 soundKey，并依次匹配：

- 脚步：
  - 优先：footstep_{move}_{strength}_{material}
  - 回退：footstep_{move}_{strength}
- 冲刺：
  - 优先：dash_{material}
  - 回退：dash

其中：
- move: walk | run
- strength: light | heavy（依据 CharacterSoundMaker.FootStepTypes 判定；walkHeavy/runHeavy → heavy）
- material: organic | mech | danger | nosound（nosound 不会触发自定义）

示例：
- footstep_walk_light_organic、footstep_run_heavy_mech
- footstep_walk_light（当未提供材质版时回退）
- dash_mech、dash

---

### 文件命名与路径模式

本模块沿用语音模块的“模板+令牌”机制构建候选路径：

- 默认模板（footstep_voice_rule.json 中可改）：
  - CustomFootStepSounds/{team}/{rank}_{voiceType}_{soundKey}{ext}
- SimpleRules（简化模式，默认启用）下，若命中某个 NameKey 的规则，使用：
  - {FilePattern}/{iconPrefix}_{voiceType}_{soundKey}{ext}
  - 其中 iconPrefix 为 normal/elite/boss（由 IconType 推导）

支持的令牌：{team}、{rank}、{voiceType}、{soundKey}、{ext}（扩展名会在 PreferredExtensions 列表中依次尝试，如 .mp3、.wav）。

支持“同名变体”：若 base.mp3 存在，且 base_1.mp3、base_2.mp3… 连续存在，将在 [0..N) 中随机选择一个（或按配置绑定至同一敌人）。

---

### 命名建议：使用 normal_scav_... 更精准

在现有资源命名中，Scav 通常以“scav”作为 voiceType 段更易匹配，例如：
- 推荐：Scav/normal_scav_footstep_run_heavy.mp3
- 不推荐：Scav/normal_duck_footstep_run_heavy.mp3

原因：
- 很多 Scav 敌人的 NameKey 形如 Cname_Scav。本模块的 SimpleRules 会从 NameKey 的后半段（"Scav"）提取一个“备用 voiceType 字符串”，优先用于替换 {voiceType}。
- 因此只要命中 SimpleRules，就会优先尝试 normal_scav_{soundKey} 的文件命名；若未命中再回退到原始 voiceType（如 duck）。

Quick check：
- 对 Scav，路径候选顺序将包含：CustomFootStepSounds/Scav/normal_scav_{soundKey}{ext} → CustomFootStepSounds/Scav/normal_duck_{soundKey}{ext} → …

---

### Quick Start（最小可用）

1) 放置资源（建议遵循上文命名）：
- DuckovCustomSounds/CustomFootStepSounds/Scav/normal_scav_footstep_walk_light.mp3
- DuckovCustomSounds/CustomFootStepSounds/Scav/normal_scav_footstep_run_heavy_mech.wav
- DuckovCustomSounds/CustomFootStepSounds/Scav/normal_scav_dash.mp3

2) 简化规则（footstep_voice_rule.json 自动示例已包含）：
- SimpleRules 数组包含：
  - { NameKey: "Cname_Scav", FilePattern: "CustomFootStepSounds/Scav" }
- 命中后模板为：{FilePattern}/{iconPrefix}_{voiceType}_{soundKey}{ext}
  - iconPrefix: 默认 normal；若 IconType 为 Elite/Boss 会自动变更
  - voiceType: 优先使用从 NameKey 提取的 "Scav"（不要求属于枚举），从而匹配 normal_scav_...

3) 不使用简化规则时（UseSimpleRules=false）：
- 可以在 Rules 中为 team=Scav 指定 FilePattern=CustomFootStepSounds/Scav/{rank}_scav_{soundKey}{ext}，显式把 {voiceType} 固定为 "scav"。

---

### footstep_voice_rule.json 结构说明（概览）

- Debug：
  - Enabled（bool）：是否打印规则与路径调试信息
  - Level（string）：日志级别（Error/Warning/Info/Debug/Verbose）
  - ValidateFileExists（bool）：路由前是否检查文件是否存在（建议 true）
- Fallback：
  - UseOriginalWhenMissing（bool）：未命中时是否使用原声（建议 true）
  - PreferredExtensions（string[]）：扩展名优先级（如 [".mp3", ".wav"]）
- DefaultPattern（string）：默认模板
- UseSimpleRules（bool）：是否启用简化规则（默认 true）
- SimpleRules（数组）：按 NameKey（与可选 IconType）绑定目录根 FilePattern
- PriorityInterruptEnabled（bool）：是否启用优先级打断（脚步默认 false）
- BindVariantIndexPerEnemy（bool）：是否将变体索引绑定到同一敌人（脚步默认 false）
- Rules（数组）：复杂规则（可按 team、IconType、HP 区间、NameKeyContains、SoundKeys 等筛选并指定 FilePattern）

---

### 示例：为 Scav 使用 normal_scav_ 前缀

1) 简化模式（推荐，匹配度高且配置少）：
```json
{
  "UseSimpleRules": true,
  "SimpleRules": [
    { "NameKey": "Cname_Scav", "IconType": "", "FilePattern": "CustomFootStepSounds/Scav" }
  ],
  "DefaultPattern": "CustomFootStepSounds/{team}/{rank}_{voiceType}_{soundKey}{ext}",
  "Fallback": { "UseOriginalWhenMissing": true, "PreferredExtensions": [".mp3", ".wav"] },
  "PriorityInterruptEnabled": false,
  "BindVariantIndexPerEnemy": false
}
```
- 路由尝试顺序（部分）：
  - CustomFootStepSounds/Scav/normal_scav_{soundKey}.mp3
  - CustomFootStepSounds/Scav/normal_scav_{soundKey}.wav
  - CustomFootStepSounds/Scav/normal_duck_{soundKey}.mp3（回退到原始 voiceType）

2) 复杂规则固定 voiceType="scav"（关闭 SimpleRules）：
```json
{
  "UseSimpleRules": false,
  "Rules": [
    {
      "Team": "scav",
      "IconType": "normal",
      "FilePattern": "CustomFootStepSounds/Scav/{rank}_scav_{soundKey}{ext}"
    }
  ],
  "DefaultPattern": "CustomFootStepSounds/{team}/{rank}_{voiceType}_{soundKey}{ext}"
}
```

---

### 3D 声音与跟随

- 自定义音效通过 FMOD Core API 播放，按原事件的 3D min/max 距离设置；
- 脚步/冲刺独立于语音的跟踪器（FootstepSoundTracker），同一发声者仅保留一个脚步/冲刺声道，避免叠音；
- 命中自定义后会阻止原版同类事件发声（避免叠加）。

---

### 常见问题

- 放置了文件但仍然播放原声？
  - 检查 footstep_voice_rule.json 是否命中（打开 Debug.Enabled=true 查看日志）。
  - 路径是否与候选路径一致（留意大小写与扩展名）。
  - 若使用 SimpleRules，确认 NameKey 正确（例如 Cname_Scav）。

- 想按地图/场景区分？
  - 目前规则支持 team、IconType、HP、NameKeyContains、SoundKeys 等；如需更多上下文维度，可后续扩展 ExternalRouter。

- 想要多个同名随机变体？
  - 放置 base_1.mp3、base_2.mp3…，编号需连续；系统会在基础文件与变体中随机选择。

---

如需扩展更多事件或额外令牌，请提出需求。
