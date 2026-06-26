using System.Text.Json.Serialization;

namespace AiCustomerService.Infrastructure.AI.Tongyi;

public class TongyiChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "qwen-plus";

    [JsonPropertyName("messages")]
    public List<TongyiMessage> Messages { get; set; } = new();

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 2000;

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
}

public class TongyiMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class TongyiChatResponse
{
    [JsonPropertyName("choices")]
    public List<TongyiChoice> Choices { get; set; } = new();

    [JsonPropertyName("usage")]
    public TongyiUsage Usage { get; set; } = new();
}

public class TongyiChoice
{
    [JsonPropertyName("message")]
    public TongyiMessage Message { get; set; } = new();

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; } = string.Empty;
}

public class TongyiUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

public class TongyiStreamChunk
{
    [JsonPropertyName("choices")]
    public List<TongyiStreamChoice>? Choices { get; set; }
}

public class TongyiStreamChoice
{
    [JsonPropertyName("delta")]
    public TongyiMessage? Delta { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

public class TongyiEmbedRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "text-embedding-v3";

    [JsonPropertyName("input")]
    public TongyiEmbedInput Input { get; set; } = new();

    [JsonPropertyName("parameters")]
    public TongyiEmbedParameters Parameters { get; set; } = new();
}

public class TongyiEmbedInput
{
    [JsonPropertyName("texts")]
    public List<string> Texts { get; set; } = new();
}

public class TongyiEmbedParameters
{
    [JsonPropertyName("dimension")]
    public int Dimension { get; set; } = 1024;

    [JsonPropertyName("encoding_format")]
    public string EncodingFormat { get; set; } = "float";
}

public class TongyiEmbedResponse
{
    [JsonPropertyName("output")]
    public TongyiEmbedOutput Output { get; set; } = new();

    [JsonPropertyName("usage")]
    public TongyiUsage Usage { get; set; } = new();
}

public class TongyiEmbedOutput
{
    [JsonPropertyName("embeddings")]
    public List<TongyiEmbedItem> Embeddings { get; set; } = new();
}

public class TongyiEmbedItem
{
    [JsonPropertyName("embedding")]
    public List<float> Embedding { get; set; } = new();

    [JsonPropertyName("text_index")]
    public int TextIndex { get; set; }
}
