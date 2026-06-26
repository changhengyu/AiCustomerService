using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AiCustomerService.Core.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace AiCustomerService.Api.Realtime;

/// <summary>
/// 实时事件总线：在 SignalR 通知与 WebSocket 通知之间共享。
/// 写入：SignalRRealtimeNotifier 调用 Publish。
/// 读取：原生 WebSocket handler 订阅 ReceiveAsync 流。
/// </summary>
public static class RealtimeBus
{
    public record Event(string Type, Guid TenantId, Guid ConversationId, object Payload, DateTime At);

    private static readonly ConcurrentDictionary<Guid, Subscription> _subs = new();

    public static IDisposable Subscribe(Guid tenantId, Func<Event, Task> handler)
    {
        var sub = new Subscription(handler);
        _subs[tenantId] = sub;
        return new Unsub(() => _subs.TryRemove(new KeyValuePair<Guid, Subscription>(tenantId, sub)));
    }

    public static Task PublishAsync(Event ev)
    {
        if (_subs.TryGetValue(ev.TenantId, out var sub))
        {
            // 失败也不抛：避免影响主业务流
            _ = Task.Run(async () =>
            {
                try { await sub.Handler(ev); } catch { /* swallow */ }
            });
        }
        return Task.CompletedTask;
    }

    private sealed record Subscription(Func<Event, Task> Handler);
    private sealed class Unsub : IDisposable
    {
        private readonly Action _a;
        public Unsub(Action a) { _a = a; }
        public void Dispose() => _a();
    }
}

/// <summary>
/// 原生 WebSocket 端点 /ws/workbench
/// 协议：服务端 JSON Lines（每行一个事件），客户端可发 JSON 命令（如订阅会话）。
/// 适用于：uni-app 小程序 / H5（uni.connectSocket 平台通用）。
/// </summary>
public static class WorkbenchWebSocketEndpoint
{
    public static void MapWorkbenchWebSocket(this IEndpointRouteBuilder routes)
    {
        routes.Map("/ws/workbench", async (HttpContext ctx) =>
        {
            if (!ctx.WebSockets.IsWebSocketRequest)
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                await ctx.Response.WriteAsync("Expected WebSocket upgrade");
                return;
            }

            // 鉴权：复用 JWT 校验（SignalR 的 OnMessageReceived 也处理 query token）
            var jwtSecret = ctx.RequestServices
                .GetRequiredService<IConfiguration>()["Jwt:Secret"]
                ?? "your-very-long-secret-key-at-least-32-chars-please-change";
            var jwtIssuer = ctx.RequestServices.GetRequiredService<IConfiguration>()["Jwt:Issuer"] ?? "AiCustomerService";
            var jwtAudience = ctx.RequestServices.GetRequiredService<IConfiguration>()["Jwt:Audience"] ?? "AiCustomerService";

            var token = ctx.Request.Query["access_token"].ToString();
            if (string.IsNullOrEmpty(token))
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var handler = new JwtSecurityTokenHandler();
            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer, ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
            };
            ClaimsPrincipal? principal = null;
            try
            {
                principal = handler.ValidateToken(token, validationParams, out _);
            }
            catch
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var tenantClaim = principal.FindFirst("tenant_id")?.Value;
            if (!Guid.TryParse(tenantClaim, out var tenantId))
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            using var ws = await ctx.WebSockets.AcceptWebSocketAsync();
            var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("WorkbenchWS");

            var subscribedConversations = new HashSet<Guid>();
            using var unsubscribe = RealtimeBus.Subscribe(tenantId, async ev =>
            {
                if (subscribedConversations.Count > 0 && !subscribedConversations.Contains(ev.ConversationId))
                    return;
                var json = JsonSerializer.Serialize(new
                {
                    @event = ev.Type,
                    conversation_id = ev.ConversationId,
                    payload = ev.Payload,
                    at = ev.At
                });
                var bytes = Encoding.UTF8.GetBytes(json + "\n");
                if (ws.State == WebSocketState.Open)
                {
                    try
                    {
                        await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch
                    {
                        /* connection closed */
                    }
                }
            });

            // 推送 connected 握手响应
            var hello = JsonSerializer.Serialize(new { @event = "connected", tenant_id = tenantId, at = DateTime.UtcNow });
            await ws.SendAsync(Encoding.UTF8.GetBytes(hello + "\n"), WebSocketMessageType.Text, true, CancellationToken.None);

            // 接收客户端命令（订阅 / 取消订阅 / typing 上报）
            var buffer = new byte[4096];
            while (ws.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                }
                catch
                {
                    break;
                }
                if (result.MessageType == WebSocketMessageType.Close) break;
                if (result.MessageType != WebSocketMessageType.Text) continue;

                var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                try
                {
                    var cmd = JsonSerializer.Deserialize<ClientCommand>(text);
                    if (cmd == null) continue;
                    switch (cmd.Action)
                    {
                        case "subscribe_conversation":
                            if (cmd.ConversationId.HasValue) subscribedConversations.Add(cmd.ConversationId.Value);
                            break;
                        case "unsubscribe_conversation":
                            if (cmd.ConversationId.HasValue) subscribedConversations.Remove(cmd.ConversationId.Value);
                            break;
                    }
                }
                catch
                {
                    /* ignore malformed command */
                }
            }

            logger.LogInformation("WS workbench disconnected tenant={TenantId}", tenantId);
        });
    }

    private class ClientCommand
    {
        [System.Text.Json.Serialization.JsonPropertyName("action")]
        public string Action { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("conversation_id")]
        public Guid? ConversationId { get; set; }
    }
}