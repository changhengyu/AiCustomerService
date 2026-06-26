namespace AiCustomerService.Core.Entities;

public class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string ChannelType { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Gender { get; set; }
    public string? Region { get; set; }
    public string? Source { get; set; }
    public string LifecycleStage { get; set; } = "lead";
    public string[] Tags { get; set; } = Array.Empty<string>();
    public int IntentionScore { get; set; }
    public string IntentionLevel { get; set; } = "cold";
    public string Metadata { get; set; } = "{}";
    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastProfileUpdateAt { get; set; }

    public Tenant? Tenant { get; set; }
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
}
