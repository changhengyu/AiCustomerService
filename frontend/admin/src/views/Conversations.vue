<script setup lang="ts">
import { onMounted, onBeforeUnmount, ref } from 'vue';
import { conversationApi, type ConversationListItem } from '@/api';

const list = ref<ConversationListItem[]>([]);
const total = ref(0);
const loading = ref(false);
const statusFilter = ref<string>('');
const page = ref(1);

async function load() {
  loading.value = true;
  try {
    const r = await conversationApi.list({ page: page.value, page_size: 20, status: statusFilter.value || undefined });
    list.value = r.items;
    total.value = r.total;
  } finally { loading.value = false; }
}

function statusType(s: string) {
  return { active: 'success', human: 'warning', closed: 'info' }[s] || '';
}

function onNewMessageEvent() {
  // Layout.vue 广播的全局事件：租户下任何会话有新消息 → 刷新列表
  load();
}

function onStatusEvent() {
  load();
}

onMounted(() => {
  load();
  window.addEventListener('aics:new_message', onNewMessageEvent);
  window.addEventListener('aics:conversation_status', onStatusEvent);
});

onBeforeUnmount(() => {
  window.removeEventListener('aics:new_message', onNewMessageEvent);
  window.removeEventListener('aics:conversation_status', onStatusEvent);
});
</script>

<template>
  <el-card>
    <div class="toolbar">
      <el-select v-model="statusFilter" placeholder="全部状态" clearable @change="load" style="width:160px;">
        <el-option label="AI 进行中" value="active" />
        <el-option label="人工接管" value="human" />
        <el-option label="已关闭" value="closed" />
      </el-select>
      <el-button @click="load">刷新</el-button>
      <span style="margin-left:12px;color:#67c23a;font-size:12px;">● 实时同步中</span>
    </div>

    <el-table :data="list" v-loading="loading">
      <el-table-column label="客户" prop="customer_nickname" />
      <el-table-column label="渠道" prop="channel_type" width="100" />
      <el-table-column label="状态" width="120">
        <template #default="{ row }">
          <el-tag :type="statusType(row.status)">{{ row.status }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="消息数" prop="message_count" width="100" />
      <el-table-column label="最后消息" prop="last_message_at" width="180" />
      <el-table-column label="操作" width="120">
        <template #default="{ row }">
          <el-button text type="primary" @click="$router.push(`/conversations/${row.id}`)">查看</el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-pagination
      v-model:current-page="page"
      :total="total"
      :page-size="20"
      layout="prev, pager, next"
      @current-change="load"
      style="margin-top:16px;"
    />
  </el-card>
</template>