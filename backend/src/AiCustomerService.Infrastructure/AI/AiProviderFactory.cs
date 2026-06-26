using System.ClientModel;
using AiCustomerService.Core.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;

namespace AiCustomerService.Infrastructure.AI;

/// <summary>
/// 多 LLM 提供商工厂。
/// 所有 OpenAI 兼容的提供商（Tongyi/OpenAI/DeepSeek/Zhipu）都通过 OpenAIClient 接入；
/// Anthropic 等需单独实现。
/// </summary>
public class AiProviderFactory : IAiProviderFactory
{
    private readonly AiProviderOptions _options;
    private readonly ILogger<AiProviderFactory> _logger;
    private readonly Dictionary<string, OpenAIClient> _clients = new();
    private readonly object _lock = new();

    public AiProviderFactory(
        Microsoft.Extensions.Options.IOptions<AiProviderOptions> options,
        ILogger<AiProviderFactory> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public IChatClient GetChatClient(string providerName)
    {
        var provider = Resolve(providerName);
        var openAi = GetOrCreateClient(provider);
        return openAi.GetChatClient(provider.ChatModel).AsIChatClient();
    }

    public IEmbeddingGenerator<string, Embedding<float>> GetEmbeddingGenerator(string providerName)
    {
        var provider = Resolve(providerName);
        var openAi = GetOrCreateClient(provider);
        return openAi.GetEmbeddingClient(provider.EmbeddingModel).AsIEmbeddingGenerator();
    }

    public List<ProviderInfoDto> ListProviders()
    {
        return _options.Providers.Select(p => new ProviderInfoDto(
            Name: p.Name,
            ChatModel: p.ChatModel,
            EmbeddingModel: p.EmbeddingModel,
            EmbeddingDimension: p.EmbeddingDimension,
            Enabled: p.Enabled,
            IsDefault: p.Name == _options.DefaultProvider
        )).ToList();
    }

    public ProviderInfoDto? GetProvider(string providerName)
    {
        var p = _options.Providers.FirstOrDefault(x =>
            x.Name.Equals(providerName, StringComparison.OrdinalIgnoreCase));
        if (p == null) return null;
        return new ProviderInfoDto(p.Name, p.ChatModel, p.EmbeddingModel, p.EmbeddingDimension, p.Enabled,
            p.Name == _options.DefaultProvider);
    }

    private ProviderConfig Resolve(string providerName)
    {
        var provider = _options.Providers
            .FirstOrDefault(p => p.Name.Equals(providerName, StringComparison.OrdinalIgnoreCase));
        if (provider == null)
        {
            _logger.LogWarning("未找到 provider {Name}，回退到默认 {Default}", providerName, _options.DefaultProvider);
            provider = _options.Providers.FirstOrDefault(p => p.Name == _options.DefaultProvider)
                ?? _options.Providers.FirstOrDefault()
                ?? throw new InvalidOperationException("未配置任何 LLM 提供商");
        }
        if (!provider.Enabled)
            throw new InvalidOperationException($"提供商 {provider.Name} 已停用");
        return provider;
    }

    private OpenAIClient GetOrCreateClient(ProviderConfig provider)
    {
        lock (_lock)
        {
            if (_clients.TryGetValue(provider.Name, out var existing))
                return existing;

            var client = new OpenAIClient(
                new ApiKeyCredential(provider.ApiKey),
                new OpenAIClientOptions
                {
                    Endpoint = new Uri(provider.Endpoint.TrimEnd('/'))
                });
            _clients[provider.Name] = client;
            _logger.LogInformation("已创建 LLM 客户端: Provider={Name} Endpoint={Endpoint}",
                provider.Name, provider.Endpoint);
            return client;
        }
    }
}
