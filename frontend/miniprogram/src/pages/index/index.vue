<script setup lang="ts">
import { ref, computed, onMounted, onShow, onUnmounted } from 'vue';
import { conversationApi, type ConversationListItem } from '@/api';
import { realtime } from '@/realtime';

const list = ref<ConversationListItem[]>([]);
const statusFilter = ref<string>('');
const loading = ref(false);
const user = ref<any>(uni.getStorageSync('user') || {});

let unsubMessage: (() => void) | null = null;
let unsubStatus: (() => void) | null = null;

async function load() {
  loading.value = true;
  try {
    const r = await conversationApi.list({ page: 1, page_size: 50, status: statusFilter.value || undefined });
    list.value = r.items;
  } finally { loading.value = false; }
}

function openChat(item: ConversationListItem) {
  uni.navigateTo({ url: `/pages/chat/index?id=${item.id}` });
}

function filterBy(status: string) {
  statusFilter.value = statusFilter.value === status ? '' : status;
  load();
}

function onAnyNewMessage() {
  // 收到租户级别的新消息事件 → 刷新列表（轻量避免每次请求详情）
  load();
}

function onAnyStatusChange() {
  load();
}

onShow(async () => {
  await load();
  // 连接 realtime（如未连接）
  await realtime.connect();
  unsubMessage = realtime.on('conversation.new_message', onAnyNewMessage);
  unsubStatus = realtime.on('conversation.status_changed', onAnyStatusChange);
});

onUnmounted(() => {
  unsubMessage?.();
  unsubStatus?.();
});

function statusInfo(s: string) {
  const map: Record<string, { label: string; color: string; bg: string }> = {
    active: { label: 'AI 处理中', color: 'var(--color-info)', bg: 'var(--color-info-soft)' },
    human: { label: '人工服务', color: 'var(--color-warning)', bg: 'var(--color-warning-soft)' },
    closed: { label: '已关闭', color: 'var(--color-text-tertiary)', bg: 'var(--color-bg-muted)' }
  };
  return map[s] || { label: s, color: 'var(--color-text-tertiary)', bg: 'var(--color-bg-muted)' };
}

const stats = computed(() => {
  const total = list.value.length;
  const active = list.value.filter(c => c.status === 'active').length;
  const human = list.value.filter(c => c.status === 'human').length;
  const closed = list.value.filter(c => c.status === 'closed').length;
  return { total, active, human, closed };
});

const filters = [
  { value: 'active', label: 'AI 处理中', color: 'var(--color-info)', bg: 'var(--color-info-soft)' },
  { value: 'human', label: '人工', color: 'var(--color-warning)', bg: 'var(--color-warning-soft)' },
  { value: 'closed', label: '已关', color: 'var(--color-text-tertiary)', bg: 'var(--color-bg-muted)' }
];

function relativeTime(s?: string) {
  if (!s) return '';
  const d = new Date(s);
  const now = new Date();
  const diff = (now.getTime() - d.getTime()) / 1000;
  if (diff < 60) return '刚刚';
  if (diff < 3600) return `${Math.floor(diff / 60)} 分钟前`;
  if (diff < 86400) return `${Math.floor(diff / 3600)} 小时前`;
  if (diff < 86400 * 7) return `${Math.floor(diff / 86400)} 天前`;
  return `${d.getMonth() + 1}/${d.getDate()}`;
}

function avatarInitial(name?: string) {
  return (name || 'U')[0].toUpperCase();
}

function avatarBg(name?: string) {
  const colors = ['#4F6EF7', '#10B981', '#F59E0B', '#06B6D4', '#8B5CF6', '#EC4899'];
  const code = (name || 'U').charCodeAt(0);
  return colors[code % colors.length];
}

onShow(load);
onMounted(load);
</script>

