<script setup lang="ts">
import { computed } from 'vue'

type Series = { name: string; data: number[]; color?: string }
const props = withDefaults(defineProps<{
  width?: number
  height?: number
  categories: string[]
  series: Series[]
  max?: number
  padding?: { top: number; right: number; bottom: number; left: number }
}>(), {
  width: 880,
  height: 360,
  max: 5,
  padding: () => ({ top: 24, right: 24, bottom: 40, left: 120 })
})

const colors = ['#0ea5e9', '#22c55e', '#f59e0b', '#ef4444', '#8b5cf6']

const inner = computed(() => {
  const w = props.width - props.padding.left - props.padding.right
  const h = props.height - props.padding.top - props.padding.bottom
  return { w, h }
})

function xScale(v: number) {
  const { w } = inner.value
  return Math.max(0, Math.min(w, (v / props.max) * w))
}

const band = computed(() => {
  const n = props.categories.length
  const { h } = inner.value
  const gap = 8
  const bandH = Math.max(10, (h - gap * (n - 1)) / n)
  return { bandH, gap }
})

function colorAt(i: number) {
  return props.series[i]?.color || colors[i % colors.length]
}
</script>

<template>
  <div class="light-chart">
    <svg :width="width" :height="height" role="img">
      <g :transform="`translate(${padding.left},${padding.top})`">
        <!-- y 轴标签 -->
        <g class="axis">
          <template v-for="(c, i) in categories" :key="i">
            <text :x="-8" :y="i*(band.bandH+band.gap) + band.bandH/2" text-anchor="end" dominant-baseline="middle">
              {{ c }}
            </text>
          </template>
        </g>
        <!-- 网格线 -->
        <g class="axis">
          <template v-for="t in max" :key="t">
            <line :x1="xScale(t)" y1="0" :x2="xScale(t)" :y2="inner.h" stroke-dasharray="2,3" />
            <text :x="xScale(t)" :y="inner.h + 18" text-anchor="middle">{{ t }}</text>
          </template>
        </g>
        <!-- 柱状（多序列并排） -->
        <template v-for="(cat, i) in categories" :key="`row-${i}`">
          <template v-for="(s, si) in series" :key="`bar-${i}-${si}`">
            <rect
              :x="0"
              :y="i*(band.bandH+band.gap) + (si*(band.bandH/series.length))"
              :width="xScale(s.data[i] || 0)"
              :height="(band.bandH/series.length) - 1"
              :fill="colorAt(si)"
              rx="3"
            />
          </template>
        </template>
        <!-- 图例 -->
        <g :transform="`translate(0, -12)`">
          <template v-for="(s, si) in series" :key="`legend-${si}`">
            <rect :x="si*160" y="-16" width="12" height="12" :fill="colorAt(si)" rx="2" />
            <text :x="si*160 + 18" y="-6" fill="#334155" font-size="12">{{ s.name }}</text>
          </template>
        </g>
      </g>
    </svg>
  </div>
  <p style="margin-top:8px;color:var(--dcs-muted);font-size:12px">
    说明：数值为“默认支持的音频格式数量”，部分模块可通过配置扩展额外格式。
  </p>
  </template>

<style scoped>
</style>

