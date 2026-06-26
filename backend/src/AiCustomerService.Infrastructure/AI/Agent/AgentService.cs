using System.Diagnostics;
using AiCustomerService.Core.DTOs.AI;
using AiCustomerService.Core.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using ChatOptions = Microsoft.Extensions.AI.ChatOptions;

namespace AiCustomerService.Infrastructure.AI.Agent;

/// <summary>
/// 智能体服务：基于 MEAI FunctionInvokingChatClient 自动工具调用。
/// 内置工具：订单查询 / 物流查询 / 退款申请 / 转人工 / 客户历史。
/// </summary>
public class AgentService
{
    private readonly IAiProviderFactory _providerFactory;
    private readonly CustomerServiceTools _tools;
    private readonly ILogger<AgentService> _logger;
    private readonly AiCustomerService.Core.Configuration.AiProviderOptions _options;

    public AgentService(
        IAiProviderFactory providerFactory,
        CustomerServiceTools tools,
        IOptions<AiCustomerService.Core.Configuration.AiProviderOptions> options,
        ILogger<AgentService> logger)
    {
        _providerFactory = providerFactory;
        _tools = tools;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// 智能体对话：自动根据 LLM 决策调用合适的工具。
    /// 最多 5 轮工具调用防止无限循环。
    /// </summary>
    public async Task<AgentChatResponse> ChatAsync(AgentChatRequest request, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var provider = string.IsNullOrEmpty(request.Provider) ? _options.DefaultProvider : request.Provider;
        var rawClient = _providerFactory.GetChatClient(provider);
        var client = new FunctionInvokingChatClient(rawClient)
        {
            MaximumIterationsPerRequest = 5
        };

        var messages = new List<Microsoft.Extensions.AI.ChatMessage>();
        if (!string.IsNullOrEmpty(request.SystemPrompt))
            messages.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.System, request.SystemPrompt));
        messages.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, request.Question));

        var options = new ChatOptions
        {
            ModelId = request.Model,
            Temperature = request.Temperature,
            Tools = _tools.AsAiTools()
        };

        try
        {
            var response = await client.GetResponseAsync(messages, options, ct);
            sw.Stop();

            // 统计被调用的工具
            var calledTools = response.Messages
                .SelectMany(m => m.Contents)
                .OfType<FunctionCallContent>()
                .Select(c => c.Name)
                .Distinct()
                .ToList();

            return new AgentChatResponse(
                Content: response.Messages.LastOrDefault()?.Text ?? string.Empty,
                ToolCalls: calledTools,
                InputTokens: (int)(response.Usage?.InputTokenCount ?? 0L),
                OutputTokens: (int)(response.Usage?.OutputTokenCount ?? 0L),
                LatencyMs: (int)sw.ElapsedMilliseconds,
                Provider: provider
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "智能体调用失败: Provider={Provider}", provider);
            throw;
        }
    }
}

public record AgentChatRequest(
    string Question,
    string? SystemPrompt = null,
    string Model = "qwen-plus",
    string? Provider = null,
    float Temperature = 0.7f
);

public record AgentChatResponse(
    string Content,
    List<string> ToolCalls,
    int InputTokens,
    int OutputTokens,
    int LatencyMs,
    string Provider
);
