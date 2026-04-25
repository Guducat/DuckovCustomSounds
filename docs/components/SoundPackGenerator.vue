<script setup lang="ts">
import { ref, computed, watch } from 'vue'

// 表单数据
const formData = ref({
  name: '',
  author: '',
  version: '1.0.0',
  description: '',
  compatibleModVersion: '2.0.0',
  requiredModules: [] as string[],
  homepage: '',
  qq: ''
})

// 可用的模块选项
const moduleOptions = [
  { value: 'CustomBGM', label: '自定义BGM' },
  { value: 'CustomFootStepSounds', label: '自定义脚步声' },
  { value: 'CustomEnemySounds', label: '自定义敌人语音' },
  { value: 'CustomGunSounds', label: '自定义枪械声音' },
  { value: 'CustomMeleeSounds', label: '自定义近战声音' },
  { value: 'CustomGrenadeSounds', label: '自定义手雷声音' },
  { value: 'CustomItemSounds', label: '自定义物品声音' },
  { value: 'CustomKillFeedback', label: '自定义击杀反馈' }
]

// 生成JSON
const generatedJson = computed(() => {
  const json: any = {
    name: formData.value.name,
    author: formData.value.author,
    version: formData.value.version
  }

  // 添加可选字段
  if (formData.value.description) {
    json.description = formData.value.description
  }
  
  if (formData.value.compatibleModVersion) {
    json.compatibleModVersion = formData.value.compatibleModVersion
  }
  
  if (formData.value.requiredModules.length > 0) {
    json.requiredModules = formData.value.requiredModules
  }
  
  // 添加optional字段
  const optional: any = {}
  if (formData.value.homepage) {
    optional.homepage = formData.value.homepage
  }
  if (formData.value.qq) {
    optional.qq = formData.value.qq
  }
  
  if (Object.keys(optional).length > 0) {
    json.optional = optional
  }

  return json
})

// 格式化的JSON字符串
const formattedJson = computed(() => {
  return JSON.stringify(generatedJson.value, null, 2)
})

// 验证表单
const validationErrors = computed(() => {
  const errors: string[] = []
  
  if (!formData.value.name.trim()) {
    errors.push('声音包名称是必填项')
  }
  
  if (!formData.value.author.trim()) {
    errors.push('作者是必填项')
  }
  
  if (!formData.value.version.trim()) {
    errors.push('版本号是必填项')
  } else if (!/^\d+\.\d+\.\d+/.test(formData.value.version)) {
    errors.push('版本号格式应为 x.y.z (如 1.0.0)')
  }
  
  return errors
})

// 复制到剪贴板
const copyToClipboard = async () => {
  try {
    await navigator.clipboard.writeText(formattedJson.value)
    copySuccess.value = true
    setTimeout(() => {
      copySuccess.value = false
    }, 2000)
  } catch (err) {
    console.error('复制失败:', err)
    copyError.value = true
    setTimeout(() => {
      copyError.value = false
    }, 2000)
  }
}

// 复制状态
const copySuccess = ref(false)
const copyError = ref(false)

// 重置表单
const resetForm = () => {
  formData.value = {
    name: '',
    author: '',
    version: '1.0.0',
    description: '',
    compatibleModVersion: '2.0.0',
    requiredModules: [],
    homepage: '',
    qq: ''
  }
}

// 加载示例数据
const loadExample = () => {
  formData.value = {
    name: '示例声音包',
    author: '您的名字',
    version: '1.0.0',
    description: '这是一个示例声音包，包含了BGM和敌人语音的替换',
    compatibleModVersion: '2.0.0',
    requiredModules: ['CustomBGM', 'CustomEnemySounds'],
    homepage: 'https://example.com',
    qq: '123456789'
  }
}
</script>

