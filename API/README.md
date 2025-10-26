# DuckovCustomSounds.API 对外扩展接口

> 命名空间：`DuckovCustomSounds.API`  目录：`API`

本模块为其他 Mod 提供扩展接口，允许：
- 注册外部语音包提供者（IVoicePackProvider）以覆盖本 Mod 的语音路由
- 查询敌人上下文（EnemyContextData）
- 直接播放 3D 音效（Play3D）与按所有者停止（StopByOwner）
- 通过“假事件”前缀 DCS:/ 使用弱依赖集成方式

---

## 快速开始

### 方式一：强依赖（引用 DLL）
1. 在你的 Mod 工程中引用 DuckovCustomSounds 的输出 DLL。
2. 实现 IVoicePackProvider 并在加载时注册：

```csharp
public sealed class MyProvider : DuckovCustomSounds.API.IVoicePackProvider
{
    public bool TryResolve(EnemyContextData ctx, string soundKey, string voiceType, out string fileFullPath)
    {
        fileFullPath = null;
        // 建议使用 ctx.NameKey 作为主要判定维度，避免多个敌人共享 VoiceType 带来的混淆
        if (ctx?.NameKey == "Cname_Scav" && soundKey == "surprise")
        {
            // 相对路径将被解析为 DuckovCustomSounds 根目录下文件
            fileFullPath = "MyMod/voices/scavs_surprise.mp3";
            return true;
        }
        return false;
    }
}

[... 在你的 Mod 启动处 ...]
DuckovCustomSounds.API.CustomModController.RegisterVoicePackProvider("MyMod", new MyProvider());
```

3. 也可以直接播放 3D：

```csharp
var go = ...; // 发声体
var ok = DuckovCustomSounds.API.CustomModController.Play3D(new PlaybackRequest{
    Source = go,
    FileFullPath = "MyMod/sfx/myshot.mp3",
    SoundKey = "normal",
    MinDistance = 1.5f,
    MaxDistance = 25f,
}, out var _);
```

4. 停止当前所有者下的声音：

```csharp
DuckovCustomSounds.API.CustomModController.StopByOwner(go);
```

### 方式二：弱依赖（“假事件”）
无需引用 DLL，直接调用游戏的 `AudioManager.Post` 即可：

```csharp
Duckov.AudioManager.Post("DCS:/MyMod/sfx/myshot.mp3", go);
```

- 以 `DCS:/` 开头表示让 DuckovCustomSounds 代理播放。
- `MyMod/sfx/myshot.mp3` 为相对 DuckovCustomSounds 模组根目录的路径；也支持绝对路径。
- 当前默认使用 SFX 总线、3D 最小/最大距离分别为 1.5/25 米。

> 注意：弱依赖方式不支持回调/句柄，也不参与规则路由，仅适用于“直接播文件”的简单场景。

---

## 接口一览

### IVoicePackProvider
```csharp
public interface IVoicePackProvider
{
    bool TryResolve(EnemyContextData ctx, string soundKey, string voiceType, out string fileFullPath);
}
```
- ctx：敌人上下文快照（见下文）。
- soundKey：语音键（normal/surprise/grenade/death 等）。
- voiceType：字符串形式的 VoiceType，避免对游戏枚举的强耦合。
- fileFullPath：返回要播放的文件路径（相对路径将被解析到 DuckovCustomSounds 根目录）。

注册 / 注销：
```csharp
CustomModController.RegisterVoicePackProvider("MyMod", provider);
CustomModController.UnregisterVoicePackProvider("MyMod");
```

### EnemyContextData
字段：
- InstanceId：发声体 GameObject 的 InstanceID
- Team：队伍（标准化小写）
- Rank：boss/elite/normal
- EnemyType：敌人类别（拍扁后的字符串）
- NameKey：游戏内名称键
- Health：当前生命值快照
- IconType：原始图标类型
- Transform：Transform（用于 3D 跟随）
- IsValid：是否有效

获取方法：
```csharp
if (CustomModController.TryGetEnemyContext(go, out var ctx)) { ... }
```

### 直接播放 API
```csharp
bool ok = CustomModController.Play3D(new PlaybackRequest{
    Source = go,
    FileFullPath = "MyMod/sfx/myshot.mp3",
    SoundKey = "normal", // 影响优先级策略
    MinDistance = 1.5f,
    MaxDistance = 25f,
    FollowTransform = true,
}, out var handle);

CustomModController.StopByOwner(go);
```
- 路由：走 FMOD Core，自动路由到 `bus:/Master/SFX`。
- 优先级：默认按 SoundKey 映射（death>surprise>grenade>normal）。当同一所有者已经在播放较高优先级声音时，会拒绝低优先级的新声音。

---

## 设计说明

- Provider 前置：在内部规则匹配前调用所有已注册的 Provider，若任何一个返回 true，则直接使用外部路径。
- 默认回退：无 Provider 命中时，继续按原有 VoiceRuleEngine 规则与默认模板处理，完全向后兼容。
- 生命周期：所有通过 API 播放的声音都会进入 CoreSoundTracker 进行生命周期管理与 3D 跟随。
- 假事件：通过 Harmony 拦截 `AudioManager.Post(string, GameObject)`，捕获 `DCS:/` 前缀。

---

## 注意事项与最佳实践
- 请保证返回的路径指向本地可读的音频文件（mp3/ogg/flac/wav 等）。mp3/ogg/flac 走流模式，wav 走 sample 模式。
- 请合理选择 `Min/MaxDistance`，避免过大范围带来的性能浪费。
- 如果你的场景会频繁触发相同路径，建议在你自己的 Provider 内做轻量缓存。
- 避免在 Provider 内抛出异常（本 Mod 会吞掉异常，但建议自行保证稳定）。
- 若使用弱依赖假事件，请确保路径拼接正确；失败时事件会被吞掉，不会触发原生 FMOD 事件。

---

## 常见问题（FAQ）
- Q：为什么我返回的相对路径找不到？
  - A：相对路径是相对于 DuckovCustomSounds 模组根目录（同 `voice_rules.json` 所在目录）。
- Q：如何改变优先级？
  - A：设置 `PlaybackRequest.SoundKey`（支持 normal/surprise/grenade/death）；或未来版本将开放自定义数值优先级映射。
- Q：如何只在没有本 Mod 命中时才兜底？
  - A：请在你的 Provider 内部做控制逻辑（例如只有在某些 soundKey/team下才返回 true）。
- Q：弱依赖能不能传更多参数？
  - A：当前版本不支持；如有需要请issues提出具体需求，或改用强依赖方式。

---

## 兼容性
- 默认情况下（未注册任何 Provider、未触发 DCS:/ 假事件）本 Mod 行为完全不变。
- 本 API 仅新增扩展点与可选路径，不改变既有配置加载、BGM、枪械或手雷音效模块的任何默认逻辑。

