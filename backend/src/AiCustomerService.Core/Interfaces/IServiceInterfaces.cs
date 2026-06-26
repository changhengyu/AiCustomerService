using AiCustomerService.Core.DTOs.AI;
using AiCustomerService.Core.DTOs.Auth;
using AiCustomerService.Core.DTOs.Conversation;
using AiCustomerService.Core.DTOs.Knowledge;
using AiCustomerService.Core.Entities;

namespace AiCustomerService.Core.Interfaces;

public interface IAIService
{
    Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct = default);
    IAsyncEnumerable<string> ChatStreamAsync(ChatRequest request, CancellationToken ct = default);
    Task<ChatResponse> RagChatAsync(RagChatRequest request, CancellationToken ct = default);
}

public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
    Task<List<float[]>> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken ct = default);
}

public interface IConversationService
{
    Task<SendMessageResponse> HandleUserMessageAsync(Guid tenantId, Guid customerId, string content,
        Guid? conversationId = null, CancellationToken ct = default);
    Task<SendMessageResponse> SendAgentMessageAsync(Guid conversationId, string content, CancellationToken ct = default);
    Task HandoffToHumanAsync(Guid conversationId, Guid? assignedTo, CancellationToken ct = default);
    Task CloseConversationAsync(Guid conversationId, CancellationToken ct = default);
    Task<ConversationDetailDto?> GetDetailAsync(Guid conversationId, CancellationToken ct = default);
    Task<PagedResult<ConversationListItemDto>> ListAsync(Guid tenantId, int page, int pageSize,
        string? status = null, CancellationToken ct = default);
}

public interface IKnowledgeService
{
    Task<Guid> UploadAsync(Guid tenantId, Guid userId, Stream stream, string fileName, string title,
        CancellationToken ct = default);
    Task<PagedResult<DocumentDto>> ListAsync(Guid tenantId, int page, int pageSize,
        CancellationToken ct = default);
    Task DeleteAsync(Guid tenantId, Guid documentId, CancellationToken ct = default);
    Task<JobStatusDto> GetJobStatusAsync(Guid documentId, CancellationToken ct = default);
    Task ReindexAsync(Guid documentId, CancellationToken ct = default);
    Task<List<ChunkDto>> GetChunksAsync(Guid documentId, int page, int pageSize,
        CancellationToken ct = default);
}

public interface ICustomerService
{
    Task<PagedResult<CustomerListItemDto>> ListAsync(Guid tenantId, int page, int pageSize,
        string? intentionLevel = null, string? keyword = null, CancellationToken ct = default);
    Task<CustomerDetailDto?> GetDetailAsync(Guid tenantId, Guid customerId, CancellationToken ct = default);
    Task UpdateTagsAsync(Guid tenantId, Guid customerId, string[] tags, CancellationToken ct = default);
}

public interface IAuthService
{
    Task<LoginResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<LoginResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
}

public interface IWeChatService
{
    Task<string?> VerifyUrlAsync(string appId, string signature, string timestamp, string nonce,
        string echostr, CancellationToken ct = default);
    Task<string> HandleMessageAsync(string appId, string xmlPayload, CancellationToken ct = default);
    Task<Customer> GetOrCreateCustomerAsync(Guid tenantId, string externalId, string channelType,
        CancellationToken ct = default);
}

public interface ITenantService
{
    Task<Tenant?> GetCurrentAsync(Guid tenantId, CancellationToken ct = default);
    Task<Tenant?> GetByWeChatAppIdAsync(string appId, CancellationToken ct = default);
    Task UpdateSettingsAsync(Guid tenantId, TenantSettingsDto settings, CancellationToken ct = default);
    Task<TenantSettingsDto> GetSettingsAsync(Guid tenantId, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface ICacheService
{
    Task<string?> GetStringAsync(string key, CancellationToken ct = default);
    Task SetStringAsync(string key, string value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}
