<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { knowledgeApi, type DocumentDto } from '@/api';

const list = ref<DocumentDto[]>([]);
const total = ref(0);
const page = ref(1);
const loading = ref(false);
const uploadDialog = ref(false);
const uploadForm = ref({ title: '', file: null as File | null });

async function load() {
  loading.value = true;
  try {
    const r = await knowledgeApi.list({ page: page.value, page_size: 20 });
    list.value = r.items;
    total.value = r.total;
  } finally { loading.value = false; }
}

function statusType(s: string) {
  return { ready: 'success', processing: 'warning', pending: 'info', failed: 'danger', deleted: 'info' }[s] || '';
}

async function upload() {
  if (!uploadForm.value.file) return ElMessage.warning('请选择文件');
  const fd = new FormData();
  fd.append('title', uploadForm.value.title || uploadForm.value.file.name);
  fd.append('file', uploadForm.value.file);
  await knowledgeApi.upload(fd);
  ElMessage.success('上传成功，正在后台处理');
  uploadDialog.value = false;
  uploadForm.value = { title: '', file: null };
  await load();
}

async function reindex(id: string) {
  await knowledgeApi.reindex(id);
  ElMessage.success('已加入重建队列');
}

async function remove(id: string) {
  await knowledgeApi.remove(id);
  ElMessage.success('已删除');
  await load();
}

onMounted(load);
</script>

<template>
  <el-card>
    <div class="toolbar">
      <h3>知识库（向量检索 + 关键词搜索）</h3>
      <el-button type="primary" @click="uploadDialog = true">上传文档</el-button>
    </div>

    <el-table :data="list" v-loading="loading">
      <el-table-column label="标题" prop="title" />
      <el-table-column label="状态" width="120">
        <template #default="{ row }">
          <el-tag :type="statusType(row.status)">{{ row.status }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="分块数" prop="chunk_count" width="100" />
      <el-table-column label="文件大小" width="120">
        <template #default="{ row }">{{ (row.file_size / 1024).toFixed(1) }} KB</template>
      </el-table-column>
      <el-table-column label="处理时间" prop="processed_at" width="180" />
      <el-table-column label="操作" width="200">
        <template #default="{ row }">
          <el-button text @click="reindex(row.id)">重建</el-button>
          <el-button text type="danger" @click="remove(row.id)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-pagination v-model:current-page="page" :total="total" :page-size="20" layout="prev, pager, next" @current-change="load" style="margin-top:16px;" />
  </el-card>

  <el-dialog v-model="uploadDialog" title="上传文档" width="500">
    <el-form>
      <el-form-item label="文档标题">
        <el-input v-model="uploadForm.title" placeholder="留空则使用文件名" />
      </el-form-item>
      <el-form-item label="文件（PDF/Word/TXT/MD/CSV）">
        <el-upload :auto-upload="false" :limit="1" :on-change="(f: any) => uploadForm.file = f.raw">
          <el-button>选择文件</el-button>
        </el-upload>
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="uploadDialog = false">取消</el-button>
      <el-button type="primary" @click="upload">上传</el-button>
    </template>
  </el-dialog>
</template>