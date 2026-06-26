using AiCustomerService.Core.DTOs.AI;
using AiCustomerService.Core.DTOs.Conversation;
using AiCustomerService.Core.DTOs.Knowledge;
using AiCustomerService.Core.Entities;
using AiCustomerService.Core.Exceptions;
using AiCustomerService.Core.Interfaces;
using AiCustomerService.Infrastructure.AI.RAG;
using AiCustomerService.Infrastructure.Data;
using AiCustomerService.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

namespace AiCustomerService.Infrastructure.Services;

public class ConversationService : IConversationService
{
    private readonly AppDbContext _db;
    private readonly IAIService _ai;
    private readonly HybridRetriever _retriever;
    private readonly ITenantContext _tenantCtx;
    private readonly ICacheService _cache;
    private readonly ProfileService _profile;
    private readonly MarketingTriggerService _triggers;
    private readonly IRealtimeNotifier _realtime;

    public ConversationService(
        AppDbContext db,
        IAIService ai,
        HybridRetriever retriever,
        ITenantContext tenantCtx,
        ICacheService cache,
        ProfileService profile,
        MarketingTriggerService triggers,
        IRealtimeNotifier realtime)
    {
        _db = db;
        _ai = ai;
        _retriever = retriever;
        _tenantCtx = tenantCtx;
        _cache = cache;
        _profile = profile;
        _triggers = triggers;
        _realtime = realtime;
    }

    public async Task<SendMessageResponse> HandleUserMessageAsync(
        Guid tenantId, Guid customerId, string content,
        Guid? conversationId = null, CancellationToken ct = default)
    {
        using var span = AppActivitySource.Source.StartActivity("chat.handle_user_message");
        span?.SetTag("tenant.id", tenantId);
        span?.SetTag("customer.id", customerId);

        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId && c.TenantId == tenantId, ct)
            ?? throw new NotFoundException("Customer.NotFound");

