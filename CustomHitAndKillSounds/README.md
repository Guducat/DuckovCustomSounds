# CustomHitAndKillSounds

自定义命中提示、击杀提示、玩家受击和 NPC 受击音效。

## 目录

```text
CustomHitAndKillSounds/
├── hitmarker.mp3
├── hitmarker_head.mp3
├── killmarker.mp3
├── killmarker_head.mp3
├── player_hurt.mp3
├── player_hurt_crit.mp3
├── npc_hurt.mp3
└── npc_hurt_crit.mp3
```

支持 `.mp3`、`.wav`、`.ogg`、`.oga`。同名文件可添加 `_1`、`_2` 后缀作为随机变体。

## 覆盖方式

命中和击杀提示音使用已知原版事件替换：

```text
SFX/Combat/Marker/hitmarker
SFX/Combat/Marker/hitmarker_head
SFX/Combat/Marker/killmarker
SFX/Combat/Marker/killmarker_head
```

受击音效使用 `Health.OnHurt` 和 `HealthSimpleBase.OnSimpleHealthHit` 运行时事件补充。玩家受击使用 `player_hurt*`，玩家造成伤害时目标播放 `npc_hurt*`。

Unity 序列化资源中的部分音频字段无法仅靠反编译 C# 全量确认。开启 `reflectionDiagnostics` 后，模块会通过反射记录疑似音频字段，作为后续补充事件名的依据。
