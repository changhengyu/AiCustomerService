using AiCustomerService.Api.Hubs;
using AiCustomerService.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace AiCustomerService.Api.Realtime;

/// <summary>
/// IRealtimeNotifier 的 SignalR 实现：把事件广播到 tenant:* 与 conversation:* 组。
/// 注册在 Api 层，因为 Infrastructure 不依赖 ASP.NET Core（保持 Clean Architecture）。
/// </summary>
public class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<WorkbenchHub> _hub;
    private readonly ILogger<SignalRRealtimeNotifier> _logger;

    public SignalRRealtimeNotifier(IHubContext<WorkbenchHub> hub, ILogger<SignalRRealtimeNotifier> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task NewMessageAsync(Guid tenantId, Guid conversationId, Guid messageId,
        string role, string content, DateTime createdAt, CancellationToken ct = default)
    {
        var payload = new
        {
            conversationId,
            messageId,
            role,
            content,
            contentType = "text",
            createdAt
        };
        // 同时广播到租户组（用于刷新会话列表）和会话组（用于详情页追加消息）
        await _hub.Clients.Group($"tenant:{tenantId}").SendAsync("conversation.new_message", payload, ct);
        await _hub.Clients.Group($"conversation:{conversationId}").SendAsync("message.new", payload, ct);
        // 原生 WebSocket 总线（供 uni-app 小程序 / H5 使用）
        await RealtimeBus.PublishAsync(new RealtimeBus.Event(
            "message.new", tenantId, conversationId, payload, DateTime.UtcNow));
        _logger.LogDebug("Realtime new_message: tenant={TenantId} conv={ConvId} role={Role}", tenantId, conversationId, role);
    }

    public async Task ConversationStatusChangedAsync(Guid tenantId, Guid conversationId,
        string status, Guid? assignedTo, CancellationToken ct = default)
    {
        var payload = new { conversationId, status, assignedTo, at = DateTime.UtcNow };
        await _hub.Clients.Group($"tenant:{tenantId}").SendAsync("conversation.status_changed", payload, ct);
        await _hub.Clients.Group($"conversation:{conversationId}").SendAsync("conversation.status", payload, ct);
        await RealtimeBus.PublishAsync(new RealtimeBus.Event(
            "conversation.status_changed", tenantId, conversationId, payload, DateTime.UtcNow));
    }

    public async Task TypingAsync(Guid tenantId, Guid conversationId, string role,
        string? delta, CancellationToken ct = default)
    {
        var payload = new { conversationId, role, delta, at = DateTime.UtcNow };
        await _hub.Clients.Group($"conversation:{conversationId}").SendAsync("typing", payload, ct);
        await RealtimeBus.PublishAsync(new RealtimeBus.Event(
            "typing", tenantId, conversationId, payload, DateTime.UtcNow));
    }

    public async Task SlaWarningAsync(Guid tenantId, Guid conversationId, string reason, CancellationToken ct = default)
    {
        var payload = new { conversationId, reason, at = DateTime.UtcNow };
        await _hub.Clients.Group($"tenant:{tenantId}").SendAsync("sla.warning", payload, ct);
        await _hub.Clients.Group($"conversation:{conversationId}").SendAsync("sla.warning", payload, ct);
        await RealtimeBus.PublishAsync(new RealtimeBus.Event(
            "sla.warning", tenantId, conversationId, payload, DateTime.UtcNow));
    }

    public async Task CustomerIntentionChangedAsync(Guid tenantId, Guid customerId, string from, string to,
        int score, CancellationToken ct = default)
    {
        var payload = new { customerId, from, to, score, at = DateTime.UtcNow };
        await _hub.Clients.Group($"tenant:{tenantId}").SendAsync("customer.intention_changed", payload, ct);
        await RealtimeBus.PublishAsync(new RealtimeBus.Event(
            "customer.intention_changed", tenantId, customerId, payload, DateTime.UtcNow));
    }
}