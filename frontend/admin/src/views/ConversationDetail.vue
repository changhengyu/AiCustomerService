<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import { conversationApi } from '@/api';

const route = useRoute();
const id = route.params.id as string;
const detail = ref<any>({});
const newMessage = ref('');
const sending = ref(false);

async function load() {
  detail.value = await conversationApi.detail(id);
}

async function send() {
  if (!newMessage.value.trim()) return;
  sending.value = true;
  try {
    await conversationApi.agentSend(id, newMessage.value);
    newMessage.value = '';
    await load();
  } finally { sending.value = false; }
}

async function handoff() {
  await conversationApi.handoff(id);
  await load();
}

async function close() {
  await conversationApi.close(id);
  await load();
}

onMounted(load);
</script>

<template>
  <el-card>
    <div class="toolbar">
      <span>状态：<el-tag>{{ detail.status }}</el-tag></span>
      <div>
        <el-button @click="handoff" :disabled="detail.status === 'human'">转人工</el-button>
        <el-button @click="close" type="danger">关闭会话</el-button>
      </div>
    </div>

    <div style="max-height:60vh;overflow:auto;border:1px solid #eee;padding:12px;background:#fafafa;">
      <div v-for="m in detail.messages" :key="m.id" style="margin:8px 0;">
        <el-tag v-if="m.role === 'user'" type="primary">用户</el-tag>
        <el-tag v-else-if="m.role === 'agent'" type="warning">客服</el-tag>
        <el-tag v-else type="success">AI</el-tag>
        <span style="margin-left:8px;">{{ m.content }}</span>
        <small style="margin-left:8px;color:#999;">{{ m.created_at }} · {{ m.tokens_used }} tokens · {{ m.latency_ms }}ms</small>
      </div>
    </div>

    <div style="margin-top:12px;display:flex;gap:8px;">
      <el-input v-model="newMessage" placeholder="输入回复内容" @keyup.enter="send" />
      <el-button type="primary" :loading="sending" @click="send">发送</el-button>
    </div>
  </el-card>
</template>