<template>
  <div class="sound-pack-generator">
    <div class="generator-container">
      <!-- 左侧：表单输入 -->
      <div class="form-section">
        <h2>声音包信息</h2>
        
        <div class="form-group">
          <label for="name">声音包名称 *</label>
          <input 
            id="name"
            v-model="formData.name" 
            type="text" 
            placeholder="输入声音包的名称"
            :class="{ 'error': validationErrors.includes('声音包名称是必填项') }"
          />
        </div>

        <div class="form-group">
          <label for="author">作者 *</label>
          <input 
            id="author"
            v-model="formData.author" 
            type="text" 
            placeholder="输入您的名字"
            :class="{ 'error': validationErrors.includes('作者是必填项') }"
          />
        </div>

        <div class="form-group">
          <label for="version">版本号 *</label>
          <input 
            id="version"
            v-model="formData.version" 
            type="text" 
            placeholder="1.0.0"
            :class="{ 'error': validationErrors.includes('版本号是必填项') || validationErrors.includes('版本号格式应为 x.y.z (如 1.0.0)') }"
          />
        </div>

        <div class="form-group">
          <label for="description">描述</label>
          <textarea 
            id="description"
            v-model="formData.description" 
            placeholder="描述您的声音包内容和特色"
            rows="3"
          ></textarea>
        </div>

        <div class="form-group">
          <label for="compatibleModVersion">兼容Mod版本</label>
          <input 
            id="compatibleModVersion"
            v-model="formData.compatibleModVersion" 
            type="text" 
            placeholder="2.0.0"
          />
        </div>

        <div class="form-group">
          <label>涉及的模块</label>
          <div class="checkbox-group">
            <label 
              v-for="module in moduleOptions" 
              :key="module.value"
              class="checkbox-item"
            >
              <input 
                type="checkbox" 
                :value="module.value"
                v-model="formData.requiredModules"
              />
              <span>{{ module.label }}</span>
            </label>
          </div>
        </div>

        <div class="form-group">
          <label for="homepage">主页链接</label>
          <input 
            id="homepage"
            v-model="formData.homepage" 
            type="url" 
            placeholder="https://your-website.com"
          />
        </div>

        <div class="form-group">
          <label for="qq">QQ/群号</label>
          <input 
            id="qq"
            v-model="formData.qq" 
            type="text" 
            placeholder="QQ号或群号"
          />
        </div>

        <!-- 操作按钮 -->
        <div class="form-actions">
          <button @click="loadExample" class="btn btn-secondary">示例数据(覆盖当前)</button>
          <button @click="resetForm" class="btn btn-outline">重置</button>
        </div>
      </div>

      <!-- 右侧：JSON预览 -->
      <div class="preview-section">
        <div class="preview-header">
          <h2>pack.json 预览</h2>
          <button 
            @click="copyToClipboard" 
            class="btn btn-primary"
            :disabled="validationErrors.length > 0"
          >
            复制JSON
          </button>
        </div>

        <!-- 验证错误提示 -->
        <div v-if="validationErrors.length > 0" class="validation-errors">
          <h3>请修正以下错误：</h3>
          <ul>
            <li v-for="error in validationErrors" :key="error">{{ error }}</li>
          </ul>
        </div>

        <!-- 复制状态提示 -->
        <div v-if="copySuccess" class="copy-feedback success">
          ✓ JSON已复制到剪贴板
        </div>
        <div v-if="copyError" class="copy-feedback error">
          ✗ 复制失败，请手动选择复制
        </div>

        <!-- JSON预览区域 -->
        <div class="json-preview">
          <pre><code>{{ formattedJson }}</code></pre>
        </div>

        <!-- 使用说明 -->
        <div class="usage-info">
          <h3>使用说明</h3>
          <ol>
            <li>填写左侧表单中的必填项（带*号）</li>
            <li>根据需要选择可选信息</li>
            <li>点击"复制JSON"按钮复制生成的pack.json内容</li>
            <li>在您的声音包文件夹中创建pack.json文件并粘贴内容</li>
            <li>确保声音包文件夹结构符合要求</li>
          </ol>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.sound-pack-generator {
  max-width: 1200px;
  margin: 0 auto;
  padding: 20px;
}

