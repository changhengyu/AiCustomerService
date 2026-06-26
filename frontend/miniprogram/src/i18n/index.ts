// uni-app i18n 简易实现：不依赖第三方包，使用 uni.getLocale + 全局 $t
import zhCN from './zh-CN.json'
import enUS from './en-US.json'

export type Locale = 'zh-CN' | 'en-US'

const messages: Record<Locale, Record<string, any>> = {
  'zh-CN': zhCN,
  'en-US': enUS
}

let currentLocale: Locale = (uni.getStorageSync('aics.locale') as Locale) || 'zh-CN'

export function setLocale(l: Locale) {
  currentLocale = l
  uni.setStorageSync('aics.locale', l)
}

export function getLocale(): Locale {
  return currentLocale
}

export function $t(key: string, params?: Record<string, string | number>): string {
  const dict = messages[currentLocale] || messages['zh-CN']
  const parts = key.split('.')
  let cur: any = dict
  for (const p of parts) cur = cur?.[p]
  let text = typeof cur === 'string' ? cur : key
  if (params) {
    for (const [k, v] of Object.entries(params))
      text = text.replace(new RegExp(`\\{${k}\\}`, 'g'), String(v))
  }
  return text
}

// Vue 插件：注入全局 $t
export default {
  install(app: any) {
    app.config.globalProperties.$t = $t
    app.config.globalProperties.$locale = currentLocale
    app.provide('i18n', { $t, setLocale, getLocale })
  }
}
