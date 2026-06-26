<script setup lang="ts">
import { useAuthStore } from '@/stores/auth';
import { useRouter } from 'vue-router';

const auth = useAuthStore();
const router = useRouter();

function logout() {
  auth.logout();
  router.push('/login');
}
</script>

<template>
  <div class="app-container">
    <aside class="sidebar">
      <h3 style="padding: 16px; color: #fff; text-align: center;">AI 客服 SaaS</h3>
      <router-link to="/dashboard">📊 仪表盘</router-link>
      <router-link to="/conversations">💬 会话</router-link>
      <router-link to="/knowledge">📚 知识库</router-link>
      <router-link to="/customers">👥 客户</router-link>
      <router-link to="/settings">⚙️ 设置</router-link>
      <a href="/hangfire" target="_blank">🛠️ Hangfire</a>
      <a href="/openapi/v1.json" target="_blank">📄 OpenAPI</a>
    </aside>
    <main class="main">
      <div class="toolbar">
        <h2>{{ $route.path }}</h2>
        <div>
          <span>{{ auth.user?.username }} ({{ auth.user?.role }})</span>
          <el-button text @click="logout" style="margin-left: 12px;">退出</el-button>
        </div>
      </div>
      <router-view />
    </main>
  </div>
</template>