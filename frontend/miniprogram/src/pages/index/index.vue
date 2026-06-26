<script setup lang="ts">
import { ref, onMounted, onShow } from 'vue';
import { conversationApi, type ConversationListItem } from '@/api';

const list = ref<ConversationListItem[]>([]);
const statusFilter = ref<string>('');
const loading = ref(false);

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

function statusColor(s: string) {
  return { active: '#52c41a', human: '#faad14', closed: '#999' }[s] || '#999';
}

onShow(load);
onMounted(load);
</script>

<template>
  <view class="filter-bar">
    <text :class="{ active: !statusFilter }" @click="statusFilter = ''; load()">全部</text>
    <text :class="{ active: statusFilter === 'active' }" @click="statusFilter = 'active'; load()">AI</text>
    <text :class="{ active: statusFilter === 'human' }" @click="statusFilter = 'human'; load()">人工</text>
    <text :class="{ active: statusFilter === 'closed' }" @click="statusFilter = 'closed'; load()">已关</text>
  </view>

  <view class="conv-list">
    <view v-for="c in list" :key="c.id" class="conv-item" @click="openChat(c)">
      <view class="avatar">{{ (c.customer_nickname || 'U')[0] }}</view>
      <view class="content">
        <view class="name">{{ c.customer_nickname || c.customer_id.slice(0, 8) }}</view>
        <view class="summary">{{ c.summary || `${c.message_count} 条消息` }}</view>
      </view>
      <view class="meta">
        <view class="status" :style="{ background: statusColor(c.status) }">{{ c.status }}</view>
        <text class="time">{{ c.last_message_at?.slice(5, 16) }}</text>
      </view>
    </view>
    <view v-if="!loading && list.length === 0" class="empty">暂无会话</view>
  </view>
</template>

<style scoped>
.filter-bar { display: flex; gap: 20rpx; padding: 20rpx; background: #fff; border-bottom: 1rpx solid #eee; }
.filter-bar text { padding: 8rpx 20rpx; border-radius: 30rpx; background: #f0f0f0; font-size: 24rpx; }
.filter-bar text.active { background: #1890ff; color: #fff; }
.conv-item { display: flex; padding: 24rpx; background: #fff; border-bottom: 1rpx solid #f0f0f0; }
.avatar { width: 80rpx; height: 80rpx; border-radius: 50%; background: #1890ff; color: #fff; text-align: center; line-height: 80rpx; font-size: 32rpx; margin-right: 20rpx; }
.content { flex: 1; }
.name { font-size: 30rpx; font-weight: bold; }
.summary { font-size: 24rpx; color: #999; margin-top: 8rpx; }
.meta { text-align: right; }
.status { display: inline-block; padding: 4rpx 12rpx; border-radius: 8rpx; color: #fff; font-size: 20rpx; }
.time { display: block; font-size: 22rpx; color: #999; margin-top: 8rpx; }
.empty { text-align: center; padding: 100rpx; color: #999; }
</style>