/**
 * SignalR 实时客户端封装
 *
 * 用法：
 *   import { realtime } from '@/realtime';
 *   realtime.on('message.new', (msg) => { ... });
 *   realtime.connect(token).catch(console.error);
 *   realtime.disconnect();
 *
 * 设计要点：
 *   1. 自动重连：onclose 后按 2s 退避策略重连最多 10 次
 *   2. 事件订阅：客户端不需要关心 connection 状态，断线期间的事件可由业务侧在重连后通过 REST 补齐
 *   3. JWT 鉴权：通过查询字符串传递 access_token（与后端 JwtBearerEvents.OnMessageReceived 配合）
 *   4. 单例：全应用共享一个连接，避免多次握手
 */
import * as signalR from '@microsoft/signalr';

export type RealtimeEvent =
  | 'connected'
  | 'subscribed'
  | 'message.new'
  | 'conversation.new_message'
  | 'conversation.status_changed'
  | 'conversation.status'
  | 'typing'
  | 'sla.warning'
  | 'customer.intention_changed';

type Handler = (payload: any) => void;

class RealtimeClient {
  private connection: signalR.HubConnection | null = null;
  private handlers = new Map<RealtimeEvent, Set<Handler>>();
  private retryCount = 0;
  private maxRetries = 10;
  private retryDelay = 2000;
  private currentToken: string | null = null;
  private isConnecting = false;

  /** 注册事件回调。返回取消订阅函数。 */
  on(event: RealtimeEvent, handler: Handler): () => void {
    if (!this.handlers.has(event)) this.handlers.set(event, new Set());
    this.handlers.get(event)!.add(handler);
    // 如果已经连接，把 handler 也挂到连接上（支持热订阅）
    this.connection?.on(event, handler);
    return () => this.off(event, handler);
  }

  off(event: RealtimeEvent, handler: Handler) {
    this.handlers.get(event)?.delete(handler);
    this.connection?.off(event, handler);
  }

  /** 建立连接。重复调用会先断开旧连接。 */
  async connect(token: string): Promise<void> {
    if (this.isConnecting) return;
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      // 已经连上了，仅刷新 token 绑定
      this.currentToken = token;
      return;
    }
    this.isConnecting = true;
    this.currentToken = token;
    try {
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/workbench', {
          accessTokenFactory: () => this.currentToken ?? ''
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (ctx) => {
            // 2s, 4s, 8s ... 上限 30s
            return Math.min(2000 * Math.pow(2, ctx.previousRetryCount), 30000);
          }
        })
        .configureLogging(signalR.LogLevel.Warning)
        .build();

      this.connection.onreconnecting(() => {
        // 业务层可在 Layout 监听此事件显示「连接中断」状态条
        console.warn('[realtime] reconnecting...');
      });
      this.connection.onreconnected(() => {
        console.info('[realtime] reconnected');
        this.retryCount = 0;
      });
      this.connection.onclose((err) => {
        console.warn('[realtime] closed', err);
        if (this.retryCount++ < this.maxRetries && this.currentToken) {
          setTimeout(() => this.connect(this.currentToken!), this.retryDelay);
        }
      });

      // 把已注册的 handler 全部挂上（用于重连后保留订阅）
      for (const [event, set] of this.handlers) {
        for (const h of set) this.connection.on(event, h);
      }

      await this.connection.start();
      this.retryCount = 0;
      console.info('[realtime] connected');
    } catch (e) {
      console.error('[realtime] connect failed', e);
      if (this.retryCount++ < this.maxRetries && this.currentToken) {
        setTimeout(() => this.connect(this.currentToken!), this.retryDelay * this.retryCount);
      }
    } finally {
      this.isConnecting = false;
    }
  }

  /** 主动断开（如登出时）。 */
  async disconnect() {
    this.currentToken = null;
    this.retryCount = 0;
    if (this.connection) {
      try {
        await this.connection.stop();
      } catch {
        /* ignore */
      }
      this.connection = null;
    }
  }

  /** 订阅某个会话的实时更新。 */
  async subscribeConversation(conversationId: string) {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('SubscribeConversation', conversationId);
    }
  }

  async unsubscribeConversation(conversationId: string) {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('UnsubscribeConversation', conversationId);
    }
  }

  async agentTyping(conversationId: string, isTyping: boolean) {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('AgentTyping', conversationId, isTyping);
    }
  }

  get state(): signalR.HubConnectionState | 'idle' {
    return this.connection?.state ?? 'idle';
  }
}

export const realtime = new RealtimeClient();