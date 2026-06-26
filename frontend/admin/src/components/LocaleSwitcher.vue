<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { setLocale, type Locale } from '../i18n'

const { locale } = useI18n()

function change(l: Locale) {
  setLocale(l)
  // 触发全局刷新：reload 简单粗暴，保证 element-plus 的所有 message 立即更新
  window.location.reload()
}
</script>

<template>
  <el-dropdown @command="change">
    <span class="locale-trigger">
      🌐 {{ locale === 'zh-CN' ? '中文' : 'English' }}
    </span>
    <template #dropdown>
      <el-dropdown-menu>
        <el-dropdown-item command="zh-CN" :disabled="locale === 'zh-CN'">中文</el-dropdown-item>
        <el-dropdown-item command="en-US" :disabled="locale === 'en-US'">English</el-dropdown-item>
      </el-dropdown-menu>
    </template>
  </el-dropdown>
</template>

<style scoped>
.locale-trigger {
  cursor: pointer;
  color: #fff;
  font-size: 14px;
  padding: 0 8px;
}
</style>
