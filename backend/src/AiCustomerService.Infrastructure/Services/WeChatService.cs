using System.Xml.Linq;
using AiCustomerService.Core.Configuration;
using AiCustomerService.Core.Entities;
using AiCustomerService.Core.Interfaces;
using AiCustomerService.Infrastructure.AI.Stt;
using AiCustomerService.Infrastructure.Data;
using AiCustomerService.Infrastructure.WeChat;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiCustomerService.Infrastructure.Services;

public class WeChatService : IWeChatService
{
    private readonly AppDbContext _db;
    private readonly WeChatOfficialClient _official;
    private readonly WeChatOptions _options;
    private readonly IConversationService _conversation;
    private readonly IAiSttProvider _stt;
    private readonly ILogger<WeChatService> _logger;

    public WeChatService(
        AppDbContext db,
        WeChatOfficialClient official,
        IOptions<WeChatOptions> options,
        IConversationService conversation,
        IAiSttProvider stt,
        ILogger<WeChatService> logger)
    {
        _db = db;
        _official = official;
        _options = options.Value;
        _conversation = conversation;
        _stt = stt;
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
        var rawContent = root.Element("Content")?.Value ?? "";

        var tenant = await ResolveTenantAsync(appId, toUser, ct);
        if (tenant == null)
        {
            _logger.LogWarning("未找到租户: appId={AppId}", appId);
            return "success";
        }

        // 解密（兼容明文 + 加密两种模式）
        string content = rawContent;
        if (msgType == "text" && IsEncryptedPayload(rawContent, root))
        {
            try
            {
                var cryptor = new WeChatMessageCryptor(
                    _options.OfficialToken, _options.EncodingAESKey, appId);
                content = cryptor.Decrypt(rawContent, out _);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "微信加密消息解密失败: msgId={MsgId}", root.Element("MsgId")?.Value);
                return "success";
            }
        }

        var customer = await GetOrCreateCustomerAsync(tenant.Id, fromUser, "wechat", ct);

        if (msgType == "text" && !string.IsNullOrEmpty(content))
        {
            var resp = await _conversation.HandleUserMessageAsync(
                tenant.Id, customer.Id, content, conversationId: null, ct);

            await _official.SendCustomerTextAsync(fromUser, resp.Reply, ct);
        }
        else if (msgType == "voice")
        {
            var mediaId = root.Element("MediaId")?.Value ?? "";
            var format = root.Element("Format")?.Value ?? "amr";

            // 下载微信临时素材
            byte[]? audioBytes = null;
            try
            {
                audioBytes = await _official.DownloadMediaAsync(mediaId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下载微信语音素材失败: mediaId={MediaId}", mediaId);
                return "success";
            }

            if (audioBytes == null || audioBytes.Length == 0) return "success";

            // STT
            string transcript;
            try
            {
                using var ms = new MemoryStream(audioBytes);
                var sttResult = await _stt.RecognizeAsync(ms, format, ct);
                transcript = sttResult.Text;
                _logger.LogInformation("微信语音转写: {Text}", transcript);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "微信 STT 失败");
                await _official.SendCustomerTextAsync(fromUser, "语音识别失败，请稍后再试或改用文字。", ct);
                return "success";
            }

            if (string.IsNullOrWhiteSpace(transcript)) return "success";

            // 走相同 chat pipeline
            var resp = await _conversation.HandleUserMessageAsync(
                tenant.Id, customer.Id, transcript, conversationId: null, ct);
            await _official.SendCustomerTextAsync(fromUser, resp.Reply, ct);
        }
        else if (msgType == "event")
        {
            var evt = root.Element("Event")?.Value ?? "";
            _logger.LogInformation("微信事件: {Event}", evt);
        }

        return "success";
    }

    /// <summary>
    /// 判断是否为加密模式：明文模式 Content 是可读字符串，加密模式是 Base64 编码的长字符串。
    /// 通过 EncodingAESKey 是否配置 + Content 长度判断。
    /// </summary>
    private bool IsEncryptedPayload(string content, XElement root)
    {
        if (string.IsNullOrEmpty(_options.EncodingAESKey)) return false;
        // 加密模式 Content 长度通常 > 200
        return content.Length > 200 || root.Element("Encrypt") != null;
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