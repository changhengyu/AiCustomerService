using System.ComponentModel.DataAnnotations;

namespace AiCustomerService.Core.Entities;

/// <summary>客户分群</summary>
public class CustomerSegment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? Description { get; set; }

    /// <summary>规则 JSON：{ intention:["high"], tags:["vip"], region:["上海"] }</summary>
    public string Rules { get; set; } = "{}";

    public bool IsDynamic { get; set; } = true;
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Tenant? Tenant { get; set; }
}
