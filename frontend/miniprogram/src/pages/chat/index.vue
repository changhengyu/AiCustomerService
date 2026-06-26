<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { onLoad, onShow } from '@dcloudio/uni-app';
import { conversationApi } from '@/api';

const id = ref('');
const detail = ref<any>({});
const text = ref('');
const sending = ref(false);

onLoad((q: any) => { id.value = q.id; });

async function load() {
  detail.value = await conversationApi.detail(id.value);
  // 滚动到底部
  setTimeout(() => uni.pageScrollTo({ scrollTop: 99999, duration: 0 }), 100);
}

async function send() {
  if (!text.value.trim()) return;
  sending.value = true;
  try {
    await conversationApi.agentSend(id.value, text.value);
    text.value = '';
    await load();
  } finally { sending.value = false; }
}

async function handoff() {
  await conversationApi.handoff(id.value);
  await load();
}

async function close() {
  await conversationApi.close(id.value);
  await load();
}

onShow(load);
onMounted(load);
</script>

<template>
  <view class="chat-page">
    <view class="status-bar">
      <text>状态：{{ detail.status }}</text>
      <view>
        <button size="mini" @click="handoff" :disabled="detail.status === 'human'">转人工</button>
        <button size="mini" type="warn" @click="close">关闭</button>
      </view>
    </view>

    <scroll-view scroll-y class="messages">
      <view v-for="m in detail.messages" :key="m.id" :class="['msg', m.role]">
        <view class="bubble">{{ m.content }}</view>
        <text class="meta">{{ m.created_at }} · {{ m.tokens_used }} tokens</text>
      </view>
    </scroll-view>

    <view class="input-bar">
      <input v-model="text" placeholder="输入回复..." @confirm="send" />
      <button type="primary" :loading="sending" @click="send">发送</button>
    </view>
  </view>
</template>

<style scoped>
.chat-page { display: flex; flex-direction: column; height: 100vh; }
.status-bar { display: flex; justify-content: space-between; padding: 16rpx 24rpx; background: #fafafa; border-bottom: 1rpx solid #eee; }
.messages { flex: 1; padding: 20rpx; overflow-y: auto; }
.msg { margin-bottom: 20rpx; }
.msg.user .bubble { background: #f0f0f0; align-self: flex-start; }
.msg.agent .bubble, .msg.assistant .bubble { background: #1890ff; color: #fff; align-self: flex-end; margin-left: auto; }
.bubble { display: inline-block; max-width: 70%; padding: 16rpx 24rpx; border-radius: 16rpx; }
.msg { display: flex; flex-direction: column; }
.meta { font-size: 20rpx; color: #999; margin-top: 4rpx; }
.input-bar { display: flex; gap: 16rpx; padding: 16rpx; background: #fff; border-top: 1rpx solid #eee; }
.input-bar input { flex: 1; padding: 16rpx; background: #f5f5f5; border-radius: 8rpx; }
</style>