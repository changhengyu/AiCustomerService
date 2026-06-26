<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { tenantApi } from '@/api';

const form = ref<any>({
  system_prompt: '',
  welcome_message: '',
  handoff_keywords: [],
  industry_id: null,
  use_industry_faq: false
});

const saving = ref(false);

onMounted(async () => {
  try {
    form.value = await tenantApi.getSettings();
  } catch (e) { /* ignore */ }
});

async function save() {
  saving.value = true;
  try {
    await tenantApi.updateSettings(form.value);
    ElMessage.success('保存成功');
  } finally { saving.value = false; }
}
</script>

<template>
  <el-card>
    <h3>租户设置</h3>
    <el-form label-width="140px">
      <el-form-item label="System Prompt">
        <el-input v-model="form.system_prompt" type="textarea" :rows="6" placeholder="定义 AI 客服的角色与风格" />
      </el-form-item>
      <el-form-item label="欢迎语">
        <el-input v-model="form.welcome_message" />
      </el-form-item>
      <el-form-item label="转人工关键词">
        <el-input v-model="form.handoff_keywords" placeholder="逗号分隔：人工, 转人工, 客服" />
      </el-form-item>
      <el-form-item label="启用行业 FAQ">
        <el-switch v-model="form.use_industry_faq" />
      </el-form-item>
      <el-form-item>
        <el-button type="primary" :loading="saving" @click="save">保存</el-button>
      </el-form-item>
    </el-form>
  </el-card>
</template>