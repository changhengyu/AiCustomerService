namespace AiCustomerService.Core.DTOs.AI;

public record ChatRequest(
    Guid TenantId,
    string Model = "qwen-plus",
    List<ChatMessage> Messages = null!,
    float Temperature = 0.7f,
    int MaxTokens = 2000,
    string? SystemPrompt = null
);

public record ChatMessage(string Role, string Content);

public record ChatResponse(
    string Content,
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    int LatencyMs,
    string FinishReason
);

public record RagChatRequest(
    Guid TenantId,
    string Question,
    Guid? ConversationId = null,
    int TopK = 5,
    float ScoreThreshold = 0.7f,
    string? SystemPrompt = null
);

public record EmbeddingResult(float[] Vector, int TokensUsed);
