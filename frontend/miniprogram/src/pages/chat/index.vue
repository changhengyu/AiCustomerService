<script setup lang="ts">
import { ref, nextTick, onUnmounted } from 'vue';
import { onLoad, onShow } from '@dcloudio/uni-app';
import { conversationApi } from '@/api';
import { realtime } from '@/realtime';

const id = ref('');
const detail = ref<any>({});
const text = ref('');
const sending = ref(false);
const aiTyping = ref(false); // 真实驱动：SignalR/WS typing 事件

let unsubMessage: (() => void) | null = null;
let unsubTyping: (() => void) | null = null;
let unsubStatus: (() => void) | null = null;

onLoad((q: any) => {
  id.value = q.id;
});

async function load() {
  const d = await conversationApi.detail(id.value);
  detail.value = d;
  setTimeout(() => uni.pageScrollTo({ scrollTop: 99999, duration: 0 }), 100);
}

async function send() {
  if (!text.value.trim() || sending.value) return;
  sending.value = true;
  const content = text.value;
  text.value = '';
  try {
    await conversationApi.agentSend(id.value, content);
    // 新消息会通过 realtime 推送来，不再手动 load()
  } catch (e) {
    text.value = content;
    uni.showToast({ title: '发送失败', icon: 'none' });
  } finally {
    sending.value = false;
  }
}

async function handoff() {
  uni.showModal({
    title: '转人工',
    content: '确定将此会话转人工处理？',
    success: async (res) => {
      if (res.confirm) {
        await conversationApi.handoff(id.value);
        uni.showToast({ title: '已转人工', icon: 'success' });
      }
    }
  });
}

async function close() {
  uni.showModal({
    title: '关闭会话',
    content: '关闭后客户将无法继续发送消息',
    success: async (res) => {
      if (res.confirm) {
        await conversationApi.close(id.value);
        uni.showToast({ title: '已关闭', icon: 'success' });
      }
    }
  });
}

function statusInfo(s: string) {
  const map: Record<string, { label: string; color: string; bg: string }> = {
    active: { label: 'AI 处理中', color: 'var(--color-info)', bg: 'var(--color-info-soft)' },
    human: { label: '人工服务', color: 'var(--color-warning)', bg: 'var(--color-warning-soft)' },
    closed: { label: '已关闭', color: 'var(--color-text-tertiary)', bg: 'var(--color-bg-muted)' }
  };
  return map[s] || map.active;
}

function roleLabel(r: string) {
  const map: Record<string, string> = { user: '客户', agent: '客服', assistant: 'AI' };
  return map[r] || r;
}

function formatTime(s: string) {
  if (!s) return '';
  const d = new Date(s);
  const now = new Date();
  if (d.toDateString() === now.toDateString()) {
    return `${d.getHours().toString().padStart(2, '0')}:${d.getMinutes().toString().padStart(2, '0')}`;
  }
  return `${d.getMonth() + 1}/${d.getDate()} ${d.getHours().toString().padStart(2, '0')}:${d.getMinutes().toString().padStart(2, '0')}`;
}

function onNewMessage(payload: any) {
  if (payload?.conversationId !== id.value && payload?.conversation_id !== id.value) return;
  if (!detail.value.messages) detail.value.messages = [];
  const msgId = payload.messageId || payload.message_id;
  if (detail.value.messages.some((m: any) => m.id === msgId)) return;
  detail.value.messages.push({
    id: msgId,
    role: payload.role,
    content: payload.content,
    content_type: payload.contentType || payload.content_type || 'text',
    created_at: payload.createdAt || payload.created_at,
    tokens_used: payload.tokens_used,
    latency_ms: payload.latency_ms
  });
  detail.value.last_message_at = payload.createdAt || payload.created_at;
  // 滚动到底部
  setTimeout(() => uni.pageScrollTo({ scrollTop: 99999, duration: 200 }), 50);
}

function onTyping(payload: any) {
  if (payload?.conversationId !== id.value && payload?.conversation_id !== id.value) return;
  if (payload.role === 'assistant') {
    aiTyping.value = payload.isTyping !== false;
  }
}

function onStatus(payload: any) {
  if (payload?.conversationId !== id.value && payload?.conversation_id !== id.value) return;
  if (detail.value) detail.value.status = payload.status;
}

onShow(async () => {
  await load();
  // 确保 realtime 已连接（App 启动时也会尝试连接）
  await realtime.connect();
  await realtime.subscribeConversation(id.value);
  unsubMessage = realtime.on('message.new', onNewMessage);
  unsubTyping = realtime.on('typing', onTyping);
  unsubStatus = realtime.on('conversation.status', onStatus);
});

onUnmounted(() => {
  unsubMessage?.();
  unsubTyping?.();
  unsubStatus?.();
  realtime.unsubscribeConversation(id.value);
});
</script>

