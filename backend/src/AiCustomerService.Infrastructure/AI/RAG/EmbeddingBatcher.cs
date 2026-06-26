using AiCustomerService.Core.Entities;
using AiCustomerService.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AiCustomerService.Infrastructure.AI.RAG;

/// <summary>
/// Embedding 批处理器：分段、大批次、失败重试、限流
/// </summary>
public class EmbeddingBatcher
{
    private const int MaxBatchSize = 25;
    private const int MaxRetries = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    private readonly IEmbeddingService _embedding;
    private readonly ILogger<EmbeddingBatcher> _logger;

    public EmbeddingBatcher(IEmbeddingService embedding, ILogger<EmbeddingBatcher> logger)
    {
        _embedding = embedding;
        _logger = logger;
    }

    public async Task<List<(TextChunk Chunk, float[] Vector)>> EmbedChunksAsync(
        List<TextChunk> chunks, CancellationToken ct = default)
    {
        var results = new List<(TextChunk, float[])>(chunks.Count);

        for (int i = 0; i < chunks.Count; i += MaxBatchSize)
        {
            var batch = chunks.Skip(i).Take(MaxBatchSize).ToList();
            var texts = batch.Select(c => c.Content).ToList();

            var vectors = await EmbedWithRetryAsync(texts, ct);
            for (int j = 0; j < batch.Count; j++)
                results.Add((batch[j], vectors[j]));

            _logger.LogInformation(
                "Embedding 进度 {Done}/{Total}", Math.Min(i + MaxBatchSize, chunks.Count), chunks.Count);
        }

        return results;
    }

    private async Task<List<float[]>> EmbedWithRetryAsync(List<string> texts, CancellationToken ct)
    {
        Exception? lastEx = null;
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await _embedding.EmbedBatchAsync(texts, ct);
            }
            catch (Exception ex)
            {
                lastEx = ex;
                _logger.LogWarning(ex, "Embedding 第 {Attempt} 次失败，等待重试", attempt);
                if (attempt < MaxRetries)
                    await Task.Delay(RetryDelay * attempt, ct);
            }
        }
        throw new InvalidOperationException("Embedding 多次重试失败", lastEx);
    }
}