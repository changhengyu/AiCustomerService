using AiCustomerService.Core.Configuration;
using AiCustomerService.Core.Interfaces;
using AiCustomerService.Infrastructure.AI.RAG;
using AiCustomerService.Infrastructure.AI.Tongyi;
using AiCustomerService.Infrastructure.Cache;
using AiCustomerService.Infrastructure.Jobs;
using AiCustomerService.Infrastructure.MultiTenancy;
using AiCustomerService.Infrastructure.Security;
using AiCustomerService.Infrastructure.Services;
using AiCustomerService.Infrastructure.WeChat;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using StackExchange.Redis;

namespace AiCustomerService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Options
        services.Configure<TongyiOptions>(config.GetSection(TongyiOptions.SectionName));
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));
        services.Configure<WeChatOptions>(config.GetSection(WeChatOptions.SectionName));

        var tongyi = config.GetSection(TongyiOptions.SectionName).Get<TongyiOptions>()
            ?? new TongyiOptions();

        // ============================================================
        // Microsoft.Extensions.AI (MEAI) 集成
        // 通过 OpenAIClient 客户端连接 Tongyi 的 OpenAI 兼容端点
        // 之后在框架内用 IChatClient / IEmbeddingGenerator 调用
        // 不再有任何业务代码直接使用 HttpClient
        // ============================================================
        var openAiClient = new OpenAIClient(
            new System.ClientModel.ApiKeyCredential(tongyi.ApiKey),
            new OpenAIClientOptions
            {
                Endpoint = new Uri($"{tongyi.Endpoint.TrimEnd('/')}/compatible-mode/v1")
            });

        // 注册 IChatClient（来自 MEAI 包）
        services.AddChatClient(sp => openAiClient
            .GetChatClient(tongyi.ChatModel)
            .AsIChatClient());

        // 注册 IEmbeddingGenerator<string, Embedding<float>>（来自 MEAI 包）
        services.AddEmbeddingGenerator(sp => openAiClient
            .GetEmbeddingClient(tongyi.EmbeddingModel)
            .AsIEmbeddingGenerator());

        // 业务 AI 服务（基于 MEAI 抽象）
        services.AddScoped<IAIService, TongyiAIService>();
        services.AddScoped<IEmbeddingService, TongyiEmbeddingService>();

        // WeChat（仍用 HttpClient，因为微信 API 不在 AI 范畴）
        services.AddHttpClient<WeChatOfficialClient>();
        services.AddScoped<WeChatOfficialClient>();

        // 单例：基础设施组件
        services.AddSingleton<TextCleaner>();
        services.AddSingleton<TextSplitter>();
        services.AddSingleton<DocumentLoader>();
        services.AddScoped<EmbeddingBatcher>();
        services.AddScoped<PgVectorStore>();
        services.AddScoped<HybridRetriever>();

        // Cache（Redis 可选，未连接时退化到内存）
        var redisConn = config.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConn))
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConn));
        services.AddScoped<ICacheService, RedisCacheService>();

        // MultiTenancy
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, TenantContext>();

        // Security
        services.AddScoped<JwtTokenService>();

        // Hangfire Jobs
        services.AddScoped<IngestDocumentJob>();
        services.AddScoped<ReindexTenantJob>();

        // 业务服务
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IKnowledgeService, KnowledgeService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IWeChatService, WeChatService>();
        services.AddScoped<IIndustryFaqService, IndustryFaqService>();
        services.AddScoped<IEvaluationService, EvaluationService>();

        return services;
    }
}
