<script setup lang="ts">
import { computed } from 'vue'

type Provider = 'youtube' | 'bilibili' | 'url'
const props = defineProps<{
  provider?: Provider
  id?: string            // YouTube: 视频 ID；B站：bvid
  url?: string           // 直链
  title?: string
  start?: number         // YouTube 起始秒
  // Bilibili 额外参数（可选）
  aid?: string | number
  cid?: string | number
  page?: number
  hq?: boolean           // 高画质
  danmaku?: boolean      // 弹幕
  isOutside?: boolean    // 站外播放
}>()

const src = computed(() => {
  const p = props.provider ?? 'url'
  const t = encodeURIComponent(props.title ?? 'Video')
  const start = props.start ? `?start=${props.start}` : ''
  if (p === 'youtube' && props.id) {
    return `https://www.youtube.com/embed/${props.id}${start}`
  }
  if (p === 'bilibili' && props.id) {
    const q = new URLSearchParams()
    q.set('bvid', props.id)
    if (props.aid) q.set('aid', String(props.aid))
    if (props.cid) q.set('cid', String(props.cid))
    if (props.page) q.set('p', String(props.page))
    q.set('high_quality', props.hq === false ? '0' : '1')
    q.set('danmaku', props.danmaku ? '1' : '0')
    q.set('autoplay', '0') // 关闭自动播放
    if (props.isOutside ?? true) q.set('isOutside', 'true')
    return `https://player.bilibili.com/player.html?${q.toString()}`
  }
  return props.url ?? ''
})
</script>

<template>
  <div v-if="src" class="video-container">
    <h3 v-if="title" class="video-title">{{ title }}</h3>
    <div class="video-embed">
      <iframe
        :src="src"
        :title="title || 'Video'"
        allow="clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
        allowfullscreen
      />
    </div>
  </div>
  <p v-else style="color:var(--dcs-muted)">未提供可用的视频地址或参数。</p>
</template>

<style scoped>
.video-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  margin: 2rem auto;
  width: 100%;
}

.video-title {
  font-size: 1.5rem;
  font-weight: 600;
  margin-bottom: 1rem;
  color: var(--vp-c-text-1);
  text-align: center;
}

.video-embed {
  position: relative;
  padding-bottom: 56.25%; /* 16:9 宽高比 */
  height: 0;
  overflow: hidden;
  width: 100%;
  max-width: 800px; /* 设置最大宽度 */
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.video-embed iframe {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  border: none;
  border-radius: 8px;
}
</style>
