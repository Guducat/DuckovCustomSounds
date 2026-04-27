---
title: 环境音拦截（实验性）
---

# 环境音拦截（实验性）

::: warning 实验性功能
此功能尚未正式支持，默认关闭。可在 ModConfig 中启用；缺少 ModConfig 时，可通过 `settings.json` 手动启用。
:::

## 功能说明

屏蔽所有 `Amb/amb_*` 前缀的游戏环境音事件（风声、虫鸣、环境底噪等），让 BGM / 自定义音效更加突出。

## 启用方式

推荐在游戏内打开 ModConfig：

```text
DCSAmbientIntercept | 环境音拦截 -> 启用环境音拦截（实验性）
DCSAmbientIntercept | 环境音拦截 -> 拦截风暴阶段提示音（实验性）
```

通过 ModConfig 修改后即时生效。

如果 ModConfig 不可用，可编辑 `settings.json`：

```json
{
  "enableAmbientIntercept": true,
  "interceptStormStingers": true
}
```

两个 `settings.json` 字段默认值均为 `false`。手动修改后需重启游戏生效。

## 源码核对

已依据游戏源码核对当前拦截范围：

| 游戏源码 | 行为 |
|----------|------|
| `AudioManager.OnSubSceneLoaded` | 读取 `SubSceneEntry.AmbientSound`，并通过 `ambientSource.Post("Amb/amb_{soundkey}")` 播放场景环境音 |
| `WeatherFxControl` | 默认雨声为 `Amb/amb_rain`，并通过 `AudioManager.Post` 播放 |
| `TimeOfDayController` | 风暴阶段提示音为 `Music/Stinger/stg_storm_1` 与 `Music/Stinger/stg_storm_2`，不属于 `Amb/amb_*`，由独立选项控制 |

本 Mod 在 `AudioObject.Post(string, bool)` 的 Harmony Prefix 中判断事件名，因此能覆盖 `AudioManager` 最终进入 `AudioObject.Post` 的环境音事件。

## 行为

| 规则 | 说明 |
|------|------|
| 拦截范围 | `AudioObject.Post` 收到的 `Amb/amb_*` 前缀环境音事件 |
| 特例放行 | `Amb/amb_storm` 保留放行 |
| 风暴阶段提示音 | `拦截风暴阶段提示音（实验性）` 开启后，额外拦截 `Music/Stinger/stg_storm_1` 与 `Music/Stinger/stg_storm_2` |
| 异常安全 | 发生异常时自动放行原方法，不影响游戏稳定性 |

## 适用场景

- 做 BGM 包时想屏蔽环境底噪，让自定义音乐更干净
- 某些场景环境音过于嘈杂，影响自定义音效辨识度

## 风险提示

- 可能使部分场景过于安静，缺乏氛围感
- 仅建议有明确需求的用户开启

## 相关日志

启用后在 player.log 中可搜索 `[AmbientIntercept]` 查看拦截记录。