        Conversation conversation;
        if (conversationId.HasValue)
        {
            conversation = await _db.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId.Value && c.TenantId == tenantId, ct)
                ?? throw new NotFoundException("Conversation.NotFound");
        }
        else
        {
            conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CustomerId = customerId,
                Status = "active",
                ChannelType = "wechat",
                CreatedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow
            };
            _db.Conversations.Add(conversation);
        }

        var userMsg = new Message
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConversationId = conversation.Id,
            Role = "user",
            ContentType = "text",
            Content = content,
            CreatedAt = DateTime.UtcNow
        };
        _db.Messages.Add(userMsg);
        conversation.LastMessageAt = DateTime.UtcNow;
        conversation.MessageCount += 1;

        List<RetrievalHit> hits;
        using (var retrieveSpan = AppActivitySource.Source.StartActivity("rag.retrieve"))
        {
            retrieveSpan?.SetTag("rag.top_k", 5);
            retrieveSpan?.SetTag("rag.min_score", 0.45);
            hits = await _retriever.RetrieveAsync(tenantId, content, topK: 5, minScore: 0.45, ct);
            retrieveSpan?.SetTag("rag.hit_count", hits.Count);
            AppMeter.RetrievalHits.Add(hits.Count);
        }
        var contextBlocks = string.Join("\n\n---\n\n",
            hits.Select((h, i) => $"[参考 {i + 1}] {h.Chunk.Content}"));

        var recent = await _db.Messages
            .Where(m => m.ConversationId == conversation.Id)
            .OrderByDescending(m => m.CreatedAt)
            .Take(10)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

        var messages = recent.Select(m => new ChatMessage(m.Role, m.Content)).ToList();

        var sysPrompt = await BuildSystemPromptAsync(tenantId, contextBlocks, ct);
        var request = new ChatRequest(
            TenantId: tenantId,
            Model: "qwen-plus",
            Messages: messages,
            Temperature: 0.7f,
            MaxTokens: 2000,
            SystemPrompt: sysPrompt);

        ChatResponse response;
        using (var llmSpan = AppActivitySource.Source.StartActivity("llm.chat"))
        {
            llmSpan?.SetTag("llm.model", request.Model);
            response = await _ai.ChatAsync(request, ct);
            llmSpan?.SetTag("llm.tokens_total", response.TotalTokens);
            llmSpan?.SetTag("llm.tokens_prompt", response.PromptTokens);
            llmSpan?.SetTag("llm.tokens_completion", response.CompletionTokens);
        }

        AppMeter.ChatTokens.Add(response.PromptTokens,
            new KeyValuePair<string, object?>("role", "prompt"));
        AppMeter.ChatTokens.Add(response.CompletionTokens,
            new KeyValuePair<string, object?>("role", "completion"));
        AppMeter.ChatLatency.Record(response.LatencyMs);

        var aiMsg = new Message
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConversationId = conversation.Id,
            Role = "assistant",
            ContentType = "text",
            Content = response.Content,
            TokensUsed = response.TotalTokens,
            LatencyMs = response.LatencyMs,
            RetrievalChunks = hits.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(
                hits.Select(h => new { h.Chunk.Id, h.Score, h.MatchType })) : null,
            CreatedAt = DateTime.UtcNow
        };
        _db.Messages.Add(aiMsg);
        conversation.MessageCount += 1;

        await UpdateCustomerIntentionAsync(customer, hits.Count, ct);

        await _db.SaveChangesAsync(ct);

        // 实时推送 AI 回复（让客户 / 其他工作台立即看到）
        await _realtime.NewMessageAsync(tenantId, conversation.Id, aiMsg.Id,
            "assistant", response.Content, aiMsg.CreatedAt, ct);

        return new SendMessageResponse(
            MessageId: aiMsg.Id,
            ConversationId: conversation.Id,
            Reply: response.Content,
            LatencyMs: response.LatencyMs,
            TokensUsed: response.TotalTokens,
            IsHandoff: false
        );
    }

    public async Task<SendMessageResponse> SendAgentMessageAsync(
        Guid conversationId, string content, CancellationToken ct = default)
    {
        var conv = await _db.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId, ct)
            ?? throw new NotFoundException("Conversation.NotFound");
        var agentId = _tenantCtx.CurrentUserId
            ?? throw new UnauthorizedException("Auth.Unauthorized");

        var msg = new Message
        {
            Id = Guid.NewGuid(),
            TenantId = conv.TenantId,
            ConversationId = conversationId,
            Role = "agent",
            ContentType = "text",
            Content = content,
            CreatedAt = DateTime.UtcNow
        };
        _db.Messages.Add(msg);
        conv.LastMessageAt = DateTime.UtcNow;
        conv.MessageCount += 1;
        await _db.SaveChangesAsync(ct);

        // 实时推送客服消息（让客户端 / 其他工作台立即看到）
        await _realtime.NewMessageAsync(conv.TenantId, conv.Id, msg.Id,
            "agent", content, msg.CreatedAt, ct);

        return new SendMessageResponse(msg.Id, conv.Id, content, 0, 0, false);
    }

    public async Task HandoffToHumanAsync(Guid conversationId, Guid? assignedTo, CancellationToken ct = default)
    {
        var conv = await _db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, ct)
            ?? throw new NotFoundException("Conversation.NotFound");
        conv.Status = "human";
        conv.AssignedTo = assignedTo ?? _tenantCtx.CurrentUserId;
        await _db.SaveChangesAsync(ct);

        // 实时推送会话状态变更
        await _realtime.ConversationStatusChangedAsync(conv.TenantId, conv.Id,
            conv.Status, conv.AssignedTo, ct);
    }

    public async Task CloseConversationAsync(Guid conversationId, CancellationToken ct = default)
    {
        var conv = await _db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, ct)
            ?? throw new NotFoundException("Conversation.NotFound");
        conv.Status = "closed";
        conv.ClosedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _realtime.ConversationStatusChangedAsync(conv.TenantId, conv.Id,
            conv.Status, conv.AssignedTo, ct);
    }

    public async Task<ConversationDetailDto?> GetDetailAsync(Guid conversationId, CancellationToken ct = default)
    {
        var conv = await _db.Conversations
            .Include(c => c.Customer)
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == conversationId, ct);
        if (conv == null) return null;

        return new ConversationDetailDto(
            Id: conv.Id,
            CustomerId: conv.CustomerId,
            CustomerNickname: conv.Customer.Nickname,
            CustomerAvatar: conv.Customer.AvatarUrl,
            ChannelType: conv.ChannelType,
            Status: conv.Status,
            AssignedTo: conv.AssignedTo,
            AssignedToName: null,
            MessageCount: conv.MessageCount,
            Summary: conv.Summary,
            CreatedAt: conv.CreatedAt,
            LastMessageAt: conv.LastMessageAt,
            Messages: conv.Messages.Select(m => new ConversationMessageDto(
                m.Id, m.Role, m.ContentType, m.Content, m.UserRating, m.TokensUsed, m.LatencyMs, m.CreatedAt
            )).ToList()
        );
    }

    public async Task<PagedResult<ConversationListItemDto>> ListAsync(
        Guid tenantId, int page, int pageSize, string? status = null, CancellationToken ct = default)
    {
        var query = _db.Conversations
            .Include(c => c.Customer)
            .Where(c => c.TenantId == tenantId);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status == status);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(c => c.LastMessageAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ConversationListItemDto(
                c.Id, c.CustomerId, c.Customer.Nickname,
                c.ChannelType, c.Status, c.MessageCount, c.Summary,
                c.LastMessageAt, c.CreatedAt
            ))
            .ToListAsync(ct);

        return new PagedResult<ConversationListItemDto>(items, total, page, pageSize);
    }

    private async Task<string> BuildSystemPromptAsync(Guid tenantId, string context, CancellationToken ct)
    {
        var cacheKey = $"sysprompt:{tenantId}";
        var cached = await _cache.GetStringAsync(cacheKey, ct);
        if (!string.IsNullOrEmpty(cached)) return cached;

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        var prompt = $@"你是 {tenant?.Name ?? "AI"} 的客服助手。
- 用友好专业的语气回答用户问题
- 优先使用提供的参考资料回答
- 不确定时诚实告知用户
- 引导用户留下联系方式或提交工单

参考资料：
{context}";

        await _cache.SetStringAsync(cacheKey, prompt, TimeSpan.FromHours(2), ct);
        return prompt;
    }

    private async Task UpdateCustomerIntentionAsync(Customer customer, int hitCount, CancellationToken ct)
    {
        if (hitCount == 0) return;
        var oldLevel = customer.IntentionLevel;
        customer.IntentionScore = Math.Min(100, customer.IntentionScore + 5);
        if (customer.IntentionScore >= 80) customer.IntentionLevel = "high";
        else if (customer.IntentionScore >= 40) customer.IntentionLevel = "medium";
        else customer.IntentionLevel = "low";
        customer.LastSeenAt = DateTime.UtcNow;

        // 触发营销事件 + 写入时间线
        if (oldLevel != customer.IntentionLevel)
        {
            _profile.AppendTimeline(customer.TenantId, customer.Id, "customer.intention_changed",
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    from = oldLevel, to = customer.IntentionLevel, score = customer.IntentionScore
                }));
            // Fire-and-forget：触发器匹配 + webhook
            _ = _triggers.OnEventAsync(customer.TenantId, "customer.intention_changed", new
            {
                customerId = customer.Id,
                from = oldLevel,
                to = customer.IntentionLevel,
                score = customer.IntentionScore
            });
            // 实时推送意向度变化
            await _realtime.CustomerIntentionChangedAsync(customer.TenantId, customer.Id,
                oldLevel, customer.IntentionLevel, customer.IntentionScore, ct);
        }
    }
}