using Microsoft.Extensions.AI;

namespace AiCustomerService.Infrastructure.AI;

/// <summary>
/// 多 LLM 适配器工厂（v0.3.0+）。
/// 租户可在设置中指定 provider，工厂返回对应的 IChatClient / IEmbeddingGenerator。
/// 所有提供商的 HTTP 通信由 MEAI / OpenAI SDK 内部处理。
/// </summary>
public interface IAiProviderFactory
{
    IChatClient GetChatClient(string providerName);
    IEmbeddingGenerator<string, Embedding<float>> GetEmbeddingGenerator(string providerName);
    List<ProviderInfoDto> ListProviders();
    ProviderInfoDto? GetProvider(string providerName);
}

public record ProviderInfoDto(
    string Name,
    string ChatModel,
    string EmbeddingModel,
    int EmbeddingDimension,
    bool Enabled,
    bool IsDefault
);
