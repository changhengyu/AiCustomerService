namespace AiCustomerService.Core.Configuration;

/// <summary>
/// 多 LLM 提供商配置（v0.3.0+）。
/// 通过 IChatClientFactory 按 provider 名称获取对应 IChatClient。
/// </summary>
public class AiProviderOptions
{
    public const string SectionName = "AiProviders";

    /// <summary>当前激活的 provider 名称（tongyi/openai/deepseek/zhipu/anthropic）</summary>
    public string DefaultProvider { get; set; } = "tongyi";

    public List<ProviderConfig> Providers { get; set; } = new();
}

public class ProviderConfig
{
    public string Name { get; set; } = string.Empty;          // tongyi/openai/deepseek/zhipu/anthropic
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;      // OpenAI 兼容端点
    public string ChatModel { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = string.Empty;
    public int EmbeddingDimension { get; set; } = 1024;
    public bool Enabled { get; set; } = true;
    public string? Note { get; set; }
}
