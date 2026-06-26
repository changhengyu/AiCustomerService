import type { LoginResponse } from './types';

const BASE = '/api/v1';

function getToken() {
  return uni.getStorageSync('access_token') || '';
}

export async function request<T = any>(
  url: string,
  method: 'GET' | 'POST' | 'PUT' | 'DELETE' = 'GET',
  data?: any
): Promise<T> {
  return new Promise((resolve, reject) => {
    uni.request({
      url: BASE + url,
      method,
      data,
      header: { Authorization: `Bearer ${getToken()}`, 'Content-Type': 'application/json' },
      success: (r) => {
        if (r.statusCode >= 200 && r.statusCode < 300) resolve(r.data as T);
        else reject(r.data);
      },
      fail: (e) => reject(e)
    });
  });
}

export const authApi = {
  login: (data: { username: string; password: string; tenant_id: string }) =>
    request<LoginResponse>('/auth/login', 'POST', data)
};

export const conversationApi = {
  list: (params: any) => request('/conversations', 'GET', params),
  detail: (id: string) => request(`/conversations/${id}`, 'GET'),
  agentSend: (id: string, content: string) =>
    request(`/conversations/${id}/messages/agent`, 'POST', { content }),
  handoff: (id: string) => request(`/conversations/${id}/handoff`, 'POST'),
  close: (id: string) => request(`/conversations/${id}/close`, 'POST')
};

export const customerApi = {
  list: (params: any) => request('/customers', 'GET', params),
  detail: (id: string) => request(`/customers/${id}`, 'GET'),
  updateTags: (id: string, tags: string[]) => request(`/customers/${id}/tags`, 'PUT', { tags })
};