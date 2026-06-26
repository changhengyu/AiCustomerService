using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AiCustomerService.Core.Configuration;
using AiCustomerService.Core.DTOs.AI;
using AiCustomerService.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiCustomerService.Infrastructure.AI.Tongyi;

public class TongyiAIService : IAIService
{
    private readonly HttpClient _http;
    private readonly TongyiOptions _options;
    private readonly ILogger<TongyiAIService> _logger;

    public TongyiAIService(
        HttpClient http,
        IOptions<TongyiOptions> options,
        ILogger<TongyiAIService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _http = http;
        _http.BaseAddress = new Uri(_options.Endpoint);
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");
        _http.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var messages = BuildMessages(request);

        var reqBody = new TongyiChatRequest
        {
            Model = request.Model,
            Messages = messages,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens,
            Stream = false
        };

        var resp = await _http.PostAsJsonAsync(
            "/compatible-mode/v1/chat/completions", reqBody, ct);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<TongyiChatResponse>(cancellationToken: ct);

        sw.Stop();
        if (result?.Choices == null || result.Choices.Count == 0)
            throw new InvalidOperationException("通义千问返回为空");

        return new ChatResponse(
            Content: result.Choices[0].Message.Content,
            PromptTokens: result.Usage.PromptTokens,
            CompletionTokens: result.Usage.CompletionTokens,
            TotalTokens: result.Usage.TotalTokens,
            LatencyMs: (int)sw.ElapsedMilliseconds,
            FinishReason: result.Choices[0].FinishReason
        );
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var messages = BuildMessages(request);
        var reqBody = new TongyiChatRequest
        {
            Model = request.Model,
            Messages = messages,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens,
            Stream = true
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/compatible-mode/v1/chat/completions")
        {
            Content = JsonContent.Create(reqBody)
        };

        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(ct);
            if (line == null) break;
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data:")) continue;
            var data = line.Substring(5).Trim();
            if (data == "[DONE]" || string.IsNullOrEmpty(data)) break;

            string? delta = null;
            try
            {
                var chunk = JsonSerializer.Deserialize<TongyiStreamChunk>(data);
                delta = chunk?.Choices?[0].Delta?.Content;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "解析流式响应失败: {Data}", data);
            }

            if (!string.IsNullOrEmpty(delta))
                yield return delta;
        }
    }

    public async Task<ChatResponse> RagChatAsync(RagChatRequest request, CancellationToken ct = default)
    {
        var sysPrompt = request.SystemPrompt ?? "你是 AI 客服。";
        var messages = new List<ChatMessage> { new("user", request.Question) };
        return await ChatAsync(new ChatRequest(
            TenantId: request.TenantId,
            Model: "qwen-plus",
            Messages: messages,
            SystemPrompt: sysPrompt
        ), ct);
    }

    private List<TongyiMessage> BuildMessages(ChatRequest request)
    {
        var messages = new List<TongyiMessage>();
        if (!string.IsNullOrEmpty(request.SystemPrompt))
            messages.Add(new TongyiMessage { Role = "system", Content = request.SystemPrompt });
        if (request.Messages != null)
            messages.AddRange(request.Messages.Select(m => new TongyiMessage
            {
                Role = m.Role,
                Content = m.Content
            }));
        return messages;
    }
}
