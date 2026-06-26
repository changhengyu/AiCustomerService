/**
 * uni-app 实时客户端（小程序 / H5 通用）
 *
 * 实现：uni.connectSocket 连接到后端原生 WebSocket 端点 /ws/workbench
 * 平台差异：
 *   - H5：uni.connectSocket 内部用 WebSocket
 *   - mp-weixin：uni.connectSocket 内部用 wx.connectSocket
 *   - 两种平台都返回相同的 UniWebSocketTask 接口，事件订阅方式一致
 *
 * 与 SignalR 协议不同：使用纯 JSON Lines 协议，每行一条事件，客户端发送 JSON 命令订阅会话。
 *
 * 用法：
 *   import { realtime } from '@/realtime';
 *   realtime.on('message.new', (p) => { ... });
 *   await realtime.connect();
 *   await realtime.subscribeConversation(id);
 */

export type RealtimeEvent =
  | 'connected'
  | 'message.new'
  | 'conversation.new_message'
  | 'conversation.status_changed'
  | 'conversation.status'
  | 'typing'
  | 'sla.warning'
  | 'customer.intention_changed';

type Handler = (payload: any) => void;

interface RealtimeEventEnvelope {
  event: RealtimeEvent;
  conversation_id?: string;
  payload: any;
  at: string;
}

class UniRealtimeClient {
  private socketTask: any = null;
  private handlers = new Map<RealtimeEvent, Set<Handler>>();
  private subscribed = new Set<string>();
  private retryCount = 0;
  private maxRetries = 8;
  private retryDelay = 2000;
  private isConnecting = false;
  private closedByUser = false;
  private pendingBuffer = '';

  on(event: RealtimeEvent, handler: Handler): () => void {
    if (!this.handlers.has(event)) this.handlers.set(event, new Set());
    this.handlers.get(event)!.add(handler);
    return () => this.off(event, handler);
  }

  off(event: RealtimeEvent, handler: Handler) {
    this.handlers.get(event)?.delete(handler);
  }

  async connect(): Promise<void> {
    if (this.isConnecting) return;
    if (this.socketTask) return;
    const token = uni.getStorageSync('access_token') || '';
    if (!token) {
      console.warn('[realtime] no access_token, skip connect');
      return;
    }
    const baseUrl = this.resolveBaseUrl();
    const url = `${baseUrl}/ws/workbench?access_token=${encodeURIComponent(token)}`;

    this.isConnecting = true;
    this.closedByUser = false;
    try {
      this.socketTask = uni.connectSocket({
        url,
        success: () => console.info('[realtime] connecting', url),
        fail: (err: any) => {
          console.error('[realtime] connect fail', err);
          this.scheduleReconnect();
        }
      });

      this.socketTask.onOpen(() => {
        console.info('[realtime] connected');
        this.retryCount = 0;
        // 重连后重新订阅之前的会话
        for (const convId of this.subscribed) {
          this.sendCommand({ action: 'subscribe_conversation', conversation_id: convId });
        }
      });

      this.socketTask.onMessage((res: any) => {
        this.handleMessage(res.data);
      });

      this.socketTask.onError((err: any) => {
        console.error('[realtime] error', err);
      });

      this.socketTask.onClose(() => {
        console.warn('[realtime] closed');
        this.socketTask = null;
        if (!this.closedByUser) this.scheduleReconnect();
      });
    } finally {
      this.isConnecting = false;
    }
  }

  disconnect() {
    this.closedByUser = true;
    this.retryCount = 0;
    if (this.socketTask) {
      try {
        this.socketTask.close({ code: 1000 });
      } catch {
        /* ignore */
      }
      this.socketTask = null;
    }
  }

  async subscribeConversation(conversationId: string) {
    this.subscribed.add(conversationId);
    this.sendCommand({ action: 'subscribe_conversation', conversation_id: conversationId });
  }

  async unsubscribeConversation(conversationId: string) {
    this.subscribed.delete(conversationId);
    this.sendCommand({ action: 'unsubscribe_conversation', conversation_id: conversationId });
  }

  private sendCommand(cmd: Record<string, any>) {
    if (!this.socketTask) return;
    try {
      this.socketTask.send({ data: JSON.stringify(cmd) });
    } catch (e) {
      console.warn('[realtime] send failed', e);
    }
  }

  private resolveBaseUrl(): string {
    // #ifdef H5
    return ''; // H5 下用相对路径，Vite 代理到后端
    // #endif
    // #ifndef H5
    const accountInfo: any = uni.getSystemInfoSync();
    // 小程序开发期通常需要手动配置后端地址（manifest.json 的 devServer 配置可省）
    // 这里用一个常见猜测：开发期走局域网或本机
    return accountInfo?.platform === 'devtools' ? 'http://localhost:5000' : '';
    // #endif
  }

  private handleMessage(raw: string | ArrayBuffer) {
    if (typeof raw !== 'string') return;
    // JSON Lines：可能一次收到多条
    this.pendingBuffer += raw;
    const lines = this.pendingBuffer.split('\n');
    this.pendingBuffer = lines.pop() ?? '';
    for (const line of lines) {
      if (!line.trim()) continue;
      try {
        const env = JSON.parse(line) as RealtimeEventEnvelope;
        const set = this.handlers.get(env.event);
        if (set) for (const h of set) h(env.payload);
      } catch (e) {
        console.warn('[realtime] parse fail', e, line);
      }
    }
  }

  private scheduleReconnect() {
    if (this.closedByUser) return;
    if (this.retryCount >= this.maxRetries) return;
    const delay = this.retryDelay * Math.pow(2, this.retryCount);
    this.retryCount++;
    console.info(`[realtime] retry in ${delay}ms (#${this.retryCount})`);
    setTimeout(() => this.connect(), delay);
  }

  get connected(): boolean {
    return !!this.socketTask;
  }
}

export const realtime = new UniRealtimeClient();