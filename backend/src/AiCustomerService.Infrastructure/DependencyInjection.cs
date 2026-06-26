using AiCustomerService.Core.Configuration;
using AiCustomerService.Core.Interfaces;
using AiCustomerService.Infrastructure.AI;
using AiCustomerService.Infrastructure.AI.Agent;
using AiCustomerService.Infrastructure.AI.RAG;
using AiCustomerService.Infrastructure.AI.Stt;
using AiCustomerService.Infrastructure.AI.Tongyi;
using AiCustomerService.Infrastructure.Cache;
using AiCustomerService.Infrastructure.Jobs;
using AiCustomerService.Infrastructure.MultiTenancy;
using AiCustomerService.Infrastructure.Security;
using AiCustomerService.Infrastructure.Services;
using AiCustomerService.Infrastructure.WeChat;
using AiCustomerService.Infrastructure.Observability;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI;
using StackExchange.Redis;
using AiCustomerService.Infrastructure.Payments;

namespace AiCustomerService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // 可观测（OpenTelemetry）
        services.AddAppTelemetry(config);

        // Options
        services.Configure<TongyiOptions>(config.GetSection(TongyiOptions.SectionName));
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));
        services.Configure<WeChatOptions>(config.GetSection(WeChatOptions.SectionName));
        services.Configure<AiProviderOptions>(config.GetSection(AiProviderOptions.SectionName));
        services.Configure<PlanPolicyOptions>(config.GetSection(PlanPolicyOptions.SectionName));
        services.Configure<StripeOptions>(config.GetSection(StripeOptions.SectionName));
        services.Configure<WeChatPayOptions>(config.GetSection(WeChatPayOptions.SectionName));

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

        // 多 LLM 适配器工厂（v0.3.0+）
        services.AddSingleton<IAiProviderFactory, AiProviderFactory>();

        // STT（v0.4.0+）
        services.Configure<AliyunSttOptions>(config.GetSection(AliyunSttOptions.SectionName));
        services.AddHttpClient("stt");
        services.AddSingleton<AiCustomerService.Infrastructure.AI.Stt.IAiSttProvider>(sp =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AliyunSttOptions>>().Value;
            return !string.IsNullOrEmpty(opts.AppKey)
                ? new AiCustomerService.Infrastructure.AI.Stt.AliyunSttProvider(
                    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AliyunSttOptions>>(),
                    sp.GetRequiredService<IHttpClientFactory>(),
                    sp.GetRequiredService<ILogger<AiCustomerService.Infrastructure.AI.Stt.AliyunSttProvider>>())
                : new AiCustomerService.Infrastructure.AI.Stt.NoopSttProvider();
        });

        // 智能体（Function Calling）
        services.AddScoped<CustomerServiceTools>();
        services.AddScoped<AgentService>();
        services.AddScoped<BiService>();
        services.AddScoped<OpenApiService>();
        services.AddScoped<ProfileService>();
        services.AddScoped<SegmentService>();
        services.AddScoped<MarketingTriggerService>();
        services.AddScoped<SubscriptionService>();
        services.AddScoped<Jobs.TrialExpiryJob>();
        services.AddScoped<Jobs.WebhookDispatchJob>();

        // 支付提供方（多注册）
        services.AddSingleton<IPaymentProvider, NoopPaymentProvider>();
        services.AddSingleton<IPaymentProvider, StripePaymentProvider>();
        services.AddSingleton<IPaymentProvider, WeChatPayProvider>();

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