<template>
  <view class="chat-page">
    <!-- 顶部状态栏 -->
    <view class="status-bar">
      <view class="status-left">
        <view class="back-btn" @click="uni.navigateBack()">
          <text>‹</text>
        </view>
        <view>
          <text class="customer-name">{{ detail.customer_nickname || '客户' }}</text>
          <view
            class="status-badge"
            :style="{ background: statusInfo(detail.status).bg, color: statusInfo(detail.status).color }"
          >
            <view class="status-dot" :style="{ background: statusInfo(detail.status).color }"></view>
            {{ statusInfo(detail.status).label }}
          </view>
        </view>
      </view>
      <view class="status-actions">
        <view
          v-if="detail.status !== 'human'"
          class="action-btn"
          @click="handoff"
        >
          <text class="icon">👤</text>
          <text class="action-text">转人工</text>
        </view>
        <view
          v-if="detail.status !== 'closed'"
          class="action-btn action-close"
          @click="close"
        >
          <text class="icon">✕</text>
          <text class="action-text">关闭</text>
        </view>
      </view>
    </view>

    <!-- 消息列表 -->
    <scroll-view scroll-y class="messages" :scroll-into-view="`msg-${detail.messages?.length || 0}`">
      <view v-if="!detail.messages || detail.messages.length === 0" class="empty-state">
        <view class="empty-icon">💬</view>
        <text class="empty-text">还没有消息</text>
      </view>
      <view
        v-for="(m, i) in detail.messages"
        :id="`msg-${i + 1}`"
        :key="m.id"
        :class="['msg', m.role]"
      >
        <view v-if="m.role === 'user'" class="bubble user-bubble">
          <text class="bubble-content">{{ m.content }}</text>
          <text class="bubble-time">{{ formatTime(m.created_at) }}</text>
        </view>
        <view v-else class="assistant-wrap">
          <view class="assistant-avatar" :class="{ 'is-ai': m.role === 'assistant' }">
            <text>{{ m.role === 'assistant' ? 'AI' : '客' }}</text>
          </view>
          <view class="bubble assistant-bubble">
            <text class="role-tag">{{ roleLabel(m.role) }}</text>
            <text class="bubble-content">{{ m.content }}</text>
            <view class="bubble-meta">
              <text class="bubble-time">{{ formatTime(m.created_at) }}</text>
              <text v-if="m.tokens_used" class="tokens">{{ m.tokens_used }} tokens</text>
            </view>
          </view>
        </view>
      </view>

      <!-- AI 输入中 -->
      <view v-if="aiTyping" class="msg assistant">
        <view class="assistant-wrap">
          <view class="assistant-avatar is-ai"><text>AI</text></view>
          <view class="bubble assistant-bubble typing">
            <view class="typing-dots">
              <view class="dot"></view>
              <view class="dot"></view>
              <view class="dot"></view>
            </view>
          </view>
        </view>
      </view>
    </scroll-view>

    <!-- 输入栏 -->
    <view v-if="detail.status !== 'closed'" class="input-bar">
      <view class="input-wrapper">
        <input
          v-model="text"
          placeholder="输入回复..."
          confirm-type="send"
          @confirm="send"
          :disabled="sending"
        />
      </view>
      <view
        class="send-btn"
        :class="{ disabled: !text.trim() || sending, loading: sending }"
        @click="send"
      >
        <text v-if="!sending">发送</text>
        <view v-else class="spinner"></view>
      </view>
    </view>
    <view v-else class="closed-tip">
      <text>会话已关闭</text>
    </view>
  </view>
</template>

<style lang="scss" scoped>
@import '@/style/theme.scss';

.chat-page {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background: var(--color-bg);
}

/* 顶部状态栏 */
.status-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 24rpx 24rpx;
  padding-top: calc(24rpx + env(safe-area-inset-top, 0px));
  background: var(--color-bg-elevated);
  border-bottom: 1rpx solid var(--color-divider);
  box-shadow: var(--shadow-xs);
}
.status-left {
  display: flex;
  align-items: center;
  gap: 16rpx;
}
.back-btn {
  width: 56rpx;
  height: 56rpx;
  border-radius: 50%;
  background: var(--color-bg-muted);
  display: flex;
  align-items: center;
  justify-content: center;
}
.back-btn text {
  font-size: 40rpx;
  color: var(--color-text-primary);
  line-height: 1;
  margin-top: -4rpx;
}
.customer-name {
  display: block;
  font-size: 30rpx;
  font-weight: 600;
  color: var(--color-text-primary);
}
.status-badge {
  display: inline-flex;
  align-items: center;
  gap: 6rpx;
  padding: 4rpx 12rpx;
  font-size: 20rpx;
  border-radius: var(--radius-full);
  margin-top: 6rpx;
}
.status-dot {
  width: 10rpx;
  height: 10rpx;
  border-radius: 50%;
}
.status-actions {
  display: flex;
  gap: 12rpx;
}
.action-btn {
  display: flex;
  align-items: center;
  gap: 6rpx;
  padding: 10rpx 18rpx;
  background: var(--color-primary-soft);
  border-radius: var(--radius-full);
}
.action-btn .icon { font-size: 24rpx; }
.action-btn .action-text { font-size: 22rpx; color: var(--color-primary); font-weight: 500; }
.action-close {
  background: var(--color-bg-muted);
}
.action-close .icon { color: var(--color-text-secondary); }
.action-close .action-text { color: var(--color-text-secondary); }

