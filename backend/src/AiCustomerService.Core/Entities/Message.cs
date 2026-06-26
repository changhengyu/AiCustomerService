namespace AiCustomerService.Core.Entities;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public Guid TenantId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text";
    public string Content { get; set; } = string.Empty;
    public string? RawPayload { get; set; }
    public int TokensUsed { get; set; }
    public string? RetrievalChunks { get; set; }
    public int LatencyMs { get; set; }
    public string? ErrorMessage { get; set; }
    public int? UserRating { get; set; }
    public string? UserFeedback { get; set; }
    public DateTime? FeedbackAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Conversation? Conversation { get; set; }
}
