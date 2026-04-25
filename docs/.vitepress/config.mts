import { defineConfig } from 'vitepress'
import type { DefaultTheme } from 'vitepress'

const githubRepo = 'https://github.com/Guducat/DuckovCustomSounds'
const editLinkPattern = `${githubRepo}/edit/v2.x/docs/:path`

const sharedThemeConfig: DefaultTheme.Config = {
  logo: {
    src: `${githubRepo}/raw/v2.x/docs/.vitepress/public/DuckovCustomSounds.png`,
    alt: 'DuckovCustomSounds'
  },
  socialLinks: [{ icon: 'github', link: githubRepo }],
  outline: { level: [2, 3] }
}

const rootNav: DefaultTheme.NavItem[] = [
  { text: '指南', link: '/guide/quickstart' },
  {
    text: '模块',
    items: [
      { text: '声音包', link: '/modules/soundpack' },
      {
        text: 'BGM',
        items: [
          { text: '概览', link: '/modules/bgm/overview' },
          { text: 'Boss BGM', link: '/modules/bgm/boss-bgm' },
          { text: '加载界面/加载完成 BGM', link: '/modules/bgm/loading-bgm' },
          { text: '场景 BGM', link: '/modules/bgm/scene-bgm' },
          { text: '标题 BGM', link: '/modules/bgm/title-bgm' },
          { text: '地堡留声机 BGM', link: '/modules/bgm/home-bgm' },
          { text: '撤离 BGM', link: '/modules/bgm/extraction-bgm' }
        ]
      },
      { text: '敌人语音', link: '/modules/enemy-voices' },
      { text: '脚步声', link: '/modules/footsteps' },
      { text: '枪械', link: '/modules/guns' },
      { text: '近战', link: '/modules/melee' },
      { text: '手雷', link: '/modules/grenade' },
      { text: '物品', link: '/modules/items' }
    ]
  },
  {
    text: '工具',
    items: [{ text: '工具概览', link: '/tools/' }]
  },
  {
    text: '高级',
    items: [
      { text: 'ModConfig 选项', link: '/advanced/modconfig' },
      { text: '日志与排错', link: '/advanced/logging-troubleshooting' },
      { text: '更新日志', link: '/changelog' }
    ]
  },
  { text: 'GitHub', link: githubRepo }
]

const rootSidebar: DefaultTheme.Sidebar = {
  '/guide/': [{ text: '起步', link: '/guide/quickstart' }],
  '/modules/': [
    { text: '声音包', items: [{ text: '声音包系统', link: '/modules/soundpack' }] },
    {
      text: 'BGM',
      items: [
        { text: '概览', link: '/modules/bgm/overview' },
        { text: 'Boss BGM', link: '/modules/bgm/boss-bgm' },
        { text: '加载界面/加载完成 BGM', link: '/modules/bgm/loading-bgm' },
        { text: '场景 BGM', link: '/modules/bgm/scene-bgm' },
        { text: '标题 BGM', link: '/modules/bgm/title-bgm' },
        { text: '地堡留声机 BGM', link: '/modules/bgm/home-bgm' },
        { text: '撤离 BGM', link: '/modules/bgm/extraction-bgm' }
      ]
    },
    {
      text: '音效模块',
      items: [
        { text: '敌人语音', link: '/modules/enemy-voices' },
        { text: '脚步声', link: '/modules/footsteps' },
        { text: '枪械', link: '/modules/guns' },
        { text: '近战', link: '/modules/melee' },
        { text: '手雷', link: '/modules/grenade' },
        { text: '物品', link: '/modules/items' }
      ]
    }
  ],
  '/tools/': [
    {
      text: '工具',
      items: [
        { text: '工具概览', link: '/tools/' },
        { text: '声音包生成器', link: '/tools/sound-pack-generator' }
      ]
    }
  ]
}

