---
layout: home
title: 鸭科夫自定义音效音乐Mod
titleTemplate: 逃离鸭科夫音频自定义Mod
hero:
  name: Duckov Custom Sounds
  text: 鸭科夫自定义音效音乐Mod
  tagline: BGM、敌人语音、脚步声、武器、物品与声音包一键切换
  image:
    src: https://github.com/Guducat/DuckovCustomSounds/raw/v2.x/docs/.vitepress/public/DuckovCustomSounds.png
    alt: Duckov Custom Sounds
  actions:
    - theme: brand
      text: 立即上手
      link: /guide/quickstart
    - theme: alt
      text: 声音包系统
      link: /modules/soundpack
features:
  - title: 声音包一键切换
    details: 基于 pack.json 管理多套资源，ModConfig UI 选择，重启后生效。
  - title: BGM 全面定制
    details: 标题/主页/撤离/场景/Boss 全部覆盖，淡入淡出与优先级自动协调。
  - title: 敌人语音规则引擎
    details: 按团队/段位/类型/NameKey 多维匹配，变体绑定与优先级控制。
  - title: 脚步声与动作音效
    details: 支持 walk/run/dash，覆盖全流程；与 FMOD 3D 距离一致，混音可控。
  - title: 武器/手雷/近战/物品
    details: 覆盖 Shoot、Explosive、Melee、Item 使用等关键事件，支持按 soundKey 与 TypeID 匹配。
---

<script setup>
const chartOption = {
  tooltip: { trigger: 'axis' },
  grid: { left: 60, right: 20, top: 20, bottom: 40 },
  xAxis: { type: 'category', data: ['Title','Home','Scene','Boss','Extraction','Voices','Footsteps','Guns','Melee','Grenade','Items'] },
  yAxis: { type: 'value', max: 5 },
  series: [{
    name: '默认支持格式数量',
    type: 'bar',
    data: [1,1,4,4,2,2,2,4,4,4,4],
    itemStyle: { color: '#0ea5e9', borderRadius: [4,4,0,0] }
  }]
}
</script>

<ClientOnly>
  <VideoEmbed provider="bilibili" id="BV11FsszdEMc" cid="33368441073" aid="115431255837354" title="效果展示" />
</ClientOnly>

<ClientOnly>
  <EChart :option="chartOption" :height="320" />
</ClientOnly>

Duckov Custom Sounds 是 Escape from Duckov 的音频扩展 Mod，替换 BGM 和各类音效，支持声音包一键切换整套资源。

从"立即上手"开始，再根据需要阅读对应模块。
