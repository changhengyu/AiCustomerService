<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { tenantApi, conversationApi, customerApi } from '@/api';

const tenant = ref<any>({});
const convCount = ref(0);
const customerCount = ref(0);

onMounted(async () => {
  try {
    tenant.value = await tenantApi.current();
    const convs = await conversationApi.list({ page: 1, page_size: 1 });
    convCount.value = convs.total;
    const cus = await customerApi.list({ page: 1, page_size: 1 });
    customerCount.value = cus.total;
  } catch (e) { /* ignore */ }
});
</script>

<template>
  <el-row :gutter="20">
    <el-col :span="6">
      <el-card>
        <h3>租户</h3>
        <p>{{ tenant.name }}</p>
        <p>套餐：{{ tenant.plan }}</p>
        <p>状态：{{ tenant.status }}</p>
      </el-card>
    </el-col>
    <el-col :span="6">
      <el-card>
        <h3>本月用量</h3>
        <p style="font-size:32px;color:#1890ff;">{{ tenant.monthly_message_used ?? 0 }}</p>
        <p>配额 {{ tenant.monthly_message_quota ?? 0 }}</p>
      </el-card>
    </el-col>
    <el-col :span="6">
      <el-card>
        <h3>会话总数</h3>
        <p style="font-size:32px;color:#52c41a;">{{ convCount }}</p>
        <router-link to="/conversations">查看会话 →</router-link>
      </el-card>
    </el-col>
    <el-col :span="6">
      <el-card>
        <h3>客户数</h3>
        <p style="font-size:32px;color:#faad14;">{{ customerCount }}</p>
        <router-link to="/customers">查看客户 →</router-link>
      </el-card>
    </el-col>
  </el-row>

  <el-card style="margin-top:20px;">
    <h3>快速开始</h3>
    <ol>
      <li>在「知识库」上传产品文档</li>
      <li>在「设置」配置 System Prompt 与欢迎语</li>
      <li>配置公众号回调地址：<code>https://your-domain/api/v1/wechat/{appId}</code></li>
      <li>访问 <a href="/hangfire" target="_blank">/hangfire</a> 查看后台任务</li>
    </ol>
  </el-card>
</template>