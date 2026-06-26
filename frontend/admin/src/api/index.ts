import { http } from './http';

export interface LoginRequest { username: string; password: string; tenant_id: string; }
export interface LoginResponse {
  access_token: string;
  refresh_token: string;
  expires_at: string;
  user: { id: string; tenant_id: string; username: string; display_name: string; role: string };
}
export interface PagedResult<T> { items: T[]; total: number; page: number; page_size: number; }
export interface ConversationListItem {
  id: string; customer_id: string; customer_nickname?: string;
  channel_type: string; status: string; message_count: number;
  summary?: string; last_message_at: string; created_at: string;
}
export interface DocumentDto {
  id: string; title: string; status: string; chunk_count: number;
  file_size: number; created_at: string; processed_at?: string;
  error_message?: string; job_id?: string;
}
export interface CustomerListItem {
  id: string; nickname?: string; avatar_url?: string; channel_type: string;
  intention_level: string; intention_score: number; tags: string[];
  last_seen_at: string;
}

export const authApi = {
  login: (data: LoginRequest) => http.post<LoginResponse, LoginResponse>('/auth/login', data),
  register: (data: any) => http.post('/auth/register', data),
  refresh: (refresh_token: string) => http.post('/auth/refresh', { refresh_token })
};

export const conversationApi = {
  list: (params: { page?: number; page_size?: number; status?: string }) =>
    http.get<PagedResult<ConversationListItem>, PagedResult<ConversationListItem>>('/conversations', { params }),
  detail: (id: string) => http.get(`/conversations/${id}`),
  handoff: (id: string, assigned_to?: string) => http.post(`/conversations/${id}/handoff`, { assigned_to }),
  close: (id: string) => http.post(`/conversations/${id}/close`),
  agentSend: (id: string, content: string) => http.post(`/conversations/${id}/messages/agent`, { content })
};

export const knowledgeApi = {
  list: (params: { page?: number; page_size?: number }) =>
    http.get<PagedResult<DocumentDto>, PagedResult<DocumentDto>>('/knowledge/documents', { params }),
  upload: (formData: FormData) => http.post('/knowledge/documents', formData, {
    headers: { 'Content-Type': 'multipart/form-data' }
  }),
  remove: (id: string) => http.delete(`/knowledge/documents/${id}`),
  job: (id: string) => http.get(`/knowledge/documents/${id}/job`),
  reindex: (id: string) => http.post(`/knowledge/documents/${id}/reindex`)
};

export const customerApi = {
  list: (params: any) => http.get<PagedResult<CustomerListItem>, PagedResult<CustomerListItem>>('/customers', { params }),
  detail: (id: string) => http.get(`/customers/${id}`),
  updateTags: (id: string, tags: string[]) => http.put(`/customers/${id}/tags`, { tags })
};

export const tenantApi = {
  current: () => http.get('/tenant'),
  getSettings: () => http.get('/tenant/settings'),
  updateSettings: (data: any) => http.put('/tenant/settings', data)
};