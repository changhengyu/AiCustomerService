using AiCustomerService.Core.Configuration;
using AiCustomerService.Core.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiCustomerService.Infrastructure.AI.Tongyi;

/// <summary>
/// 通义千问 Embedding 服务（基于 Microsoft.Extensions.AI 框架）。
/// 通过 IEmbeddingGenerator&lt;string, Embedding&lt;float&gt;&gt; 调用 Tongyi 兼容端点。
/// 所有 HTTP 通信由 MEAI / OpenAI SDK 内部处理，开发者不再直接接触 HttpClient。
/// </summary>
public class TongyiEmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;
    private readonly ILogger<TongyiEmbeddingService> _logger;

    public TongyiEmbeddingService(
        IEmbeddingGenerator<string, Embedding<float>> generator,
        ILogger<TongyiEmbeddingService> logger)
    {
        _generator = generator;
        _logger = logger;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var result = await _generator.GenerateAsync(text, cancellationToken: ct);
        return result.Vector.ToArray();
    }

    public async Task<List<float[]>> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken ct = default)
    {
        var input = texts.ToList();
        if (input.Count == 0) return new List<float[]>();

        try
        {
            // 一次性批量生成：MEAI 会自动按服务端的 batch 限制拆分
            var results = await _generator.GenerateAsync(input, cancellationToken: ct);
            return results.Select(r => r.Vector.ToArray()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Embedding 批量生成失败: Count={Count}", input.Count);
            throw;
        }
    }
}
