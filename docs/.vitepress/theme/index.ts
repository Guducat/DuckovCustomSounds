import DefaultTheme from 'vitepress/theme'
import type { Theme } from 'vitepress'
import VideoEmbed from '../../components/VideoEmbed.vue'
import LightChart from '../../components/LightChart.vue'
import EChart from '../../components/EChart.vue'
import './styles.css'

export default {
  extends: DefaultTheme,
  enhanceApp({ app }) {
    app.component('VideoEmbed', VideoEmbed)
    app.component('LightChart', LightChart)
    app.component('EChart', EChart)
  }
} satisfies Theme
