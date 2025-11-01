import { defineConfig } from 'vitepress'

export default defineConfig({
  title: '鸭科夫自定义音效音乐|Duckov Custom Sounds',
  description: '逃离鸭科夫音乐Mod文档，音频自定义与声音包系统',
  base: process.env.VITEPRESS_BASE || '/DuckovCustomSounds/',
  lastUpdated: true,
  cleanUrls: true,
  head: [
    ['link', { rel: 'icon', href: 'https://github.com/Guducat/DuckovCustomSounds/raw/v1.x/docs/.vitepress/public/favicon.png' }]
  ],
  locales: {
    root: { label: '简体中文', lang: 'zh-CN' },
    en: { label: 'English', lang: 'en-US', link: '/en/' }
  },
  themeConfig: {
    logo: { src: 'https://github.com/Guducat/DuckovCustomSounds/raw/v1.x/docs/.vitepress/public/DuckovCustomSounds.png', alt: 'DuckovCustomSounds' },
    
    // 1. 将所有语言共享的配置移到根级别
    socialLinks: [{ icon: 'github', link: 'https://github.com/Guducat/DuckovCustomSounds' }],
    outline: { level: [2, 3] },
    
    // 1. 为 root 语言（中文）添加搜索框的中文翻译
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

    // 2. 移除根级别的 nav 和 sidebar，因为它们在 locales.root 中被完整定义
    // 上一条注释是错的。现在更正：
    // 以下是 root 语言（简体中文）的配置
    siteTitle: '鸭科夫自定义音效音乐Mod',
    nav: [
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
          { text: '物品', link: '/modules/items' },
          { text: '击杀反馈', link: '/modules/kill-feedback' }
        ]
      },
      {
        text: '工具',
        items: [
          { text: '工具概览', link: '/tools/' }
        ]
      },
      {
        text: '高级',
        items: [
          { text: 'ModConfig 选项', link: '/advanced/modconfig' },
          { text: '日志与排错', link: '/advanced/logging-troubleshooting' },
          { text: '更新日志', link: '/changelog' }
        ]
      },
      { text: 'GitHub', link: 'https://github.com/Guducat/DuckovCustomSounds' }
    ],
    sidebar: {
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
            { text: '物品', link: '/modules/items' },
            { text: '击杀反馈', link: '/modules/kill-feedback' }
          ]
        }
      ],
      '/tools/': [
        { text: '工具', items: [
          { text: '工具概览', link: '/tools/' },
          { text: '声音包生成器', link: '/tools/sound-pack-generator' }
        ]}
      ]
    },
    editLink: {
      pattern: 'https://github.com/Guducat/DuckovCustomSounds/edit/v1.x/docs/:path',
      text: '在 GitHub 上编辑此页'
    },
    lastUpdated: { text: '最后更新' },

    locales: {
      // 3. 删除了 locales.root，因为它现在是 themeConfig 根配置
      
      en: {
        siteTitle: 'Duckov Custom Sounds Mod',
        nav: [
          { text: 'Guide', link: '/en/' },
          {
            text: 'Modules',
            items: [
              { text: 'Sound Pack', link: '/en/modules/soundpack' },
              {
                text: 'BGM',
                items: [
                  { text: 'Overview', link: '/en/modules/bgm/overview' },
                  { text: 'Boss BGM', link: '/en/modules/bgm/boss-bgm' },
                  { text: 'Scene BGM', link: '/en/modules/bgm/scene-bgm' },
                  { text: 'Title BGM', link: '/en/modules/bgm/title-bgm' },
                  { text: 'Home BGM', link: '/en/modules/bgm/home-bgm' },
                  { text: 'Extraction BGM', link: '/en/modules/bgm/extraction-bgm' }
                ]
              }
            ]
          },
          { text: 'GitHub', link: 'https://github.com/Guducat/DuckovCustomSounds' }
        ],
        sidebar: {
          '/en/modules/': [
            { text: 'Sound Pack', items: [{ text: 'Sound Pack System', link: '/en/modules/soundpack' }] },
            {
              text: 'BGM',
              items: [
                { text: 'Overview', link: '/en/modules/bgm/overview' },
                { text: 'Boss BGM', link: '/en/modules/bgm/boss-bgm' },
                { text: 'Scene BGM', link: '/en/modules/bgm/scene-bgm' },
                { text: 'Title BGM', link: '/en/modules/bgm/title-bgm' },
                { text: 'Home BGM', link: '/en/modules/bgm/home-bgm' },
                { text: 'Extraction BGM', link: '/en/modules/bgm/extraction-bgm' }
              ]
            }
          ]
        },
        editLink: {
          pattern: 'https://github.com/Guducat/DuckovCustomSounds/edit/v1.x/docs/:path',
          text: 'Edit this page on GitHub'
        },
        lastUpdated: { text: 'Last updated' },
        
        // 2. 为 en 语言（英文）添加独立的 search 配置
        // 这样它就不会继承根级别（中文）的翻译
        search: {
          provider: 'local',
          options: {
            // 这里可以留空，VitePress 会使用默认的英文翻译
            // 或者显式提供英文翻译（但通常没必要）
            // translations: {
            //   button: {
            //     placeholder: 'Search (Ctrl+K)'
            //   }
            // }
          }
        }
      }
    }
  }
})
