using System.ComponentModel.DataAnnotations;

namespace AiCustomerService.Core.Entities;

/// <summary>意向度规则（关键字命中加分）</summary>
public class IntentionRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    [MaxLength(64)]
    public string RuleName { get; set; } = string.Empty;

    /// <summary>关键字数组（PG text[]）</summary>
    public string[] Keywords { get; set; } = Array.Empty<string>();

    public int ScoreDelta { get; set; } = 5;

    [MaxLength(16)]
    public string TargetLevel { get; set; } = "high";

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
