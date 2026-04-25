<script setup lang="ts">
import { onMounted, onBeforeUnmount, ref, watch, nextTick } from 'vue'

const props = withDefaults(defineProps<{
  option: any
  theme?: string | object
  width?: number | string
  height?: number | string
  renderer?: 'canvas' | 'svg'
}>(), {
  renderer: 'canvas',
  width: '100%',
  height: 360
})

const el = ref<HTMLElement | null>(null)
let chart: any = null
let ro: ResizeObserver | null = null
let api: { init: any, use: any } | null = null

async function init() {
  if (!el.value) return
  // 按需引入（devDependencies 安装的 ECharts）
  const core = await import('echarts/core')
  const charts = await import('echarts/charts')
  const comps = await import('echarts/components')
  const renderers = await import('echarts/renderers')

  core.use([
    comps.TooltipComponent,
    comps.GridComponent,
    comps.LegendComponent,
    comps.TitleComponent,
    comps.DatasetComponent,
    charts.BarChart,
    charts.LineChart,
    charts.PieChart,
    charts.RadarChart,
    props.renderer === 'svg' ? renderers.SVGRenderer : renderers.CanvasRenderer
  ] as any)

  api = core as any
  const style: any = el.value.style
  style.width = typeof props.width === 'number' ? `${props.width}px` : props.width
  style.height = typeof props.height === 'number' ? `${props.height}px` : props.height
  chart = (core as any).init(el.value, props.theme, { renderer: props.renderer })
  chart.setOption(props.option || {}, true)
  ro = new ResizeObserver(() => chart?.resize?.())
  ro.observe(el.value)
}

onMounted(async () => {
  await nextTick()
  try { await init() } catch (e) { console.warn('[EChart] load failed', e) }
})

onBeforeUnmount(() => {
  try { ro?.disconnect() } catch {}
  try { chart?.dispose?.() } catch {}
  ro = null
  chart = null
  api = null
})

watch(() => props.option, (val) => {
  if (chart && val) chart.setOption(val, true)
}, { deep: true })
</script>

<template>
  <div class="echart-root" ref="el" />
  <p v-if="!option" style="font-size:12px;color:var(--dcs-muted)">未提供 ECharts 选项（option）。</p>
</template>

<style scoped>
.echart-root {
  width: 100%;
  min-height: 240px;
}
</style>
