<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { customerApi, type CustomerListItem } from '@/api';

const list = ref<CustomerListItem[]>([]);
const total = ref(0);
const page = ref(1);
const filters = ref({ intentionLevel: '', keyword: '' });
const loading = ref(false);

async function load() {
  loading.value = true;
  try {
    const r = await customerApi.list({ page: page.value, page_size: 20, ...filters.value });
    list.value = r.items;
    total.value = r.total;
  } finally { loading.value = false; }
}

function intentionColor(l: string) {
  return { high: 'danger', medium: 'warning', low: 'info', cold: '' }[l] || '';
}

onMounted(load);
</script>

<template>
  <el-card>
    <div class="toolbar">
      <el-input v-model="filters.keyword" placeholder="搜索昵称/外部ID" style="width:200px;margin-right:8px;" clearable @change="load" />
      <el-select v-model="filters.intentionLevel" placeholder="意向" clearable style="width:120px;margin-right:8px;" @change="load">
        <el-option label="高" value="high" />
        <el-option label="中" value="medium" />
        <el-option label="低" value="low" />
        <el-option label="冷" value="cold" />
      </el-select>
      <el-button @click="load">查询</el-button>
    </div>

    <el-table :data="list" v-loading="loading">
      <el-table-column label="客户">
        <template #default="{ row }">
          <el-avatar v-if="row.avatar_url" :src="row.avatar_url" :size="32" />
          <span style="margin-left:8px;">{{ row.nickname || row.id.slice(0, 8) }}</span>
        </template>
      </el-table-column>
      <el-table-column label="渠道" prop="channel_type" width="120" />
      <el-table-column label="意向" width="100">
        <template #default="{ row }">
          <el-tag :type="intentionColor(row.intention_level)">{{ row.intention_level }}</el-tag>
          <small> {{ row.intention_score }}</small>
        </template>
      </el-table-column>
      <el-table-column label="标签" width="240">
        <template #default="{ row }">
          <el-tag v-for="t in row.tags" :key="t" size="small" style="margin-right:4px;">{{ t }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="最近互动" prop="last_seen_at" width="180" />
    </el-table>

    <el-pagination v-model:current-page="page" :total="total" :page-size="20" layout="prev, pager, next" @current-change="load" style="margin-top:16px;" />
  </el-card>
</template>