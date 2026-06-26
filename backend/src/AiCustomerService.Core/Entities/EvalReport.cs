namespace AiCustomerService.Core.Entities;

/// <summary>
/// 评测报告（持久化）
/// </summary>
public class EvalReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string DatasetName { get; set; } = "default";
    public int TotalCases { get; set; }
    public double FaithfulnessAvg { get; set; }
    public double AnswerRelevancyAvg { get; set; }
    public double ContextPrecisionAvg { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "completed";
    /// <summary>JSON 序列化的 List&lt;EvalResultItemDto&gt;</summary>
    public string ItemsJson { get; set; } = "[]";
}