<template>
  <view class="page">
    <!-- 顶部欢迎卡 -->
    <view class="hero">
      <view class="hero-bg"></view>
      <view class="hero-content">
        <view class="hero-greeting">
          <text class="hello">👋 你好，{{ user.display_name || user.username || '客服' }}</text>
          <text class="sub">今日有 {{ stats.human }} 个会话需要人工接管</text>
        </view>
        <view class="hero-avatar">
          <text>{{ avatarInitial(user.display_name || user.username) }}</text>
        </view>
      </view>
    </view>

    <!-- 数据看板 -->
    <view class="stats">
      <view class="stat-card stat-total">
        <text class="stat-num">{{ stats.total }}</text>
        <text class="stat-label">总会话</text>
      </view>
      <view class="stat-card stat-active">
        <text class="stat-num">{{ stats.active }}</text>
        <text class="stat-label">AI 处理</text>
      </view>
      <view class="stat-card stat-human">
        <text class="stat-num">{{ stats.human }}</text>
        <text class="stat-label">人工</text>
      </view>
      <view class="stat-card stat-closed">
        <text class="stat-num">{{ stats.closed }}</text>
        <text class="stat-label">已关</text>
      </view>
    </view>

    <!-- 筛选 -->
    <view class="filter-bar">
      <text
        :class="['filter-chip', { active: !statusFilter }]"
        @click="statusFilter = ''; load()"
      >全部</text>
      <text
        v-for="f in filters"
        :key="f.value"
        :class="['filter-chip', { active: statusFilter === f.value }]"
        :style="statusFilter === f.value ? { background: f.bg, color: f.color, borderColor: f.color } : {}"
        @click="filterBy(f.value)"
      >{{ f.label }}</text>
    </view>

    <!-- 会话列表 -->
    <view class="conv-list">
      <view v-if="loading" class="loading">
        <view class="dot"></view>
        <view class="dot"></view>
        <view class="dot"></view>
      </view>
      <view v-else-if="list.length === 0" class="empty">
        <view class="empty-icon">💬</view>
        <text class="empty-title">暂无会话</text>
        <text class="empty-sub">客户消息会显示在这里</text>
      </view>
      <view
        v-for="c in list"
        :key="c.id"
        class="conv-item"
        @click="openChat(c)"
      >
        <view class="avatar" :style="{ background: avatarBg(c.customer_nickname || c.customer_id) }">
          {{ avatarInitial(c.customer_nickname) }}
        </view>
        <view class="content">
          <view class="row-1">
            <text class="name">{{ c.customer_nickname || c.customer_id.slice(0, 8) }}</text>
            <text class="time">{{ relativeTime(c.last_message_at) }}</text>
          </view>
          <view class="row-2">
            <text class="summary">{{ c.summary || `${c.message_count} 条消息` }}</text>
            <view
              class="status-pill"
              :style="{ background: statusInfo(c.status).bg, color: statusInfo(c.status).color }"
            >
              {{ statusInfo(c.status).label }}
            </view>
          </view>
        </view>
      </view>
    </view>
  </view>
</template>

<style lang="scss" scoped>
@import '@/style/theme.scss';

.page {
  min-height: 100vh;
  background: var(--color-bg);
  padding-bottom: env(safe-area-inset-bottom, 40rpx);
}

/* 顶部欢迎区 */
.hero {
  position: relative;
  padding: 48rpx 32rpx 80rpx;
  background: linear-gradient(135deg, var(--color-primary) 0%, #6B83FA 100%);
  overflow: hidden;
}
.hero-bg {
  position: absolute;
  top: -200rpx;
  right: -200rpx;
  width: 600rpx;
  height: 600rpx;
  background: radial-gradient(circle, rgba(255,255,255,0.15) 0%, transparent 70%);
  border-radius: 50%;
}
.hero-content {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.hello {
  display: block;
  font-size: 36rpx;
  font-weight: 600;
  color: #FFFFFF;
  letter-spacing: 0.5rpx;
}
.sub {
  display: block;
  font-size: 24rpx;
  color: rgba(255, 255, 255, 0.85);
  margin-top: 12rpx;
}
.hero-avatar {
  width: 96rpx;
  height: 96rpx;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.2);
  backdrop-filter: blur(10rpx);
  display: flex;
  align-items: center;
  justify-content: center;
  border: 2rpx solid rgba(255, 255, 255, 0.3);
}
.hero-avatar text {
  font-size: 40rpx;
  color: #FFFFFF;
  font-weight: 600;
}

/* 数据看板 */
.stats {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16rpx;
  margin: -48rpx 24rpx 0;
  background: var(--color-bg-elevated);
  border-radius: var(--radius-xl);
  padding: 24rpx;
  box-shadow: var(--shadow-md);
  position: relative;
  z-index: 2;
}
.stat-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 16rpx 0;
}
.stat-num {
  font-size: 40rpx;
  font-weight: 700;
  line-height: 1.2;
}
.stat-label {
  font-size: 22rpx;
  color: var(--color-text-tertiary);
  margin-top: 6rpx;
}
.stat-total .stat-num { color: var(--color-text-primary); }
.stat-active .stat-num { color: var(--color-info); }
.stat-human .stat-num { color: var(--color-warning); }
.stat-closed .stat-num { color: var(--color-text-tertiary); }

