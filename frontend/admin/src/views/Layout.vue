<script setup lang="ts">
import { onMounted, onBeforeUnmount, ref } from 'vue';
import { useAuthStore } from '@/stores/auth';
import { useRouter } from 'vue-router';
import { realtime } from '@/realtime';

const auth = useAuthStore();
const router = useRouter();
const connState = ref<string>('idle');

function logout() {
  realtime.disconnect();
  auth.logout();
  router.push('/login');
}

onMounted(async () => {
  if (!auth.accessToken) return;
  // 全局监听新消息（用于会话列表红点 + 角标）
  realtime.on('conversation.new_message', () => {
    // 简单实现：广播事件给 Conversations 列表，让它刷新
    window.dispatchEvent(new CustomEvent('aics:new_message'));
  });
  realtime.on('conversation.status_changed', () => {
    window.dispatchEvent(new CustomEvent('aics:conversation_status'));
  });
  realtime.on('customer.intention_changed', () => {
    window.dispatchEvent(new CustomEvent('aics:intention_changed'));
  });
  realtime.on('sla.warning', (p) => {
    console.warn('[sla.warning]', p);
  });

  await realtime.connect(auth.accessToken);
  // 定时同步状态用于 UI 展示
  const interval = setInterval(() => {
    connState.value = realtime.state;
  }, 1000);
  onBeforeUnmount(() => clearInterval(interval));
});
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
        <div style="display: flex; align-items: center; gap: 12px;">
          <span :title="`SignalR: ${connState}`" :style="{
            display: 'inline-block', width: '8px', height: '8px', borderRadius: '50%',
            background: connState === 'Connected' ? '#67c23a' : connState === 'idle' ? '#909399' : '#e6a23c'
          }" />
          <span>{{ auth.user?.username }} ({{ auth.user?.role }})</span>
          <el-button text @click="logout" style="margin-left: 12px;">退出</el-button>
        </div>
      </div>
      <router-view />
    </main>
  </div>
</template>