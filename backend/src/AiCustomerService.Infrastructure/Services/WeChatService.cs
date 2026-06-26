using System.Xml.Linq;
using AiCustomerService.Core.Entities;
using AiCustomerService.Core.Exceptions;
using AiCustomerService.Core.Interfaces;
using AiCustomerService.Infrastructure.Data;
using AiCustomerService.Infrastructure.WeChat;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AiCustomerService.Infrastructure.Services;

public class WeChatService : IWeChatService
{
    private readonly AppDbContext _db;
    private readonly WeChatOfficialClient _official;
    private readonly IConversationService _conversation;
    private readonly ILogger<WeChatService> _logger;

    public WeChatService(
        AppDbContext db,
        WeChatOfficialClient official,
        IConversationService conversation,
        ILogger<WeChatService> logger)
    {
        _db = db;
        _official = official;
        _conversation = conversation;
        _logger = logger;
    }

    public Task<string?> VerifyUrlAsync(
        string appId, string signature, string timestamp, string nonce, string echostr,
        CancellationToken ct = default)
    {
        var ok = _official.VerifySignature(signature, timestamp, nonce);
        return Task.FromResult(ok ? echostr : null);
    }

    public async Task<string> HandleMessageAsync(string appId, string xmlPayload, CancellationToken ct = default)
    {
        XDocument doc;
        try
        {
            doc = XDocument.Parse(xmlPayload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析微信 XML 失败");
            return "success";
        }

        var root = doc.Root;
        if (root == null) return "success";

        var fromUser = root.Element("FromUserName")?.Value ?? "";
        var toUser = root.Element("ToUserName")?.Value ?? "";
        var msgType = root.Element("MsgType")?.Value ?? "";
        var content = root.Element("Content")?.Value ?? "";

        var tenant = await ResolveTenantAsync(appId, toUser, ct);
        if (tenant == null)
        {
            _logger.LogWarning("未找到租户: appId={AppId}", appId);
            return "success";
        }

        var customer = await GetOrCreateCustomerAsync(tenant.Id, fromUser, "wechat", ct);

        if (msgType == "text" && !string.IsNullOrEmpty(content))
        {
            var resp = await _conversation.HandleUserMessageAsync(
                tenant.Id, customer.Id, content, conversationId: null, ct);

            await _official.SendCustomerTextAsync(fromUser, resp.Reply, ct);
        }
        else if (msgType == "event")
        {
            var evt = root.Element("Event")?.Value ?? "";
            _logger.LogInformation("微信事件: {Event}", evt);
        }

        return "success";
    }

    public async Task<Customer> GetOrCreateCustomerAsync(
        Guid tenantId, string externalId, string channelType, CancellationToken ct = default)
    {
        var existing = await _db.Customers.FirstOrDefaultAsync(
            c => c.TenantId == tenantId && c.ExternalId == externalId && c.ChannelType == channelType, ct);
        if (existing != null) return existing;

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ExternalId = externalId,
            ChannelType = channelType,
            Nickname = $"微信用户_{(externalId.Length >= 6 ? externalId[^6..] : externalId)}",
            IntentionLevel = "cold",
            IntentionScore = 0,
            Tags = Array.Empty<string>(),
            Metadata = "{}",
            FirstSeenAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);
        return customer;
    }

    private async Task<Tenant?> ResolveTenantAsync(string appId, string toUser, CancellationToken ct)
    {
        // 通过 ChannelConfig.AppId 查找租户
        var config = await _db.ChannelConfigs
            .FirstOrDefaultAsync(c => c.ChannelType == "wechat" &&
                (c.AppId == appId || c.AppId == toUser), ct);
        if (config != null)
            return await _db.Tenants.FirstOrDefaultAsync(t => t.Id == config.TenantId, ct);

        // 兜底：返回第一个 active 租户（仅 demo）
        return await _db.Tenants.FirstOrDefaultAsync(t => t.Status == "active", ct);
    }
}