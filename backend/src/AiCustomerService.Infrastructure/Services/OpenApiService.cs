using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AiCustomerService.Core.Entities;
using AiCustomerService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AiCustomerService.Infrastructure.Services;

/// <summary>开放 API 服务：API Key 管理 + Webhook Outbox 投递</summary>
public class OpenApiService
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<OpenApiService> _log;

    public OpenApiService(AppDbContext db, IHttpClientFactory http, ILogger<OpenApiService> log)
    { _db = db; _http = http; _log = log; }

    // ============================================================
    // API Key
    // ============================================================

    public async Task<(ApiKey key, string plaintext)> CreateApiKeyAsync(
        Guid tenantId, string name, string scopes, DateTime? expiresAt, CancellationToken ct = default)
    {
        var raw = GenerateKey();
        var prefix = $"ak_live_{raw[..8]}";
        var hashed = HashKey(raw);

        var entity = new ApiKey
        {
            TenantId = tenantId,
            Prefix = prefix,
            HashedKey = hashed,
            Name = name,
            Scopes = scopes,
            ExpiresAt = expiresAt
        };
        _db.ApiKeys.Add(entity);
        await _db.SaveChangesAsync(ct);

        // 完整 Key 仅返回一次：前缀 + 明文后缀
        return (entity, $"{prefix}{raw[8..]}");
    }

    public async Task<List<ApiKey>> ListApiKeysAsync(Guid tenantId, CancellationToken ct = default)
        => await _db.ApiKeys
            .Where(k => k.TenantId == tenantId)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new ApiKey
            {
                Id = k.Id, Prefix = k.Prefix, Name = k.Name,
                Scopes = k.Scopes, CreatedAt = k.CreatedAt,
                LastUsedAt = k.LastUsedAt, ExpiresAt = k.ExpiresAt,
                Revoked = k.Revoked,
                HashedKey = string.Empty, TenantId = k.TenantId
            }).ToListAsync(ct);

    public async Task<bool> RevokeApiKeyAsync(Guid tenantId, Guid keyId, CancellationToken ct = default)
    {
        var k = await _db.ApiKeys.FirstOrDefaultAsync(x => x.Id == keyId && x.TenantId == tenantId, ct);
        if (k == null) return false;
        k.Revoked = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>验证 API Key，返回对应的租户 ID；失败返回 null</summary>
    public async Task<Guid?> ValidateApiKeyAsync(string fullKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fullKey) || !fullKey.StartsWith("ak_live_")) return null;
        // 拆前缀 + 剩余部分
        var hashed = HashKey(fullKey["ak_live_".Length..]);
        var key = await _db.ApiKeys.FirstOrDefaultAsync(k =>
            k.HashedKey == hashed && !k.Revoked, ct);
        if (key == null) return null;
        if (key.ExpiresAt.HasValue && key.ExpiresAt < DateTime.UtcNow) return null;

        // 异步更新最后使用时间（不阻塞请求）
        _ = Task.Run(async () =>
        {
            key.LastUsedAt = DateTime.UtcNow;
            try { await _db.SaveChangesAsync(); } catch { }
        });
        return key.TenantId;
    }

    private static string GenerateKey()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string HashKey(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    // ============================================================
    // Webhook
    // ============================================================

    public async Task<WebhookConfig> CreateWebhookAsync(
        Guid tenantId, string name, string url, string events, CancellationToken ct = default)
    {
        var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        var entity = new WebhookConfig
        {
            TenantId = tenantId, Name = name, Url = url,
            Secret = secret, Events = events, Active = true
        };
        _db.WebhookConfigs.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<List<WebhookConfig>> ListWebhooksAsync(Guid tenantId, CancellationToken ct = default)
        => await _db.WebhookConfigs.Where(w => w.TenantId == tenantId)
            .OrderByDescending(w => w.CreatedAt).ToListAsync(ct);

    public async Task<bool> DeleteWebhookAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        var w = await _db.WebhookConfigs.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        if (w == null) return false;
        _db.WebhookConfigs.Remove(w);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>发布事件（Outbox 模式：先入库，Hangfire 后台投递）</summary>
    public async Task PublishAsync(Guid tenantId, string eventType, object payload, CancellationToken ct = default)
    {
        var subs = await _db.WebhookConfigs
            .Where(w => w.TenantId == tenantId && w.Active
                && (w.Events == "*" || w.Events.Contains(eventType)))
            .ToListAsync(ct);

        var json = JsonSerializer.Serialize(payload);
        foreach (var sub in subs)
        {
            _db.WebhookDeliveries.Add(new WebhookDelivery
            {
                TenantId = tenantId,
                WebhookConfigId = sub.Id,
                EventType = eventType,
                Payload = json,
                Status = "pending",
                CreatedAt = DateTime.UtcNow,
                NextRetryAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>执行一次投递（Hangfire 调用）</summary>
    public async Task<int> DispatchPendingAsync(int maxBatch = 50, CancellationToken ct = default)
    {
        var due = await _db.WebhookDeliveries
            .Where(d => d.Status == "pending"
                && (d.NextRetryAt == null || d.NextRetryAt <= DateTime.UtcNow))
            .OrderBy(d => d.CreatedAt)
            .Take(maxBatch)
            .ToListAsync(ct);

        var client = _http.CreateClient("webhook");
        client.Timeout = TimeSpan.FromSeconds(10);
        int delivered = 0;

        foreach (var d in due)
        {
            var sub = await _db.WebhookConfigs.FindAsync(new object[] { d.WebhookConfigId }, ct);
            if (sub == null || !sub.Active)
            {
                d.Status = "failed";
                continue;
            }

            var signature = ComputeHmac(sub.Secret, d.Payload);
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Post, sub.Url);
                req.Content = new StringContent(d.Payload, Encoding.UTF8, "application/json");
                req.Headers.Add("X-Webhook-Event", d.EventType);
                req.Headers.Add("X-Webhook-Signature", $"sha256={signature}");
                req.Headers.Add("X-Webhook-Delivery", d.Id.ToString());

                var resp = await client.SendAsync(req, ct);
                d.HttpStatus = (int)resp.StatusCode;
                d.ResponseBody = (await resp.Content.ReadAsStringAsync(ct))?[..Math.Min(512, 512)];
                d.AttemptCount++;
                if (resp.IsSuccessStatusCode)
                {
                    d.Status = "success";
                    d.DeliveredAt = DateTime.UtcNow;
                    sub.LastTriggeredAt = DateTime.UtcNow;
                    sub.FailureCount = 0;
                    delivered++;
                }
                else
                {
                    ScheduleRetry(d);
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Webhook 投递失败: {Id}", d.Id);
                d.AttemptCount++;
                ScheduleRetry(d);
            }
        }
        await _db.SaveChangesAsync(ct);
        return delivered;
    }

    private static void ScheduleRetry(WebhookDelivery d)
    {
        // 指数退避：1, 2, 4, 8, 16, 32 分钟
        var minutes = Math.Pow(2, Math.Min(d.AttemptCount, 6));
        d.NextRetryAt = DateTime.UtcNow.AddMinutes(minutes);
        if (d.AttemptCount >= 6) d.Status = "failed";
    }

    private static string ComputeHmac(string secret, string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
