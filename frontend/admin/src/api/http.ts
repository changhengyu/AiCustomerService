import axios, { type AxiosInstance } from 'axios';
import { ElMessage } from 'element-plus';
import { useAuthStore } from '@/stores/auth';

export const http: AxiosInstance = axios.create({
  baseURL: '/api/v1',
  timeout: 30000
});

http.interceptors.request.use((config) => {
  const auth = useAuthStore();
  if (auth.accessToken) {
    config.headers.Authorization = `Bearer ${auth.accessToken}`;
  }
  return config;
});

http.interceptors.response.use(
  (r) => r.data,
  (err) => {
    const msg = err.response?.data?.message ?? err.message ?? '请求失败';
    ElMessage.error(msg);
    if (err.response?.status === 401) {
      const auth = useAuthStore();
      auth.logout();
      window.location.href = '/login';
    }
    return Promise.reject(err);
  }
);