.generator-container {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 30px;
  margin-top: 20px;
}

@media (max-width: 968px) {
  .generator-container {
    grid-template-columns: 1fr;
  }
}

.form-section, .preview-section {
  background: var(--vp-c-bg-soft);
  border-radius: 8px;
  padding: 24px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.form-group {
  margin-bottom: 20px;
}

.form-group label {
  display: block;
  margin-bottom: 6px;
  font-weight: 600;
  color: var(--vp-c-text-1);
}

.form-group input,
.form-group textarea {
  width: 100%;
  padding: 10px 12px;
  border: 1px solid var(--vp-c-border);
  border-radius: 6px;
  font-size: 14px;
  background: var(--vp-c-bg);
  color: var(--vp-c-text-1);
  transition: border-color 0.2s;
}

.form-group input:focus,
.form-group textarea:focus {
  outline: none;
  border-color: var(--vp-c-brand);
  box-shadow: 0 0 0 2px rgba(100, 108, 255, 0.2);
}

.form-group input.error {
  border-color: #ef4444;
}

.checkbox-group {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 8px;
  margin-top: 8px;
}

.checkbox-item {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  font-size: 14px;
}

.checkbox-item input[type="checkbox"] {
  width: auto;
  margin: 0;
}

.form-actions {
  display: flex;
  gap: 12px;
  margin-top: 24px;
}

.btn {
  padding: 10px 16px;
  border: none;
  border-radius: 6px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-primary {
  background: var(--vp-c-brand);
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background: var(--vp-c-brand-dark);
}

.btn-primary:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-secondary {
  background: var(--vp-c-brand);
  color: white;
}

.btn-secondary:hover {
  background: var(--vp-c-brand-dark);
}

.btn-outline {
  background: transparent;
  color: var(--vp-c-text-1);
  border: 1px solid var(--vp-c-border);
}

.btn-outline:hover {
  background: var(--vp-c-bg-soft);
}

.preview-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.validation-errors {
  background: #fef2f2;
  border: 1px solid #fecaca;
  border-radius: 6px;
  padding: 16px;
  margin-bottom: 20px;
}

.validation-errors h3 {
  color: #dc2626;
  margin: 0 0 8px 0;
  font-size: 16px;
}

.validation-errors ul {
  margin: 0;
  padding-left: 20px;
}

.validation-errors li {
  color: #dc2626;
  margin-bottom: 4px;
}

.copy-feedback {
  padding: 12px;
  border-radius: 6px;
  margin-bottom: 16px;
  font-weight: 500;
}

.copy-feedback.success {
  background: #f0fdf4;
  color: #16a34a;
  border: 1px solid #bbf7d0;
}

.copy-feedback.error {
  background: #fef2f2;
  color: #dc2626;
  border: 1px solid #fecaca;
}

.json-preview {
  background: #1e1e1e;
  border-radius: 6px;
  padding: 20px;
  margin-bottom: 20px;
  overflow-x: auto;
}

.json-preview pre {
  margin: 0;
  font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
  font-size: 14px;
  line-height: 1.5;
  color: #d4d4d4;
}

.usage-info {
  background: var(--vp-c-bg);
  border-radius: 6px;
  padding: 16px;
  border: 1px solid var(--vp-c-border);
}

.usage-info h3 {
  margin: 0 0 12px 0;
  font-size: 16px;
  color: var(--vp-c-text-1);
}

.usage-info ol {
  margin: 0;
  padding-left: 20px;
}

.usage-info li {
  margin-bottom: 8px;
  color: var(--vp-c-text-2);
  font-size: 14px;
}

h2 {
  margin: 0 0 20px 0;
  font-size: 20px;
  color: var(--vp-c-text-1);
}
</style>