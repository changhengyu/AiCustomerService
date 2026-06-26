namespace AiCustomerService.Core.Entities;

public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string Plan { get; set; } = "free";
    public string Status { get; set; } = "active";
    public int MonthlyMessageQuota { get; set; } = 100;
    public int MonthlyMessageUsed { get; set; }
    public string Settings { get; set; } = "{}";
    /// <summary>试用期截止时间（仅 Plan=trial 时有值）</summary>
    public DateTime? TrialEndsAt { get; set; }
    /// <summary>所属行业代码：general / ecommerce / education / saas / finance / medical</summary>
    public string? IndustryCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<ChannelConfig> ChannelConfigs { get; set; } = new List<ChannelConfig>();
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public ICollection<KnowledgeDocument> Documents { get; set; } = new List<KnowledgeDocument>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
