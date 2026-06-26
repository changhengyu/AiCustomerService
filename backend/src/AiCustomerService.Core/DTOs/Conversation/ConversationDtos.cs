namespace AiCustomerService.Core.DTOs.Conversation;

public record SendMessageRequest(string Content, string? ContentType = null, Guid? ConversationId = null);

public record SendMessageResponse(
    Guid MessageId,
    Guid ConversationId,
    string Reply,
    int LatencyMs,
    int TokensUsed,
    bool IsHandoff
);

public record HandoffRequest(Guid? AssignedTo = null, string? Note = null);

public record AgentSendMessageRequest(string Content);
