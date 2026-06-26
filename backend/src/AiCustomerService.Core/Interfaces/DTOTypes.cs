using AiCustomerService.Core.DTOs.Conversation;

namespace AiCustomerService.Core.Interfaces;

public record TenantSettingsDto(
    string? SystemPrompt,
    string? WelcomeMessage,
    string[]? HandoffKeywords,
    Guid? IndustryId,
    bool UseIndustryFaq
);

public record ConversationListItemDto(
    Guid Id,
    Guid CustomerId,
    string? CustomerNickname,
    string ChannelType,
    string Status,
    int MessageCount,
    string? Summary,
    DateTime LastMessageAt,
    DateTime CreatedAt
);

public record ConversationDetailDto(
    Guid Id,
    Guid CustomerId,
    string? CustomerNickname,
    string? CustomerAvatar,
    string ChannelType,
    string Status,
    Guid? AssignedTo,
    string? AssignedToName,
    int MessageCount,
    string? Summary,
    DateTime CreatedAt,
    DateTime LastMessageAt,
    List<ConversationMessageDto> Messages
);

public record ConversationMessageDto(
    Guid Id,
    string Role,
    string ContentType,
    string Content,
    int? UserRating,
    int TokensUsed,
    int LatencyMs,
    DateTime CreatedAt
);

public record CustomerListItemDto(
    Guid Id,
    string? Nickname,
    string? AvatarUrl,
    string ChannelType,
    string IntentionLevel,
    int IntentionScore,
    string[] Tags,
    DateTime LastSeenAt
);

public record CustomerDetailDto(
    Guid Id,
    string? Nickname,
    string? AvatarUrl,
    string? Phone,
    string? Region,
    string ChannelType,
    string IntentionLevel,
    int IntentionScore,
    string[] Tags,
    string Metadata,
    DateTime FirstSeenAt,
    DateTime LastSeenAt,
    int TotalConversations
);

public record JobStatusDto(string State, int Processed, int Total, string? ErrorMessage);
