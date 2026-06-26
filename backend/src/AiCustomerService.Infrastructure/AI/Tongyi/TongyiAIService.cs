using System.Diagnostics;
using System.Runtime.CompilerServices;
using AiCustomerService.Core.Configuration;
using AiCustomerService.Core.DTOs.AI;
using AiCustomerService.Core.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
// 解决与 Microsoft.Extensions.AI.ChatResponse 的类型名冲突
using ChatResponse = AiCustomerService.Core.DTOs.AI.ChatResponse;
using ChatMessage = AiCustomerService.Core.DTOs.AI.ChatMessage;
using ChatRequest = AiCustomerService.Core.DTOs.AI.ChatRequest;

namespace AiCustomerService.Infrastructure.AI.Tongyi;

/// <summary>
/// 通义千问 AI 服务（基于 Microsoft.Extensions.AI 框架）。
/// 使用 OpenAIClient 连接 Tongyi 的 OpenAI 兼容端点（/compatible-mode/v1）。
/// 所有 HTTP 通信由 MEAI / OpenAI SDK 处理，开发者面向 IChatClient 编程。
/// </summary>
public class TongyiAIService : IAIService
{
    private readonly IChatClient _chatClient;
    private readonly TongyiOptions _options;
    private readonly ILogger<TongyiAIService> _logger;

    public TongyiAIService(
        IChatClient chatClient,
        IOptions<TongyiOptions> options,
        ILogger<TongyiAIService> logger)
    {
        _chatClient = chatClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var messages = BuildMessages(request);

        var options = new ChatOptions
        {
            ModelId = request.Model,
            Temperature = request.Temperature,
            MaxOutputTokens = request.MaxTokens
        };

        try
        {
            var response = await _chatClient.GetResponseAsync(messages, options, ct);
            sw.Stop();

            var content = response.Messages.LastOrDefault()?.Text ?? string.Empty;
            var usage = response.Usage;
            return new ChatResponse(
                Content: content,
                PromptTokens: (int)(usage?.InputTokenCount ?? 0L),
                CompletionTokens: (int)(usage?.OutputTokenCount ?? 0L),
                TotalTokens: (int)(usage?.TotalTokenCount ?? 0L),
                LatencyMs: (int)sw.ElapsedMilliseconds,
                FinishReason: response.FinishReason?.ToString() ?? "stop"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "通义千问 Chat 调用失败: Model={Model}", request.Model);
            throw;
        }
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var messages = BuildMessages(request);
        var options = new ChatOptions
        {
            ModelId = request.Model,
            Temperature = request.Temperature,
            MaxOutputTokens = request.MaxTokens
        };

        await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, options, ct))
        {
            foreach (var content in update.Contents)
            {
                if (content is TextContent text && !string.IsNullOrEmpty(text.Text))
                {
                    yield return text.Text;
                }
            }
        }
    }

    public async Task<ChatResponse> RagChatAsync(RagChatRequest request, CancellationToken ct = default)
    {
        var sysPrompt = request.SystemPrompt ?? "你是 AI 客服。";
        var messages = new List<Microsoft.Extensions.AI.ChatMessage>
        {
            new(ChatRole.User, request.Question)
        };
        return await ChatAsync(new ChatRequest(
            TenantId: request.TenantId,
            Model: _options.ChatModel,
            Messages: new List<ChatMessage> { new("user", request.Question) },
            SystemPrompt: sysPrompt
        ), ct);
    }

    private List<Microsoft.Extensions.AI.ChatMessage> BuildMessages(ChatRequest request)
    {
        var messages = new List<Microsoft.Extensions.AI.ChatMessage>();
        if (!string.IsNullOrEmpty(request.SystemPrompt))
            messages.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.System, request.SystemPrompt));
        if (request.Messages != null)
        {
            foreach (var m in request.Messages)
            {
                var role = m.Role.ToLowerInvariant() switch
                {
                    "system" => ChatRole.System,
                    "assistant" or "ai" => ChatRole.Assistant,
                    _ => ChatRole.User
                };
                messages.Add(new Microsoft.Extensions.AI.ChatMessage(role, m.Content));
            }
        }
        return messages;
    }
}
