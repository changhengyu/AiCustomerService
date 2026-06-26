using System.ComponentModel.DataAnnotations;

namespace AiCustomerService.Core.Entities;

/// <summary>客户时间线事件 — 自动追加</summary>
public class CustomerTimelineEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid CustomerId { get; set; }

    [MaxLength(64)]
    public string EventType { get; set; } = string.Empty;

    public string Payload { get; set; } = "{}";
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
