using System.ComponentModel.DataAnnotations;

namespace AiCustomerService.Core.Entities;

/// <summary>Webhook 配置 — 事件订阅</summary>
public class WebhookConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(512)]
    public string Url { get; set; } = string.Empty;

    /// <summary>HMAC 签名密钥</summary>
    [MaxLength(128)]
    public string Secret { get; set; } = string.Empty;

    /// <summary>订阅事件类型（逗号分隔）</summary>
    [MaxLength(512)]
    public string Events { get; set; } = string.Empty;

    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastTriggeredAt { get; set; }
    public int FailureCount { get; set; } = 0;

    public Tenant? Tenant { get; set; }
}

/// <summary>Webhook 投递日志（用于 Outbox 模式）</summary>
public class WebhookDelivery
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid WebhookConfigId { get; set; }

    [MaxLength(64)]
    public string EventType { get; set; } = string.Empty;

    public string Payload { get; set; } = "{}";

    [MaxLength(16)]
    public string Status { get; set; } = "pending"; // pending/success/failed

    public int HttpStatus { get; set; } = 0;
    public string? ResponseBody { get; set; }
    public int AttemptCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }
    public DateTime? NextRetryAt { get; set; }
}
