using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using AiCustomerService.Api.Hubs;
using AiCustomerService.Api.Localization;
using AiCustomerService.Api.Realtime;
using AiCustomerService.Core.Exceptions;
using AiCustomerService.Core.Interfaces;
using AiCustomerService.Infrastructure;
using AiCustomerService.Infrastructure.Data;
using AiCustomerService.Infrastructure.MultiTenancy;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ===== Serilog =====
builder.Host.UseSerilog((ctx, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration)
      .WriteTo.Console()
      .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day));

// ===== EF Core / PostgreSQL + pgvector =====
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Host=localhost;Port=5432;Database=ai_customer_service;Username=postgres;Password=postgres123";

builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(connectionString, npgsql => npgsql.UseVector()));

// ===== Infrastructure (AI / Cache / WeChat / Hangfire Jobs / JWT / Tenant) =====
builder.Services.AddInfrastructure(builder.Configuration);

// ===== i18n（本地化）=====
builder.Services.AddLocalization(o => o.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(o =>
{
    var cultures = new[] { new CultureInfo("zh-CN"), new CultureInfo("en-US") };
    o.DefaultRequestCulture = new RequestCulture("zh-CN");
    o.SupportedCultures = cultures;
    o.SupportedUICultures = cultures;
    o.RequestCultureProviders = new IRequestCultureProvider[]
    {
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});

// ===== Controllers =====
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        o.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ===== Swagger / OpenAPI =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// ===== Health Checks =====
builder.Services.AddHealthChecks();

// ===== CORS =====
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p => p
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()));

// ===== JWT =====
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "your-very-long-secret-key-at-least-32-chars-please-change";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AiCustomerService";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AiCustomerService";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwtSecret))
        };
        // SignalR 不能走 Authorization 头（浏览器 Web API 限制），
        // 因此允许通过查询字符串 ?access_token=xxx 完成握手鉴权。
        opts.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    ctx.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// ===== SignalR =====
builder.Services.AddSignalR(opts =>
{
    opts.EnableDetailedErrors = builder.Environment.IsDevelopment();
    opts.KeepAliveInterval = TimeSpan.FromSeconds(15);
    opts.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// ===== Realtime Notifier =====
builder.Services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();

// ===== Hangfire =====
builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(opts => opts.UseNpgsqlConnection(connectionString)));
builder.Services.AddHangfireServer();

// ===== Rate Limiting =====
// 详见 docs/架构设计.md 4.3 限流
// - login: 5 次/分钟/IP（防爆破）
// - register: 3 次/小时/IP（防恶意注册）
// - knowledge upload: 20 次/小时/租户（防滥用存储）
// - chat: 100 次/小时/租户（trial plan）
// - default: 600 次/分钟/IP（基础防 DDoS）
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // 通用 IP 限流（兜底）
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    {
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"global:{ip}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 600,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

    // 登录 IP 限流：5 次/分钟
    options.AddPolicy("login-ip", ctx =>
    {
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"login:{ip}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    // 注册 IP 限流：3 次/小时
    options.AddPolicy("register-ip", ctx =>
    {
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"register:{ip}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0
            });
    });

    // 文档上传租户限流：20 次/小时
    options.AddPolicy("upload-tenant", ctx =>
    {
        var tenantId = ctx.User.FindFirst("tenant_id")?.Value
            ?? ctx.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"upload:{tenantId}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0
            });
    });

    // 聊天租户限流（按 plan）：100 次/小时 trial，500/h pro
    options.AddPolicy("chat-tenant", ctx =>
    {
        var tenantId = ctx.User.FindFirst("tenant_id")?.Value
            ?? ctx.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";
        var plan = ctx.User.FindFirst("plan")?.Value ?? "trial";
        var limit = plan switch { "pro" => 500, "enterprise" => 5000, _ => 100 };
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"chat:{plan}:{tenantId}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limit,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0
            });
    });

    // 429 响应统一格式
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        var payload = JsonSerializer.Serialize(new
        {
            code = "quota_exceeded",
            message = "请求过于频繁，请稍后再试",
            trace_id = context.HttpContext.TraceIdentifier
        }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });
        await context.HttpContext.Response.WriteAsync(payload, cancellationToken);
    };
});

var app = builder.Build();

// ===== 全局异常处理 =====
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async ctx =>
    {
        var feature = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var ex = feature?.Error;
        ctx.Response.ContentType = "application/json";

        var (status, code) = ex switch
        {
            NotFoundException => (404, "not_found"),
            UnauthorizedException => (401, "unauthorized"),
            ForbiddenException => (403, "forbidden"),
            ValidationException => (400, "validation_error"),
            QuotaExceededException => (429, "quota_exceeded"),
            _ => (500, "internal_error")
        };

        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(new
        {
            code,
            message = ex?.Message ?? "服务器内部错误",
            trace_id = ctx.TraceIdentifier
        });
    });
});

// ===== Pipeline =====
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSerilogRequestLogging();
app.UseRequestLocalization();
app.UseMiddleware<LocalizedExceptionMiddleware>();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapHub<WorkbenchHub>("/hubs/workbench");
app.MapWorkbenchWebSocket(); // 原生 WebSocket（供 uni-app 小程序 / H5）
app.UseHangfireDashboard();

// 注册周期任务
using (var scope = app.Services.CreateScope())
{
    var jobMgr = scope.ServiceProvider.GetRequiredService<Hangfire.IRecurringJobManager>();
    jobMgr.AddOrUpdate<AiCustomerService.Infrastructure.Jobs.TrialExpiryJob>(
        "trial-expiry-daily", j => j.RunAsync(CancellationToken.None), Hangfire.Cron.Daily(3));
    jobMgr.AddOrUpdate<AiCustomerService.Infrastructure.Jobs.WebhookDispatchJob>(
        "webhook-dispatch-minute", j => j.RunAsync(CancellationToken.None), Hangfire.Cron.Minutely());
}

// 自动迁移（开发环境）
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    // 植入行业 FAQ 种子数据（幂等）
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    AiCustomerService.Infrastructure.Data.IndustryFaqSeeder.SeedAsync(db, logger).GetAwaiter().GetResult();
}

app.MapGet("/", () => Results.Ok(new
{
    name = "AI Customer Service API",
    version = "1.0.0",
    framework = ".NET 10",
    docs = "/openapi/v1.json",
    health = "/health",
    hangfire = "/hangfire"
}));

app.Run();