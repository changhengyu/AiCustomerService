namespace AiCustomerService.Core.Configuration;

public class TongyiOptions
{
    public const string SectionName = "Tongyi";
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://dashscope.aliyuncs.com";
    public string ChatModel { get; set; } = "qwen-plus";
    public string EmbeddingModel { get; set; } = "text-embedding-v3";
}

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "AiCustomerService";
    public string Audience { get; set; } = "AiCustomerService";
    public string Secret { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 720;
    public int RefreshExpiryDays { get; set; } = 30;
}

public class WeChatOptions
{
    public const string SectionName = "WeChat";
    public string OfficialToken { get; set; } = string.Empty;
    public string EncodingAESKey { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
}
