namespace AiCustomerService.Core.Entities;

public class Subscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Plan { get; set; } = "free";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public long AmountCents { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentId { get; set; }
    public string Status { get; set; } = "pending";
    public string? InvoiceUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }

    public Tenant? Tenant { get; set; }
}

public class IntentionRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string[] Keywords { get; set; } = Array.Empty<string>();
    public int ScoreDelta { get; set; }
    public string? TargetLevel { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AiUsageLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? ConversationId { get; set; }
    public string Model { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
    public long CostCents { get; set; }
    public int LatencyMs { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
