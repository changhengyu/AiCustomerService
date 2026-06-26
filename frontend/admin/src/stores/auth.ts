import { defineStore } from 'pinia';
import { ref } from 'vue';
import { authApi, type LoginResponse } from '@/api';

export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref<string>(localStorage.getItem('access_token') || '');
  const refreshToken = ref<string>(localStorage.getItem('refresh_token') || '');
  const user = ref<LoginResponse['user'] | null>(null);

  function setSession(r: LoginResponse) {
    accessToken.value = r.access_token;
    refreshToken.value = r.refresh_token;
    user.value = r.user;
    localStorage.setItem('access_token', r.access_token);
    localStorage.setItem('refresh_token', r.refresh_token);
  }

  async function login(username: string, password: string, tenantId: string) {
    const r = await authApi.login({ username, password, tenant_id: tenantId });
    setSession(r);
    return r;
  }

  function logout() {
    accessToken.value = '';
    refreshToken.value = '';
    user.value = null;
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
  }

  return { accessToken, refreshToken, user, login, logout };
});