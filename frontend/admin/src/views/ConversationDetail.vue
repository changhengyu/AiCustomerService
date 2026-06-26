<script setup lang="ts">
import { onMounted, onBeforeUnmount, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import { conversationApi } from '@/api';
import { realtime } from '@/realtime';

const route = useRoute();
const id = route.params.id as string;
const detail = ref<any>({});
const newMessage = ref('');
const sending = ref(false);
const aiTyping = ref(false); // 真实驱动：监听 typing 事件

let unsubMessage: (() => void) | null = null;
let unsubTyping: (() => void) | null = null;
let unsubStatus: (() => void) | null = null;

async function load() {
  detail.value = await conversationApi.detail(id);
}

async function send() {
  if (!newMessage.value.trim()) return;
  sending.value = true;
  try {
    await conversationApi.agentSend(id, newMessage.value);
    newMessage.value = '';
    realtime.agentTyping(id, false);
    // 不再 await load() — 新消息会通过 SignalR 推送过来
  } finally {
    sending.value = false;
  }
}

async function handoff() {
  await conversationApi.handoff(id);
}

async function close() {
  await conversationApi.close(id);
}

function onInput() {
  realtime.agentTyping(id, true);
}

function onNewMessage(payload: any) {
  if (payload.conversationId !== id) return;
  // 把新消息追加到本地消息列表，避免重新拉取整页
  if (!detail.value.messages) detail.value.messages = [];
  // 去重（避免自己刚发的消息重复追加）
  if (detail.value.messages.some((m: any) => m.id === payload.messageId)) return;
  detail.value.messages.push({
    id: payload.messageId,
    role: payload.role,
    content: payload.content,
    content_type: payload.contentType,
    created_at: payload.createdAt
  });
  detail.value.last_message_at = payload.createdAt;
}

function onTyping(payload: any) {
  if (payload.conversationId !== id) return;
  if (payload.role === 'assistant') {
    aiTyping.value = payload.isTyping !== false;
    if (aiTyping.value && payload.delta) {
      // 流式 chunk：可在此处做打字机效果（v0.5 简化版只显示省略号）
    }
  }
}

function onStatus(payload: any) {
  if (payload.conversationId !== id) return;
  if (detail.value) detail.value.status = payload.status;
}

onMounted(async () => {
  await load();
  // 订阅本会话的实时事件
  await realtime.subscribeConversation(id);
  unsubMessage = realtime.on('message.new', onNewMessage);
  unsubTyping = realtime.on('typing', onTyping);
  unsubStatus = realtime.on('conversation.status', onStatus);
});

onBeforeUnmount(async () => {
  unsubMessage?.();
  unsubTyping?.();
  unsubStatus?.();
  await realtime.unsubscribeConversation(id);
});
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
      <div v-if="aiTyping" style="margin:8px 0;color:#999;">
        <el-tag type="success">AI</el-tag>
        <span style="margin-left:8px;">AI 正在输入<span class="typing-dots">…</span></span>
      </div>
    </div>

    <div style="margin-top:12px;display:flex;gap:8px;">
      <el-input v-model="newMessage" placeholder="输入回复内容" @keyup.enter="send" @input="onInput" />
      <el-button type="primary" :loading="sending" @click="send">发送</el-button>
    </div>
  </el-card>
</template>

<style scoped>
.typing-dots::after {
  content: '';
  display: inline-block;
  width: 1em;
  text-align: left;
  animation: dots 1.2s steps(3, end) infinite;
}
@keyframes dots {
  0%, 20% { content: ''; }
  40% { content: '.'; }
  60% { content: '..'; }
  80%, 100% { content: '...'; }
}
</style>