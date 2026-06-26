using System.ComponentModel.DataAnnotations;

namespace AiCustomerService.Core.Entities;

/// <summary>营销触发器 — 监听事件并自动执行动作</summary>
public class MarketingTrigger
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    /// <summary>监听事件，如 customer.intention_changed / subscription.expired</summary>
    [MaxLength(64)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>条件 JSON：{ from:"low", to:"high" } 或 {} 表示无条件触发</summary>
    public string Conditions { get; set; } = "{}";

    /// <summary>动作 JSON：[{type:"add_tag",tag:"vip"},{type:"add_note",content:"..."},{type:"webhook",url:"..."}]</summary>
    public string Actions { get; set; } = "[]";

    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Tenant? Tenant { get; set; }
}
