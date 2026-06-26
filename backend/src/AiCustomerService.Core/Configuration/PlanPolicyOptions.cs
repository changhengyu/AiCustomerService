namespace AiCustomerService.Core.Configuration;

/// <summary>套餐策略</summary>
public record PlanPolicy(
    string Name,
    decimal PriceCents,
    int MonthlyMessageQuota,
    int ChatRateLimit,
    int UploadRateLimit,
    int MaxDocuments,
    int MaxAgents
);

/// <summary>集中化套餐策略配置</summary>
public class PlanPolicyOptions
{
    public const string SectionName = "PlanPolicy";
    public Dictionary<string, PlanPolicy> Plans { get; set; } = new();
}