/* 筛选 */
.filter-bar {
  display: flex;
  gap: 16rpx;
  padding: 32rpx 24rpx 16rpx;
  overflow-x: auto;
  white-space: nowrap;
}
.filter-chip {
  display: inline-flex;
  align-items: center;
  padding: 12rpx 24rpx;
  font-size: 24rpx;
  background: var(--color-bg-elevated);
  color: var(--color-text-secondary);
  border-radius: var(--radius-full);
  border: 1rpx solid var(--color-divider);
  transition: all 0.2s;
}
.filter-chip.active {
  background: var(--color-primary-soft);
  color: var(--color-primary);
  border-color: var(--color-primary);
  font-weight: 500;
}

/* 会话列表 */
.conv-list {
  padding: 16rpx 24rpx 24rpx;
}
.conv-item {
  display: flex;
  gap: 20rpx;
  padding: 24rpx;
  background: var(--color-bg-elevated);
  border-radius: var(--radius-lg);
  margin-bottom: 16rpx;
  box-shadow: var(--shadow-xs);
  transition: all 0.2s;
}
.conv-item:active {
  transform: scale(0.98);
  background: var(--color-bg-muted);
}
.avatar {
  flex-shrink: 0;
  width: 88rpx;
  height: 88rpx;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #FFFFFF;
  font-size: 32rpx;
  font-weight: 600;
}
.content {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  justify-content: center;
  gap: 10rpx;
}
.row-1 {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.name {
  font-size: 30rpx;
  font-weight: 600;
  color: var(--color-text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  max-width: 360rpx;
}
.time {
  font-size: 22rpx;
  color: var(--color-text-tertiary);
  flex-shrink: 0;
}
.row-2 {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12rpx;
}
.summary {
  flex: 1;
  font-size: 24rpx;
  color: var(--color-text-secondary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.status-pill {
  flex-shrink: 0;
  padding: 4rpx 14rpx;
  font-size: 20rpx;
  border-radius: var(--radius-full);
  font-weight: 500;
}

/* 加载动画 */
.loading {
  display: flex;
  justify-content: center;
  gap: 12rpx;
  padding: 80rpx 0;
}
.dot {
  width: 16rpx;
  height: 16rpx;
  border-radius: 50%;
  background: var(--color-primary);
  animation: bounce 1.4s ease-in-out infinite both;
}
.dot:nth-child(1) { animation-delay: -0.32s; }
.dot:nth-child(2) { animation-delay: -0.16s; }
@keyframes bounce {
  0%, 80%, 100% { transform: scale(0.6); opacity: 0.5; }
  40% { transform: scale(1); opacity: 1; }
}

/* 空状态 */
.empty {
  text-align: center;
  padding: 120rpx 0;
}
.empty-icon {
  font-size: 96rpx;
  margin-bottom: 24rpx;
  opacity: 0.6;
}
.empty-title {
  display: block;
  font-size: 30rpx;
  color: var(--color-text-primary);
  font-weight: 500;
}
.empty-sub {
  display: block;
  font-size: 24rpx;
  color: var(--color-text-tertiary);
  margin-top: 12rpx;
}
</style>
