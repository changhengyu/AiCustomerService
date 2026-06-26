namespace AiCustomerService.Core.Interfaces;

/// <summary>
/// 实时事件通知抽象。Infrastructure 层通过该接口发送事件，由 Api 层
/// 的 SignalR 实现将事件转发到在线工作台 / 移动端。
/// 服务层不直接依赖 SignalR，保持 Clean Architecture 边界。
/// </summary>
public interface IRealtimeNotifier
{
    /// <summary>
    /// 通知租户下所有在线工作台：新消息到达。
    /// </summary>
    Task NewMessageAsync(Guid tenantId, Guid conversationId, Guid messageId,
        string role, string content, DateTime createdAt, CancellationToken ct = default);

    /// <summary>
    /// 通知租户下所有在线工作台：会话状态变化（转人工 / 关闭 / 分配）。
    /// </summary>
    Task ConversationStatusChangedAsync(Guid tenantId, Guid conversationId,
        string status, Guid? assignedTo, CancellationToken ct = default);

    /// <summary>
    /// 通知指定会话的订阅者：AI 正在打字（流式输出 chunk）。
    /// </summary>
    Task TypingAsync(Guid tenantId, Guid conversationId, string role, string? delta, CancellationToken ct = default);

    /// <summary>
    /// 通知租户：SLA 预警（如首次响应超时即将发生）。
    /// </summary>
    Task SlaWarningAsync(Guid tenantId, Guid conversationId, string reason, CancellationToken ct = default);

    /// <summary>
    /// 通知租户：客户意向度变化。
    /// </summary>
    Task CustomerIntentionChangedAsync(Guid tenantId, Guid customerId, string from, string to,
        int score, CancellationToken ct = default);
}