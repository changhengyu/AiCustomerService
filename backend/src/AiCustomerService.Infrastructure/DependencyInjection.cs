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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        // HttpClients
        services.AddHttpClient<IAIService, TongyiAIService>();
        services.AddHttpClient<IEmbeddingService, TongyiEmbeddingService>();
        services.AddHttpClient<WeChatOfficialClient>();

        // 单例：基础设施组件
        services.AddSingleton<TextCleaner>();
        services.AddSingleton<TextSplitter>();
        services.AddSingleton<DocumentLoader>();
        services.AddSingleton<EmbeddingBatcher>();
        services.AddScoped<PgVectorStore>();
        services.AddScoped<HybridRetriever>();

        // Cache（Redis 可选，未连接时退化到内存）
        var redisConn = config.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConn))
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConn));
        services.AddScoped<ICacheService, RedisCacheService>();

        // WeChat
        services.AddScoped<WeChatOfficialClient>();

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

        return services;
    }
}