/* 消息列表 */
.messages {
  flex: 1;
  padding: 24rpx;
  background: var(--color-bg);
}
.empty-state {
  text-align: center;
  padding: 200rpx 0;
}
.empty-icon {
  font-size: 80rpx;
  opacity: 0.4;
}
.empty-text {
  display: block;
  margin-top: 16rpx;
  font-size: 26rpx;
  color: var(--color-text-tertiary);
}
.msg {
  margin-bottom: 28rpx;
}
.user-bubble {
  background: var(--color-primary);
  color: #FFFFFF;
  padding: 20rpx 24rpx;
  border-radius: var(--radius-xl);
  border-bottom-right-radius: 6rpx;
  max-width: 80%;
  margin-left: auto;
  box-shadow: 0 2rpx 8rpx rgba(79, 110, 247, 0.2);
}
.bubble-content {
  display: block;
  font-size: 28rpx;
  line-height: 1.5;
  word-break: break-word;
  white-space: pre-wrap;
}
.user-bubble .bubble-time {
  display: block;
  font-size: 20rpx;
  color: rgba(255, 255, 255, 0.7);
  margin-top: 8rpx;
  text-align: right;
}

.assistant-wrap {
  display: flex;
  gap: 12rpx;
  max-width: 85%;
}
.assistant-avatar {
  flex-shrink: 0;
  width: 64rpx;
  height: 64rpx;
  border-radius: 50%;
  background: var(--color-warning);
  color: #FFFFFF;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24rpx;
  font-weight: 600;
}
.assistant-avatar.is-ai {
  background: linear-gradient(135deg, var(--color-primary) 0%, #6B83FA 100%);
}
.assistant-bubble {
  flex: 1;
  background: var(--color-bg-elevated);
  padding: 20rpx 24rpx;
  border-radius: var(--radius-xl);
  border-top-left-radius: 6rpx;
  box-shadow: var(--shadow-xs);
}
.role-tag {
  display: inline-block;
  font-size: 20rpx;
  color: var(--color-text-tertiary);
  margin-bottom: 8rpx;
}
.assistant-bubble .bubble-content {
  color: var(--color-text-primary);
}
.bubble-meta {
  display: flex;
  align-items: center;
  gap: 12rpx;
  margin-top: 8rpx;
}
.bubble-time {
  font-size: 20rpx;
  color: var(--color-text-tertiary);
}
.tokens {
  font-size: 20rpx;
  color: var(--color-text-tertiary);
  padding: 2rpx 10rpx;
  background: var(--color-bg-muted);
  border-radius: var(--radius-full);
}

/* AI 输入动画 */
.typing-dots {
  display: flex;
  gap: 8rpx;
  padding: 8rpx 0;
}
.typing-dots .dot {
  width: 12rpx;
  height: 12rpx;
  border-radius: 50%;
  background: var(--color-text-tertiary);
  animation: typingBounce 1.4s ease-in-out infinite both;
}
.typing-dots .dot:nth-child(1) { animation-delay: -0.32s; }
.typing-dots .dot:nth-child(2) { animation-delay: -0.16s; }
@keyframes typingBounce {
  0%, 80%, 100% { transform: scale(0.6); opacity: 0.4; }
  40% { transform: scale(1); opacity: 1; }
}

/* 输入栏 */
.input-bar {
  display: flex;
  gap: 16rpx;
  padding: 20rpx 24rpx;
  padding-bottom: calc(20rpx + env(safe-area-inset-bottom, 0px));
  background: var(--color-bg-elevated);
  border-top: 1rpx solid var(--color-divider);
  align-items: center;
}
.input-wrapper {
  flex: 1;
  background: var(--color-bg-muted);
  border-radius: var(--radius-full);
  padding: 0 24rpx;
}
.input-wrapper input {
  height: 72rpx;
  font-size: 28rpx;
  color: var(--color-text-primary);
}
.send-btn {
  flex-shrink: 0;
  min-width: 112rpx;
  height: 72rpx;
  padding: 0 28rpx;
  background: var(--color-primary);
  color: #FFFFFF;
  border-radius: var(--radius-full);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 26rpx;
  font-weight: 500;
  transition: all 0.2s;
}
.send-btn:active {
  transform: scale(0.96);
  background: var(--color-primary-pressed);
}
.send-btn.disabled {
  background: var(--color-bg-muted);
  color: var(--color-text-tertiary);
}
.spinner {
  width: 28rpx;
  height: 28rpx;
  border: 3rpx solid rgba(255, 255, 255, 0.3);
  border-top-color: #FFFFFF;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}
@keyframes spin {
  to { transform: rotate(360deg); }
}

.closed-tip {
  text-align: center;
  padding: 24rpx;
  padding-bottom: calc(24rpx + env(safe-area-inset-bottom, 0px));
  background: var(--color-bg-muted);
  color: var(--color-text-tertiary);
  font-size: 24rpx;
}
</style>