using System.Security.Claims;
using AiCustomerService.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AiCustomerService.Api.Hubs;

/// <summary>
/// 工作台实时推送 Hub。
/// - 客户端通过查询字符串 ?access_token=xxx 完成 JWT 鉴权握手
/// - 自动加入 "tenant:{tenantId}" 与 "conversation:{conversationId}" 组
/// - 服务端通过 IRealtimeNotifier 触发推送，按租户 / 会话组广播
///
/// 客户端连接示例：
///   admin:  new HubConnectionBuilder().withUrl("/hubs/workbench", { accessTokenFactory: () => token }).build()
///   H5:     uni.connectSocket({ url: `wss://${host}/hubs/workbench?access_token=${token}` })
/// </summary>
[Authorize]
public class WorkbenchHub : Hub
{
    private readonly ITenantContext _tenantCtx;
    private readonly ILogger<WorkbenchHub> _logger;

    public WorkbenchHub(ITenantContext tenantCtx, ILogger<WorkbenchHub> logger)
    {
        _tenantCtx = tenantCtx;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var tenantId = _tenantCtx.RequireTenantId();
        var userId = _tenantCtx.CurrentUserId;
        var groupName = $"tenant:{tenantId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("SignalR connected: connectionId={ConnId} tenant={TenantId} user={UserId}",
            Context.ConnectionId, tenantId, userId);
        await Clients.Caller.SendAsync("connected", new { tenantId, userId, connectionId = Context.ConnectionId });
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var tenantId = _tenantCtx.CurrentTenantId;
        if (tenantId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant:{tenantId.Value}");
        }
        _logger.LogInformation("SignalR disconnected: connectionId={ConnId} reason={Reason}",
            Context.ConnectionId, exception?.Message);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// 客户端订阅某个会话的实时更新（用于工作台进入会话详情时调用）。
    /// </summary>
    public async Task SubscribeConversation(Guid conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation:{conversationId}");
        await Clients.Caller.SendAsync("subscribed", new { conversationId });
    }

    /// <summary>
    /// 客户端取消订阅某个会话（用于工作台离开会话详情时调用）。
    /// </summary>
    public async Task UnsubscribeConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation:{conversationId}");
    }

    /// <summary>
    /// 客服端上报"正在输入"状态，转发给同会话其他订阅者。
    /// </summary>
    public async Task AgentTyping(Guid conversationId, bool isTyping)
    {
        var tenantId = _tenantCtx.RequireTenantId();
        var userId = _tenantCtx.CurrentUserId;
        await Clients.OthersInGroup($"conversation:{conversationId}").SendAsync("typing", new
        {
            conversationId,
            role = "agent",
            userId,
            isTyping
        });
    }
}