export interface LoginResponse {
  access_token: string;
  refresh_token: string;
  expires_at: string;
  user: { id: string; tenant_id: string; username: string; display_name: string; role: string };
}

export interface ConversationListItem {
  id: string;
  customer_id: string;
  customer_nickname?: string;
  channel_type: string;
  status: string;
  message_count: number;
  summary?: string;
  last_message_at: string;
  created_at: string;
}