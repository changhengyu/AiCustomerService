namespace AiCustomerService.Core.Entities;

public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid CustomerId { get; set; }
    public string ChannelType { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public Guid? AssignedTo { get; set; }
    public int MessageCount { get; set; }
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
    public string? Summary { get; set; }
    public string Metadata { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }

    public Tenant? Tenant { get; set; }
    public Customer? Customer { get; set; }
    public User? AssignedUser { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
