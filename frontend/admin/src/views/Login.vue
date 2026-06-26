<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/auth';

const router = useRouter();
const auth = useAuthStore();

const form = ref({
  username: 'admin',
  password: 'admin123',
  tenantId: '11111111-1111-1111-1111-111111111111'
});

const loading = ref(false);
const error = ref('');

async function submit() {
  loading.value = true;
  error.value = '';
  try {
    await auth.login(form.value.username, form.value.password, form.value.tenantId);
    router.push('/dashboard');
  } catch (e: any) {
    error.value = e?.response?.data?.message || '登录失败';
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <div style="height:100vh;display:flex;justify-content:center;align-items:center;background:#f0f2f5;">
    <el-card style="width:400px;">
      <h2 style="text-align:center;margin-top:0;">AI 客服 SaaS 登录</h2>
      <el-form @submit.prevent="submit">
        <el-form-item label="租户 ID">
          <el-input v-model="form.tenantId" />
        </el-form-item>
        <el-form-item label="用户名">
          <el-input v-model="form.username" />
        </el-form-item>
        <el-form-item label="密码">
          <el-input v-model="form.password" type="password" show-password />
        </el-form-item>
        <el-button type="primary" :loading="loading" @click="submit" style="width:100%;">登录</el-button>
        <el-alert v-if="error" :title="error" type="error" show-icon style="margin-top:12px;" />
      </el-form>
    </el-card>
  </div>
</template>