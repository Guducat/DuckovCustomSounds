# DuckovCustomSounds.API 对外扩展接口

命名空间：`DuckovCustomSounds.API`

本模块只保留 DuckovCustomSounds 自身提供的扩展能力：

1. 注册外部语音包 Provider，在敌人语音规则解析前提供自定义音频文件。
2. 查询 DuckovCustomSounds 已收集的敌人上下文快照。
3. 保留 `DCS:/` 假事件兼容入口，历史集成仍可运行。

普通自定义音效播放已经由游戏本体提供 `Duckov.AudioManager.PostCustomSFX`。新代码应优先使用游戏本体接口，API 模块只处理游戏本体缺少的语音扩展能力。

## 推荐用法

### 注册外部语音包 Provider

```csharp
public sealed class MyProvider : DuckovCustomSounds.API.IVoicePackProvider
{
    public bool TryResolve(
        DuckovCustomSounds.API.EnemyContextData ctx,
        string soundKey,
        string voiceType,
        out string fileFullPath)
    {
        fileFullPath = string.Empty;

        if (ctx.NameKey == "Cname_Scav" && soundKey == "surprise")
        {
            fileFullPath = "MyMod/voices/scavs_surprise.mp3";
            return true;
        }

        return false;
    }
}

DuckovCustomSounds.API.CustomModController.RegisterVoicePackProvider("MyMod", new MyProvider());
```

相对文件名会按当前声音包根目录解析。Provider 返回 `false` 时，DuckovCustomSounds 会继续执行内部语音规则。

### 查询敌人上下文

```csharp
if (DuckovCustomSounds.API.CustomModController.TryGetEnemyContext(go, out var ctx))
{
    var nameKey = ctx.NameKey;
    var team = ctx.Team;
}
```

`EnemyContextData` 是只读用途的快照 DTO，包含 `InstanceId`、`Team`、`Rank`、`EnemyType`、`NameKey`、`Health`、`IconType`、`Transform`、`IsValid`。

### 播放普通自定义音效

新代码应使用游戏本体接口：

```csharp
Duckov.AudioManager.PostCustomSFX(filePath, go, loop: false);
```

DuckovCustomSounds 不再提供 `Play3D`、`PlaybackRequest`、`StopByOwner` 等播放门面，避免与游戏本体接口重复。

## DCS:/ 兼容入口

历史弱依赖代码仍可使用：

```csharp
Duckov.AudioManager.Post("DCS:/MyMod/sfx/myshot.mp3", go);
```

`DCS:/` 会被 DuckovCustomSounds 拦截，相对文件名按当前声音包根目录解析，最终仍委托给 `Duckov.AudioManager.PostCustomSFX` 播放。

该入口仅用于兼容历史集成。新集成应使用 `AudioManager.PostCustomSFX`。

## 行为边界

Provider 会在内部语音规则之前执行。任意 Provider 命中后，内部规则将使用该音频文件。

Provider 按注册顺序遍历，相同 `modId` 重复注册会替换原 Provider，并保留原注册位置。

Provider 内部异常会被捕获，后续 Provider 与内部规则继续执行。

默认情况下，未注册 Provider 且未触发 `DCS:/` 假事件时，既有声音包规则与游戏原生播放行为保持原样。