const enNav: DefaultTheme.NavItem[] = [
  { text: 'Guide', link: '/en/guide/quickstart' },
  {
    text: 'Modules',
    items: [
      { text: 'Sound Pack', link: '/en/modules/soundpack' },
      {
        text: 'BGM',
        items: [
          { text: 'Overview', link: '/en/modules/bgm/overview' },
          { text: 'Boss BGM', link: '/en/modules/bgm/boss-bgm' },
          { text: 'Loading / Load Complete BGM', link: '/en/modules/bgm/loading-bgm' },
          { text: 'Scene BGM', link: '/en/modules/bgm/scene-bgm' },
          { text: 'Title BGM', link: '/en/modules/bgm/title-bgm' },
          { text: 'Home Phonograph BGM', link: '/en/modules/bgm/home-bgm' },
          { text: 'Extraction BGM', link: '/en/modules/bgm/extraction-bgm' }
        ]
      },
      { text: 'Enemy Voices', link: '/en/modules/enemy-voices' },
      { text: 'Footsteps', link: '/en/modules/footsteps' },
      { text: 'Guns', link: '/en/modules/guns' },
      { text: 'Melee', link: '/en/modules/melee' },
      { text: 'Grenades', link: '/en/modules/grenade' },
      { text: 'Items', link: '/en/modules/items' }
    ]
  },
  {
    text: 'Tools',
    items: [{ text: 'Tools Overview', link: '/en/tools/' }]
  },
  {
    text: 'Advanced',
    items: [
      { text: 'ModConfig Options', link: '/en/advanced/modconfig' },
      { text: 'Logging & Troubleshooting', link: '/en/advanced/logging-troubleshooting' },
      { text: 'Changelog', link: '/en/changelog' }
    ]
  },
  { text: 'GitHub', link: githubRepo }
]

const enSidebar: DefaultTheme.Sidebar = {
  '/en/guide/': [{ text: 'Quickstart', link: '/en/guide/quickstart' }],
  '/en/modules/': [
    { text: 'Sound Pack', items: [{ text: 'Sound Pack System', link: '/en/modules/soundpack' }] },
    {
      text: 'BGM',
      items: [
        { text: 'Overview', link: '/en/modules/bgm/overview' },
        { text: 'Boss BGM', link: '/en/modules/bgm/boss-bgm' },
        { text: 'Loading / Load Complete BGM', link: '/en/modules/bgm/loading-bgm' },
        { text: 'Scene BGM', link: '/en/modules/bgm/scene-bgm' },
        { text: 'Title BGM', link: '/en/modules/bgm/title-bgm' },
        { text: 'Home Phonograph BGM', link: '/en/modules/bgm/home-bgm' },
        { text: 'Extraction BGM', link: '/en/modules/bgm/extraction-bgm' }
      ]
    },
    {
      text: 'Sound Modules',
      items: [
        { text: 'Enemy Voices', link: '/en/modules/enemy-voices' },
        { text: 'Footsteps', link: '/en/modules/footsteps' },
        { text: 'Guns', link: '/en/modules/guns' },
        { text: 'Melee', link: '/en/modules/melee' },
        { text: 'Grenades', link: '/en/modules/grenade' },
        { text: 'Items', link: '/en/modules/items' }
      ]
    }
  ],
  '/en/tools/': [
    {
      text: 'Tools',
      items: [
        { text: 'Tools Overview', link: '/en/tools/' },
        { text: 'Sound Pack Generator', link: '/en/tools/sound-pack-generator' }
      ]
    }
  ]
}

// Add future languages here, then reference them from top-level locales.
const localeThemeConfig: Record<string, DefaultTheme.Config> = {
  en: {
    siteTitle: 'Duckov Custom Sounds Mod',
    nav: enNav,
    sidebar: enSidebar,
    editLink: {
      pattern: editLinkPattern,
      text: 'Edit this page on GitHub'
    },
    lastUpdated: { text: 'Last updated' },
    search: {
      provider: 'local',
      options: {}
    }
  }
}

export default defineConfig({
  title: '鸭科夫自定义音效音乐|Duckov Custom Sounds',
  description: '逃离鸭科夫音乐Mod文档，音频自定义与声音包系统',
  base: process.env.VITEPRESS_BASE || '/DuckovCustomSounds/',
  lastUpdated: true,
  cleanUrls: true,
  head: [
    ['link', { rel: 'icon', href: `${githubRepo}/raw/v2.x/docs/.vitepress/public/favicon.png` }]
  ],
  locales: {
    root: { label: '简体中文', lang: 'zh-CN' },
    en: {
      label: 'English',
      lang: 'en-US',
      link: '/en/',
      themeConfig: localeThemeConfig.en
    }
  },
  themeConfig: {
    ...sharedThemeConfig,
    search: {
      provider: 'local',
      options: {
        translations: {
          button: {
            placeholder: '搜索 (Ctrl+K)'
          },
          modal: {
            noResults: '未找到相关结果',
            resetButtonTitle: '清除查询条件',
            footer: {
              select: '选择',
              navigate: '切换',
              close: '关闭'
            }
          }
        }
      }
    },
    siteTitle: '鸭科夫自定义音效音乐Mod',
    nav: rootNav,
    sidebar: rootSidebar,
    editLink: {
      pattern: editLinkPattern,
      text: '在 GitHub 上编辑此页'
    },
    lastUpdated: { text: '最后更新' }
  }
})
