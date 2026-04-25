---
layout: home
title: Duckov Custom Sounds
titleTemplate: Audio customization mod for Escape from Duckov
hero:
  name: Duckov Custom Sounds Mod
  tagline: BGM, enemy voices, footsteps, weapons, items & one-click Sound Pack switching
  image:
    src: https://github.com/Guducat/DuckovCustomSounds/raw/v2.x/docs/.vitepress/public/DuckovCustomSounds.png
    alt: Duckov Custom Sounds
  actions:
    - theme: brand
      text: Get Started
      link: /guide/quickstart
    - theme: alt
      text: Sound Pack
      link: /en/modules/soundpack
features:
  - title: One‑click Sound Pack
    details: Multi‑set resources via pack.json. Choose in ModConfig UI, restart to apply.
  - title: Full BGM Customization
    details: Title/Home/Extraction/Scene/Boss fully covered, with auto fade and priority handling.
  - title: Rule‑driven Enemy Voices
    details: Multi‑dimension matching by team/rank/type/NameKey, variant binding and priority control.
  - title: Footsteps & Movement Sounds
    details: Supports walk/run/dash across the full flow; matches FMOD 3D distance, mix volume controllable.
  - title: Weapons / Grenades / Melee / Items
    details: Covers Shoot, Explosive, Melee, and Item usage events, with soundKey and TypeID matching.
---

<script setup>
const bgmFormats = '.mp3, .wav, .ogg, .oga, .flac, .aif, .aiff, .mp2, .m4a, .mp4, .wma, .asf, .fsb, .it, .mid, .midi, .mod, .s3m, .xm'
const sfx4Formats = '.mp3, .wav, .ogg, .oga'
const fmtMap = {
  'Title': bgmFormats,
  'Home': bgmFormats,
  'Scene': bgmFormats,
  'Boss': bgmFormats,
  'Extraction': bgmFormats,
  'Voices': '.mp3, .wav（extendable to .ogg, .flac via voice_rules.json）',
  'Footsteps': '.mp3, .wav（extendable to .ogg, .flac via footsteps.json）',
  'Guns': sfx4Formats,
  'Melee': sfx4Formats,
  'Grenade': sfx4Formats,
  'Items': sfx4Formats,
}
const chartOption = {
  tooltip: {
    trigger: 'axis',
    formatter: (params) => {
      const p = Array.isArray(params) ? params[0] : params
      const name = p.name || ''
      const val = p.value || 0
      const fmts = fmtMap[name] || ''
      return `<strong>${name}</strong><br/>Default supported formats: <strong>${val}</strong><br/><span style="font-size:11px;color:#64748b">Formats: ${fmts}</span>`
    }
  },
  grid: { left: 60, right: 20, top: 20, bottom: 40 },
  xAxis: { type: 'category', data: ['Title','Home','Scene','Boss','Extraction','Voices','Footsteps','Guns','Melee','Grenade','Items'] },
  yAxis: { type: 'value', max: 20 },
  series: [{
    name: 'Default supported formats',
    type: 'bar',
    data: [19,19,19,19,19,2,2,4,4,4,4],
    itemStyle: { color: '#0ea5e9', borderRadius: [4,4,0,0] }
  }]
}
</script>

<ClientOnly>
  <VideoEmbed provider="bilibili" id="BV11FsszdEMc" cid="33368441073" aid="115431255837354" title="Demo" />
</ClientOnly>

<ClientOnly>
  <EChart :option="chartOption" :height="320" />
</ClientOnly>

Duckov Custom Sounds is an audio extension mod for Escape from Duckov, replacing BGM and various sound effects, with one‑click Sound Pack switching for entire resource sets.

Start with "Get Started", then read the relevant module pages as needed.
