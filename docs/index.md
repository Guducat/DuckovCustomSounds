---
layout: home
title: 鸭科夫自定义音效音乐Mod
titleTemplate: 逃离鸭科夫音频自定义Mod
hero:
  name: Duckov Custom Sounds
  text: 鸭科夫自定义音效音乐Mod
  tagline: BGM、敌人语音、脚步声、武器、物品、击杀反馈与声音包一键切换
  image:
    src: https://raw.githubusercontent.com/Guducat/DuckovCustomSounds/refs/heads/v1.x/docs/DuckovCustomSounds.png
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
    details: 基于 pack.json 的多套资源方案，ModConfig UI 选择，重启后生效，想换就换无负担！
  - title: BGM 全面定制
    details: 标题/主页/撤离/场景/Boss 覆盖，淡入淡出与优先级协调，体验感拉满~
  - title: 敌人语音规则引擎
    details: 团队/段位/类型/NameKey 多维匹配，变体绑定与优先级，路径模板灵活扩展。
  - title: 脚步声与动作音效
    details: 支持 walk/run/dash、游戏流程全覆盖；与 FMOD 3D 距离一致，混音可控。
  - title: 武器/手雷/近战/物品
    details: 覆盖Shoot/Explosive/Melee/Item使用等关键事件，按soundKey与TypeID匹配，MOD也能适配！
  - title: 击杀反馈
    details: 连杀判定、UI 显示、2D/3D 声效与音量，图标/文本可定制。
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
  <!-- 使用 B 站视频：替换为你的 bvid/aid/cid 即可 -->
  <VideoEmbed provider="bilibili" id="BV11FsszdEMc" cid="33368441073" aid="115431255837354" title="效果展示" />
</ClientOnly>

<ClientOnly>
  <!-- 使用 ECharts（CDN 动态加载）作为外部图表库 -->
  <EChart :option="chartOption" :height="320" />
</ClientOnly>

欢迎来到 Duckov Custom Sounds 文档站。本项目是 Escape from Duckov 的音频扩展 Mod，提供从 BGM 到各类 SFX 的全覆盖定制能力，并支持“声音包”快速切换整套资源。

建议从“立即上手”开始，随后根据需要阅读对应模块。
