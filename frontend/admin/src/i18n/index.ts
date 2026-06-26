import { createI18n } from 'vue-i18n'
import zhCN from './zh-CN'
import enUS from './en-US'

export type Locale = 'zh-CN' | 'en-US'

const stored = (typeof localStorage !== 'undefined' && localStorage.getItem('aics.locale')) as Locale | null

export const i18n = createI18n({
  legacy: false,
  globalInjection: true,
  locale: stored || 'zh-CN',
  fallbackLocale: 'zh-CN',
  messages: {
    'zh-CN': zhCN,
    'en-US': enUS
  }
})

export function setLocale(l: Locale) {
  i18n.global.locale.value = l
  if (typeof localStorage !== 'undefined') localStorage.setItem('aics.locale', l)
  document.documentElement.lang = l
}

document.documentElement.lang = i18n.global.locale.value
