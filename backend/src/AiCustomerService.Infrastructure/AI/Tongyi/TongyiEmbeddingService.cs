using System.Net.Http.Json;
using AiCustomerService.Core.Configuration;
using AiCustomerService.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiCustomerService.Infrastructure.AI.Tongyi;

public class TongyiEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _http;
    private readonly TongyiOptions _options;
    private readonly ILogger<TongyiEmbeddingService> _logger;

    public TongyiEmbeddingService(
        HttpClient http,
        IOptions<TongyiOptions> options,
        ILogger<TongyiEmbeddingService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _http = http;
        _http.BaseAddress = new Uri(_options.Endpoint);
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var result = await EmbedBatchAsync(new[] { text }, ct);
        return result[0];
    }

    public async Task<List<float[]>> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken ct = default)
    {
        var input = texts.ToList();
        if (input.Count == 0) return new List<float[]>();

        var reqBody = new TongyiEmbedRequest
        {
            Model = _options.EmbeddingModel,
            Input = new TongyiEmbedInput { Texts = input },
            Parameters = new TongyiEmbedParameters { Dimension = 1024, EncodingFormat = "float" }
        };

        var resp = await _http.PostAsJsonAsync(
            "/api/v1/services/embeddings/text-embedding/text-embedding", reqBody, ct);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<TongyiEmbedResponse>(cancellationToken: ct);

        if (result?.Output?.Embeddings == null)
            throw new InvalidOperationException("Embedding 返回为空");

        return result.Output.Embeddings
            .OrderBy(e => e.TextIndex)
            .Select(e => e.Embedding.ToArray())
            .ToList();
    }
}
